using UnityEngine;

[CreateAssetMenu(fileName = "AchievementData", menuName = "ScriptableObject/AchievementData")]
public class AchievementData : ScriptableObject
{
    public enum ConditionType
    {
        SkinCount,
        TotalBounceCount,
        HardModeReached,
        GarbageCount,
        ConsecutivePlayDays,
        TotalPlayTime,
        ScoreReach,
        AllRegularAchievements
    }

    public int id;
    public string achievementName;
    [TextArea(1, 3)] public string description;
    public ConditionType conditionType;
    public float conditionValue;
    [Tooltip("達成するまで実績一覧に表示しません。")]
    public bool hiddenUntilCompleted;
    [Tooltip("実績コンプリートとリンゴスキンの条件から除外します。")]
    public bool excludedFromRegularCompletion;
}
