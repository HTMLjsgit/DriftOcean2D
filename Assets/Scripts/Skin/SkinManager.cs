using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
public class SkinManager : MonoBehaviour
{
    public static SkinManager instance;
    private SkinDatabase _skinDatabase;
    private AdsManager _adsManager;
    private UGSCloudSaveManager _cloudSaveManager;

    [Header("Current Status")]
    // 現在装備中のスキンのID
    public int currentSkinID;
    bool anyNewUnlock = false;

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

    void Start()
    {
        _skinDatabase = SkinDatabase.instance;
        _adsManager = AdsManager.instance;
        _cloudSaveManager = UGSCloudSaveManager.instance;

        if (_cloudSaveManager != null)
        {
            _cloudSaveManager.OnDataLoaded += LoadFromUGS;

            // データが既に読み込まれている場合は即座にロード
            if (_cloudSaveManager.IsDataLoaded)
            {
                LoadFromUGS();
                Debug.Log("[DEBUG] SkinManager: Data already loaded, LoadFromUGS called immediately");
            }
        }
        else
        {
            Debug.LogError("UGSCloudSaveManager not found. SkinManager requires UGSCloudSaveManager.");
        }
    }

    /// <summary>
    /// UGS Cloud Saveからデータをロード
    /// </summary>
    private void LoadFromUGS()
    {
        // 常に最新のインスタンスを取得（シーン遷移時の参照切れを防ぐ）
        _cloudSaveManager = UGSCloudSaveManager.instance;

        if (_cloudSaveManager == null)
        {
            Debug.LogError("Cannot load from UGS: UGSCloudSaveManager is not available");
            return;
        }

        currentSkinID = _cloudSaveManager.GetCurrentSkinID();
        Debug.Log($"SkinManager loaded from UGS: currentSkinID={currentSkinID}");
    }

    /// <summary>
    /// ゲームオーバー時に呼ばれる：統計を更新し、スキンの解放チェックを行う
    /// </summary>
    /// <param name="runScore">今回のスコア</param>
    /// <param name="runTime">今回の生存時間(秒)</param>
    /// <param name="noInputAchieved">無操作条件達成フラグ</param>
    /// <param name="usedContinue">コンティニュー使用フラグ</param>
    public async Task ReportGameResult(float runScore, float runTime, bool noInputAchieved = false, bool usedContinue = false)
    {
        _skinDatabase = SkinDatabase.instance;
        _cloudSaveManager = UGSCloudSaveManager.instance;

        PlayerStats stats = _cloudSaveManager.GetStats();

        // 連続生存条件の更新（イルカ用：1分以上生存したか）
        float survivalThreshold = 60f; // 1分
        bool survivedLongEnough = runTime >= survivalThreshold;
        await _cloudSaveManager.UpdateConsecutiveSurvival(survivedLongEnough);
        int consecutiveSurvivalCount = _cloudSaveManager.GetConsecutiveSurvivalCount();

        // 無操作解放フラグの更新（海綿体用）
        if (noInputAchieved && !_cloudSaveManager.HasNoInputUnlocked())
        {
            await _cloudSaveManager.SetNoInputUnlocked();
        }

        // ノーコンティニューでハードモード到達の判定（カニ用）
        // ハードモード = 6分（360秒）以上生存
        float hardModeThreshold = 360f;
        if (!usedContinue && runTime >= hardModeThreshold && !_cloudSaveManager.HasNoContinueHardModeUnlocked())
        {
            await _cloudSaveManager.SetNoContinueHardModeUnlocked();
        }

        // 解放条件のチェック
        await CheckUnlockConditions(
            runScore, runTime,
            stats.totalPlayCount, stats.totalPlayTime, stats.deathCount, stats.bestScore,
            consecutiveSurvivalCount, noInputAchieved, usedContinue
        );
    }

