using UnityEngine;
using System;
using System.Threading.Tasks;



using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Exceptions;
using Unity.Services.Leaderboards.Models;
using System.Collections.Generic;

/// <summary>
/// Unity Gaming Services (UGS) 統合管理クラス
/// Authentication、Cloud Save、Leaderboardsを管理
/// </summary>
public class UGSManager : MonoBehaviour
{
    public static UGSManager instance;

#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private bool simulateOfflineInEditor = false;
    private bool _lastSimulateOfflineInEditor = false;
    private bool _isApplyingDebugConnectionState = false;
#endif

    [Header("Settings")]
    [SerializeField] private bool useUGS = true; // UGSを使用するか

    // 初期化状態
    private bool isInitialized = false;
    private bool isSignedIn = false;

    // イベント
    public event Action OnSignInSuccess;
    public event Action OnSignInFailed;

    void Awake()
    {
        if (instance == null)
        {
            Debug.Log($"[DEBUG] UGSManager Awake: Creating new singleton instance");
            instance = this;
            DontDestroyOnLoad(gameObject);
#if UNITY_EDITOR
            _lastSimulateOfflineInEditor = simulateOfflineInEditor;
#endif
        }
        else
        {
            Debug.Log($"[DEBUG] UGSManager Awake: Destroying duplicate instance");
            Destroy(gameObject);
        }
    }

    async void Start()
    {
        Debug.Log($"[DEBUG] UGSManager Start called");
        await InitializeUGS();
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return;
        }

        if (_lastSimulateOfflineInEditor == simulateOfflineInEditor || _isApplyingDebugConnectionState)
        {
            return;
        }

        _lastSimulateOfflineInEditor = simulateOfflineInEditor;
        ApplyDebugConnectionStateChange();
#endif
    }

    void OnDestroy()
    {
        Debug.Log($"[DEBUG] UGSManager OnDestroy called");
    }

    /// <summary>
    /// UGSの初期化とサインイン
    /// </summary>
    private async Task InitializeUGS()
    {
        if (!IsServiceEnabled())
        {
            SetOfflineState("UGS is disabled or offline simulation is enabled.");
            return;
        }

        try
        {
            Debug.Log("Initializing Unity Gaming Services...");

            // UGS初期化
            await UnityServices.InitializeAsync();
            Debug.Log("Unity Gaming Services initialized successfully!");

            // 匿名サインイン
            await SignInAnonymously();

            isInitialized = isSignedIn;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize UGS: {e.Message}");
            SetOfflineState($"Failed to initialize UGS: {e.Message}");
        }
    }

    /// <summary>
    /// 匿名サインイン
    /// </summary>
    private async Task SignInAnonymously()
    {
        try
        {
            // 既にサインイン済みかチェック
            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log($"Already signed in. Player ID: {AuthenticationService.Instance.PlayerId}");
                Debug.Log($"現在ログインしているプレイヤーID: {AuthenticationService.Instance.PlayerId}");
                isSignedIn = true;
                OnSignInSuccess?.Invoke();
                return;
            }

            Debug.Log("Signing in anonymously...");
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            // サインイン完了後、フラグを設定してからイベント発火
            isSignedIn = true;
            Debug.Log($"Sign in successful! Player ID: {AuthenticationService.Instance.PlayerId}");
            Debug.Log($"現在ログインしているプレイヤーID: {AuthenticationService.Instance.PlayerId}");

            // 少し待ってからイベント発火（確実に初期化完了させる）
            await Task.Delay(100);
            OnSignInSuccess?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to sign in: {e.Message}");
            SetOfflineState($"Failed to sign in: {e.Message}");
        }
    }

    private void SetOfflineState(string reason)
    {
        isInitialized = false;
        isSignedIn = false;
        Debug.LogWarning(reason);
        OnSignInFailed?.Invoke();
    }

#if UNITY_EDITOR
    private async void ApplyDebugConnectionStateChange()
    {
        _isApplyingDebugConnectionState = true;

        try
        {
            if (simulateOfflineInEditor)
            {
                SetOfflineState("[DEBUG] Offline simulation enabled in Editor.");
                return;
            }

            Debug.Log("[DEBUG] Offline simulation disabled. Reinitializing UGS...");
            await InitializeUGS();
        }
        finally
        {
            _isApplyingDebugConnectionState = false;
        }
    }
