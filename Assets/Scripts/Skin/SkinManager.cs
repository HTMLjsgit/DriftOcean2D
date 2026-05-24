using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class SkinManager : MonoBehaviour
{
    public static SkinManager instance;

    private SkinDatabase _skinDatabase;
    private AdsManager _adsManager;
    private UGSCloudSaveManager _cloudSaveManager;

    [Header("Offline Mode")]
    [SerializeField] private int _offlineModeSkinID = 1;

    [Header("Current Status")]
    public int currentSkinID;

    private bool anyNewUnlock;

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

            if (_cloudSaveManager.IsDataLoaded)
            {
                LoadFromUGS();
            }
        }
    }

    void OnDestroy()
    {
        if (_cloudSaveManager != null)
        {
            _cloudSaveManager.OnDataLoaded -= LoadFromUGS;
        }
    }

    private async void LoadFromUGS()
    {
        _cloudSaveManager = UGSCloudSaveManager.instance;

        if (IsOfflineModeActive())
        {
            currentSkinID = _offlineModeSkinID;
            return;
        }

        if (_cloudSaveManager == null)
        {
            Debug.LogError("Cannot load skins because UGSCloudSaveManager is missing.");
            return;
        }

        currentSkinID = _cloudSaveManager.GetCurrentSkinID();
        await SyncUnlocksFromCurrentStats();
    }

    public async Task ReportGameResult(float runScore, float runTime, bool noInputAchieved = false, bool usedContinue = false)
    {
        _skinDatabase = SkinDatabase.instance;
        _cloudSaveManager = UGSCloudSaveManager.instance;

        if (IsOfflineModeActive() || _cloudSaveManager == null)
        {
            return;
        }

        PlayerStats stats = _cloudSaveManager.GetStats();

        float survivalThreshold = 60f;
        bool survivedLongEnough = runTime >= survivalThreshold;
        await _cloudSaveManager.UpdateConsecutiveSurvival(survivedLongEnough);
        int consecutiveSurvivalCount = _cloudSaveManager.GetConsecutiveSurvivalCount();

        if (noInputAchieved && !_cloudSaveManager.HasNoInputUnlocked())
        {
            await _cloudSaveManager.SetNoInputUnlocked();
        }

        float hardModeThreshold = 360f;
        if (!usedContinue && runTime >= hardModeThreshold && !_cloudSaveManager.HasNoContinueHardModeUnlocked())
        {
            await _cloudSaveManager.SetNoContinueHardModeUnlocked();
        }

        float maxDifficultySurvivalTime = 0f;
        bool reachedMaxDifficulty = false;
        if (DifficultyManager.instance != null && DifficultyManager.instance.maxDifficultyMode)
        {
            reachedMaxDifficulty = true;
            maxDifficultySurvivalTime = runTime;
        }

        await CheckUnlockConditions(
            runScore,
            runTime,
            stats.totalPlayCount,
            stats.totalPlayTime,
            stats.deathCount,
            stats.bestScore,
            consecutiveSurvivalCount,
            noInputAchieved,
            usedContinue,
            reachedMaxDifficulty,
            maxDifficultySurvivalTime
        );
    }

    public async Task SyncUnlocksFromCurrentStats()
    {
        _cloudSaveManager = UGSCloudSaveManager.instance;
        _skinDatabase = SkinDatabase.instance;

        if (_cloudSaveManager == null || IsOfflineModeActive())
        {
            return;
        }

        PlayerStats stats = _cloudSaveManager.GetStats();
        await CheckUnlockConditions(
            0f,
            0f,
            stats.totalPlayCount,
            stats.totalPlayTime,
            stats.deathCount,
            stats.bestScore,
            stats.consecutiveSurvivalCount,
            false,
            false,
            false,
            0f
        );
    }

    private async Task CheckUnlockConditions(
        float runScore,
        float runTime,
        int playCount,
        float totalTime,
        int deathCount,
        float bestScore,
        int consecutiveSurvivalCount,
        bool noInputAchieved,
        bool usedContinue,
        bool reachedMaxDifficulty,
        float maxDifficultySurvivalTime)
    {
        if (_skinDatabase == null)
        {
            return;
        }

        int unlockedCount = 0;

        foreach (SkinData skin in _skinDatabase.GetAllSkins())
        {
            if (skin == null)
            {
                continue;
            }

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
                    unlock = bestScore >= skin.conditionValue;
                    break;
                case SkinData.UnlockType.PlayCount:
                    unlock = playCount >= (int)skin.conditionValue;
                    break;
                case SkinData.UnlockType.SurvivalTime:
                    unlock = runTime >= skin.conditionValue;
                    break;
                case SkinData.UnlockType.TotalPlayTime:
                    unlock = totalTime >= skin.conditionValue;
                    break;
                case SkinData.UnlockType.DeathCount:
                    unlock = deathCount >= (int)skin.conditionValue;
                    break;
                case SkinData.UnlockType.NoInput:
                    unlock = _cloudSaveManager.HasNoInputUnlocked();
                    break;
                case SkinData.UnlockType.SNSShare:
                    unlock = _cloudSaveManager.HasSNSShared();
                    break;
                case SkinData.UnlockType.ConsecutiveSurvival:
                    unlock = consecutiveSurvivalCount >= (int)skin.conditionValue;
                    break;
                case SkinData.UnlockType.NoContinueHardMode:
                    unlock = _cloudSaveManager.HasNoContinueHardModeUnlocked();
                    break;
                case SkinData.UnlockType.MaxDifficultySurvival:
                    unlock = reachedMaxDifficulty &&
                             (skin.conditionValue == 0f || maxDifficultySurvivalTime >= skin.conditionValue);
                    break;
                case SkinData.UnlockType.AdWatch:
                case SkinData.UnlockType.CompleteAll:
                case SkinData.UnlockType.TapUnlock:
                    break;
            }

            if (!unlock)
            {
                continue;
            }

            await UnlockSkin(skin.id);
            unlockedCount++;
            anyNewUnlock = true;
            Debug.Log($"Skin unlocked: {skin.skinName}");
        }

        SkinData goldSkin = _skinDatabase
            .GetAllSkins()
            .FirstOrDefault(s => s != null && s.unlockType == SkinData.UnlockType.CompleteAll);

        if (goldSkin != null && !IsUnlocked(goldSkin.id))
        {
            if (unlockedCount >= _skinDatabase.GetAllSkins().Count - 1)
            {
                await UnlockSkin(goldSkin.id);
            }
        }
    }

    public async Task UnlockBySNSShare()
    {
        _cloudSaveManager = UGSCloudSaveManager.instance;

        if (IsOfflineModeActive() || _cloudSaveManager == null)
        {
            return;
        }

        await _cloudSaveManager.SetSNSShared();

        _skinDatabase = SkinDatabase.instance;
        SkinData snsSkin = _skinDatabase
            .GetAllSkins()
            .FirstOrDefault(s => s != null && s.unlockType == SkinData.UnlockType.SNSShare);

        if (snsSkin != null && !IsUnlocked(snsSkin.id))
        {
            await UnlockSkin(snsSkin.id);
        }
    }

    public async Task UnlockSkin(int id)
    {
        _cloudSaveManager = UGSCloudSaveManager.instance;

        if (IsOfflineModeActive() || _cloudSaveManager == null)
        {
            return;
        }

        await _cloudSaveManager.UnlockSkin(id);
    }

    public bool IsUnlocked(int id)
    {
        _cloudSaveManager = UGSCloudSaveManager.instance;

        if (IsOfflineModeActive())
        {
            return id == _offlineModeSkinID;
        }

        if (_cloudSaveManager == null)
        {
            return false;
        }

        return _cloudSaveManager.IsSkinUnlocked(id);
    }

    public async Task EquipSkin(int id)
    {
        _cloudSaveManager = UGSCloudSaveManager.instance;

        if (IsOfflineModeActive())
        {
            currentSkinID = _offlineModeSkinID;
            return;
        }

        if (_cloudSaveManager == null)
        {
            Debug.LogError("Cannot equip skin because UGSCloudSaveManager is missing.");
            return;
        }

        if (!IsUnlocked(id))
        {
            Debug.LogWarning($"Skin ID {id} is locked.");
            return;
        }

        currentSkinID = id;
        await _cloudSaveManager.EquipSkin(id);
    }

    public Sprite GetCurrentSkinSprite()
    {
        if (_skinDatabase == null)
        {
            _skinDatabase = SkinDatabase.instance;
        }

        SkinData skin = _skinDatabase != null
            ? _skinDatabase.GetAllSkins().FirstOrDefault(s => s != null && s.id == currentSkinID)
            : null;

        return skin != null ? skin.skinSprite : null;
    }

    public async void UnlockSkinByAd(int skinID, System.Action onSuccess = null, System.Action onFailed = null)
    {
        if (IsOfflineModeActive())
        {
            onFailed?.Invoke();
            return;
        }

        _skinDatabase = SkinDatabase.instance;
        SkinData skin = _skinDatabase
            .GetAllSkins()
            .FirstOrDefault(s => s != null && s.id == skinID);

        if (skin == null)
        {
            onFailed?.Invoke();
            return;
        }

        if (IsUnlocked(skinID))
        {
            onSuccess?.Invoke();
            return;
        }

        if (_adsManager == null)
        {
            await UnlockSkin(skinID);
            onSuccess?.Invoke();
            return;
        }

        _adsManager.ShowRewardedAd(
            onSuccess: async () =>
            {
                await UnlockSkin(skinID);
                onSuccess?.Invoke();
            },
            onFailed: () =>
            {
                onFailed?.Invoke();
            }
        );
    }

    private bool IsOfflineModeActive()
    {
        _cloudSaveManager = UGSCloudSaveManager.instance;
        return _cloudSaveManager != null && _cloudSaveManager.IsOfflineModeActive();
    }
}