    /// <summary>
    /// 全スキンをチェックして解放処理を行う
    /// </summary>
    private async Task CheckUnlockConditions(
        float runScore, float runTime,
        int playCount, float totalTime, int deathCount, float bestScore,
        int consecutiveSurvivalCount, bool noInputAchieved, bool usedContinue)
    {
        if (_skinDatabase == null)
        {
            return;
        }

        int unlockedCount = 0;

        foreach (var skin in _skinDatabase.GetAllSkins())
        {
            if (IsUnlocked(skin.id))
            {
                unlockedCount++;
                continue;
            }

            bool unlock = false;

            switch (skin.unlockType)
            {
                case SkinData.UnlockType.None:
                    unlock = true;
                    break;
                case SkinData.UnlockType.ScoreReach:
                    if (bestScore >= skin.conditionValue) unlock = true;
                    break;
                case SkinData.UnlockType.PlayCount:
                    if (playCount >= (int)skin.conditionValue) unlock = true;
                    break;
                case SkinData.UnlockType.SurvivalTime:
                    if (runTime >= skin.conditionValue) unlock = true;
                    break;
                case SkinData.UnlockType.TotalPlayTime:
                    if (totalTime >= skin.conditionValue) unlock = true;
                    break;
                case SkinData.UnlockType.DeathCount:
                    if (deathCount >= (int)skin.conditionValue) unlock = true;
                    break;

                // 新しい解放条件
                case SkinData.UnlockType.NoInput:
                    // 無操作条件（海綿体用）: 30秒操作なしで解放
                    if (_cloudSaveManager.HasNoInputUnlocked()) unlock = true;
                    break;
                case SkinData.UnlockType.SNSShare:
                    // SNSシェア条件（アカウミガメ用）: SNSでシェアすると解放
                    if (_cloudSaveManager.HasSNSShared()) unlock = true;
                    break;
                case SkinData.UnlockType.ConsecutiveSurvival:
                    // 連続生存条件（イルカ用）: conditionValue回連続で1分以上生存
                    if (consecutiveSurvivalCount >= (int)skin.conditionValue) unlock = true;
                    break;
                case SkinData.UnlockType.NoContinueHardMode:
                    // ノーコンティニューでハードモード到達（カニ用）
                    if (_cloudSaveManager.HasNoContinueHardModeUnlocked()) unlock = true;
                    break;
            }

            if (unlock)
            {
                await UnlockSkin(skin.id);
                unlockedCount++;
                anyNewUnlock = true;
                Debug.Log($"Skin Unlocked! : {skin.skinName}");
            }
        }

        // コンプリート条件（ゴールドクラゲ）のチェック
        SkinData goldSkin = _skinDatabase.GetAllSkins().FirstOrDefault(s => s.unlockType == SkinData.UnlockType.CompleteAll);
        if (goldSkin != null && !IsUnlocked(goldSkin.id))
        {
            if (unlockedCount >= _skinDatabase.GetAllSkins().Count - 1)
            {
                await UnlockSkin(goldSkin.id);
                Debug.Log("ALL COMPLETE! Gold Skin Unlocked!");
            }
        }
    }

    /// <summary>
    /// SNSシェアによるスキン解放（アカウミガメ用）
    /// 外部から呼び出される
    /// </summary>
    public async Task UnlockBySNSShare()
    {
        _cloudSaveManager = UGSCloudSaveManager.instance;

        if (_cloudSaveManager == null)
        {
            Debug.LogError("Cannot unlock by SNS share: UGSCloudSaveManager is not available");
            return;
        }

        // SNSシェアフラグを設定
        await _cloudSaveManager.SetSNSShared();

        // SNSShare条件のスキンを探して解放
        _skinDatabase = SkinDatabase.instance;
        var snsSkin = _skinDatabase.GetAllSkins().FirstOrDefault(s => s.unlockType == SkinData.UnlockType.SNSShare);
        if (snsSkin != null && !IsUnlocked(snsSkin.id))
        {
            await UnlockSkin(snsSkin.id);
            Debug.Log($"Skin unlocked by SNS share: {snsSkin.skinName}");
        }
    }

