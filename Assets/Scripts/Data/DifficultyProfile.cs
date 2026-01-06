using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Difficulty_Stage", menuName = "ScriptableObject/DifficultyProfile")]
public class DifficultyProfile : ScriptableObject
{
    [Header("Trigger Condition")]
    public float thresholdYear; // この年数（スコア）を超えたら発動

    [Header("Rest Phase")]
    // ★追加: この難易度になる前に挟む休憩時間（秒）
    // 0なら休憩なしで即切り替え
    public float restDuration = 0f; 

    [Header("Difficulty Params")]
    public float spawnInterval = 1.5f; 
    public float speedMultiplier = 1.2f; 

    [Header("New Trash")]
    public List<ObstacleData> newObstaclesToAdd; 

    [Header("中間難易度かどうか")]
    public bool middleDifficulty;

    [Header("最高難易度かどうか")]
    public bool maxDifficulty;
}