using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Play Mode中のゲーム画面から、一時UGSテストデータを操作するメニュー。
/// 製品ビルドでは自動的に無効になる。
/// </summary>
public class RuntimeProgressTestMenu : MonoBehaviour
{
    private enum TestTab
    {
        Achievements,
        Skins
    }

    private enum ProgressSection
    {
        Achievements,
        Garbage,
        Reset
    }

    private static RuntimeProgressTestMenu _instance;

    private readonly HashSet<int> _selectedAchievementIDs = new HashSet<int>();
    private readonly HashSet<int> _selectedGarbageIDs = new HashSet<int>();
    private readonly HashSet<int> _selectedSkinIDs = new HashSet<int>();

    private Vector2 _achievementScroll;
    private Vector2 _skinScroll;
    private TestTab _selectedTab;
    private ProgressSection _progressSection;
    private bool _isOpen;
    private bool _isBusy;
    private string _statusMessage = "操作する項目を選んでください。";
    private bool _statusIsError;
    private float _timeScaleBeforeOpen = 1f;
    private bool _pausedByMenu;
    private EventSystem _blockedEventSystem;

    private GUIStyle _hamburgerStyle;
    private GUIStyle _panelStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _sectionStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _statusStyle;
    private GUIStyle _cardStyle;
    private GUIStyle _summaryStyle;
    private GUIStyle _tabStyle;
    private GUIStyle _selectedTabStyle;
    private GUIStyle _buttonStyle;
    private GUIStyle _secondaryButtonStyle;
    private GUIStyle _dangerButtonStyle;
    private GUIStyle _rowStyle;
    private GUIStyle _selectedRowStyle;
    private readonly List<Texture2D> _generatedStyleTextures = new List<Texture2D>();

    private void Awake()
    {
#if UNITY_EDITOR
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
#else
        gameObject.SetActive(false);
#endif
    }

    private void OnDisable()
    {
        RestoreGameInput();
    }

    private void OnDestroy()
    {
        RestoreGameInput();
#if UNITY_EDITOR
        DestroyGeneratedStyleTextures();
#endif
        if (_instance == this)
        {
            _instance = null;
        }
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        GUI.depth = -10000;
        EnsureStyles();

        Matrix4x4 previousMatrix = GUI.matrix;
        float scale = Mathf.Clamp(
            Mathf.Min(Screen.width / 600f, Screen.height / 1000f),
            0.6f,
            2f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

        float viewWidth = Screen.width / scale;
        float viewHeight = Screen.height / scale;
        if (!_isOpen)
        {
            Rect hamburgerRect = new Rect(viewWidth - 98f, 22f, 76f, 76f);
            if (GUI.Button(hamburgerRect, "≡", _hamburgerStyle))
            {
                SetOpen(true);
            }
        }
        else
        {
            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.78f);
            GUI.DrawTexture(new Rect(0f, 0f, viewWidth, viewHeight), Texture2D.whiteTexture);
            GUI.color = previousColor;

            float panelWidth = Mathf.Min(1080f, viewWidth - 18f);
            Rect panelRect = new Rect(
                (viewWidth - panelWidth) * 0.5f,
                9f,
                panelWidth,
                Mathf.Max(360f, viewHeight - 18f));
            GUILayout.BeginArea(panelRect, _panelStyle);
            DrawMenuContents();
            GUILayout.EndArea();

            Rect closeRect = new Rect(panelRect.xMax - 78f, panelRect.y + 12f, 62f, 62f);
            if (GUI.Button(closeRect, "×", _hamburgerStyle))
            {
                SetOpen(false);
            }
        }

        GUI.matrix = previousMatrix;
    }

