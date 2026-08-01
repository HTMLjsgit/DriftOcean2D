using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "OceanLogCatalog", menuName = "ScriptableObject/OceanLogCatalog")]
public class OceanLogCatalog : ScriptableObject
{
    public List<ObstacleData> garbageEntries = new List<ObstacleData>();
    public List<AchievementData> achievements = new List<AchievementData>();
    public ResultCommentCatalog resultComments;

    [Header("Reward Skin IDs")]
    public int allAchievementsSkinId = 21;
    public int secretAchievementSkinId = 22;
}
