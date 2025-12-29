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

    private UGSManager _ugsManager;
    private PlayerCloudData _playerData;

    // データロード完了イベント
    public event Action OnDataLoaded;

    // データが読み込まれているかのフラグ
    public bool IsDataLoaded { get; private set; } = false;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    async void Start()
    {
        _ugsManager = UGSManager.instance;

        if (_ugsManager != null)
        {
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
        else
        {
            Debug.LogError("UGSManager not found. UGSCloudSaveManager requires UGSManager.");
        }
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

        if (_ugsManager == null || !_ugsManager.IsSignedIn())
        {
            Debug.LogWarning("[DEBUG] UGSManager not ready. Cannot load player data.");
            return;
        }

        _playerData = await _ugsManager.LoadData<PlayerCloudData>(CLOUD_SAVE_KEY_PLAYER_DATA);

        // データが null の場合は新規プレイヤー
        if (_playerData == null)
        {
            Debug.Log("[DEBUG] No cloud data found. Creating new player data.");
            _playerData = new PlayerCloudData();
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

        IsDataLoaded = true;

        // PlayerNameManagerに名前を同期（常に使用）
        PlayerNameManager.instance.SyncFromUGS(_playerData.playerName);

        OnDataLoaded?.Invoke();
        Debug.Log("[DEBUG] OnDataLoaded event invoked");
    }

    /// <summary>
    /// Cloud Saveからプレイヤーデータを強制的に再ロード（公開メソッド）
    /// </summary>
    public async Task ReloadPlayerData()
    {
        Debug.Log("[DEBUG] ReloadPlayerData called - forcing reload from cloud...");
        await LoadPlayerData();
        Debug.Log("[DEBUG] ReloadPlayerData completed");
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
        string name = _playerData?.playerName ?? "プレイヤー";
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
        return await SavePlayerData();
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
    public async Task<bool> UpdateStats(float runScore, float runTime)
    {
        if (_playerData == null) return false;

        _playerData.stats.totalPlayCount++;
        _playerData.stats.totalPlayTime += runTime;
        _playerData.stats.deathCount++;

        if (runScore > _playerData.stats.bestScore)
        {
            _playerData.stats.bestScore = runScore;
        }

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
}