    /// <summary>
    /// スキンを解放状態にする
    /// </summary>
    public async Task UnlockSkin(int id)
    {
        // 常に最新のインスタンスを取得（シーン遷移時の参照切れを防ぐ）
        _cloudSaveManager = UGSCloudSaveManager.instance;

        if (_cloudSaveManager == null)
        {
            Debug.LogError("Cannot unlock skin: UGSCloudSaveManager is not available");
            return;
        }

        await _cloudSaveManager.UnlockSkin(id);
    }

    /// <summary>
    /// スキンが解放されているか確認
    /// </summary>
    public bool IsUnlocked(int id)
    {
        // 常に最新のインスタンスを取得（シーン遷移時の参照切れを防ぐ）
        _cloudSaveManager = UGSCloudSaveManager.instance;

        if (_cloudSaveManager == null)
        {
            Debug.LogError("Cannot check unlock status: UGSCloudSaveManager is not available");
            return false;
        }

        bool unlocked = _cloudSaveManager.IsSkinUnlocked(id);
        Debug.Log($"[DEBUG] SkinManager.IsUnlocked({id}): {unlocked} (from UGS)");
        return unlocked;
    }

    /// <summary>
    /// スキンを装備する
    /// </summary>
    public async Task EquipSkin(int id)
    {
        // 常に最新のインスタンスを取得（シーン遷移時の参照切れを防ぐ）
        _cloudSaveManager = UGSCloudSaveManager.instance;

        Debug.Log($"EquipSkin called: id={id}, IsUnlocked={IsUnlocked(id)}");

        if (_cloudSaveManager == null)
        {
            Debug.LogError("Cannot equip skin: UGSCloudSaveManager is not available");
            return;
        }

        if (IsUnlocked(id))
        {
            currentSkinID = id;
            await _cloudSaveManager.EquipSkin(id);
            Debug.Log($"Skin {id} equipped and saved to UGS Cloud Save");
        }
        else
        {
            Debug.LogWarning($"Skin ID {id} is locked!");
        }
    }

    /// <summary>
    /// 現在装備中のスキンのSpriteを取得（Playerが表示するときに使う）
    /// </summary>
    public Sprite GetCurrentSkinSprite()
    {
        var skin = _skinDatabase.GetAllSkins().FirstOrDefault(s => s.id == currentSkinID);
        return skin != null ? skin.skinSprite : null;
    }

    /// <summary>
    /// 広告視聴でスキンを解放する（AdWatchタイプのスキン用）
    /// </summary>
    /// <param name="skinID">解放するスキンのID</param>
    /// <param name="onSuccess">解放成功時のコールバック</param>
    /// <param name="onFailed">解放失敗時のコールバック</param>
    public async void UnlockSkinByAd(int skinID, System.Action onSuccess = null, System.Action onFailed = null)
    {
        // スキンが存在するかチェック
        var skin = _skinDatabase.GetAllSkins().FirstOrDefault(s => s.id == skinID);
        if (skin == null)
        {
            Debug.LogError($"Skin ID {skinID} not found!");
            onFailed?.Invoke();
            return;
        }

        // すでに解放済みかチェック
        if (IsUnlocked(skinID))
        {
            Debug.LogWarning($"Skin ID {skinID} is already unlocked!");
            onSuccess?.Invoke();
            return;
        }

        // 広告がない場合の処理
        if (_adsManager == null)
        {
            Debug.LogWarning("AdsManager not found. Unlocking skin without ad (test mode).");
            await UnlockSkin(skinID);
            onSuccess?.Invoke();
            return;
        }

        // 広告を表示
        _adsManager.ShowRewardedAd(
            onSuccess: async () =>
            {
                // 広告視聴成功 - スキンを解放
                await UnlockSkin(skinID);
                Debug.Log($"Skin unlocked by ad: {skin.skinName}");
                onSuccess?.Invoke();
            },
            onFailed: () =>
            {
                // 広告視聴失敗
                Debug.LogWarning($"Failed to unlock skin {skinID} by ad.");
                onFailed?.Invoke();
            }
        );
    }
}
