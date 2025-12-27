using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;

/// <summary>
/// UGS Leaderboards専用マネージャー
/// オンラインランキングを管理
/// </summary>
public class UGSLeaderboardManager : MonoBehaviour
{
    public static UGSLeaderboardManager instance;

    [Header("Settings")]
    [SerializeField] private string leaderboardId = "drift_ocean_ranking"; // Leaderboard ID（UGSダッシュボードで設定）
    // メタデータが有効にならない場合は、新しいIDに変更してください（例: drift_ocean_ranking_v2）

    private UGSManager _ugsManager;
    private UGSCloudSaveManager _cloudSaveManager;

    // キャッシュ
    private List<UGSRankingEntry> _cachedRankings = new List<UGSRankingEntry>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        _ugsManager = UGSManager.instance;
        _cloudSaveManager = UGSCloudSaveManager.instance;
    }

    /// <summary>
    /// スコアをリーダーボードに送信
    /// </summary>
    public async Task<bool> SubmitScore(float score)
    {
        Debug.Log($"[DEBUG] SubmitScore called. _ugsManager is null: {_ugsManager == null}");
        if (_ugsManager != null)
        {
            Debug.Log($"[DEBUG] SubmitScore: _ugsManager.IsSignedIn(): {_ugsManager.IsSignedIn()}");
        }

        if (_ugsManager == null || !_ugsManager.IsSignedIn())
        {
            Debug.LogWarning("UGS not signed in. Cannot submit score.");
            return false;
        }

        if (_cloudSaveManager == null)
        {
            Debug.LogWarning("CloudSaveManager not found.");
            return false;
        }

        // プレイヤー名とスキンIDをメタデータとして送信
        string playerName = _cloudSaveManager.GetPlayerName();
        int skinID = _cloudSaveManager.GetCurrentSkinID();

        Debug.Log($"[DEBUG] CloudSaveManager player name: '{playerName}'");
        Debug.Log($"[DEBUG] CloudSaveManager skin ID: {skinID}");

        var metadata = new Dictionary<string, string>
        {
            { "playerName", playerName },
            { "skinID", skinID.ToString() }
        };

        Debug.Log($"[DEBUG] Metadata dictionary created: playerName={metadata["playerName"]}, skinID={metadata["skinID"]}");
        Debug.Log($"Submitting score: {score}, Player: {playerName}, Skin: {skinID}");

        bool success = await _ugsManager.SubmitScore(leaderboardId, score, metadata);

        if (success)
        {
            Debug.Log("Score submitted successfully!");

            // スコア送信後にランキングを更新（エラーが発生しても続行）
            try
            {
                await RefreshLeaderboard();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to refresh leaderboard after score submission: {e.Message}");
                // スコアは既に送信されているので、リフレッシュ失敗は無視
            }
        }
        else
        {
            Debug.LogError("Failed to submit score!");
        }

        return success;
    }

    /// <summary>
    /// リーダーボードを取得してキャッシュ
    /// </summary>
    public async Task<bool> RefreshLeaderboard(int limit = 10)
    {
        // デバッグ用：詳細なログを出力
        Debug.Log($"[DEBUG] RefreshLeaderboard called. _ugsManager is null: {_ugsManager == null}");
        if (_ugsManager != null)
        {
            Debug.Log($"[DEBUG] _ugsManager.IsSignedIn(): {_ugsManager.IsSignedIn()}");
        }

        if (_ugsManager == null || !_ugsManager.IsSignedIn())
        {
            Debug.LogWarning("UGS not signed in. Cannot refresh leaderboard.");
            return false;
        }

        Debug.Log($"Refreshing leaderboard... (Limit: {limit})");

        var scoresPage = await _ugsManager.GetLeaderboard(leaderboardId, limit);

        if (scoresPage == null || scoresPage.Results == null)
        {
            Debug.LogWarning("Failed to retrieve leaderboard.");
            return false;
        }

        // キャッシュをクリア
        _cachedRankings.Clear();

        // データを変換
        foreach (var entry in scoresPage.Results)
        {
            string playerName = "Unknown";
            int skinID = 0;

            // メタデータからプレイヤー名とスキンIDを取得
            try
            {
                Debug.Log($"Entry PlayerID: {entry.PlayerId}, Metadata: {entry.Metadata}");

                if (!string.IsNullOrEmpty(entry.Metadata))
                {
                    // MetadataはJSON文字列としてデシリアライズ
                    var metadataDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(entry.Metadata);

                    if (metadataDict != null)
                    {
                        if (metadataDict.TryGetValue("playerName", out var nameValue))
                        {
                            playerName = nameValue;
                            Debug.Log($"Player name from metadata: {playerName}");
                        }
                        if (metadataDict.TryGetValue("skinID", out var skinValue))
                        {
                            int.TryParse(skinValue, out skinID);
                            Debug.Log($"SkinID from metadata: {skinID}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Failed to deserialize metadata");
                    }
                }
                else
                {
                    Debug.LogWarning("Entry has no metadata");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse metadata: {e.Message}\n{e.StackTrace}");
            }

            var rankingEntry = new UGSRankingEntry
            {
                rank = entry.Rank + 1, // 0-indexed → 1-indexed
                playerName = playerName,
                score = (float)entry.Score,
                skinID = skinID,
                playerId = entry.PlayerId
            };

            _cachedRankings.Add(rankingEntry);
        }

        Debug.Log($"Leaderboard refreshed: {_cachedRankings.Count} entries");
        return true;
    }

    /// <summary>
    /// キャッシュされたランキングを取得
    /// </summary>
    public List<UGSRankingEntry> GetCachedRankings()
    {
        return _cachedRankings;
    }

    /// <summary>
    /// プレイヤー自身のランクとスコアを取得
    /// </summary>
    public async Task<UGSRankingEntry> GetPlayerRank()
    {
        if (_ugsManager == null || !_ugsManager.IsSignedIn())
        {
            Debug.LogWarning("UGS not signed in. Cannot get player rank.");
            return null;
        }

        var playerEntry = await _ugsManager.GetPlayerScore(leaderboardId);

        if (playerEntry == null)
        {
            Debug.LogWarning("Player has no score yet.");
            return null;
        }

        string playerName = "Unknown";
        int skinID = 0;

        try
        {
            if (!string.IsNullOrEmpty(playerEntry.Metadata))
            {
                // MetadataはJSON文字列としてデシリアライズ
                var metadataDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(playerEntry.Metadata);

                if (metadataDict != null)
                {
                    if (metadataDict.TryGetValue("playerName", out var nameValue))
                    {
                        playerName = nameValue;
                    }
                    if (metadataDict.TryGetValue("skinID", out var skinValue))
                    {
                        int.TryParse(skinValue, out skinID);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to parse metadata: {e.Message}\n{e.StackTrace}");
        }

        return new UGSRankingEntry
        {
            rank = playerEntry.Rank + 1,
            playerName = playerName,
            score = (float)playerEntry.Score,
            skinID = skinID,
            playerId = playerEntry.PlayerId
        };
    }
}

/// <summary>
/// UGS用のランキングエントリー
/// </summary>
[Serializable]
public class UGSRankingEntry
{
    public int rank;
    public string playerName;
    public float score;
    public int skinID;
    public string playerId;
}
