using System;
using System.Collections.Generic;

/// <summary>
/// Cloud Saveに保存するプレイヤーデータ
/// </summary>
[Serializable]
public class PlayerCloudData
{
    public string playerName = "Player";
    public int currentSkinID = 1; // 初期スキンID（ミズクラゲ）
    public List<int> unlockedSkinIDs = new List<int> { 1 }; // 初期スキンは解放済み
    public List<int> notifiedSkinIDs = new List<int> { 1 }; // お知らせ済みスキンリスト（タイトル画面で通知済み）
    public List<int> seenSkinIDs = new List<int> { 1 }; // 見た（装備した）スキンリスト（Newラベル用）
    public List<int> viewedSkinInventoryUnlockedSkins = new List<int> { 1 }; // スキン一覧で確認済みの解放スキンリスト（SkinボタンのNewラベル用）
    public PlayerStats stats = new PlayerStats();

    // ぷにぷにタップ回数（スキンID → タップ回数）※旧仕様の互換性のため残す（使用しない）
    public SerializableDictionary<int, int> skinTapCounts = new SerializableDictionary<int, int>();

    // ぷにぷにグローバルタップ回数（解放済みスキンをタップした総回数）
    public int globalTapCount = 0;
}

/// <summary>
/// Dictionaryをシリアライズ可能にするクラス
/// </summary>
[Serializable]
public class SerializableDictionary<TKey, TValue>
{
    public List<TKey> keys = new List<TKey>();
    public List<TValue> values = new List<TValue>();

    public int Count => keys.Count;

    public bool ContainsKey(TKey key)
    {
        return keys.Contains(key);
    }

    public TValue this[TKey key]
    {
        get
        {
            int index = keys.IndexOf(key);
            if (index >= 0)
                return values[index];
            return default(TValue);
        }
        set
        {
            int index = keys.IndexOf(key);
            if (index >= 0)
            {
                values[index] = value;
            }
            else
            {
                keys.Add(key);
                values.Add(value);
            }
        }
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        int index = keys.IndexOf(key);
        if (index >= 0)
        {
            value = values[index];
            return true;
        }
        value = default(TValue);
        return false;
    }
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
