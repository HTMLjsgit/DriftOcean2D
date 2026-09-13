using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Services.Leaderboards.Models;
using UnityEngine;
using UnityEngine.Serialization;

public class UGSLeaderboardManager : MonoBehaviour
{
    private const string LegacyAllTimeLeaderboardId = "drift_ocean_ranking";
    private const string DefaultWeeklyLeaderboardId = "drift_ocean_weekly_ranking";
    private const string DefaultDailyLeaderboardId = "drift_ocean_daily_ranking";

    public static UGSLeaderboardManager instance;

    [Header("Settings")]
    [FormerlySerializedAs("leaderboardId")]
    [SerializeField] private string weeklyLeaderboardId = DefaultWeeklyLeaderboardId;
    [SerializeField] private string dailyLeaderboardId = DefaultDailyLeaderboardId;

    private UGSManager _ugsManager;
    private UGSCloudSaveManager _cloudSaveManager;

    private readonly List<UGSRankingEntry> _cachedWeeklyRankings = new List<UGSRankingEntry>();
    private readonly List<UGSRankingEntry> _cachedDailyRankings = new List<UGSRankingEntry>();

    void Awake()
    {
        EnsureLeaderboardIds();

        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(transform.root.gameObject);
        }
        else
        {
            Destroy(transform.root.gameObject);
        }
    }

    void Start()
    {
        CacheManagers();
    }

    void OnValidate()
    {
        EnsureLeaderboardIds();
    }

    public async Task<bool> SubmitScore(float score)
    {
        CacheManagers();

        if (_ugsManager == null || !_ugsManager.IsSignedIn())
        {
            Debug.LogWarning("UGS not signed in. Cannot submit score.");
            return false;
        }

        if (_cloudSaveManager == null)
        {
            Debug.LogWarning("Cloud save manager not found. Cannot submit score metadata.");
            return false;
        }

        var metadata = new Dictionary<string, string>
        {
            { "playerName", PlayerNameFilter.DisplayName(_cloudSaveManager.GetPlayerName()) },
            { "skinID", _cloudSaveManager.GetCurrentSkinID().ToString() }
        };

        bool weeklySuccess = await _ugsManager.SubmitScore(weeklyLeaderboardId, score, metadata);
        bool dailySuccess = await _ugsManager.SubmitScore(dailyLeaderboardId, score, metadata);
        bool success = weeklySuccess && dailySuccess;

        if (!success)
        {
            Debug.LogError($"Failed to submit score. Weekly: {weeklySuccess}, Daily: {dailySuccess}");
            return false;
        }

        try
        {
            await RefreshWeeklyLeaderboard();
            await RefreshDailyLeaderboard();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Score submission succeeded, but leaderboard refresh failed: {e.Message}");
        }

        return true;
    }

    public Task<bool> RefreshLeaderboard(int limit = 10)
    {
        return RefreshWeeklyLeaderboard(limit);
    }

    public Task<UGSRankingEntry> GetPlayerRank()
    {
        return GetPlayerWeeklyRank();
    }

    public List<UGSRankingEntry> GetCachedRankings()
    {
        return GetCachedWeeklyRankings();
    }

    public async Task<bool> RefreshWeeklyLeaderboard(int limit = 10)
    {
        return await RefreshLeaderboardCache(weeklyLeaderboardId, _cachedWeeklyRankings, "weekly", limit);
    }

    public async Task<bool> RefreshDailyLeaderboard(int limit = 10)
    {
        return await RefreshLeaderboardCache(dailyLeaderboardId, _cachedDailyRankings, "daily", limit);
    }

    public List<UGSRankingEntry> GetCachedWeeklyRankings()
    {
        return _cachedWeeklyRankings;
    }

    public List<UGSRankingEntry> GetCachedDailyRankings()
    {
        return _cachedDailyRankings;
    }

    public async Task<UGSRankingEntry> GetPlayerWeeklyRank()
    {
        return await GetPlayerEntry(weeklyLeaderboardId, "weekly");
    }

    public async Task<UGSRankingEntry> GetPlayerDailyRank()
    {
        return await GetPlayerEntry(dailyLeaderboardId, "daily");
    }

    private void CacheManagers()
    {
        if (_ugsManager == null)
        {
            _ugsManager = UGSManager.instance;
        }

        if (_cloudSaveManager == null)
        {
            _cloudSaveManager = UGSCloudSaveManager.instance;
        }
    }

    private void EnsureLeaderboardIds()
    {
        if (string.IsNullOrWhiteSpace(weeklyLeaderboardId) || weeklyLeaderboardId == LegacyAllTimeLeaderboardId)
        {
            weeklyLeaderboardId = DefaultWeeklyLeaderboardId;
        }

        if (string.IsNullOrWhiteSpace(dailyLeaderboardId))
        {
            dailyLeaderboardId = DefaultDailyLeaderboardId;
        }
    }

    private async Task<bool> RefreshLeaderboardCache(string leaderboardId, List<UGSRankingEntry> cache, string label, int limit)
    {
        CacheManagers();

        if (_ugsManager == null || !_ugsManager.IsSignedIn())
        {
            Debug.LogWarning($"UGS not signed in. Cannot refresh {label} leaderboard.");
            return false;
        }

        LeaderboardScoresPage scoresPage = await _ugsManager.GetLeaderboard(leaderboardId, limit);
        if (scoresPage?.Results == null)
        {
            Debug.LogWarning($"Failed to retrieve {label} leaderboard.");
            return false;
        }

        cache.Clear();

        foreach (LeaderboardEntry entry in scoresPage.Results)
        {
            cache.Add(CreateRankingEntry(entry));
        }

        Debug.Log($"{label} leaderboard refreshed: {cache.Count} entries");
        return true;
    }

    private async Task<UGSRankingEntry> GetPlayerEntry(string leaderboardId, string label)
    {
        CacheManagers();

        if (_ugsManager == null || !_ugsManager.IsSignedIn())
        {
            Debug.LogWarning($"UGS not signed in. Cannot get player {label} rank.");
            return null;
        }

        LeaderboardEntry playerEntry = await _ugsManager.GetPlayerScore(leaderboardId);
        if (playerEntry == null)
        {
            Debug.LogWarning($"Player has no {label} score yet.");
            return null;
        }

        return CreateRankingEntry(playerEntry);
    }

    private UGSRankingEntry CreateRankingEntry(LeaderboardEntry entry)
    {
        ParseMetadata(entry.Metadata, out string playerName, out int skinID);

        return new UGSRankingEntry
        {
            rank = entry.Rank + 1,
            playerName = playerName,
            score = (float)entry.Score,
            skinID = skinID,
            playerId = entry.PlayerId
        };
    }

    private void ParseMetadata(string metadataJson, out string playerName, out int skinID)
    {
        playerName = "Unknown";
        skinID = 1;

        if (string.IsNullOrEmpty(metadataJson))
        {
            return;
        }

        try
        {
            Dictionary<string, string> metadata = JsonConvert.DeserializeObject<Dictionary<string, string>>(metadataJson);
            if (metadata == null)
            {
                return;
            }

            if (metadata.TryGetValue("playerName", out string nameValue) && !string.IsNullOrWhiteSpace(nameValue))
            {
                playerName = nameValue;
            }

            if (metadata.TryGetValue("skinID", out string skinValue) && int.TryParse(skinValue, out int parsedSkinId))
            {
                skinID = parsedSkinId;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to parse leaderboard metadata: {e.Message}");
        }
    }
}

[Serializable]
public class UGSRankingEntry
{
    public int rank;
    public string playerName;
    public float score;
    public int skinID;
    public string playerId;
}
