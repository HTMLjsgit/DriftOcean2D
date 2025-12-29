using UnityEngine;

[CreateAssetMenu(fileName = "SkinData", menuName = "ScriptableObject/SkinData")]
public class SkinData : ScriptableObject
{
    public enum UnlockType
    {
        None,           // 初期解放
        ScoreReach,     // 特定のスコア到達（ベストスコア）
        PlayCount,      // 累計プレイ回数
        SurvivalTime,   // 1回のプレイでの生存時間
        TotalPlayTime,  // 累計プレイ時間
        DeathCount,     // 累計死亡回数
        AdWatch,        // 広告視聴（仕様書に記載あり）
        CompleteAll,    // 他のスキンを全て解放
        NoInput,        // 一定時間操作なし（海綿体用）
        SNSShare,       // SNSシェア（アカウミガメ用）
        ConsecutiveSurvival, // 連続で一定時間生存（イルカ用）
        NoContinueHardMode   // コンティニューなしでハードモード到達（カニ用）
    }

    public int id;
    public string skinName;
    public Sprite skinSprite;
    public UnlockType unlockType;
    
    [Header("条件値 (秒数・回数・スコア)")]
    public float conditionValue; 

    [Header("説明文 (ロック時に表示)")]
    public string lockedDescription; // 例：「3分間生き残る」
}