    private void DrawMenuContents()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("TEST MENU", _titleStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Space(66f);
        GUILayout.EndHorizontal();

        GUILayout.Label("ゲーム進行をPlay Mode中だけ変更できます", _sectionStyle);

        UGSCloudSaveManager cloudSave = UGSCloudSaveManager.instance;
        AchievementManager achievementManager = AchievementManager.instance;
        OceanLogCatalog catalog = achievementManager != null ? achievementManager.Catalog : null;
        SkinDatabase skinDatabase = SkinDatabase.instance;

        if (cloudSave == null || !cloudSave.IsDataLoaded || achievementManager == null || catalog == null || skinDatabase == null)
        {
            GUILayout.Space(18f);
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("データを読み込んでいます…", _sectionStyle);
            GUILayout.Label("UGS・実績・スキンの準備が終わるまで少し待ってください。", _labelStyle);
            GUILayout.EndVertical();
            return;
        }

        DrawCurrentStatus(cloudSave, catalog, skinDatabase);
        GUILayout.Space(10f);

        GUI.enabled = !_isBusy;
        GUILayout.BeginHorizontal();
        DrawMainTabButton(TestTab.Achievements, "実績・海洋ゴミ");
        DrawMainTabButton(TestTab.Skins, "スキン");
        GUILayout.EndHorizontal();

        GUILayout.Space(10f);
        if (_selectedTab == TestTab.Achievements)
        {
            DrawProgressTab(cloudSave, catalog);
        }
        else
        {
            _skinScroll = GUILayout.BeginScrollView(_skinScroll, GUILayout.ExpandHeight(true));
            DrawSkinTab(cloudSave, skinDatabase);
            GUILayout.EndScrollView();
        }

        GUILayout.Space(8f);
        DrawRestoreButton(cloudSave);
        GUI.enabled = true;

        Color oldColor = GUI.contentColor;
        GUI.contentColor = _statusIsError ? new Color(1f, 0.62f, 0.62f) : new Color(0.62f, 1f, 0.72f);
        GUILayout.Label(_statusMessage, _statusStyle);
        GUI.contentColor = oldColor;
    }

    private void DrawCurrentStatus(
        UGSCloudSaveManager cloudSave,
        OceanLogCatalog catalog,
        SkinDatabase skinDatabase)
    {
        int unlockedAchievements = catalog.achievements.Count(item =>
            item != null && cloudSave.IsAchievementUnlocked(item.id));
        int discoveredGarbage = cloudSave.GetDiscoveredObstacleIDs().Count;
        HashSet<int> unlockedSkins = cloudSave.GetUnlockedSkinIDs().ToHashSet();
        int skinCount = skinDatabase.GetAllSkins().Count(item => item != null && item.id > 0);
        int currentSkinID = SkinManager.instance != null ? SkinManager.instance.currentSkinID : 1;

        GUILayout.BeginHorizontal();
        DrawSummaryCard("実績", $"{unlockedAchievements} / {catalog.achievements.Count}");
        DrawSummaryCard("ゴミ図鑑", $"{discoveredGarbage} / {catalog.garbageEntries.Count}");
        DrawSummaryCard("スキン", $"{unlockedSkins.Count(id => id > 0)} / {skinCount}　ID {currentSkinID}");
        GUILayout.EndHorizontal();

        GUILayout.Label(
            cloudSave.IsEditorTestSessionActive
                ? "● 一時テスト中　Cloud Saveへの書き込みは停止しています"
                : "○ UGSの保存済みデータを表示中",
            _statusStyle);
    }

    private void DrawSummaryCard(string title, string value)
    {
        GUILayout.BeginVertical(_summaryStyle, GUILayout.ExpandWidth(true));
        GUILayout.Label(title, _labelStyle);
        GUILayout.Label(value, _sectionStyle);
        GUILayout.EndVertical();
    }

    private void DrawMainTabButton(TestTab tab, string label)
    {
        GUIStyle style = _selectedTab == tab ? _selectedTabStyle : _tabStyle;
        if (GUILayout.Button(label, style, GUILayout.ExpandWidth(true)))
        {
            _selectedTab = tab;
        }
    }

    private void DrawProgressSectionButton(ProgressSection section, string label)
    {
        GUIStyle style = _progressSection == section ? _selectedTabStyle : _tabStyle;
        if (GUILayout.Button(label, style, GUILayout.ExpandWidth(true)))
        {
            _progressSection = section;
            _achievementScroll = Vector2.zero;
        }
    }

