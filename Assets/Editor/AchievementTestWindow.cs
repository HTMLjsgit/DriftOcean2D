using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class AchievementTestWindow : EditorWindow
{
    private enum TestTab
    {
        Achievements,
        Skins
    }

    private static readonly string[] TabLabels = { "実績・海洋ゴミ", "スキン" };

    private readonly HashSet<int> _selectedAchievementIDs = new HashSet<int>();
    private readonly HashSet<int> _selectedGarbageIDs = new HashSet<int>();
    private readonly HashSet<int> _selectedSkinIDs = new HashSet<int>();

    private Vector2 _achievementScrollPosition;
    private Vector2 _skinScrollPosition;
    private TestTab _selectedTab;
    private bool _isBusy;
    private string _statusMessage = "Play Modeに入ると操作できます。";
    private MessageType _statusType = MessageType.Info;

    [MenuItem("Tools/Debug/実績・海洋ゴミテスト")]
    private static void OpenWindow()
    {
        AchievementTestWindow window = GetWindow<AchievementTestWindow>("進行テスト");
        window.minSize = new Vector2(520f, 620f);
        window.Show();
    }

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        _isBusy = false;
        _statusMessage = state == PlayModeStateChange.EnteredPlayMode
            ? "データの読み込み完了後に操作できます。"
            : "Play Modeに入ると操作できます。";
        _statusType = MessageType.Info;
        Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("ゲーム進行 テスト操作", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "最初のテスト操作から一時データモードになります。Cloud Saveへは書き込まず、Play Mode終了時に変更はすべて破棄されます。",
            MessageType.Info);

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Play Mode中だけ操作できます。", MessageType.Info);
            return;
        }

        UGSCloudSaveManager cloudSave = UGSCloudSaveManager.instance;
        AchievementManager achievementManager = AchievementManager.instance;
        OceanLogCatalog catalog = achievementManager != null ? achievementManager.Catalog : null;
        SkinDatabase skinDatabase = SkinDatabase.instance;

        if (!CanUseTestControls(cloudSave, achievementManager, catalog, skinDatabase))
        {
            return;
        }

        DrawCurrentStatus(cloudSave, catalog, skinDatabase);
        if (cloudSave.IsEditorTestSessionActive)
        {
            EditorGUILayout.HelpBox(
                "一時データモード中：通常のゲーム処理からのCloud Save書き込みも停止しています。",
                MessageType.Warning);
        }

        EditorGUILayout.HelpBox(_statusMessage, _statusType);

        GUIStyle tabStyle = new GUIStyle(EditorStyles.miniButtonMid)
        {
            fixedHeight = 32f,
            fontStyle = FontStyle.Bold,
            fontSize = 13
        };
        _selectedTab = (TestTab)GUILayout.Toolbar((int)_selectedTab, TabLabels, tabStyle);
        EditorGUILayout.Space(8f);

        using (new EditorGUI.DisabledScope(_isBusy))
        {
            if (_selectedTab == TestTab.Achievements)
            {
                _achievementScrollPosition = EditorGUILayout.BeginScrollView(_achievementScrollPosition);
                DrawAchievementSection(cloudSave, catalog);
                EditorGUILayout.Space(12f);
                DrawGarbageSection(cloudSave, catalog);
                EditorGUILayout.Space(12f);
                DrawResetSection(cloudSave);
                EditorGUILayout.EndScrollView();
            }
            else
            {
                _skinScrollPosition = EditorGUILayout.BeginScrollView(_skinScrollPosition);
                DrawSkinSection(cloudSave, skinDatabase);
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space(8f);
            DrawRestoreSection(cloudSave);
        }
    }

    private bool CanUseTestControls(
        UGSCloudSaveManager cloudSave,
        AchievementManager achievementManager,
        OceanLogCatalog catalog,
        SkinDatabase skinDatabase)
    {
        if (cloudSave == null || achievementManager == null || catalog == null || skinDatabase == null)
        {
            EditorGUILayout.HelpBox("実績・スキン管理オブジェクトの生成を待っています。", MessageType.Info);
            Repaint();
            return false;
        }

        if (!cloudSave.IsDataLoaded)
        {
            EditorGUILayout.HelpBox("プレイヤーデータの読み込みを待っています。", MessageType.Info);
            Repaint();
            return false;
        }

        if (cloudSave.IsOfflineModeActive())
        {
            EditorGUILayout.HelpBox("UGSがオフラインのため、元になるプレイヤーデータを取得できません。", MessageType.Error);
            return false;
        }

        return true;
    }

    private static void DrawCurrentStatus(
        UGSCloudSaveManager cloudSave,
        OceanLogCatalog catalog,
        SkinDatabase skinDatabase)
    {
        List<AchievementData> regularAchievements = catalog.achievements
            .Where(item => item != null && !item.hiddenUntilCompleted)
            .ToList();
        List<AchievementData> secretAchievements = catalog.achievements
            .Where(item => item != null && item.hiddenUntilCompleted)
            .ToList();
        int unlockedRegularAchievements = regularAchievements
            .Count(item => cloudSave.IsAchievementUnlocked(item.id));
        int unlockedSecretAchievements = secretAchievements
            .Count(item => cloudSave.IsAchievementUnlocked(item.id));
        int discoveredGarbage = cloudSave.GetDiscoveredObstacleIDs().Count;
        List<SkinData> skins = GetTestableSkins(skinDatabase);
        HashSet<int> unlockedSkinIDs = cloudSave.GetUnlockedSkinIDs().ToHashSet();
        int unlockedSkins = skins.Count(item => unlockedSkinIDs.Contains(item.id));

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(
            "通常実績",
            $"{unlockedRegularAchievements}/{regularAchievements.Count} 解除済み");
        EditorGUILayout.LabelField(
            "シークレット実績",
            unlockedSecretAchievements > 0 ? "解除済み" : "未解除");
        EditorGUILayout.LabelField(
            "海洋ゴミ図鑑",
            $"{discoveredGarbage}/{catalog.garbageEntries.Count} 発見済み");
        EditorGUILayout.LabelField(
            "スキン",
            $"{unlockedSkins}/{skins.Count} 開放済み");
        EditorGUILayout.LabelField(
            "データ状態",
            cloudSave.IsEditorTestSessionActive ? "一時データ（Play Mode終了時に破棄）" : "保存済みデータ");
        EditorGUILayout.LabelField(
            "適用先",
            cloudSave.IsEditorTestSessionActive
                ? "UGSCloudSaveManager（一時コピー）"
                : "UGSCloudSaveManager（ロード済みデータ）");
        EditorGUILayout.EndVertical();
    }

    private void DrawAchievementSection(UGSCloudSaveManager cloudSave, OceanLogCatalog catalog)
    {
        List<AchievementData> regularAchievements = catalog.achievements
            .Where(item => item != null && !item.hiddenUntilCompleted)
            .ToList();
        List<AchievementData> secretAchievements = catalog.achievements
            .Where(item => item != null && item.hiddenUntilCompleted)
            .ToList();
        HashSet<int> secretAchievementIDs = secretAchievements
            .Select(item => item.id)
            .ToHashSet();
        _selectedAchievementIDs.RemoveWhere(secretAchievementIDs.Contains);

        EditorGUILayout.LabelField("実績", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "通常実績だけを選択・解除します。シークレット実績は含まれません。",
            EditorStyles.wordWrappedMiniLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("全部選択"))
        {
            SetAllSelected(
                _selectedAchievementIDs,
                regularAchievements.Select(item => item.id));
        }

        if (GUILayout.Button("選択を全部外す"))
        {
            _selectedAchievementIDs.Clear();
        }

        using (new EditorGUI.DisabledScope(_selectedAchievementIDs.Count == 0))
        {
            if (GUILayout.Button($"選択した実績を解除 ({_selectedAchievementIDs.Count})"))
            {
                UnlockAchievementsAsync(_selectedAchievementIDs.ToArray());
            }
        }

        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("実績全開放！（シークレット除外）"))
        {
            UnlockAchievementsAsync(regularAchievements.Select(item => item.id).ToArray());
        }

        EditorGUILayout.Space(4f);
        foreach (AchievementData achievement in regularAchievements)
        {
            bool selected = _selectedAchievementIDs.Contains(achievement.id);
            bool nextSelected = EditorGUILayout.ToggleLeft(
                $"[{achievement.id:00}] {achievement.achievementName}  " +
                $"({GetConditionLabel(achievement)})" +
                (cloudSave.IsAchievementUnlocked(achievement.id) ? "  [解除済み]" : string.Empty),
                selected);
            SetSelected(_selectedAchievementIDs, achievement.id, nextSelected);
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("シークレット実績", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "通常の全部選択・全開放からは除外されています。",
            EditorStyles.wordWrappedMiniLabel);

        bool allSecretsUnlocked = secretAchievements.Count > 0 &&
                                  secretAchievements.All(item => cloudSave.IsAchievementUnlocked(item.id));
        using (new EditorGUI.DisabledScope(secretAchievements.Count == 0 || allSecretsUnlocked))
        {
            if (GUILayout.Button(
                    allSecretsUnlocked
                        ? "シークレット実績は解放済み"
                        : "シークレット実績解放！"))
            {
                UnlockAchievementsAsync(secretAchievements.Select(item => item.id).ToArray());
            }
        }
    }

    private void DrawGarbageSection(UGSCloudSaveManager cloudSave, OceanLogCatalog catalog)
    {
        EditorGUILayout.LabelField("海洋ゴミ図鑑", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "チェックしたゴミに衝突した扱いにして、図鑑登録と実績判定を行います。",
            EditorStyles.wordWrappedMiniLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("全部選択"))
        {
            SetAllSelected(
                _selectedGarbageIDs,
                catalog.garbageEntries.Where(item => item != null).Select(item => item.id));
        }

        if (GUILayout.Button("選択を全部外す"))
        {
            _selectedGarbageIDs.Clear();
        }

        using (new EditorGUI.DisabledScope(_selectedGarbageIDs.Count == 0))
        {
            if (GUILayout.Button($"選択したゴミに衝突 ({_selectedGarbageIDs.Count})"))
            {
                DiscoverGarbageAsync(_selectedGarbageIDs.ToArray());
            }
        }

        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button($"全{catalog.garbageEntries.Count}種類のゴミに衝突"))
        {
            DiscoverGarbageAsync(
                catalog.garbageEntries.Where(item => item != null).Select(item => item.id).ToArray());
        }

        EditorGUILayout.Space(4f);
        foreach (ObstacleData garbage in catalog.garbageEntries.Where(item => item != null))
        {
            bool selected = _selectedGarbageIDs.Contains(garbage.id);
            bool nextSelected = EditorGUILayout.ToggleLeft(
                $"[{garbage.id:00}] {garbage.displayName}" +
                (cloudSave.IsObstacleDiscovered(garbage.id) ? "  [発見済み]" : string.Empty),
                selected);
            SetSelected(_selectedGarbageIDs, garbage.id, nextSelected);
        }
    }

    private void DrawSkinSection(UGSCloudSaveManager cloudSave, SkinDatabase skinDatabase)
    {
        List<SkinData> skins = GetTestableSkins(skinDatabase);
        HashSet<int> unlockedSkinIDs = cloudSave.GetUnlockedSkinIDs().ToHashSet();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("スキン開放テスト", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "チェックしたスキンを一時的に開放・未開放へ変更できます。初期スキンID 1はリセットされません。",
            EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("全部選択"))
        {
            SetAllSelected(_selectedSkinIDs, skins.Select(item => item.id));
        }

        if (GUILayout.Button("選択を全部外す"))
        {
            _selectedSkinIDs.Clear();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(_selectedSkinIDs.Count == 0))
        {
            if (GUILayout.Button($"選択したスキンを開放 ({_selectedSkinIDs.Count})"))
            {
                UnlockSkins(_selectedSkinIDs.ToArray());
            }

            if (GUILayout.Button($"選択したスキンを未開放へ戻す ({_selectedSkinIDs.Count})"))
            {
                ResetSkins(_selectedSkinIDs.ToArray());
            }
        }

        EditorGUILayout.EndHorizontal();

        using (new EditorGUI.DisabledScope(_selectedSkinIDs.Count != 1))
        {
            if (GUILayout.Button("選択した1体を開放して装備", GUILayout.Height(28f)))
            {
                UnlockAndEquipSkin(_selectedSkinIDs.Single());
            }
        }

        GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
        if (GUILayout.Button("全スキン開放（報酬・シークレット含む）", GUILayout.Height(28f)))
        {
            UnlockSkins(skins.Select(item => item.id).ToArray());
        }

        GUI.backgroundColor = new Color(1f, 0.65f, 0.65f);
        if (GUILayout.Button("全スキンを未開放へ戻す（ID 1のみ残す）", GUILayout.Height(28f)))
        {
            ConfirmAndResetAllSkins(cloudSave);
        }

        GUI.backgroundColor = Color.white;

        EditorGUILayout.HelpBox(
            "透明スキンなどの隠し報酬もテスト用に直接操作できます。ゲーム本編の解除条件は変更されません。",
            MessageType.Info);

        EditorGUILayout.Space(4f);
        foreach (SkinData skin in skins)
        {
            bool selected = _selectedSkinIDs.Contains(skin.id);
            bool nextSelected = EditorGUILayout.ToggleLeft(
                $"[{skin.id:00}] {GetSkinDisplayName(skin)}  " +
                $"({GetSkinUnlockLabel(skin)})" +
                (unlockedSkinIDs.Contains(skin.id) ? "  [開放済み]" : string.Empty),
                selected);
            SetSelected(_selectedSkinIDs, skin.id, nextSelected);
        }
    }

    private void DrawResetSection(UGSCloudSaveManager cloudSave)
    {
        EditorGUILayout.LabelField("リセット", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "一時データ上で、実績の解除状態と海洋ゴミ図鑑だけをリセットします。",
            MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("実績だけリセット"))
        {
            ConfirmAndResetAchievements(cloudSave);
        }

        if (GUILayout.Button("海洋ゴミ図鑑だけリセット"))
        {
            ConfirmAndResetGarbage(cloudSave);
        }

        EditorGUILayout.EndHorizontal();

        GUI.backgroundColor = new Color(1f, 0.65f, 0.65f);
        if (GUILayout.Button("実績＋海洋ゴミ図鑑をまとめてリセット"))
        {
            ConfirmAndResetAll(cloudSave);
        }

        GUI.backgroundColor = Color.white;
    }

    private void DrawRestoreSection(UGSCloudSaveManager cloudSave)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("一時テストデータ", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(!cloudSave.IsEditorTestSessionActive))
        {
            if (GUILayout.Button("テスト開始前の全データに今すぐ戻す", GUILayout.Height(26f)))
            {
                RestoreTestSession(cloudSave);
            }
        }

        EditorGUILayout.EndVertical();
    }

    private async void UnlockAchievementsAsync(IReadOnlyCollection<int> achievementIDs)
    {
        if (!TryBeginOperation("実績を解除しています…", out UGSCloudSaveManager cloudSave))
        {
            return;
        }

        try
        {
            bool changed = cloudSave.EditorUnlockAchievements(achievementIDs);
            if (!changed)
            {
                SetOperationResult("実績のテスト変更に失敗しました。", MessageType.Error);
                return;
            }

            await AchievementManager.instance.EditorSyncRewardSkins();
            RefreshOceanLogUI();
            SetOperationResult($"実績を{achievementIDs.Count}件解除しました。", MessageType.Info);
        }
        catch (Exception exception)
        {
            HandleOperationException(exception);
        }
        finally
        {
            EndOperation();
        }
    }

    private async void DiscoverGarbageAsync(IReadOnlyCollection<int> garbageIDs)
    {
        if (!TryBeginOperation("海洋ゴミ図鑑へ登録しています…", out UGSCloudSaveManager cloudSave))
        {
            return;
        }

        try
        {
            bool changed = cloudSave.EditorDiscoverObstacles(garbageIDs);
            if (!changed)
            {
                SetOperationResult("海洋ゴミ図鑑のテスト変更に失敗しました。", MessageType.Error);
                return;
            }

            List<AchievementData> unlocked = await AchievementManager.instance.EvaluateAchievements();
            RefreshOceanLogUI();
            string achievementResult = unlocked.Count > 0
                ? $" 実績も{unlocked.Count}件解除されました。"
                : string.Empty;
            SetOperationResult(
                $"選択したゴミ{garbageIDs.Count}種類を図鑑へ登録しました。{achievementResult}",
                MessageType.Info);
        }
        catch (Exception exception)
        {
            HandleOperationException(exception);
        }
        finally
        {
            EndOperation();
        }
    }

    private void UnlockSkins(IReadOnlyCollection<int> skinIDs)
    {
        if (!TryBeginOperation("スキンを開放しています…", out UGSCloudSaveManager cloudSave))
        {
            return;
        }

        try
        {
            bool changed = cloudSave.EditorUnlockSkins(skinIDs);
            RefreshSkinUI();
            SetOperationResult(
                changed ? $"スキンを{skinIDs.Count}件開放しました。" : "スキンのテスト変更に失敗しました。",
                changed ? MessageType.Info : MessageType.Error);
        }
        catch (Exception exception)
        {
            HandleOperationException(exception);
        }
        finally
        {
            EndOperation();
        }
    }

    private void UnlockAndEquipSkin(int skinID)
    {
        if (!TryBeginOperation("スキンを開放して装備しています…", out UGSCloudSaveManager cloudSave))
        {
            return;
        }

        try
        {
            bool changed = cloudSave.EditorUnlockAndEquipSkin(skinID);
            RefreshSkinUI();
            SetOperationResult(
                changed ? $"スキンID {skinID}を開放して装備しました。" : "スキンのテスト変更に失敗しました。",
                changed ? MessageType.Info : MessageType.Error);
        }
        catch (Exception exception)
        {
            HandleOperationException(exception);
        }
        finally
        {
            EndOperation();
        }
    }

    private void ResetSkins(IReadOnlyCollection<int> skinIDs)
    {
        int[] resettableSkinIDs = skinIDs.Where(id => id > 1).ToArray();
        if (resettableSkinIDs.Length == 0)
        {
            SetOperationResult("初期スキンID 1はリセット対象外です。", MessageType.Warning);
            Repaint();
            return;
        }

        if (!TryBeginOperation("スキンをリセットしています…", out UGSCloudSaveManager cloudSave))
        {
            return;
        }

        try
        {
            bool changed = cloudSave.EditorResetSkins(resettableSkinIDs);
            RefreshSkinUI();
            SetOperationResult(
                changed ? $"選択したスキンを{resettableSkinIDs.Length}件リセットしました。" : "スキンのテスト変更に失敗しました。",
                changed ? MessageType.Info : MessageType.Error);
        }
        catch (Exception exception)
        {
            HandleOperationException(exception);
        }
        finally
        {
            EndOperation();
        }
    }

    private void ConfirmAndResetAllSkins(UGSCloudSaveManager cloudSave)
    {
        if (!EditorUtility.DisplayDialog(
                "全スキンをリセット",
                "初期スキンID 1だけを残し、ほかのスキンをすべて未開放へ戻します。続けますか？",
                "全リセット",
                "キャンセル"))
        {
            return;
        }

        if (!BeginOperation("全スキンをリセットしています…")) return;

        try
        {
            bool changed = cloudSave.EditorResetAllSkins();
            RefreshSkinUI();
            SetOperationResult(
                changed ? "全スキンを初期状態へリセットしました。" : "スキンのテスト変更に失敗しました。",
                changed ? MessageType.Info : MessageType.Error);
        }
        catch (Exception exception)
        {
            HandleOperationException(exception);
        }
        finally
        {
            EndOperation();
        }
    }

    private void ConfirmAndResetAchievements(UGSCloudSaveManager cloudSave)
    {
        if (EditorUtility.DisplayDialog(
                "実績をリセット",
                "解除済み実績をすべて未解除へ戻します。続けますか？",
                "リセット",
                "キャンセル"))
        {
            ResetAchievements(cloudSave);
        }
    }

    private void ConfirmAndResetGarbage(UGSCloudSaveManager cloudSave)
    {
        if (EditorUtility.DisplayDialog(
                "海洋ゴミ図鑑をリセット",
                "発見済み・閲覧済みの海洋ゴミをすべて未発見へ戻します。続けますか？",
                "リセット",
                "キャンセル"))
        {
            ResetGarbage(cloudSave);
        }
    }

    private void ConfirmAndResetAll(UGSCloudSaveManager cloudSave)
    {
        if (EditorUtility.DisplayDialog(
                "テストデータをまとめてリセット",
                "実績と海洋ゴミ図鑑をすべてリセットします。続けますか？",
                "まとめてリセット",
                "キャンセル"))
        {
            ResetAll(cloudSave);
        }
    }

    private void ResetAchievements(UGSCloudSaveManager cloudSave)
    {
        if (!BeginOperation("実績をリセットしています…")) return;

        try
        {
            bool saved = cloudSave.EditorResetAchievements();
            RefreshOceanLogUI();
            SetOperationResult(
                saved ? "実績をすべてリセットしました。" : "実績のテスト変更に失敗しました。",
                saved ? MessageType.Info : MessageType.Error);
        }
        catch (Exception exception)
        {
            HandleOperationException(exception);
        }
        finally
        {
            EndOperation();
        }
    }

    private void ResetGarbage(UGSCloudSaveManager cloudSave)
    {
        if (!BeginOperation("海洋ゴミ図鑑をリセットしています…")) return;

        try
        {
            bool saved = cloudSave.EditorResetOceanLog();
            RefreshOceanLogUI();
            SetOperationResult(
                saved ? "海洋ゴミ図鑑をすべてリセットしました。" : "海洋ゴミ図鑑のテスト変更に失敗しました。",
                saved ? MessageType.Info : MessageType.Error);
        }
        catch (Exception exception)
        {
            HandleOperationException(exception);
        }
        finally
        {
            EndOperation();
        }
    }

    private void ResetAll(UGSCloudSaveManager cloudSave)
    {
        if (!BeginOperation("実績と海洋ゴミ図鑑をリセットしています…")) return;

        try
        {
            bool achievementsSaved = cloudSave.EditorResetAchievements();
            bool garbageSaved = achievementsSaved && cloudSave.EditorResetOceanLog();
            bool saved = achievementsSaved && garbageSaved;
            RefreshOceanLogUI();
            SetOperationResult(
                saved
                    ? "実績と海洋ゴミ図鑑をすべてリセットしました。"
                    : "まとめてリセットする途中でテスト変更に失敗しました。",
                saved ? MessageType.Info : MessageType.Error);
        }
        catch (Exception exception)
        {
            HandleOperationException(exception);
        }
        finally
        {
            EndOperation();
        }
    }

    private bool TryBeginOperation(string message, out UGSCloudSaveManager cloudSave)
    {
        cloudSave = UGSCloudSaveManager.instance;
        return cloudSave != null && BeginOperation(message);
    }

    private void RestoreTestSession(UGSCloudSaveManager cloudSave)
    {
        if (!EditorUtility.DisplayDialog(
                "テスト開始前へ戻す",
                "Play Modeは続けたまま、最初のテスト操作前の状態へ戻します。続けますか？",
                "元に戻す",
                "キャンセル"))
        {
            return;
        }

        bool restored = cloudSave.EditorRestoreTestSession();
        RefreshOceanLogUI();
        RefreshSkinUI();
        SetOperationResult(
            restored ? "テスト開始前の状態へ戻しました。" : "戻す一時データがありません。",
            restored ? MessageType.Info : MessageType.Warning);
        Repaint();
    }

    private bool BeginOperation(string message)
    {
        if (_isBusy || !EditorApplication.isPlaying)
        {
            return false;
        }

        _isBusy = true;
        _statusMessage = message;
        _statusType = MessageType.Info;
        Repaint();
        return true;
    }

    private void EndOperation()
    {
        _isBusy = false;
        Repaint();
    }

    private void SetOperationResult(string message, MessageType type)
    {
        _statusMessage = message;
        _statusType = type;
    }

    private void HandleOperationException(Exception exception)
    {
        Debug.LogException(exception);
        SetOperationResult($"処理中にエラーが発生しました: {exception.Message}", MessageType.Error);
    }

    private static void RefreshOceanLogUI()
    {
        foreach (OceanLogUI oceanLogUI in Resources.FindObjectsOfTypeAll<OceanLogUI>())
        {
            if (oceanLogUI != null && oceanLogUI.gameObject.scene.IsValid())
            {
                oceanLogUI.Refresh();
            }
        }
    }

    private static void RefreshSkinUI()
    {
        foreach (SkinInventryManager skinInventory in Resources.FindObjectsOfTypeAll<SkinInventryManager>())
        {
            if (skinInventory != null && skinInventory.gameObject.scene.IsValid())
            {
                skinInventory.ApplySkinSprites();
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

    private static List<SkinData> GetTestableSkins(SkinDatabase skinDatabase)
    {
        return skinDatabase.GetAllSkins()
            .Where(item => item != null && item.id > 0)
            .OrderBy(item => item.id)
            .ToList();
    }

    private static string GetSkinDisplayName(SkinData skin)
    {
        return string.IsNullOrWhiteSpace(skin.skinName) ? skin.name : skin.skinName;
    }

    private static string GetSkinUnlockLabel(SkinData skin)
    {
        switch (skin.unlockType)
        {
            case SkinData.UnlockType.None:
                return "初期開放";
            case SkinData.UnlockType.ScoreReach:
                return $"スコア {Mathf.RoundToInt(skin.conditionValue)}";
            case SkinData.UnlockType.PlayCount:
                return $"プレイ {Mathf.RoundToInt(skin.conditionValue)}回";
            case SkinData.UnlockType.SurvivalTime:
                return $"生存 {skin.conditionValue:0.#}秒";
            case SkinData.UnlockType.TotalPlayTime:
                return $"累計プレイ {skin.conditionValue / 60f:0.#}分";
            case SkinData.UnlockType.DeathCount:
                return $"ゲームオーバー {Mathf.RoundToInt(skin.conditionValue)}回";
            case SkinData.UnlockType.AdWatch:
                return "広告視聴";
            case SkinData.UnlockType.CompleteAll:
                return "通常スキンコンプリート";
            case SkinData.UnlockType.NoInput:
                return "無操作";
            case SkinData.UnlockType.SNSShare:
                return "SNSシェア";
            case SkinData.UnlockType.ConsecutiveSurvival:
                return "連続生存";
            case SkinData.UnlockType.NoContinueHardMode:
                return "ノーコンティニューハード";
            case SkinData.UnlockType.TapUnlock:
                return $"同じスキンを{Mathf.RoundToInt(skin.conditionValue)}回タップ";
            case SkinData.UnlockType.MaxDifficultySurvival:
                return "最高難易度";
            case SkinData.UnlockType.AchievementCompleteReward:
                return "実績コンプリート報酬";
            case SkinData.UnlockType.SecretAchievementReward:
                return "シークレット実績報酬";
            default:
                return skin.unlockType.ToString();
        }
    }

    private static void SetAllSelected(HashSet<int> selection, IEnumerable<int> IDs)
    {
        selection.Clear();
        foreach (int id in IDs)
        {
            selection.Add(id);
        }
    }

    private static void SetSelected(HashSet<int> selection, int id, bool selected)
    {
        if (selected)
        {
            selection.Add(id);
        }
        else
        {
            selection.Remove(id);
        }
    }

    private static string GetConditionLabel(AchievementData achievement)
    {
        int integerValue = Mathf.RoundToInt(achievement.conditionValue);
        switch (achievement.conditionType)
        {
            case AchievementData.ConditionType.SkinCount:
                return $"スキン {integerValue}種類";
            case AchievementData.ConditionType.TotalBounceCount:
                return $"累計タップ {integerValue}回";
            case AchievementData.ConditionType.HardModeReached:
                return "ハードモード到達";
            case AchievementData.ConditionType.GarbageCount:
                return $"海洋ゴミ図鑑 {integerValue}種類";
            case AchievementData.ConditionType.ConsecutivePlayDays:
                return $"連続プレイ {integerValue}日";
            case AchievementData.ConditionType.TotalPlayTime:
                return $"累計プレイ {achievement.conditionValue / 60f:0.#}分";
            case AchievementData.ConditionType.ScoreReach:
                return $"スコア {integerValue}";
            case AchievementData.ConditionType.AllRegularAchievements:
                return "通常実績をすべて解除";
            default:
                return achievement.conditionType.ToString();
        }
    }
}
