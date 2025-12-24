using UnityEngine;
using System.Collections.Generic;
using System.Linq; 

public class RankingManager : MonoBehaviour
{
    public static RankingManager instance;

    private const string KEY_RANKING_DATA = "LocalRankingData";
    private const int MAX_RANKING_COUNT = 10; 

    public List<RankingEntry> currentRanking = new List<RankingEntry>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadRanking();
        }
        else Destroy(gameObject);
    }

    public void LoadRanking()
    {
        if (PlayerPrefs.HasKey(KEY_RANKING_DATA))
        {
            string json = PlayerPrefs.GetString(KEY_RANKING_DATA);
            RankingListWrapper wrapper = JsonUtility.FromJson<RankingListWrapper>(json);
            currentRanking = wrapper.list;
        }
        else
        {
            currentRanking = new List<RankingEntry>();
        }
    }

    private void SaveRanking()
    {
        RankingListWrapper wrapper = new RankingListWrapper();
        wrapper.list = currentRanking;
        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(KEY_RANKING_DATA, json);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// スコアがランキング圏内（10位以内）に入るかチェック
    /// </summary>
    /// <param name="score">チェックするスコア</param>
    /// <returns>ランキング入りするならtrue</returns>
    public bool IsRankingEligible(float score)
    {
        // ランキングが10件未満なら必ず入る
        if (currentRanking.Count < MAX_RANKING_COUNT)
        {
            return true;
        }

        // 10位のスコアより高ければランキング入り
        float lowestScore = currentRanking[currentRanking.Count - 1].score;
        return score > lowestScore;
    }

    // ★修正: 引数に skinID を追加
    public bool TryAddScore(string playerName, float score, int skinID)
    {
        // 登録
        RankingEntry newEntry = new RankingEntry(playerName, score, skinID);
        currentRanking.Add(newEntry);

        // ソート（降順）
        currentRanking = currentRanking.OrderByDescending(x => x.score).ToList();

        // 10位あふれ処理
        if (currentRanking.Count > MAX_RANKING_COUNT)
        {
            if (currentRanking[currentRanking.Count - 1] == newEntry)
            {
                currentRanking.RemoveAt(currentRanking.Count - 1);
                return false;
            }
            currentRanking.RemoveAt(currentRanking.Count - 1);
        }

        SaveRanking();
        return true;
    }

    /// <summary>
    /// 現在のランキングを取得
    /// </summary>
    public List<RankingEntry> GetCurrentRanking()
    {
        return currentRanking;
    }
}