    private void DrawProgressTab(UGSCloudSaveManager cloudSave, OceanLogCatalog catalog)
    {
        GUILayout.BeginHorizontal();
        DrawProgressSectionButton(ProgressSection.Achievements, "実績");
        DrawProgressSectionButton(ProgressSection.Garbage, "海洋ゴミ");
        DrawProgressSectionButton(ProgressSection.Reset, "リセット");
        GUILayout.EndHorizontal();
        GUILayout.Space(8f);

        _achievementScroll = GUILayout.BeginScrollView(_achievementScroll, GUILayout.ExpandHeight(true));
        switch (_progressSection)
        {
            case ProgressSection.Achievements:
                DrawAchievementSection(cloudSave, catalog);
                break;
            case ProgressSection.Garbage:
                DrawGarbageSection(cloudSave, catalog);
                break;
            case ProgressSection.Reset:
                DrawProgressResetSection();
                break;
        }
        GUILayout.EndScrollView();
    }

    private void DrawAchievementSection(UGSCloudSaveManager cloudSave, OceanLogCatalog catalog)
    {
        List<AchievementData> regularAchievements = catalog.achievements
            .Where(item => item != null && !item.hiddenUntilCompleted)
            .OrderBy(item => item.id)
            .ToList();
        List<AchievementData> secretAchievements = catalog.achievements
            .Where(item => item != null && item.hiddenUntilCompleted)
            .OrderBy(item => item.id)
            .ToList();

        _selectedAchievementIDs.RemoveWhere(id => secretAchievements.Any(item => item.id == id));

        GUILayout.BeginVertical(_cardStyle);
        GUILayout.Label("通常実績", _sectionStyle);
        GUILayout.Label("開放したい実績をタップして選択してください。", _labelStyle);
        DrawSelectionButtons(_selectedAchievementIDs, regularAchievements.Select(item => item.id));
        if (GUILayout.Button(
                $"選択した実績を解除　{_selectedAchievementIDs.Count}件",
                _buttonStyle))
        {
            UnlockAchievements(_selectedAchievementIDs.ToArray());
        }
        if (GUILayout.Button("通常実績をすべて解除", _buttonStyle))
        {
            UnlockAchievements(regularAchievements.Select(item => item.id).ToArray());
        }
        GUILayout.EndVertical();

        GUILayout.Space(8f);
        foreach (AchievementData achievement in regularAchievements)
        {
            string state = cloudSave.IsAchievementUnlocked(achievement.id) ? "　✓ 解除済み" : string.Empty;
            DrawToggle(
                _selectedAchievementIDs,
                achievement.id,
                $"[{achievement.id:00}]  {achievement.achievementName}{state}");
        }

        GUILayout.Space(10f);
        GUILayout.BeginVertical(_cardStyle);
        GUILayout.Label("シークレット", _sectionStyle);
        GUILayout.Label("通常実績の全開放には含まれません。", _labelStyle);
        if (GUILayout.Button("シークレット実績を解放", _buttonStyle))
        {
            UnlockAchievements(secretAchievements.Select(item => item.id).ToArray());
        }
        GUILayout.EndVertical();
    }

    private void DrawGarbageSection(UGSCloudSaveManager cloudSave, OceanLogCatalog catalog)
    {
        List<ObstacleData> garbageEntries = catalog.garbageEntries
            .Where(item => item != null)
            .OrderBy(item => item.id)
            .ToList();

        GUILayout.BeginVertical(_cardStyle);
        GUILayout.Label("海洋ゴミ図鑑", _sectionStyle);
        GUILayout.Label("衝突した扱いにするゴミをタップして選択してください。", _labelStyle);
        DrawSelectionButtons(_selectedGarbageIDs, garbageEntries.Select(item => item.id));
        if (GUILayout.Button(
                $"選択したゴミを登録　{_selectedGarbageIDs.Count}件",
                _buttonStyle))
        {
            DiscoverGarbage(_selectedGarbageIDs.ToArray());
        }
        if (GUILayout.Button($"全{garbageEntries.Count}種類を登録", _buttonStyle))
        {
            DiscoverGarbage(garbageEntries.Select(item => item.id).ToArray());
        }
        GUILayout.EndVertical();

        GUILayout.Space(8f);
        HashSet<int> discoveredIDs = cloudSave.GetDiscoveredObstacleIDs().ToHashSet();
        foreach (ObstacleData garbage in garbageEntries)
        {
            string state = discoveredIDs.Contains(garbage.id) ? "　✓ 発見済み" : string.Empty;
            DrawToggle(
                _selectedGarbageIDs,
                garbage.id,
                $"[{garbage.id:00}]  {garbage.displayName}{state}");
        }
    }

