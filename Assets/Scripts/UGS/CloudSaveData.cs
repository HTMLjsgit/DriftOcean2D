using System;
using System.Collections.Generic;

/// <summary>
/// Cloud Saveに保存するプレイヤーデータ
/// </summary>
[Serializable]
public class PlayerCloudData
{
    public string playerName = "プレイヤー";
    public int currentSkinID = 0;
    public List<int> unlockedSkinIDs = new List<int> { 0 }; // 初期スキンは解放済み
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
}
