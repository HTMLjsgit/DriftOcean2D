using System;
using System.Collections.Generic;

[Serializable]
public class RankingEntry
{
    public string playerName;
    public float score;
    public string date; // "2025/12/06" のような文字列で保存
    public int skinID;  // ★追加: その時のスキンID
    public RankingEntry(string name, float score, int skinID)
    {
        this.playerName = name;
        this.score = score;
        this.skinID = skinID;
        this.date = DateTime.Now.ToString("yyyy/MM/dd");
    }
}

// PlayerPrefsにリストごと保存するためのラッパークラス
[Serializable]
public class RankingListWrapper
{
    public List<RankingEntry> list = new List<RankingEntry>();
}