    private void DrawProgressResetSection()
    {
        GUILayout.BeginVertical(_cardStyle);
        GUILayout.Label("実績をリセット", _sectionStyle);
        GUILayout.Label("すべての実績を未解除へ戻します。", _labelStyle);
        if (GUILayout.Button("実績だけリセット", _dangerButtonStyle))
        {
            ResetAchievements();
        }
        GUILayout.EndVertical();

        GUILayout.Space(10f);
        GUILayout.BeginVertical(_cardStyle);
        GUILayout.Label("海洋ゴミ図鑑をリセット", _sectionStyle);
        GUILayout.Label("発見済み・閲覧済みのゴミを未発見へ戻します。", _labelStyle);
        if (GUILayout.Button("海洋ゴミ図鑑だけリセット", _dangerButtonStyle))
        {
            ResetGarbage();
        }
        GUILayout.EndVertical();

        GUILayout.Space(10f);
        GUILayout.BeginVertical(_cardStyle);
        GUILayout.Label("まとめてリセット", _sectionStyle);
        GUILayout.Label("実績と海洋ゴミ図鑑の両方をリセットします。", _labelStyle);
        if (GUILayout.Button("実績＋海洋ゴミ図鑑をリセット", _dangerButtonStyle))
        {
            ResetAchievementsAndGarbage();
        }
        GUILayout.EndVertical();
    }

    private void DrawSkinTab(UGSCloudSaveManager cloudSave, SkinDatabase skinDatabase)
    {
        List<SkinData> skins = skinDatabase.GetAllSkins()
            .Where(item => item != null && item.id > 0)
            .OrderBy(item => item.id)
            .ToList();
        HashSet<int> unlockedSkinIDs = cloudSave.GetUnlockedSkinIDs().ToHashSet();

        GUILayout.BeginVertical(_cardStyle);
        GUILayout.Label("スキン開放・装備", _sectionStyle);
        GUILayout.Label("操作したいスキンを下の一覧から選択します。初期スキンID 1は常に残ります。", _labelStyle);
        DrawSelectionButtons(_selectedSkinIDs, skins.Select(item => item.id));

        if (GUILayout.Button(
                $"選択を開放 ({_selectedSkinIDs.Count})",
                _buttonStyle))
        {
            UnlockSkins(_selectedSkinIDs.ToArray());
        }
        if (GUILayout.Button(
                $"選択を未開放へ ({_selectedSkinIDs.Count})",
                _dangerButtonStyle))
        {
            ResetSkins(_selectedSkinIDs.ToArray());
        }

        GUI.enabled = !_isBusy && _selectedSkinIDs.Count == 1;
        if (GUILayout.Button("選択した1体を開放して装備", _buttonStyle))
        {
            UnlockAndEquipSkin(_selectedSkinIDs.Single());
        }
        GUI.enabled = !_isBusy;

        if (GUILayout.Button("全スキン開放", _buttonStyle))
        {
            UnlockSkins(skins.Select(item => item.id).ToArray());
        }

        if (GUILayout.Button("ID 1だけ残して全リセット", _dangerButtonStyle))
        {
            ResetAllSkins();
        }
        GUILayout.EndVertical();

        GUILayout.Space(8f);
        foreach (SkinData skin in skins)
        {
            string skinName = string.IsNullOrWhiteSpace(skin.skinName) ? skin.name : skin.skinName;
            string equipped = SkinManager.instance != null && SkinManager.instance.currentSkinID == skin.id
                ? "　★ 装備中"
                : string.Empty;
            string state = unlockedSkinIDs.Contains(skin.id) ? "　✓ 開放済み" : string.Empty;
            DrawToggle(_selectedSkinIDs, skin.id, $"[{skin.id:00}]  {skinName}{equipped}{state}");
        }
    }

    private void DrawRestoreButton(UGSCloudSaveManager cloudSave)
    {
        GUI.enabled = !_isBusy && cloudSave.IsEditorTestSessionActive;
        if (GUILayout.Button("テスト開始前の全データに今すぐ戻す", _dangerButtonStyle))
        {
            bool restored = cloudSave.EditorRestoreTestSession();
            RefreshAllRuntimeUI();
            SetStatus(
                restored ? "テスト開始前の状態へ戻しました。" : "戻す一時データがありません。",
                !restored);
        }
        GUI.enabled = !_isBusy;
    }

