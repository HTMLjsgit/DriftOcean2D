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
    private const string Output = "Builds/SeptemberUpdate/QA";
    private static readonly List<string> Passed = new List<string>();
    private static readonly List<GameObject> Obstacles = new List<GameObject>();
    private static int _phase;
    private static double _until;
    private static bool _ready;
    private static CompanionFish _survivor;
    private static Vector3 _before;
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
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
        Directory.CreateDirectory(Output);
        SessionState.SetString(RunningKey + ".scene", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        SnapshotString("PlayerName");
        SnapshotFloat("OfflinePendingBestScore");
        SnapshotFloat("OfflinePendingLeaderboardScore");
        SessionState.SetBool(RunningKey, true);
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Main.unity");
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
        if (scene.name != "Main") return;
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
        UnityEngine.Object.FindFirstObjectByType<AdsManager>().enabled = false;
        Passed.Clear();
        Obstacles.Clear();
        _phase = 0;
        _ready = true;
        _until = EditorApplication.timeSinceStartup + 2;
    }

    private static void Tick()
    {
        if (!_ready || !EditorApplication.isPlaying || !SessionState.GetBool(RunningKey, false) || EditorApplication.timeSinceStartup < _until) return;
        try { Step(); }
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
                var pickup = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<PlanktonPickup>("Assets/Prefabs/OceanLife/Plankton.prefab"));
                pickup.Initialize(life, player.transform.position, 0f, -20f, 0f, Color.white);
                UnityEngine.Object.Destroy(pickup.gameObject, 0.3f);
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
                Wait(0.1);
                break;
            case 8:
                Check(_survivor != null && Vector3.Distance(_before, _survivor.transform.position) < 0.02f, "Fish briefly wait at the result screen");
                Wait(0.8);
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
                var prefab = AssetDatabase.LoadAssetAtPath<PlanktonPickup>("Assets/Prefabs/OceanLife/Plankton.prefab");
                for (int i = 0; i < 8; i++)
                {
                    var p = UnityEngine.Object.Instantiate(prefab);
                    p.Initialize(life, new Vector2(-0.8f + i * 0.4f, 0.4f + Mathf.Sin(i * 0.7f) * 0.12f), 0f, -20, 0f, life.Settings.colors[i].color);
                }
                ScreenCapture.CaptureScreenshot(Path.Combine(Output, "gameplay.png"));
                Wait(0.3);
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
        foreach (string field in new[] { "_scoreText", "_resultCommentText", "_unlockNoticeText" })
            Check(!count.Overlaps(WorldRect(Get<TMPro.TextMeshProUGUI>(manager, field).rectTransform)), "Plankton result does not overlap " + field);
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
        File.WriteAllText(Path.Combine(Output, "integration-checks.txt"), string.Join("\n", Passed.Select(p => "PASS " + p)) +
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
