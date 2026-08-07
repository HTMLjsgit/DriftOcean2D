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
        NoContinueHardMode,  // コンティニューなしでハードモード到達（カニ用）
        TapUnlock,      // ぷにぷにタップ回数で解放（conditionValueでタップ回数指定）
        MaxDifficultySurvival, // 最高難易度で生存（conditionValue=0でハードモード到達、>0で生存時間指定）
        AchievementCompleteReward,
        SecretAchievementReward
    }

    public int id;
    public string skinName;
    public Sprite skinSprite;

    [Header("Platform Sprite")]
    [Tooltip("iOSで別画像を使う場合のみ設定します。未設定時は通常画像を使います。")]
    public Sprite iosSkinSprite;

    [Header("Encyclopedia")]
    [TextArea(2, 5)]
    public string description;
    [Tooltip("解放されるまでスキン一覧に枠自体を表示しません。")]
    public bool hiddenUntilUnlocked;
    [Tooltip("初期20種のコンプリート判定に含めるスキンです。")]
    public bool countsTowardsOriginalCollection = true;

    [Header("Detail Appearance")]
    [Tooltip("スキン詳細カードに専用の背景色を使用します。")]
    public bool useCustomDetailBackground;
    public Color detailBackgroundColor = Color.white;

    public UnlockType unlockType;

    [Header("条件値 (秒数・回数・スコア)")]
    public float conditionValue;

    [Header("説明文 (ロック時に表示)")]
    public string lockedDescription; // 例：「3分間生き残る」

    public Sprite GetDisplaySprite()
    {
#if UNITY_IOS
        if (iosSkinSprite != null)
        {
            return iosSkinSprite;
        }
#endif
        return skinSprite;
    }
}
