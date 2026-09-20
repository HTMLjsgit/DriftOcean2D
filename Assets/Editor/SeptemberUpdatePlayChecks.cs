using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Repeatable integration check using the real Main scene and physics, with services disabled.</summary>
[InitializeOnLoad]
public static class SeptemberUpdatePlayChecks
{
    private const string RunningKey = "DriftOcean.SeptemberPlayChecks";
    private const string Output = "Temp/SeptemberFeedbackQA";
    private static readonly List<string> Passed = new List<string>();
    private static readonly List<GameObject> Obstacles = new List<GameObject>();
    private static int _phase;
    private static double _until;
    private static bool _ready;
    private static CompanionFish _survivor;
    private static Vector3 _before;
    private static PlanktonPickup[] _nearby;
    private static float[] _initialDistances;
    private static PlanktonPickup _outside;
    private static PlanktonPickup _sway;
    private static PlanktonPickup _latePlankton;
    private static float _stoppedHeight;
    private static float _beforeX;
    private static BindingFlags Fields => BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    static SeptemberUpdatePlayChecks()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(RunningKey, false)) return;
            SessionState.SetBool(RunningKey, false);
            string previous = SessionState.GetString(RunningKey + ".scene", "");
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
            RestoreString("PlayerName");
            RestoreFloat("OfflinePendingBestScore");
            RestoreFloat("OfflinePendingLeaderboardScore");
            PlayerPrefs.Save();
            if (SessionState.GetBool(RunningKey + ".batch", false))
            {
                SessionState.SetBool(RunningKey + ".batch", false);
                int exitCode = SessionState.GetInt(RunningKey + ".exit", 1);
                EditorApplication.delayCall += () => EditorApplication.Exit(exitCode);
            }
        };
    }

    public static void BeginBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Use Begin for interactive checks.");
        SessionState.SetBool(RunningKey + ".batch", true);
        Begin();
    }

    [MenuItem("DriftOcean/September Update/Run Main Scene Integration Checks")]
    public static void Begin()
    {
        SessionState.SetBool(RunningKey + ".title", false);
        BeginScene("Main");
    }

    [MenuItem("DriftOcean/September Update/Run Title Ocean Log Checks")]
    public static void BeginTitle()
    {
        SessionState.SetBool(RunningKey + ".title", true);
        BeginScene("Title");
    }

    private static void BeginScene(string scene)
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
        Directory.CreateDirectory(Output);
        SessionState.SetString(RunningKey + ".scene", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        SnapshotString("PlayerName");
        SnapshotFloat("OfflinePendingBestScore");
        SnapshotFloat("OfflinePendingLeaderboardScore");
        SessionState.SetBool(RunningKey, true);
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/" + scene + ".unity");
        EditorApplication.isPlaying = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Prepare()
    {
        if (!SessionState.GetBool(RunningKey, false)) return;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != (SessionState.GetBool(RunningKey + ".title", false) ? "Title" : "Main")) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        // Runs before Start: prevent network sign-in and advertising SDK initialization.
        var ugs = UnityEngine.Object.FindFirstObjectByType<UGSManager>();
        ugs.enabled = false;
        Set(ugs, "simulateOfflineInEditor", true);
        var cloud = UnityEngine.Object.FindFirstObjectByType<UGSCloudSaveManager>();
        cloud.enabled = false;
        Set(cloud, "_ugsManager", ugs);
        Set(cloud, "_playerData", new PlayerCloudData());
        typeof(UGSCloudSaveManager).GetProperty("IsEditorTestSessionActive").SetValue(cloud, true);
        typeof(UGSCloudSaveManager).GetProperty("IsDataLoaded").SetValue(cloud, true);
        var ads = UnityEngine.Object.FindFirstObjectByType<AdsManager>();
        if (ads != null) ads.enabled = false;
        Passed.Clear();
        Obstacles.Clear();
        _phase = 0;
        _ready = true;
        _until = EditorApplication.timeSinceStartup + 2;
    }

    private static void Tick()
    {
        if (!_ready || !EditorApplication.isPlaying || !SessionState.GetBool(RunningKey, false) || EditorApplication.timeSinceStartup < _until) return;
        try
        {
            if (SessionState.GetBool(RunningKey + ".title", false))
            {
                if (_phase == 0) { _phase++; CheckOceanLog(); Wait(0.4); }
                else if (_phase == 1) { _phase++; CaptureOceanLog(); Wait(0.25); }
                else Finish(null);
            }
            else Step();
        }
        catch (Exception exception) { Finish(exception.ToString()); }
    }

    private static void Step()
    {
        var life = OceanLifeManager.instance;
        var player = PlayerController.instance;
        var game = GameManager.instance;
        switch (_phase++)
        {
            case 0:
                Check(life != null && life.enabled, "Main scene references are connected");
                player.SetGravityScale(0f);
                player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
                ObstaclesSpawner.instance.enabled = false;
                ObstaclesSpawner.instance.ClearAllObstacles();
                DifficultyManager.instance.enabled = false;
                ScoreManager.instance.ScoreMeasureStop();
                StageManager.instance.SetCurrentPlay(false);
                game.SetGameState(GameManager.GameState.Playing);
                life.ResetRun();
                Set(life, "_spawnTimer", float.PositiveInfinity);
                Collect(99);
                Check(life.PlanktonCount == 99 && life.FishCount == 0, "99 plankton do not summon a fish");
                SpawnCheckPlankton(player.transform.position);
                Wait(0.15);
                break;
            case 1:
                Check(life.PlanktonCount == 100 && life.FishCount == 1, "Real player collision collects once and summons the first fish");
                Collect(200);
                Check(life.PlanktonCount == 300 && life.FishCount == 3, "Maximum three fish at 300 plankton");
                Wait(1.3);
                break;
            case 2:
                _survivor = Followers(life)[1];
                _before = Followers(life)[0].transform.position;
                player.GetComponent<Rigidbody2D>().position += Vector2.up;
                Wait(0.06);
                break;
            case 3:
                Check(Mathf.Abs(Followers(life)[0].transform.position.y - _before.y) < 0.3f, "Following has a visible delay after player movement");
                Wait(0.75);
                break;
            case 4:
                Check(Followers(life)[0].transform.position.y > _before.y + 0.7f, "Fish catch up to the delayed player path");
                foreach (int slot in new[] { 0, 2 })
                {
                    var obstacle = new GameObject("IntegrationCheckObstacle");
                    obstacle.tag = "Obstacle";
                    obstacle.transform.position = Followers(life)[slot].transform.position;
                    var body = obstacle.AddComponent<Rigidbody2D>();
                    body.gravityScale = 0;
                    body.constraints = RigidbodyConstraints2D.FreezeAll;
                    obstacle.AddComponent<BoxCollider2D>().size = Vector2.one * 0.025f;
                    Obstacles.Add(obstacle);
                }
                Wait(0.05);
                break;
            case 5:
                Check(life.FishCount == 1 && Followers(life)[0] == _survivor, "Only the two colliding fish leave; middle fish survives");
                foreach (var obstacle in Obstacles) UnityEngine.Object.Destroy(obstacle);
                Obstacles.Clear();
                Check(game.state == GameManager.GameState.Playing, "Fish hits do not damage or shield the player");
                Wait(0.6);
                break;
            case 6:
                Vector2 expected = (Vector2)player.transform.position + life.Settings.formationOffsets[0];
                Check(Vector2.Distance(_survivor.transform.position, expected) < 0.08f, "Surviving middle fish moves into the upper-left slot");
                Collect(200);
                Check(life.FishCount == 3 && life.PlanktonCount == 500, "Lost fish are replaced at subsequent hundreds");
                CheckNameChange();
                Wait(0.9);
                break;
            case 7:
                NicknameInputUI.instance.HidePanel();
                GameOverManager.instance.GameOver();
                Check(Time.timeScale == 0 && game.state == GameManager.GameState.GameOver, "Player game over is not prevented by companions");
                var label = Get<TMPro.TextMeshProUGUI>(GameOverManager.instance, "_planktonCountText");
                Check(label.text == "PLANKTON × 500" && label.gameObject.activeInHierarchy, "Result screen shows the run's plankton count");
                _before = _survivor.transform.position;
                Check(!life.CollectPlankton(player) && life.PlanktonCount == 500, "Result screen cannot collect plankton");
                Wait(1.75);
                break;
            case 8:
                Check(_survivor != null && Vector3.Distance(_before, _survivor.transform.position) < 0.02f, "Fish remain visible and still for almost two seconds at the result screen");
                Wait(1.4);
                break;
            case 9:
                Check(life.GetComponentsInChildren<CompanionFish>().Length == 0, "Fish escape while Time.timeScale is zero");
                var notice = Get<GameObject>(GameOverManager.instance, "_unlockNoticePanel");
                notice.SetActive(true);
                Get<TMPro.TextMeshProUGUI>(GameOverManager.instance, "_unlockNoticeText").text = "SKIN UNLOCKED!\nACHIEVEMENT UNLOCKED!";
                CheckResultLayout();
                ScreenCapture.CaptureScreenshot(Path.Combine(Output, "result.png"));
                Wait(0.25);
                break;
            case 10:
                Invoke(GameOverManager.instance, "ExecuteContinue");
                Set(life, "_spawnTimer", float.PositiveInfinity);
                Check(life.PlanktonCount == 500 && life.FishCount == 0 && Time.timeScale == 1, "Continue preserves count and does not revive escaped fish");
                Collect(99);
                Check(life.FishCount == 0, "Continue still respects the next 100 threshold");
                Collect(1);
                Check(life.FishCount == 1 && life.PlanktonCount == 600, "A new fish arrives at the next hundred after continue");
                Invoke(GameOverManager.instance, "OnRetryButtonClicked");
                Check(life.PlanktonCount == 0 && life.FishCount == 0, "Retry resets all collection and companion progress");
                Set(life, "_spawnTimer", float.PositiveInfinity);
                Collect(300);
                Wait(1);
                break;
            case 11:
                for (int i = 0; i < 8; i++)
                {
                    SpawnCheckPlankton(new Vector2(-0.8f + i * 0.4f, 0.4f + Mathf.Sin(i * 0.7f) * 0.12f), color: life.Settings.colors[i].color);
                }
                ScreenCapture.CaptureScreenshot(Path.Combine(Output, "gameplay.png"));
                Wait(0.3);
                break;
            case 12:
                life.ResetRun();
                Set(life, "_spawnTimer", float.PositiveInfinity);
                player.SetGravityScale(0f);
                player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
                ScoreManager.instance.ScoreMeasureStop();
                ScoreManager.instance.currentScore = 1980f;
                StageManager.instance.SetCurrentPlay(false);
                _nearby = new[]
                {
                    SpawnCheckPlankton((Vector2)player.transform.position + new Vector2(0.5f, 0.3f)),
                    SpawnCheckPlankton((Vector2)player.transform.position + new Vector2(0.58f, -0.2f)),
                    SpawnCheckPlankton((Vector2)player.transform.position + new Vector2(0.05f, 0.65f))
                };
                _initialDistances = _nearby.Select(p => Vector2.Distance(p.GetComponent<Rigidbody2D>().position, player.transform.position)).ToArray();
                _outside = SpawnCheckPlankton((Vector2)player.transform.position + new Vector2(1.35f, 1.2f));
                Wait(0.12);
                break;
            case 13:
                Check(life.PlanktonCount == 0, "Nearby plankton move visibly before being collected");
                for (int i = 0; i < _nearby.Length; i++)
                    Check(Vector2.Distance(_nearby[i].GetComponent<Rigidbody2D>().position, player.transform.position) < _initialDistances[i] - 0.01f,
                        "Nearby plankton " + (i + 1) + " is pulled toward the player");
                Wait(0.65);
                break;
            case 14:
                Check(life.PlanktonCount == 3 && _nearby.All(p => !p.gameObject.activeSelf), "All three nearby plankton are absorbed and recycled");
                Check(_outside.gameObject.activeSelf && !Get<bool>(_outside, "_attracted"), "Distant plankton are not attracted");
                Wait(0.15);
                break;
            case 15:
                Check(life.PlanktonCount == 3, "Attracted plankton each count exactly once");
                life.ResetRun();
                Set(life, "_spawnTimer", float.PositiveInfinity);
                ScoreManager.instance.currentScore = 1989.5f;
                _sway = SpawnCheckPlankton(new Vector2(1.5f, 2f), 0.5f, 0.7f);
                _before = _sway.GetComponent<Rigidbody2D>().position;
                Wait(0.2);
                break;
            case 16:
                Check(Mathf.Abs(_sway.GetComponent<Rigidbody2D>().position.y - _before.y) > 0.01f, "Plankton still sway before 1990");
                _stoppedHeight = _sway.GetComponent<Rigidbody2D>().position.y;
                _beforeX = _sway.GetComponent<Rigidbody2D>().position.x;
                ScoreManager.instance.currentScore = 1990f;
                Wait(0.25);
                break;
            case 17:
                Check(Mathf.Abs(_sway.GetComponent<Rigidbody2D>().position.y - _stoppedHeight) < 0.001f, "Existing plankton stop swaying at 1990 without jumping vertically");
                Check(_sway.GetComponent<Rigidbody2D>().position.x < _beforeX - 0.05f, "Horizontal drifting continues after 1990");
                _latePlankton = SpawnCheckPlankton(new Vector2(1.3f, 2.3f), 0.5f, 1.4f);
                Wait(0.25);
                break;
            case 18:
                Check(Mathf.Abs(_latePlankton.GetComponent<Rigidbody2D>().position.y - 2.3f) < 0.001f, "New plankton spawned after 1990 do not sway");
                _outside = SpawnCheckPlankton((Vector2)player.transform.position + new Vector2(0.5f, 0.3f));
                game.SetGameState(GameManager.GameState.GameOver);
                Time.timeScale = 0f;
                _before = _outside.GetComponent<Rigidbody2D>().position;
                Wait(0.2);
                break;
            case 19:
                Check(life.PlanktonCount == 0 && Vector2.Distance(_outside.GetComponent<Rigidbody2D>().position, _before) < 0.001f,
                    "Attraction and collection stop during game over");
                Time.timeScale = 1f;
                game.SetGameState(GameManager.GameState.Playing);
                life.ResetRun();
                Set(life, "_spawnTimer", float.PositiveInfinity);
                CheckOceanLog();
                Wait(0.4);
                break;
            case 20:
                CaptureOceanLog();
                Wait(0.25);
                break;
            default:
                Finish(null);
                break;
        }
    }

    private static void CheckNameChange()
    {
        var ranking = RankingUIManager.instance;
        var ugs = UGSManager.instance;
        var historical = new UGSRankingEntry { playerName = "Before", score = 2000, skinID = 1 };
        Set(ranking, "_playerWeeklyEntry", historical);
        Set(ugs, "isSignedIn", true);
        try
        {
            Invoke(ranking, "OnNameChanged", "After");
            Check(Get<TMPro.TextMeshProUGUI>(ranking, "_yourNameText").text == "After", "High-score name updates immediately");
            Check(historical.playerName == "Before", "Changing a name preserves the historical ranking entry");
            var input = NicknameInputUI.instance;
            bool called = false;
            input.ShowPanel("What's your name", _ => called = true);
            Get<TMPro.TMP_InputField>(input, "_nameInputField").text = "ＰＯＲＮ";
            Invoke(input, "OnSubmitClicked");
            Check(!called && Get<GameObject>(input, "_inputPanel").activeSelf, "Blocked name stays in the input panel without submitting");
            var message = Get<TMPro.TextMeshProUGUI>(input, "_messageText");
            message.ForceMeshUpdate();
            Check(message.margin == Vector4.zero && message.rectTransform.rect.width <= 560 && !message.isTextOverflowing,
                "Validation message fits inside the nickname panel");
            ScreenCapture.CaptureScreenshot(Path.Combine(Output, "name-filter.png"));
        }
        finally { Set(ugs, "isSignedIn", false); }
    }

    private static void CheckResultLayout()
    {
        var manager = GameOverManager.instance;
        Canvas.ForceUpdateCanvases();
        Rect count = WorldRect(Get<TMPro.TextMeshProUGUI>(manager, "_planktonCountText").rectTransform);
        var score = Get<TMPro.TextMeshProUGUI>(manager, "_scoreText");
        Rect card = WorldRect((RectTransform)score.transform.parent);
        Check(Vector2.Distance(WorldRect(score.rectTransform).center, card.center) < 0.001f, "Year text is centered in the Score frame");
        Check(Mathf.Abs(count.center.x - card.center.x) < 0.001f && count.center.y < card.center.y && card.Contains(count.min) && card.Contains(count.max),
            "Plankton count is centered in the lower part of the Score frame");
        Check(score.text.EndsWith(Application.systemLanguage == SystemLanguage.Japanese ? "年" : " years"), "Result score includes the year unit");
        foreach (string field in new[] { "_scoreText", "_resultCommentText", "_unlockNoticeText" })
            Check(!count.Overlaps(WorldRect(Get<TMPro.TextMeshProUGUI>(manager, field).rectTransform)), "Plankton result does not overlap " + field);
    }

    private static PlanktonPickup SpawnCheckPlankton(Vector2 position, float speed = 0f, float phase = 0f, Color? color = null)
    {
        var life = OceanLifeManager.instance;
        var pickup = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<PlanktonPickup>("Assets/Prefabs/OceanLife/Plankton.prefab"), life.transform);
        pickup.Initialize(life, position, speed, -20f, phase, color ?? Color.white);
        Get<List<PlanktonPickup>>(life, "_activePlankton").Add(pickup);
        return pickup;
    }

    private static void CheckOceanLog()
    {
        var ui = UnityEngine.Object.FindFirstObjectByType<OceanLogUI>();
        var catalog = AssetDatabase.LoadAssetAtPath<OceanLogCatalog>("Assets/Scriptable/OceanLog/OceanLogCatalog.asset");
        var data = Get<PlayerCloudData>(UGSCloudSaveManager.instance, "_playerData");
        data.discoveredObstacleIDs = catalog.garbageEntries.Select(entry => entry.id).ToList();
        data.viewedObstacleIDs.Clear();
        ui.Open();
        Check(Get<TMPro.TextMeshProUGUI>(ui, "_detailDecompositionLabel") != null &&
            Get<TMPro.TextMeshProUGUI>(ui, "_detailMaterialsLabel") != null &&
            Get<TMPro.TextMeshProUGUI>(ui, "_detailSourcesLabel") != null, "All Ocean Log detail headers are connected in " + ui.gameObject.scene.name);
        var entries = Get<List<OceanLogGarbageEntryUI>>(ui, "_garbageEntries");
        for (int i = 0; i < catalog.garbageEntries.Count; i++)
        {
            Get<UnityEngine.UI.Button>(entries[i], "_button").onClick.Invoke();
            string expected = Application.systemLanguage == SystemLanguage.Japanese ? catalog.garbageEntries[i].displayName : catalog.garbageEntries[i].displayNameEnglish;
            Check(Get<GameObject>(ui, "_detailPanel").activeInHierarchy && Get<TMPro.TextMeshProUGUI>(ui, "_detailNameText").text == expected,
                "Ocean Log entry " + catalog.garbageEntries[i].id + " opens the correct details");
            Get<UnityEngine.UI.Button>(ui, "_detailCloseButton").onClick.Invoke();
        }
        Check(data.viewedObstacleIDs.Count == catalog.garbageEntries.Count, "Viewing details updates the NEW markers");
        ui.Close();
        ui.Open();
        Get<UnityEngine.UI.Button>(entries[0], "_button").onClick.Invoke();
        Check(Get<GameObject>(ui, "_detailPanel").activeInHierarchy, "Ocean Log details still open after closing and reopening the log");
    }

    private static void CaptureOceanLog()
    {
        // Wait for the text and nested layout groups to settle after switching entries.
        var ui = UnityEngine.Object.FindFirstObjectByType<OceanLogUI>();
        ScreenCapture.CaptureScreenshot(Path.Combine(Output, ui.gameObject.scene.name + "-ocean-log.png"));
    }

    private static Rect WorldRect(RectTransform transform)
    {
        var corners = new Vector3[4];
        transform.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
    }

    private static void Collect(int count)
    {
        for (int i = 0; i < count; i++) OceanLifeManager.instance.CollectPlankton(PlayerController.instance);
        Set(OceanLifeManager.instance, "_pendingSounds", 0);
    }
    private static List<CompanionFish> Followers(OceanLifeManager manager) => Get<List<CompanionFish>>(manager, "_followers");
    private static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, Fields).GetValue(target);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Fields).SetValue(target, value);
    private static void Invoke(object target, string method, params object[] args) => target.GetType().GetMethod(method, Fields).Invoke(target, args);
    private static void Wait(double seconds) => _until = EditorApplication.timeSinceStartup + seconds;
    private static void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
        Passed.Add(description);
    }
    private static void Finish(string error)
    {
        _ready = false;
        string report = SessionState.GetBool(RunningKey + ".title", false) ? "title-checks.txt" : "main-checks.txt";
        File.WriteAllText(Path.Combine(Output, report), string.Join("\n", Passed.Select(p => "PASS " + p)) +
            (error == null ? "\nALL CHECKS PASSED\n" : "\nFAIL " + error));
        if (error == null) Debug.Log("[SeptemberUpdate] All " + Passed.Count + " integration checks passed.");
        else Debug.LogError("[SeptemberUpdate] " + error);
        Time.timeScale = 1;
        SessionState.SetInt(RunningKey + ".exit", error == null ? 0 : 1);
        EditorApplication.isPlaying = false;
    }

    private static void SnapshotString(string key)
    {
        SessionState.SetBool(RunningKey + key + ".exists", PlayerPrefs.HasKey(key));
        SessionState.SetString(RunningKey + key, PlayerPrefs.GetString(key));
    }
    private static void SnapshotFloat(string key)
    {
        SessionState.SetBool(RunningKey + key + ".exists", PlayerPrefs.HasKey(key));
        SessionState.SetFloat(RunningKey + key, PlayerPrefs.GetFloat(key));
    }
    private static void RestoreString(string key)
    {
        if (SessionState.GetBool(RunningKey + key + ".exists", false)) PlayerPrefs.SetString(key, SessionState.GetString(RunningKey + key, ""));
        else PlayerPrefs.DeleteKey(key);
    }
    private static void RestoreFloat(string key)
    {
        if (SessionState.GetBool(RunningKey + key + ".exists", false)) PlayerPrefs.SetFloat(key, SessionState.GetFloat(RunningKey + key, 0));
        else PlayerPrefs.DeleteKey(key);
    }
}
