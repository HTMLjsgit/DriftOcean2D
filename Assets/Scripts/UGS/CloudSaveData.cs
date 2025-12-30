using System;
using System.Collections.Generic;

/// <summary>
/// Cloud Saveに保存するプレイヤーデータ
/// </summary>
[Serializable]
public class PlayerCloudData
{
    public string playerName = "プレイヤー";
    public int currentSkinID = 1; // 初期スキンID（ミズクラゲ）
    public List<int> unlockedSkinIDs = new List<int> { 1 }; // 初期スキンは解放済み
    public List<int> notifiedSkinIDs = new List<int> { 1 }; // お知らせ済みスキンリスト（タイトル画面で通知済み）
    public List<int> seenSkinIDs = new List<int> { 1 }; // 見た（装備した）スキンリスト（Newラベル用）
    public PlayerStats stats = new PlayerStats();
}

/// <summary>
/// プレイヤーの統計データ
/// </summary>
[Serializable]
public class PlayerStats
{
    public int totalPlayCount = 0;
    public float totalPlayTime = 0f;
    public int deathCount = 0;
    public float bestScore = 0f;

    // 連続生存条件用（イルカ：3回連続で1分以上生存）
    public int consecutiveSurvivalCount = 0;  // 連続で条件を満たした回数

    // SNSシェア済みフラグ（アカウミガメ用）
    public bool hasSNSShared = false;

    // 無操作解放済みフラグ（海綿体用）
    public bool hasNoInputUnlocked = false;

    // ノーコンティニューでハードモード到達済みフラグ（カニ用）
    public bool hasNoContinueHardModeUnlocked = false;
}
