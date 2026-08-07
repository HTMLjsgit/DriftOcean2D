using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager instance;

    [SerializeField] private OceanLogCatalog _catalog;

    public OceanLogCatalog Catalog => _catalog;

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

    public async Task RegisterPlayStarted()
    {
        UGSCloudSaveManager cloudSave = UGSCloudSaveManager.instance;
        if (!cloudSave.IsDataLoaded || cloudSave.IsOfflineModeActive())
        {
            return;
        }

        await cloudSave.RegisterPlayDay();
    }

    public async Task<List<AchievementData>> EvaluateAchievements()
    {
        UGSCloudSaveManager cloudSave = UGSCloudSaveManager.instance;
        if (cloudSave.IsOfflineModeActive())
        {
            return new List<AchievementData>();
        }

        PlayerStats stats = cloudSave.GetStats();
        HashSet<int> completed = cloudSave.GetUnlockedAchievementIDs().ToHashSet();
        List<AchievementData> newlyCompleted = new List<AchievementData>();

        foreach (AchievementData achievement in _catalog.achievements)
        {
            if (completed.Contains(achievement.id) ||
                achievement.conditionType == AchievementData.ConditionType.AllRegularAchievements)
            {
                continue;
            }

            if (IsConditionMet(achievement, stats, cloudSave))
            {
                completed.Add(achievement.id);
                newlyCompleted.Add(achievement);
            }
        }

        foreach (AchievementData achievement in _catalog.achievements)
        {
            if (completed.Contains(achievement.id) ||
                achievement.conditionType != AchievementData.ConditionType.AllRegularAchievements)
            {
                continue;
            }

            if (AreBaseAchievementsComplete(completed))
            {
                completed.Add(achievement.id);
                newlyCompleted.Add(achievement);
            }
        }

        if (newlyCompleted.Count > 0)
        {
            await cloudSave.UnlockAchievements(newlyCompleted.Select(item => item.id));
        }

        await UnlockRewardSkinsIfNeeded(completed);
        return newlyCompleted;
    }

    public AchievementData GetAchievement(int achievementID)
    {
        return _catalog.achievements.FirstOrDefault(item => item.id == achievementID);
    }

    public ObstacleData GetGarbageEntry(int obstacleID)
    {
        return _catalog.garbageEntries.FirstOrDefault(item => item.id == obstacleID);
    }

    public string GetResultComment(float score)
    {
        return _catalog.resultComments.GetComment(score);
    }

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
    public async Task EditorSyncRewardSkins()
    {
        UGSCloudSaveManager cloudSave = UGSCloudSaveManager.instance;
        if (cloudSave == null || cloudSave.IsOfflineModeActive())
        {
            return;
        }

        await UnlockRewardSkinsIfNeeded(cloudSave.GetUnlockedAchievementIDs().ToHashSet());
    }
#endif

    private bool IsConditionMet(
        AchievementData achievement,
        PlayerStats stats,
        UGSCloudSaveManager cloudSave)
    {
        switch (achievement.conditionType)
        {
            case AchievementData.ConditionType.SkinCount:
                int originalSkinCount = cloudSave.GetUnlockedSkinIDs().Count(id => id >= 1 && id <= 20);
                return originalSkinCount >= Mathf.RoundToInt(achievement.conditionValue);
            case AchievementData.ConditionType.TotalBounceCount:
                return stats.totalBounceCount >= Mathf.RoundToInt(achievement.conditionValue);
            case AchievementData.ConditionType.HardModeReached:
                return stats.hasReachedHardMode;
            case AchievementData.ConditionType.GarbageCount:
                return cloudSave.GetDiscoveredObstacleIDs().Count >= Mathf.RoundToInt(achievement.conditionValue);
            case AchievementData.ConditionType.ConsecutivePlayDays:
                return stats.consecutivePlayDays >= Mathf.RoundToInt(achievement.conditionValue);
            case AchievementData.ConditionType.TotalPlayTime:
                return stats.totalPlayTime >= achievement.conditionValue;
            case AchievementData.ConditionType.ScoreReach:
                return stats.bestScore >= achievement.conditionValue;
            default:
                return false;
        }
    }

    private bool AreBaseAchievementsComplete(HashSet<int> completed)
    {
        return _catalog.achievements
            .Where(item => !item.excludedFromRegularCompletion &&
                           item.conditionType != AchievementData.ConditionType.AllRegularAchievements)
            .All(item => completed.Contains(item.id));
    }

    private async Task UnlockRewardSkinsIfNeeded(HashSet<int> completed)
    {
        bool allRegularComplete = _catalog.achievements
            .Where(item => !item.excludedFromRegularCompletion)
            .All(item => completed.Contains(item.id));

        if (allRegularComplete && !SkinManager.instance.IsUnlocked(_catalog.allAchievementsSkinId))
        {
            await SkinManager.instance.UnlockSkin(_catalog.allAchievementsSkinId);
        }

        AchievementData secretAchievement = _catalog.achievements
            .FirstOrDefault(item => item.hiddenUntilCompleted);

        if (completed.Contains(secretAchievement.id) &&
            !SkinManager.instance.IsUnlocked(_catalog.secretAchievementSkinId))
        {
            await SkinManager.instance.UnlockSkin(_catalog.secretAchievementSkinId);
        }
    }
}
