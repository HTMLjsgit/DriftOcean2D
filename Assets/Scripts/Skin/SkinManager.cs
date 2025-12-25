using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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

    // --- 永続化データのキー（PlayerPrefsフォールバック用） ---
    private const string KEY_TOTAL_PLAY_COUNT = "Stats_PlayCount";
    private const string KEY_TOTAL_PLAY_TIME = "Stats_TotalTime";
    private const string KEY_DEATH_COUNT = "Stats_DeathCount";
    private const string KEY_BEST_SCORE = "Stats_BestScore";
    private const string KEY_SKIN_UNLOCKED_PREFIX = "Skin_Unlocked_";
    private const string KEY_EQUIPPED_SKIN = "Skin_Equipped";

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

        // UGS使用時はCloud Saveデータロード完了後に読み込み
        if (_cloudSaveManager != null)
        {
            _cloudSaveManager.OnDataLoaded += LoadFromUGS;
        }
        else
        {
            // UGS未使用時はPlayerPrefsから読み込み
            LoadStatus();
        }
    }

    /// <summary>
    /// UGS Cloud Saveからデータをロード
    /// </summary>
    private void LoadFromUGS()
    {
        currentSkinID = _cloudSaveManager.GetCurrentSkinID();
        Debug.Log($"SkinManager loaded from UGS: currentSkinID={currentSkinID}");
    }

    /// <summary>
    /// ゲーム開始時にデータをロード（PlayerPrefsフォールバック用）
    /// </summary>
    private void LoadStatus()
    {
        // 初期スキン(ID:0)は必ず解放扱いにする
        PlayerPrefs.SetInt(KEY_SKIN_UNLOCKED_PREFIX + "0", 1);

        // 装備中のスキンをロード（なければ0番）
        currentSkinID = PlayerPrefs.GetInt(KEY_EQUIPPED_SKIN, 0);
        Debug.Log($"SkinManager LoadStatus (PlayerPrefs): currentSkinID={currentSkinID}");
    }

    /// <summary>
    /// ゲームオーバー時に呼ばれる：統計を更新し、スキンの解放チェックを行う
    /// </summary>
    /// <param name="runScore">今回のスコア</param>
    /// <param name="runTime">今回の生存時間(秒)</param>
    public async System.Threading.Tasks.Task ReportGameResult(float runScore, float runTime)
    {
        _skinDatabase = SkinDatabase.instance;
        _cloudSaveManager = UGSCloudSaveManager.instance;

        PlayerStats stats = _cloudSaveManager.GetStats();

        // 解放条件のチェック
        await CheckUnlockConditions(runScore, runTime, stats.totalPlayCount, stats.totalPlayTime, stats.deathCount, stats.bestScore);
    }

    /// <summary>
    /// 全スキンをチェックして解放処理を行う
    /// </summary>
    private async System.Threading.Tasks.Task CheckUnlockConditions(float runScore, float runTime, int playCount, float totalTime, int deathCount, float bestScore)
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
    /// スキンを解放状態にする
    /// </summary>
    public async System.Threading.Tasks.Task UnlockSkin(int id)
    {
        // UGS使用時はCloud Saveに保存
        if (_cloudSaveManager != null)
        {
            await _cloudSaveManager.UnlockSkin(id);
            Debug.Log($"Skin {id} unlocked and saved to UGS Cloud Save");
        }
        else
        {
            // UGS未使用時はPlayerPrefsに保存
            PlayerPrefs.SetInt(KEY_SKIN_UNLOCKED_PREFIX + id, 1);
            PlayerPrefs.Save();
            Debug.Log($"Skin {id} unlocked and saved to PlayerPrefs");
        }
    }

    /// <summary>
    /// スキンが解放されているか確認
    /// </summary>
    public bool IsUnlocked(int id)
    {
        // UGS使用時はCloud Saveから確認
        if (_cloudSaveManager != null)
        {
            bool unlocked = _cloudSaveManager.IsSkinUnlocked(id);
            Debug.Log($"[DEBUG] SkinManager.IsUnlocked({id}): {unlocked} (from UGS)");
            return unlocked;
        }
        else
        {
            // UGS未使用時はPlayerPrefsから確認
            bool unlocked = PlayerPrefs.GetInt(KEY_SKIN_UNLOCKED_PREFIX + id, 0) == 1;
            Debug.Log($"[DEBUG] SkinManager.IsUnlocked({id}): {unlocked} (from PlayerPrefs)");
            return unlocked;
        }
    }

    /// <summary>
    /// スキンを装備する
    /// </summary>
    public async System.Threading.Tasks.Task EquipSkin(int id)
    {
        Debug.Log($"EquipSkin called: id={id}, IsUnlocked={IsUnlocked(id)}");

        if (IsUnlocked(id))
        {
            currentSkinID = id;

            // UGS使用時はCloud Saveに保存
            if (_cloudSaveManager != null)
            {
                await _cloudSaveManager.EquipSkin(id);
                Debug.Log($"Skin {id} equipped and saved to UGS Cloud Save");
            }
            else
            {
                // UGS未使用時はPlayerPrefsに保存
                PlayerPrefs.SetInt(KEY_EQUIPPED_SKIN, id);
                PlayerPrefs.Save();
                Debug.Log($"Skin {id} equipped and saved to PlayerPrefs");
            }
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