    private void DrawSelectionButtons(HashSet<int> selection, IEnumerable<int> allIDs)
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("すべて選択", _secondaryButtonStyle))
        {
            selection.Clear();
            foreach (int id in allIDs)
            {
                selection.Add(id);
            }
        }

        if (GUILayout.Button("選択をクリア", _secondaryButtonStyle))
        {
            selection.Clear();
        }
        GUILayout.EndHorizontal();
    }

    private void DrawToggle(HashSet<int> selection, int id, string label)
    {
        bool selected = selection.Contains(id);
        string marker = selected ? "✓  " : "　 ";
        GUIStyle style = selected ? _selectedRowStyle : _rowStyle;
        if (GUILayout.Button(marker + label, style))
        {
            if (selected)
            {
                selection.Remove(id);
            }
            else
            {
                selection.Add(id);
            }
        }
    }

    private async void UnlockAchievements(int[] achievementIDs)
    {
        if (achievementIDs.Length == 0 || !BeginOperation("実績を解除しています…")) return;

        try
        {
            UGSCloudSaveManager cloudSave = UGSCloudSaveManager.instance;
            bool changed = cloudSave.EditorUnlockAchievements(achievementIDs);
            if (changed)
            {
                await AchievementManager.instance.EditorSyncRewardSkins();
                RefreshAllRuntimeUI();
            }
            SetStatus(changed ? $"実績を{achievementIDs.Length}件解除しました。" : "実績の変更に失敗しました。", !changed);
        }
        catch (Exception exception)
        {
            HandleException(exception);
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async void DiscoverGarbage(int[] garbageIDs)
    {
        if (garbageIDs.Length == 0 || !BeginOperation("海洋ゴミ図鑑へ登録しています…")) return;

        try
        {
            bool changed = UGSCloudSaveManager.instance.EditorDiscoverObstacles(garbageIDs);
            List<AchievementData> unlocked = changed
                ? await AchievementManager.instance.EvaluateAchievements()
                : new List<AchievementData>();
            RefreshAllRuntimeUI();
            SetStatus(
                changed
                    ? $"ゴミ{garbageIDs.Length}種類を登録しました。新規実績: {unlocked.Count}件"
                    : "ゴミ図鑑の変更に失敗しました。",
                !changed);
        }
        catch (Exception exception)
        {
            HandleException(exception);
        }
        finally
        {
            _isBusy = false;
        }
    }

    private void UnlockSkins(int[] skinIDs)
    {
        ExecuteImmediate(
            "スキンを開放しています…",
            () => UGSCloudSaveManager.instance.EditorUnlockSkins(skinIDs),
            $"スキンを{skinIDs.Length}件開放しました。");
    }

    private void UnlockAndEquipSkin(int skinID)
    {
        ExecuteImmediate(
            "スキンを装備しています…",
            () => UGSCloudSaveManager.instance.EditorUnlockAndEquipSkin(skinID),
            $"スキンID {skinID}を開放して装備しました。");
    }

    private void ResetSkins(int[] skinIDs)
    {
        int[] resettableIDs = skinIDs.Where(id => id > 1).ToArray();
        ExecuteImmediate(
            "選択したスキンをリセットしています…",
            () => UGSCloudSaveManager.instance.EditorResetSkins(resettableIDs),
            $"スキンを{resettableIDs.Length}件、未開放へ戻しました。");
    }

    private void ResetAllSkins()
    {
        ExecuteImmediate(
            "全スキンをリセットしています…",
            () => UGSCloudSaveManager.instance.EditorResetAllSkins(),
            "全スキンを初期状態へ戻しました。現在のスキンIDは1です。");
    }

    private void ResetAchievements()
    {
        ExecuteImmediate(
            "実績をリセットしています…",
            () => UGSCloudSaveManager.instance.EditorResetAchievements(),
            "実績をすべて未解除へ戻しました。");
    }

    private void ResetGarbage()
    {
        ExecuteImmediate(
            "ゴミ図鑑をリセットしています…",
            () => UGSCloudSaveManager.instance.EditorResetOceanLog(),
            "海洋ゴミ図鑑を未発見へ戻しました。");
    }

    private void ResetAchievementsAndGarbage()
    {
        ExecuteImmediate(
            "実績とゴミ図鑑をリセットしています…",
            () => UGSCloudSaveManager.instance.EditorResetAchievements() &&
                  UGSCloudSaveManager.instance.EditorResetOceanLog(),
            "実績と海洋ゴミ図鑑をリセットしました。");
    }

    private void ExecuteImmediate(string workingMessage, Func<bool> operation, string successMessage)
    {
        if (!BeginOperation(workingMessage)) return;

        try
        {
            bool changed = operation();
            RefreshAllRuntimeUI();
            SetStatus(changed ? successMessage : "テストデータの変更に失敗しました。", !changed);
        }
        catch (Exception exception)
        {
            HandleException(exception);
        }
        finally
        {
            _isBusy = false;
        }
    }

    private bool BeginOperation(string message)
    {
        if (_isBusy || UGSCloudSaveManager.instance == null)
        {
            return false;
        }

        _isBusy = true;
        SetStatus(message, false);
        return true;
    }

    private void HandleException(Exception exception)
    {
        Debug.LogException(exception);
        SetStatus($"エラー: {exception.Message}", true);
    }

    private void SetStatus(string message, bool isError)
    {
        _statusMessage = message;
        _statusIsError = isError;
    }

    private static void RefreshAllRuntimeUI()
    {
        foreach (OceanLogUI oceanLogUI in Resources.FindObjectsOfTypeAll<OceanLogUI>())
        {
            if (oceanLogUI != null && oceanLogUI.gameObject.scene.IsValid())
            {
                oceanLogUI.Refresh();
            }
        }

        foreach (SkinInventryManager inventory in Resources.FindObjectsOfTypeAll<SkinInventryManager>())
        {
            if (inventory != null && inventory.gameObject.scene.IsValid())
            {
                inventory.ApplySkinSprites();
            }
        }

        foreach (PlayerSkinHandler playerSkin in Resources.FindObjectsOfTypeAll<PlayerSkinHandler>())
        {
            if (playerSkin != null && playerSkin.gameObject.scene.IsValid())
            {
                playerSkin.RefreshSkin();
            }
        }
    }

    private void SetOpen(bool open)
    {
        if (_isOpen == open)
        {
            return;
        }

        _isOpen = open;
        if (open)
        {
            _timeScaleBeforeOpen = Time.timeScale;
            Time.timeScale = 0f;
            _pausedByMenu = true;

            _blockedEventSystem = EventSystem.current;
            if (_blockedEventSystem != null)
            {
                _blockedEventSystem.enabled = false;
            }
        }
        else
        {
            RestoreGameInput();
        }
    }

    private void RestoreGameInput()
    {
        if (_blockedEventSystem != null)
        {
            _blockedEventSystem.enabled = true;
            _blockedEventSystem = null;
        }

        if (_pausedByMenu)
        {
            Time.timeScale = _timeScaleBeforeOpen;
            _pausedByMenu = false;
        }
    }

    private void EnsureStyles()
    {
        if (_hamburgerStyle != null)
        {
            return;
        }

        Color panel = new Color(0.045f, 0.067f, 0.105f, 1f);
        Color card = new Color(0.075f, 0.11f, 0.17f, 1f);
        Color cardLight = new Color(0.095f, 0.145f, 0.225f, 1f);
        Color accent = new Color(0.16f, 0.48f, 0.95f, 1f);
        Color accentHover = new Color(0.23f, 0.57f, 1f, 1f);
        Color danger = new Color(0.72f, 0.17f, 0.22f, 1f);
        Color dangerHover = new Color(0.87f, 0.24f, 0.3f, 1f);
        Color secondary = new Color(0.16f, 0.22f, 0.32f, 1f);
        Color secondaryHover = new Color(0.22f, 0.3f, 0.43f, 1f);
        Color text = new Color(0.94f, 0.97f, 1f, 1f);
        Color mutedText = new Color(0.74f, 0.8f, 0.88f, 1f);

        _hamburgerStyle = CreateButtonStyle(accent, accentHover, 44, 62f);
        _hamburgerStyle.alignment = TextAnchor.MiddleCenter;

        _panelStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(20, 20, 18, 18),
            normal = { background = CreateSolidTexture(panel) }
        };

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 38,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = text },
            fixedHeight = 48f
        };

        _sectionStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            normal = { textColor = text },
            wordWrap = true,
            margin = new RectOffset(2, 2, 4, 4)
        };

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            normal = { textColor = mutedText },
            wordWrap = true,
            margin = new RectOffset(2, 2, 2, 2)
        };

        _statusStyle = new GUIStyle(_labelStyle)
        {
            fontSize = 21,
            fontStyle = FontStyle.Bold,
            normal =
            {
                textColor = text,
                background = CreateSolidTexture(card)
            },
            padding = new RectOffset(14, 14, 10, 10),
            margin = new RectOffset(0, 0, 6, 4)
        };

        _cardStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = CreateSolidTexture(card) },
            padding = new RectOffset(16, 16, 14, 14),
            margin = new RectOffset(0, 6, 3, 8)
        };

        _summaryStyle = new GUIStyle(_cardStyle)
        {
            normal = { background = CreateSolidTexture(cardLight) },
            alignment = TextAnchor.MiddleLeft,
            fixedHeight = 92f,
            margin = new RectOffset(3, 3, 2, 2)
        };

        _tabStyle = CreateButtonStyle(secondary, secondaryHover, 23, 60f);
        _selectedTabStyle = CreateButtonStyle(accent, accentHover, 23, 60f);
        _buttonStyle = CreateButtonStyle(accent, accentHover, 21, 58f);
        _secondaryButtonStyle = CreateButtonStyle(secondary, secondaryHover, 20, 52f);
        _dangerButtonStyle = CreateButtonStyle(danger, dangerHover, 21, 58f);

        _rowStyle = CreateButtonStyle(card, cardLight, 21, 66f);
        _rowStyle.alignment = TextAnchor.MiddleLeft;
        _rowStyle.padding = new RectOffset(18, 14, 7, 7);
        _rowStyle.margin = new RectOffset(0, 6, 4, 4);

        _selectedRowStyle = CreateButtonStyle(
            new Color(0.11f, 0.32f, 0.62f, 1f),
            new Color(0.15f, 0.4f, 0.75f, 1f),
            21,
            66f);
        _selectedRowStyle.alignment = TextAnchor.MiddleLeft;
        _selectedRowStyle.padding = new RectOffset(18, 14, 7, 7);
        _selectedRowStyle.margin = new RectOffset(0, 6, 4, 4);

        GUI.skin.verticalScrollbar.fixedWidth = 28f;
        GUI.skin.verticalScrollbarThumb.fixedWidth = 28f;
    }

    private GUIStyle CreateButtonStyle(
        Color normalColor,
        Color hoverColor,
        int fontSize,
        float height)
    {
        GUIStyle style = new GUIStyle(GUI.skin.button)
        {
            fontSize = fontSize,
            fontStyle = FontStyle.Bold,
            fixedHeight = height,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            padding = new RectOffset(10, 10, 5, 5),
            margin = new RectOffset(3, 3, 3, 3)
        };

        Texture2D normalTexture = CreateSolidTexture(normalColor);
        Texture2D hoverTexture = CreateSolidTexture(hoverColor);
        style.normal.background = normalTexture;
        style.normal.textColor = Color.white;
        style.hover.background = hoverTexture;
        style.hover.textColor = Color.white;
        style.active.background = hoverTexture;
        style.active.textColor = Color.white;
        style.focused.background = normalTexture;
        style.focused.textColor = Color.white;
        style.onNormal.background = hoverTexture;
        style.onNormal.textColor = Color.white;
        style.onHover.background = hoverTexture;
        style.onHover.textColor = Color.white;
        return style;
    }

    private Texture2D CreateSolidTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1)
        {
            name = "RuntimeTestMenuStyle",
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.SetPixel(0, 0, color);
        texture.Apply();
        _generatedStyleTextures.Add(texture);
        return texture;
    }

    private void DestroyGeneratedStyleTextures()
    {
        foreach (Texture2D texture in _generatedStyleTextures)
        {
            if (texture != null)
            {
                Destroy(texture);
            }
        }
        _generatedStyleTextures.Clear();
    }
#endif
}
