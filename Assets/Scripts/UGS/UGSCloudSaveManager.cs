using UnityEngine;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// UGS Cloud Save専用マネージャー
/// プレイヤーデータ（名前、スキン、統計）を管理
/// </summary>
public class UGSCloudSaveManager : MonoBehaviour
{
    public static UGSCloudSaveManager instance;

    private const string CLOUD_SAVE_KEY_PLAYER_DATA = "PlayerData";
    private const string OFFLINE_PENDING_BEST_SCORE_KEY = "OfflinePendingBestScore";
    private const string OFFLINE_PENDING_LEADERBOARD_SCORE_KEY = "OfflinePendingLeaderboardScore";

    private UGSManager _ugsManager;
    private PlayerCloudData _playerData;

#if UNITY_EDITOR
    private PlayerCloudData _editorTestDataSnapshot;
    public bool IsEditorTestSessionActive { get; private set; }
#endif

    // データロード完了イベント
    public event Action OnDataLoaded;

    // データが読み込まれているかのフラグ
    public bool IsDataLoaded { get; private set; } = false;
    public bool WasPlayerDataCreatedThisSession { get; private set; } = false;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // このComponentはUGS Prefabの子なので、ルートごと保持しないと
            // TitleからMainへ移動した際に一時テストデータが破棄される。
            DontDestroyOnLoad(transform.root.gameObject);
        }
        else
        {
            Destroy(transform.root.gameObject);
        }
    }

    async void Start()
    {
        _ugsManager = UGSManager.instance;
        _ugsManager.OnSignInFailed += OnUGSSignInFailed;
        // サインイン成功後にデータをロード
        _ugsManager.OnSignInSuccess += OnUGSSignInSuccess;

        // 既にサインイン済みの場合（2回目以降のシーン読み込み）は即座にリロード
        if (_ugsManager.IsSignedIn())
        {
            Debug.Log("[UGSCloudSaveManager] Already signed in, reloading player data...");
            await System.Threading.Tasks.Task.Delay(100); // 少し待ってから実行
            await LoadPlayerData();
        }
    }

    void Update()
    {
        if (_ugsManager == null)
        {
            return;
        }

        if (!_ugsManager.IsServiceEnabled() && !IsDataLoaded)
        {
            ApplyUnavailablePlayerDataState("[UGSCloudSaveManager] UGS is unavailable. Applying offline simulation state.");
        }
    }

    private void OnUGSSignInFailed()
    {
        ApplyUnavailablePlayerDataState("[UGSCloudSaveManager] UGS sign-in unavailable. Clearing cloud-backed player data.");
    }

    /// <summary>
    /// UGSサインイン成功時のコールバック
    /// </summary>
    private async void OnUGSSignInSuccess()
    {
        // サインイン処理が完全に完了するまで少し待つ
        await Task.Delay(500);

        // データをロード
        await LoadPlayerData();
    }

    /// <summary>
    /// Cloud Saveからプレイヤーデータをロード
    /// </summary>
    private async Task LoadPlayerData()
    {
        Debug.Log("[DEBUG] LoadPlayerData started...");

#if UNITY_EDITOR
        if (IsEditorTestSessionActive)
        {
            Debug.Log("[UGSCloudSaveManager] Editor test session is active. Skipping Cloud Save load.");
            return;
        }
#endif

        if (_ugsManager == null || !_ugsManager.IsSignedIn())
        {
            Debug.LogWarning("[DEBUG] UGSManager not ready. Cannot load player data.");
            return;
        }

        PlayerCloudData loadedPlayerData = await _ugsManager.LoadData<PlayerCloudData>(CLOUD_SAVE_KEY_PLAYER_DATA);

#if UNITY_EDITOR
        // テスト開始前に発行済みだった非同期ロードが後から完了しても、
        // 一時データをCloud Saveの内容で上書きしない。
        if (IsEditorTestSessionActive)
        {
            Debug.Log("[UGSCloudSaveManager] Discarded a Cloud Save load that completed during the Editor test session.");
            return;
        }
#endif

        _playerData = loadedPlayerData;
        WasPlayerDataCreatedThisSession = false;

        // データが null の場合は新規プレイヤー
        if (_playerData == null)
        {
            Debug.Log("[DEBUG] No cloud data found. Creating new player data.");
            _playerData = new PlayerCloudData();
            WasPlayerDataCreatedThisSession = true;
            Debug.Log($"[DEBUG] New PlayerCloudData created with default name: '{_playerData.playerName}'");
            await SavePlayerData(); // 初回保存
        }
        else
        {
            Debug.Log($"[DEBUG] Player data loaded: Name='{_playerData.playerName}', CurrentSkinID={_playerData.currentSkinID}, Unlocked Skins=[{string.Join(", ", _playerData.unlockedSkinIDs)}], BestScore={_playerData.stats.bestScore}");

            // 重複削除処理
            var originalCount = _playerData.unlockedSkinIDs.Count;
            _playerData.unlockedSkinIDs = _playerData.unlockedSkinIDs.Distinct().ToList();

            if (_playerData.unlockedSkinIDs.Count != originalCount)
            {
                Debug.LogWarning($"[DEBUG] Removed {originalCount - _playerData.unlockedSkinIDs.Count} duplicate skin IDs. New list: [{string.Join(", ", _playerData.unlockedSkinIDs)}]");
                await SavePlayerData(); // 修正したデータを保存
            }
        }

        await ApplyPendingOfflineBestScoreIfNeeded();

        IsDataLoaded = true;

        // PlayerNameManagerに名前を同期（常に使用）
        PlayerNameManager.instance.SyncFromUGS(_playerData.playerName);

        OnDataLoaded?.Invoke();
        Debug.Log("[DEBUG] OnDataLoaded event invoked");
    }

    /// <summary>
    /// Cloud Saveからプレイヤーデータを強制的に再ロード（公開メソッド）
    /// </summary>
    private void ApplyUnavailablePlayerDataState(string logMessage)
    {
#if UNITY_EDITOR
        if (IsEditorTestSessionActive)
        {
            Debug.Log("[UGSCloudSaveManager] Editor test session is active. Keeping the in-memory test data despite UGS becoming unavailable.");
            return;
        }
#endif

        Debug.LogWarning(logMessage);

        _playerData = null;
        IsDataLoaded = true;

        OnDataLoaded?.Invoke();
        Debug.Log("[DEBUG] OnDataLoaded event invoked for unavailable/offline state");
    }

    private async Task ApplyPendingOfflineBestScoreIfNeeded()
    {
        if (_playerData == null)
        {
            return;
        }

        float pendingOfflineBest = GetPendingOfflineBestScore();
        if (pendingOfflineBest > 0f)
        {
            if (pendingOfflineBest > _playerData.stats.bestScore)
            {
                _playerData.stats.bestScore = pendingOfflineBest;
                Debug.Log($"[UGSCloudSaveManager] Applying pending offline best score: {pendingOfflineBest:F2}");

                bool saved = await SavePlayerData();
                if (!saved)
                {
                    return;
                }

                SetPendingOfflineLeaderboardScore(pendingOfflineBest);
            }

            ClearPendingOfflineBestScore();
        }

        await SubmitPendingOfflineLeaderboardScoreIfNeeded();
    }

    public async Task ReloadPlayerData()
    {
#if UNITY_EDITOR
        if (IsEditorTestSessionActive)
        {
            Debug.Log("[UGSCloudSaveManager] Editor test session is active. Keeping the in-memory test data instead of reloading Cloud Save.");
            return;
        }
#endif

        Debug.Log("[DEBUG] ReloadPlayerData called - forcing reload from cloud...");

        if (_ugsManager == null || !_ugsManager.IsSignedIn())
        {
            ApplyUnavailablePlayerDataState("[UGSCloudSaveManager] Reload skipped because UGS is unavailable.");
            return;
        }

        await LoadPlayerData();
        Debug.Log("[DEBUG] ReloadPlayerData completed");
    }

    public void RecordOfflineBestScore(float score)
    {
        float pendingBest = GetPendingOfflineBestScore();
        if (score <= pendingBest)
        {
            return;
        }

        PlayerPrefs.SetFloat(OFFLINE_PENDING_BEST_SCORE_KEY, score);
        PlayerPrefs.Save();
        Debug.Log($"[UGSCloudSaveManager] Recorded offline pending best score: {score:F2}");
    }

    public float GetPendingOfflineBestScore()
    {
        return PlayerPrefs.GetFloat(OFFLINE_PENDING_BEST_SCORE_KEY, 0f);
    }

    public float GetDisplayBestScore()
    {
        float cloudBest = _playerData?.stats.bestScore ?? 0f;
        return Mathf.Max(cloudBest, GetPendingOfflineBestScore());
    }

    public bool IsOfflineModeActive()
    {
        return IsDataLoaded && (_ugsManager == null || !_ugsManager.IsSignedIn());
    }

    private void ClearPendingOfflineBestScore()
    {
        PlayerPrefs.DeleteKey(OFFLINE_PENDING_BEST_SCORE_KEY);
        PlayerPrefs.Save();
    }

    private void SetPendingOfflineLeaderboardScore(float score)
    {
        float pendingScore = GetPendingOfflineLeaderboardScore();
        if (score <= pendingScore)
        {
            return;
        }

        PlayerPrefs.SetFloat(OFFLINE_PENDING_LEADERBOARD_SCORE_KEY, score);
        PlayerPrefs.Save();
        Debug.Log($"[UGSCloudSaveManager] Recorded offline pending leaderboard score: {score:F2}");
    }

    private float GetPendingOfflineLeaderboardScore()
    {
        return PlayerPrefs.GetFloat(OFFLINE_PENDING_LEADERBOARD_SCORE_KEY, 0f);
    }

    private async Task SubmitPendingOfflineLeaderboardScoreIfNeeded()
    {
        float pendingScore = GetPendingOfflineLeaderboardScore();
        if (pendingScore <= 0f)
        {
            return;
        }

        UGSLeaderboardManager leaderboardManager = UGSLeaderboardManager.instance;
        if (leaderboardManager == null)
        {
            Debug.LogWarning("[UGSCloudSaveManager] Leaderboard manager is missing. Keeping pending offline leaderboard score.");
            return;
        }

        bool submitted = await leaderboardManager.SubmitScore(pendingScore);
        if (!submitted)
        {
            Debug.LogWarning($"[UGSCloudSaveManager] Failed to submit pending offline leaderboard score: {pendingScore:F2}");
            return;
        }

        ClearPendingOfflineLeaderboardScore();
    }

    private void ClearPendingOfflineLeaderboardScore()
    {
        PlayerPrefs.DeleteKey(OFFLINE_PENDING_LEADERBOARD_SCORE_KEY);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Cloud Saveにプレイヤーデータを保存
    /// </summary>
    public async Task<bool> SavePlayerData()
    {
        if (_playerData == null)
        {
            Debug.LogWarning("Player data is null. Cannot save.");
            return false;
        }

        // 保存前に重複を削除（データクリーンアップ）
        _playerData.unlockedSkinIDs = _playerData.unlockedSkinIDs.Distinct().ToList();
        _playerData.notifiedSkinIDs = _playerData.notifiedSkinIDs.Distinct().ToList();
        _playerData.seenSkinIDs = _playerData.seenSkinIDs.Distinct().ToList();
        _playerData.viewedSkinInventoryUnlockedSkins = _playerData.viewedSkinInventoryUnlockedSkins.Distinct().ToList();
        _playerData.discoveredObstacleIDs = _playerData.discoveredObstacleIDs.Distinct().ToList();
        _playerData.viewedObstacleIDs = _playerData.viewedObstacleIDs.Distinct().ToList();
        _playerData.unlockedAchievementIDs = _playerData.unlockedAchievementIDs.Distinct().ToList();

#if UNITY_EDITOR
        if (IsEditorTestSessionActive)
        {
            Debug.Log("[UGSCloudSaveManager] Editor test session is active. Skipping Cloud Save write.");
            return true;
        }
#endif

        if (_ugsManager != null && _ugsManager.IsSignedIn())
        {
            Debug.Log("Saving player data to Cloud Save...");
            bool success = await _ugsManager.SaveData(CLOUD_SAVE_KEY_PLAYER_DATA, _playerData);

            if (success)
            {
                Debug.Log("Player data saved successfully!");
            }

            return success;
        }
        else
        {
            Debug.LogError("UGS not available. Cannot save player data.");
            return false;
        }
    }

    #region Player Name

    /// <summary>
    /// プレイヤー名を取得
    /// </summary>
    public string GetPlayerName()
    {
        string name = _playerData?.playerName ?? "player";
        Debug.Log($"[DEBUG] UGSCloudSaveManager.GetPlayerName() returning: '{name}', _playerData is null: {_playerData == null}");
        return name;
    }

    /// <summary>
    /// プレイヤー名を設定して保存
    /// </summary>
    public async Task<bool> SetPlayerName(string newName)
    {
        if (_playerData == null) return false;

        _playerData.playerName = newName;
        bool saved = await SavePlayerData();
        if (saved)
        {
            WasPlayerDataCreatedThisSession = false;
        }

        return saved;
    }

    #endregion

    #region Skin Management

    /// <summary>
    /// スキンが解放されているか
    /// </summary>
    public bool IsSkinUnlocked(int skinID)
    {
        bool result = _playerData?.unlockedSkinIDs.Contains(skinID) ?? false;
        Debug.Log($"[DEBUG] UGSCloudSaveManager.IsSkinUnlocked({skinID}): {result}, _playerData is null: {_playerData == null}, unlockedSkinIDs: [{string.Join(", ", _playerData?.unlockedSkinIDs ?? new List<int>())}]");
        return result;
    }

    /// <summary>
    /// スキンを解放して保存
    /// </summary>
    public async Task<bool> UnlockSkin(int skinID)
    {
        if (_playerData == null)
        {
            Debug.LogError($"[DEBUG] UnlockSkin({skinID}) failed: _playerData is null");
            return false;
        }

        // 既に解放済みかチェック
        if (_playerData.unlockedSkinIDs.Contains(skinID))
        {
            Debug.Log($"[DEBUG] Skin {skinID} is already unlocked. Skipping.");
            return true; // 既に解放済み
        }

        // 解放処理
        _playerData.unlockedSkinIDs.Add(skinID);
        Debug.Log($"[DEBUG] Skin {skinID} unlocked! Unlocked skins: [{string.Join(", ", _playerData.unlockedSkinIDs)}]");

        bool saved = await SavePlayerData();
        Debug.Log($"[DEBUG] UnlockSkin({skinID}) save result: {saved}");
        return saved;
    }

    /// <summary>
    /// 現在装備中のスキンIDを取得
    /// </summary>
    public int GetCurrentSkinID()
    {
        int skinID = _playerData?.currentSkinID ?? 1; // デフォルトは初期スキンID 1
        Debug.Log($"[DEBUG] UGSCloudSaveManager.GetCurrentSkinID() returning: {skinID}, _playerData.currentSkinID: {_playerData?.currentSkinID}");
        return skinID;
    }

    /// <summary>
    /// スキンを装備して保存
    /// </summary>
    public async Task<bool> EquipSkin(int skinID)
    {
        if (_playerData == null)
        {
            Debug.LogError("[DEBUG] EquipSkin failed: _playerData is null");
            return false;
        }

        Debug.Log($"[DEBUG] EquipSkin called: skinID={skinID}, unlocked skins: [{string.Join(", ", _playerData.unlockedSkinIDs)}]");

        // 解放済みスキンのみ装備可能
        if (_playerData.unlockedSkinIDs.Contains(skinID))
        {
            Debug.Log($"[DEBUG] Before equip: _playerData.currentSkinID={_playerData.currentSkinID}");
            _playerData.currentSkinID = skinID;
            Debug.Log($"[DEBUG] After equip: _playerData.currentSkinID={_playerData.currentSkinID}");
            Debug.Log($"Skin {skinID} equipped!");

            bool saved = await SavePlayerData();
            Debug.Log($"[DEBUG] SavePlayerData result: {saved}");
            return saved;
        }
        else
        {
            Debug.LogWarning($"Skin {skinID} is not unlocked. Cannot equip. Unlocked skins: [{string.Join(", ", _playerData.unlockedSkinIDs)}]");
            return false;
        }
    }

    /// <summary>
    /// 解放済みスキンのリストを取得
    /// </summary>
    public List<int> GetUnlockedSkinIDs()
    {
        return _playerData?.unlockedSkinIDs ?? new List<int> { 1 }; // デフォルトは初期スキンID 1
    }

    #endregion

    #region Stats Management

    /// <summary>
    /// 統計データを取得
    /// </summary>
    public PlayerStats GetStats()
    {
        return _playerData?.stats ?? new PlayerStats();
    }

    /// <summary>
    /// 統計データを更新して保存
    /// </summary>
    public async Task<bool> UpdateStats(
        float runScore,
        float runTime,
        int bounceCount = 0,
        bool reachedHardMode = false)
    {
        if (_playerData == null) return false;

        _playerData.stats.totalPlayCount++;
        _playerData.stats.totalPlayTime += runTime;
        _playerData.stats.deathCount++;
        _playerData.stats.totalBounceCount += Mathf.Max(0, bounceCount);
        _playerData.stats.hasReachedHardMode |= reachedHardMode;

        if (runScore > _playerData.stats.bestScore)
        {
            _playerData.stats.bestScore = runScore;
        }

        return await SavePlayerData();
    }

    public async Task<bool> RegisterPlayDay()
    {
        if (_playerData == null) return false;

        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        if (_playerData.stats.lastPlayDateUtc == today)
        {
            return true;
        }

        if (DateTime.TryParse(_playerData.stats.lastPlayDateUtc, out DateTime lastPlayDate) &&
            lastPlayDate.Date == DateTime.UtcNow.Date.AddDays(-1))
        {
            _playerData.stats.consecutivePlayDays++;
        }
        else
        {
            _playerData.stats.consecutivePlayDays = 1;
        }

        _playerData.stats.lastPlayDateUtc = today;
        return await SavePlayerData();
    }

    /// <summary>
    /// 連続生存カウンターを更新（イルカ用：連続で1分以上生存）
    /// </summary>
    /// <param name="survived">今回1分以上生存したか</param>
    public async Task<bool> UpdateConsecutiveSurvival(bool survived)
    {
        if (_playerData == null) return false;

        if (survived)
        {
            _playerData.stats.consecutiveSurvivalCount++;
            Debug.Log($"Consecutive survival count: {_playerData.stats.consecutiveSurvivalCount}");
        }
        else
        {
            _playerData.stats.consecutiveSurvivalCount = 0;
            Debug.Log("Consecutive survival count reset to 0");
        }

        return await SavePlayerData();
    }

    /// <summary>
    /// 連続生存カウントを取得
    /// </summary>
    public int GetConsecutiveSurvivalCount()
    {
        return _playerData?.stats.consecutiveSurvivalCount ?? 0;
    }

    /// <summary>
    /// SNSシェアフラグを設定（アカウミガメ用）
    /// </summary>
    public async Task<bool> SetSNSShared()
    {
        if (_playerData == null) return false;

        _playerData.stats.hasSNSShared = true;
        Debug.Log("SNS share flag set to true");
        return await SavePlayerData();
    }

    /// <summary>
    /// SNSシェア済みか確認
    /// </summary>
    public bool HasSNSShared()
    {
        return _playerData?.stats.hasSNSShared ?? false;
    }

    /// <summary>
    /// 無操作解放フラグを設定（海綿体用）
    /// </summary>
    public async Task<bool> SetNoInputUnlocked()
    {
        if (_playerData == null) return false;

        _playerData.stats.hasNoInputUnlocked = true;
        Debug.Log("No input unlock flag set to true");
        return await SavePlayerData();
    }

    /// <summary>
    /// 無操作解放済みか確認
    /// </summary>
    public bool HasNoInputUnlocked()
    {
        return _playerData?.stats.hasNoInputUnlocked ?? false;
    }

    /// <summary>
    /// ノーコンティニューハードモード到達フラグを設定（カニ用）
    /// </summary>
    public async Task<bool> SetNoContinueHardModeUnlocked()
    {
        if (_playerData == null) return false;

        _playerData.stats.hasNoContinueHardModeUnlocked = true;
        Debug.Log("No continue hard mode unlock flag set to true");
        return await SavePlayerData();
    }

    /// <summary>
    /// ノーコンティニューハードモード到達済みか確認
    /// </summary>
    public bool HasNoContinueHardModeUnlocked()
    {
        return _playerData?.stats.hasNoContinueHardModeUnlocked ?? false;
    }

    #endregion

    #region Ocean Log

    public bool IsObstacleDiscovered(int obstacleID)
    {
        return _playerData?.discoveredObstacleIDs.Contains(obstacleID) ?? false;
    }

    public List<int> GetDiscoveredObstacleIDs()
    {
        return _playerData?.discoveredObstacleIDs.ToList() ?? new List<int>();
    }

    public async Task<bool> DiscoverObstacle(int obstacleID)
    {
        if (_playerData == null || obstacleID <= 0) return false;

        if (_playerData.discoveredObstacleIDs.Contains(obstacleID))
        {
            return true;
        }

        _playerData.discoveredObstacleIDs.Add(obstacleID);
        return await SavePlayerData();
    }

    public bool HasNewObstacleEntries()
    {
        if (_playerData == null) return false;

        return _playerData.discoveredObstacleIDs.Any(id => !_playerData.viewedObstacleIDs.Contains(id));
    }

    public bool IsObstacleEntryNew(int obstacleID)
    {
        if (_playerData == null) return false;

        return _playerData.discoveredObstacleIDs.Contains(obstacleID) &&
               !_playerData.viewedObstacleIDs.Contains(obstacleID);
    }

    public async Task<bool> MarkOceanLogAsViewed()
    {
        if (_playerData == null) return false;

        foreach (int obstacleID in _playerData.discoveredObstacleIDs)
        {
            if (!_playerData.viewedObstacleIDs.Contains(obstacleID))
            {
                _playerData.viewedObstacleIDs.Add(obstacleID);
            }
        }

        return await SavePlayerData();
    }

    public bool IsAchievementUnlocked(int achievementID)
    {
        return _playerData?.unlockedAchievementIDs.Contains(achievementID) ?? false;
    }

    public List<int> GetUnlockedAchievementIDs()
    {
        return _playerData?.unlockedAchievementIDs.ToList() ?? new List<int>();
    }

    public async Task<bool> UnlockAchievements(IEnumerable<int> achievementIDs)
    {
        if (_playerData == null || achievementIDs == null) return false;

        bool changed = false;
        foreach (int achievementID in achievementIDs)
        {
            if (!_playerData.unlockedAchievementIDs.Contains(achievementID))
            {
                _playerData.unlockedAchievementIDs.Add(achievementID);
                changed = true;
            }
        }

        return !changed || await SavePlayerData();
    }

#if UNITY_EDITOR
    public bool EditorUnlockAchievements(IEnumerable<int> achievementIDs)
    {
        if (!EditorBeginTestSession() || achievementIDs == null) return false;

        foreach (int achievementID in achievementIDs.Where(id => id > 0).Distinct())
        {
            if (!_playerData.unlockedAchievementIDs.Contains(achievementID))
            {
                _playerData.unlockedAchievementIDs.Add(achievementID);
            }
        }

        return true;
    }

    public bool EditorUnlockSkins(IEnumerable<int> skinIDs)
    {
        if (!EditorBeginTestSession() || skinIDs == null) return false;

        foreach (int skinID in skinIDs.Where(id => id > 0).Distinct())
        {
            if (!_playerData.unlockedSkinIDs.Contains(skinID))
            {
                _playerData.unlockedSkinIDs.Add(skinID);
            }
        }

        return true;
    }

    public bool EditorUnlockAndEquipSkin(int skinID)
    {
        if (!EditorBeginTestSession() || skinID <= 0) return false;

        if (!_playerData.unlockedSkinIDs.Contains(skinID))
        {
            _playerData.unlockedSkinIDs.Add(skinID);
        }

        _playerData.currentSkinID = skinID;
        if (!_playerData.seenSkinIDs.Contains(skinID))
        {
            _playerData.seenSkinIDs.Add(skinID);
        }

        EditorSyncRuntimeSkin();
        return true;
    }

    public bool EditorResetSkins(IEnumerable<int> skinIDs)
    {
        if (!EditorBeginTestSession() || skinIDs == null) return false;

        HashSet<int> resetIDs = skinIDs
            .Where(id => id > 1)
            .ToHashSet();

        _playerData.unlockedSkinIDs.RemoveAll(resetIDs.Contains);
        _playerData.notifiedSkinIDs.RemoveAll(resetIDs.Contains);
        _playerData.seenSkinIDs.RemoveAll(resetIDs.Contains);
        _playerData.viewedSkinInventoryUnlockedSkins.RemoveAll(resetIDs.Contains);

        if (resetIDs.Contains(_playerData.currentSkinID))
        {
            _playerData.currentSkinID = 1;
        }

        EditorEnsureInitialSkinState();
        EditorSyncRuntimeSkin();
        return true;
    }

    public bool EditorResetAllSkins()
    {
        if (!EditorBeginTestSession()) return false;

        _playerData.unlockedSkinIDs = new List<int> { 1 };
        _playerData.notifiedSkinIDs = new List<int> { 1 };
        _playerData.seenSkinIDs = new List<int> { 1 };
        _playerData.viewedSkinInventoryUnlockedSkins = new List<int> { 1 };
        _playerData.currentSkinID = 1;

        EditorSyncRuntimeSkin();
        return true;
    }

    public bool EditorDiscoverObstacles(IEnumerable<int> obstacleIDs)
    {
        if (!EditorBeginTestSession() || obstacleIDs == null) return false;

        foreach (int obstacleID in obstacleIDs.Where(id => id > 0).Distinct())
        {
            if (_playerData.discoveredObstacleIDs.Contains(obstacleID))
            {
                continue;
            }

            _playerData.discoveredObstacleIDs.Add(obstacleID);
        }

        return true;
    }

    public bool EditorResetAchievements()
    {
        if (!EditorBeginTestSession()) return false;

        _playerData.unlockedAchievementIDs.Clear();
        return true;
    }

    public bool EditorResetOceanLog()
    {
        if (!EditorBeginTestSession()) return false;

        _playerData.discoveredObstacleIDs.Clear();
        _playerData.viewedObstacleIDs.Clear();
        return true;
    }

    public bool EditorRestoreTestSession()
    {
        if (!IsEditorTestSessionActive || _editorTestDataSnapshot == null)
        {
            return false;
        }

        _playerData = ClonePlayerData(_editorTestDataSnapshot);
        _editorTestDataSnapshot = null;
        IsEditorTestSessionActive = false;
        EditorSyncRuntimeSkin();
        Debug.Log("[UGSCloudSaveManager] Restored the data captured before the Editor test session.");
        return true;
    }

    private bool EditorBeginTestSession()
    {
        if (_playerData == null)
        {
            return false;
        }

        if (IsEditorTestSessionActive)
        {
            return true;
        }

        _editorTestDataSnapshot = ClonePlayerData(_playerData);
        if (_editorTestDataSnapshot == null)
        {
            return false;
        }

        IsEditorTestSessionActive = true;
        Debug.Log("[UGSCloudSaveManager] Started an in-memory Editor test session. Cloud Save writes are disabled until Play Mode ends.");
        return true;
    }

    private void EditorEnsureInitialSkinState()
    {
        if (!_playerData.unlockedSkinIDs.Contains(1))
        {
            _playerData.unlockedSkinIDs.Add(1);
        }

        if (!_playerData.notifiedSkinIDs.Contains(1))
        {
            _playerData.notifiedSkinIDs.Add(1);
        }

        if (!_playerData.seenSkinIDs.Contains(1))
        {
            _playerData.seenSkinIDs.Add(1);
        }

        if (!_playerData.viewedSkinInventoryUnlockedSkins.Contains(1))
        {
            _playerData.viewedSkinInventoryUnlockedSkins.Add(1);
        }
    }

    private void EditorSyncRuntimeSkin()
    {
        if (SkinManager.instance != null)
        {
            SkinManager.instance.ApplyCurrentSkinID(_playerData.currentSkinID);
        }
    }

    private static PlayerCloudData ClonePlayerData(PlayerCloudData source)
    {
        return source == null
            ? null
            : JsonUtility.FromJson<PlayerCloudData>(JsonUtility.ToJson(source));
    }
#endif

    #endregion

    #region Skin Notification (New Label & Unlock Notice)

    /// <summary>
    /// 未通知（タイトル画面でお知らせ表示していない）の解放済みスキンIDリストを取得
    /// </summary>
    public List<int> GetUnnotifiedSkinIDs()
    {
        if (_playerData == null) return new List<int>();

        // 解放済みだがお知らせ済みリストにないスキンを返す
        return _playerData.unlockedSkinIDs
            .Where(id => !_playerData.notifiedSkinIDs.Contains(id))
            .ToList();
    }

    /// <summary>
    /// スキンをお知らせ済みとしてマーク
    /// </summary>
    public async Task<bool> MarkSkinsAsNotified(List<int> skinIDs)
    {
        if (_playerData == null || skinIDs == null || skinIDs.Count == 0) return false;

        foreach (var id in skinIDs)
        {
            if (!_playerData.notifiedSkinIDs.Contains(id))
            {
                _playerData.notifiedSkinIDs.Add(id);
            }
        }

        Debug.Log($"Marked skins as notified: [{string.Join(", ", skinIDs)}]");
        return await SavePlayerData();
    }

    /// <summary>
    /// スキンが新規（未装備/未閲覧）かチェック（Newラベル用）
    /// </summary>
    public bool IsSkinNew(int skinID)
    {
        if (_playerData == null) return false;

        // 解放済みかつ見ていない（装備していない）スキンは新規
        return _playerData.unlockedSkinIDs.Contains(skinID) &&
               !_playerData.seenSkinIDs.Contains(skinID);
    }

    /// <summary>
    /// スキンを見た（装備した）としてマーク（Newラベルを消す）
    /// </summary>
    public async Task<bool> MarkSkinAsSeen(int skinID)
    {
        if (_playerData == null) return false;

        if (!_playerData.seenSkinIDs.Contains(skinID))
        {
            _playerData.seenSkinIDs.Add(skinID);
            Debug.Log($"Skin {skinID} marked as seen (New label removed)");
            return await SavePlayerData();
        }

        return true;
    }

    /// <summary>
    /// スキン一覧を見たとしてマーク（SkinボタンのNewラベル用）
    /// 現在の解放済みスキンをすべて記録
    /// </summary>
    public async Task<bool> MarkSkinInventoryAsViewed()
    {
        if (_playerData == null) return false;

        // 現在解放済みのスキンをすべて記録
        foreach (var skinID in _playerData.unlockedSkinIDs)
        {
            if (!_playerData.viewedSkinInventoryUnlockedSkins.Contains(skinID))
            {
                _playerData.viewedSkinInventoryUnlockedSkins.Add(skinID);
            }
        }

        Debug.Log($"Skin inventory viewed. Marked skins: [{string.Join(", ", _playerData.viewedSkinInventoryUnlockedSkins)}]");
        return await SavePlayerData();
    }

    /// <summary>
    /// SkinボタンにNewを表示すべきか（スキン一覧で未確認の新しいスキンがあるか）
    /// </summary>
    public bool HasNewSkinsInInventory()
    {
        if (_playerData == null) return false;

        // 解放済みだがスキン一覧で確認していないスキンがあるか
        foreach (var skinID in _playerData.unlockedSkinIDs)
        {
            if (!_playerData.viewedSkinInventoryUnlockedSkins.Contains(skinID))
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Tap Unlock (ぷにぷにタップ回数でアンロック)

    /// <summary>
    /// グローバルタップ回数を取得
    /// </summary>
    public int GetGlobalTapCount()
    {
        if (_playerData == null) return 0;
        return _playerData.globalTapCount;
    }

    /// <summary>
    /// グローバルタップ回数を増やす
    /// </summary>
    public async Task<int> IncrementGlobalTapCount()
    {
        if (_playerData == null) return 0;

        int currentCount = _playerData.globalTapCount;
        _playerData.globalTapCount++;

        Debug.Log($"[UGSCloudSaveManager] Global tap count: {currentCount} -> {_playerData.globalTapCount}");

        await SavePlayerData();

        return _playerData.globalTapCount;
    }

    /// <summary>
    /// グローバルタップ回数をリセット
    /// </summary>
    public async Task<bool> ResetGlobalTapCount()
    {
        if (_playerData == null) return false;

        _playerData.globalTapCount = 0;

        Debug.Log($"[UGSCloudSaveManager] Global tap count reset");

        return await SavePlayerData();
    }

    #endregion

}