#endif

    #region Cloud Save

    /// <summary>
    /// データをCloud Saveに保存
    /// </summary>
    public async Task<bool> SaveData(string key, object value)
    {
        if (!isInitialized || !isSignedIn)
        {
            Debug.LogWarning("UGS not initialized or not signed in. Cannot save data.");
            return false;
        }

        try
        {
            var data = new Dictionary<string, object> { { key, value } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            Debug.Log($"Cloud Save success: {key}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save data to Cloud Save: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// データをCloud Saveから取得
    /// </summary>
    public async Task<T> LoadData<T>(string key, T defaultValue = default)
    {
        if (!isInitialized || !isSignedIn)
        {
            Debug.LogWarning("UGS not initialized or not signed in. Returning default value.");
            return defaultValue;
        }

        try
        {
            var data = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { key });

            if (data.TryGetValue(key, out var item))
            {
                Debug.Log($"Cloud Load success: {key}");
                return item.Value.GetAs<T>();
            }
            else
            {
                Debug.LogWarning($"Key '{key}' not found in Cloud Save. Returning default value.");
                return defaultValue;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load data from Cloud Save: {e.Message}");
            return defaultValue;
        }
    }

    /// <summary>
    /// 複数のデータをまとめて保存
    /// </summary>
    public async Task<bool> SaveMultipleData(Dictionary<string, object> data)
    {
        if (!isInitialized || !isSignedIn)
        {
            Debug.LogWarning("UGS not initialized or not signed in. Cannot save data.");
            return false;
        }

        try
        {
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            Debug.Log($"Cloud Save success: {data.Count} items saved");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save multiple data to Cloud Save: {e.Message}");
            return false;
        }
    }

    #endregion

    #region Leaderboards

    /// <summary>
    /// スコアをリーダーボードに送信
    /// </summary>
    public async Task<bool> SubmitScore(string leaderboardId, float score, Dictionary<string, string> metadata = null)
    {
        if (!isInitialized || !isSignedIn)
        {
            Debug.LogWarning("UGS not initialized or not signed in. Cannot submit score.");
            return false;
        }

        try
        {
            var options = new AddPlayerScoreOptions();
            if (metadata != null)
            {
                // Dictionary<string, string>をDictionary<string, object>に変換
                var metadataDict = new Dictionary<string, object>();
                foreach (var kvp in metadata)
                {
                    metadataDict[kvp.Key] = kvp.Value;
                }
                options.Metadata = metadataDict;

                Debug.Log($"Submitting score with metadata: playerName={metadata.GetValueOrDefault("playerName")}, skinID={metadata.GetValueOrDefault("skinID")}");
            }

            await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score, options);
            Debug.Log($"Score submitted to leaderboard '{leaderboardId}': {score}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to submit score: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// リーダーボードからトップスコアを取得
    /// </summary>
    public async Task<LeaderboardScoresPage> GetLeaderboard(string leaderboardId, int limit = 10)
    {
        if (!isInitialized || !isSignedIn)
        {
            Debug.LogWarning("UGS not initialized or not signed in. Cannot get leaderboard.");
            return null;
        }

        try
        {
            var options = new GetScoresOptions
            {
                Limit = limit,
                Offset = 0,
                IncludeMetadata = true  // メタデータを含める
            };

            var scoresResponse = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId, options);
            Debug.Log($"Leaderboard '{leaderboardId}' retrieved: {scoresResponse.Results.Count} entries (with metadata)");
            return scoresResponse;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to get leaderboard: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// プレイヤー自身のスコアを取得
    /// </summary>
    public async Task<LeaderboardEntry> GetPlayerScore(string leaderboardId)
    {
        if (!isInitialized || !isSignedIn)
        {
            Debug.LogWarning("UGS not initialized or not signed in. Cannot get player score.");
            return null;
        }

        try
        {
            var options = new GetPlayerScoreOptions
            {
                IncludeMetadata = true
            };

            var playerEntry = await LeaderboardsService.Instance.GetPlayerScoreAsync(leaderboardId, options);
            Debug.Log($"Player score retrieved: Rank {playerEntry.Rank}, Score {playerEntry.Score}");
            return playerEntry;
        }
        catch (LeaderboardsException e) when (e.Reason == LeaderboardsExceptionReason.EntryNotFound)
        {
            Debug.Log($"Player has no score entry yet on leaderboard '{leaderboardId}'.");
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to get player score: {e.Message}");
            return null;
        }
    }

    #endregion
    #region Public Status Methods

    /// <summary>
    /// UGSが初期化されているか
    /// </summary>
    public bool IsInitialized()
    {
        return isInitialized;
    }

    /// <summary>
    /// サインイン済みか
    /// </summary>
    public bool IsSignedIn()
    {
        return isSignedIn;
    }

    public bool IsServiceEnabled()
    {
        return useUGS && !IsOfflineSimulationActive();
    }

    public bool IsOfflineSimulationActive()
    {
#if UNITY_EDITOR
        return simulateOfflineInEditor;
#else
        return false;
#endif
    }

    /// <summary>
    /// プレイヤーIDを取得
    /// </summary>
    public string GetPlayerId()
    {
        if (isSignedIn)
        {
            return AuthenticationService.Instance.PlayerId;
        }
        return "LocalPlayer";
    }

    #endregion
}
