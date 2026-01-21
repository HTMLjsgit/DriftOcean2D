using UnityEngine;
using System;
using System.Collections;

using GoogleMobileAds.Api;

/// <summary>
/// Google AdMob広告管理クラス
/// リワード広告（スキン解放・コンティニュー用）とインタースティシャル広告（5回ごと）を管理
/// </summary>
public class AdsManager : MonoBehaviour
{
    public static AdsManager instance;

    [Header("AdMob Settings")]
    [SerializeField] private bool useTestAds = true; // テスト広告を使用するか

    [Header("Ad Unit IDs (本番用)")]
    [SerializeField] private string androidRewardedAdUnitId = ""; // テストID
    [SerializeField] private string androidInterstitialAdUnitId = ""; // テストID
    [SerializeField] private string iosRewardedAdUnitId = ""; // iOS テストID
    [SerializeField] private string iosInterstitialAdUnitId = ""; // iOS テストID

    [Header("Play Count Ad Settings")]
    [SerializeField] private int adIntervalPlayCount = 5; // 5回ごとに広告表示

    private UGSCloudSaveManager _cloudSaveManager;

    private RewardedAd rewardedAd;
    private InterstitialAd interstitialAd;


    // コールバック
    private Action onRewardedAdSuccess;
    private Action onRewardedAdFailed;
    private Action onInterstitialAdClosed;

    private bool isInitialized = false;
    private bool rewardGranted = false; // 報酬が付与されたかどうかのフラグ

    // テスト用広告ユニットID
    private const string TEST_REWARDED_AD_UNIT_ID = "ca-app-pub-3940256099942544/5224354917";
    private const string TEST_INTERSTITIAL_AD_UNIT_ID = "ca-app-pub-3940256099942544/1033173712";

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
        _cloudSaveManager = UGSCloudSaveManager.instance;
        InitializeAds();
    }

    /// <summary>
    /// 広告SDKの初期化
    /// </summary>
    private void InitializeAds()
    {
        try
        {
            Debug.Log("AdMob SDK initialization started...");

            // AdMob SDK初期化
            MobileAds.Initialize(initStatus =>
            {
                Debug.Log("AdMob SDK initialized successfully!");
                isInitialized = true;

                // 初期化完了後に広告をロード
                LoadRewardedAd();
                LoadInterstitialAd();
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"AdMob initialization error: {e.Message}");
        }
    }


    #region Rewarded Ad

    /// <summary>
    /// リワード広告をロード
    /// </summary>
    private void LoadRewardedAd()
    {
        // 既存の広告があれば破棄
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        // プラットフォームに応じた広告ユニットIDを選択
        string adUnitId;
#if UNITY_IOS
        adUnitId = useTestAds ? TEST_REWARDED_AD_UNIT_ID : iosRewardedAdUnitId;
#elif UNITY_ANDROID
        adUnitId = useTestAds ? TEST_REWARDED_AD_UNIT_ID : androidRewardedAdUnitId;
#else
        adUnitId = TEST_REWARDED_AD_UNIT_ID; // エディタ等ではテストIDを使用
#endif
        Debug.Log($"Loading rewarded ad with ID: {adUnitId}");

        // 広告リクエストを作成
        var request = new AdRequest();

        // リワード広告をロード
        RewardedAd.Load(adUnitId, request, (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError($"Rewarded ad failed to load: {error}");
                return;
            }

            Debug.Log("Rewarded ad loaded successfully!");
            rewardedAd = ad;

            // イベントリスナーを登録
            RegisterRewardedAdEvents(rewardedAd);
        });
    }

    /// <summary>
    /// リワード広告のイベントリスナーを登録
    /// </summary>
    private void RegisterRewardedAdEvents(RewardedAd ad)
    {
        // 広告が報酬を付与したとき
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log($"Rewarded ad paid {adValue.Value} {adValue.CurrencyCode}");
        };

        // 広告が開かれたとき
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Rewarded ad full screen content opened");
        };

        // 広告が閉じられたとき
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Rewarded ad full screen content closed");

            // 報酬が付与されていた場合、ここで成功コールバックを呼ぶ
            if (rewardGranted)
            {
                Debug.Log("Reward was granted. Calling success callback.");
                onRewardedAdSuccess?.Invoke();
            }
            else
            {
                // 報酬が付与されなかった場合の処理
                Debug.Log("Reward was not granted. Calling failed callback.");
                onRewardedAdFailed?.Invoke();
            }

            // フラグとコールバックをクリア
            rewardGranted = false;
            onRewardedAdSuccess = null;
            onRewardedAdFailed = null;

            // 次の広告をロード
            LoadRewardedAd();
        };

        // 広告の表示に失敗したとき
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError($"Rewarded ad failed to show: {error}");

            onRewardedAdFailed?.Invoke();
            rewardGranted = false;
            onRewardedAdSuccess = null;
            onRewardedAdFailed = null;

            // 次の広告をロード
            LoadRewardedAd();
        };
    }

    #endregion

    #region Interstitial Ad

    /// <summary>
    /// インタースティシャル広告をロード
    /// </summary>
    private void LoadInterstitialAd()
    {
        // 既存の広告があれば破棄
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        // プラットフォームに応じた広告ユニットIDを選択
        string adUnitId;
#if UNITY_IOS
        adUnitId = useTestAds ? TEST_INTERSTITIAL_AD_UNIT_ID : iosInterstitialAdUnitId;
#elif UNITY_ANDROID
        adUnitId = useTestAds ? TEST_INTERSTITIAL_AD_UNIT_ID : androidInterstitialAdUnitId;
#else
        adUnitId = TEST_INTERSTITIAL_AD_UNIT_ID; // エディタ等ではテストIDを使用
#endif
        Debug.Log($"Loading interstitial ad with ID: {adUnitId}");

        // 広告リクエストを作成
        var request = new AdRequest();

        // インタースティシャル広告をロード
        InterstitialAd.Load(adUnitId, request, (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError($"Interstitial ad failed to load: {error}");
                return;
            }

            Debug.Log("Interstitial ad loaded successfully!");
            interstitialAd = ad;

            // イベントリスナーを登録
            RegisterInterstitialAdEvents(interstitialAd);
        });
    }

    /// <summary>
    /// インタースティシャル広告のイベントリスナーを登録
    /// </summary>
    private void RegisterInterstitialAdEvents(InterstitialAd ad)
    {
        // 広告が支払われたとき
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log($"Interstitial ad paid {adValue.Value} {adValue.CurrencyCode}");
        };

        // 広告が開かれたとき
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Interstitial ad full screen content opened");
        };

        // 広告が閉じられたとき
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Interstitial ad full screen content closed");

            // コールバックを呼ぶ
            onInterstitialAdClosed?.Invoke();
            onInterstitialAdClosed = null;

            // 次の広告をロード
            LoadInterstitialAd();
        };

        // 広告の表示に失敗したとき
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError($"Interstitial ad failed to show: {error}");

            // 失敗してもコールバックを呼ぶ（ゲームを続行できるように）
            onInterstitialAdClosed?.Invoke();
            onInterstitialAdClosed = null;

            // 次の広告をロード
            LoadInterstitialAd();
        };
    }

    #endregion


    #region Public Methods

    /// <summary>
    /// リワード広告を表示（コンティニュー・スキン解放用）
    /// </summary>
    /// <param name="onSuccess">広告視聴成功時のコールバック</param>
    /// <param name="onFailed">広告視聴失敗時のコールバック</param>
    public void ShowRewardedAd(Action onSuccess, Action onFailed = null)
    {

        if (!isInitialized)
        {
            Debug.LogWarning("AdMob SDK is not initialized yet.");
            onFailed?.Invoke();
            return;
        }

        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            onRewardedAdSuccess = onSuccess;
            onRewardedAdFailed = onFailed;
            rewardGranted = false; // フラグをリセット

            // 報酬付与時のコールバックを設定
            rewardedAd.Show((Reward reward) =>
            {
                Debug.Log($"Rewarded ad granted reward: {reward.Amount} {reward.Type}");

                // 報酬が付与されたことをフラグで記録（広告を閉じた時に成功コールバックを呼ぶ）
                rewardGranted = true;
            });
        }
        else
        {
            Debug.LogWarning("Rewarded ad is not ready yet.");
            onFailed?.Invoke();

            // 広告をロード
            LoadRewardedAd();
        }
    }

    /// <summary>
    /// インタースティシャル広告を表示（5回ごと）
    /// </summary>
    public void ShowInterstitialAd()
    {

        if (!isInitialized)
        {
            Debug.LogWarning("AdMob SDK is not initialized yet.");
            return;
        }

        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            Debug.Log("Showing interstitial ad");
            interstitialAd.Show();
        }
        else
        {
            Debug.LogWarning("Interstitial ad is not ready yet.");

            // 広告をロード
            LoadInterstitialAd();
        }

    }

    /// <summary>
    /// ゲームプレイ開始時に呼び出す（プレイ回数カウント）
    /// </summary>
    /// <param name="onComplete">広告表示完了後（または広告なしの場合は即座に）呼ばれるコールバック</param>
    public void OnGamePlayStart(Action onComplete = null)
    {
        // UGSCloudSaveManagerからtotalPlayCountを取得
        _cloudSaveManager = UGSCloudSaveManager.instance;

        if (_cloudSaveManager == null)
        {
            Debug.LogWarning("[AdsManager] UGSCloudSaveManager not found. Cannot check play count for ads.");
            onComplete?.Invoke();
            return;
        }

        // UGSのtotalPlayCountを取得（これから始まるプレイ分を含めて+1）
        int totalPlayCount = _cloudSaveManager.GetStats().totalPlayCount + 1;

        Debug.Log($"[AdsManager] Total Play Count (including this play): {totalPlayCount}, Ad interval: {adIntervalPlayCount}");

        // adIntervalPlayCountの倍数でインタースティシャル広告を表示
        if (totalPlayCount % adIntervalPlayCount == 0)
        {
            Debug.Log($"[AdsManager] Showing interstitial ad (total plays: {totalPlayCount})");
            ShowInterstitialAdWithCallback(onComplete);
        }
        else
        {
            Debug.Log($"[AdsManager] No ad this time. Next ad at play count: {(totalPlayCount / adIntervalPlayCount + 1) * adIntervalPlayCount}");
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// インタースティシャル広告を表示（コールバック付き）
    /// </summary>
    /// <param name="onClosed">広告が閉じられた後に呼ばれるコールバック</param>
    public void ShowInterstitialAdWithCallback(Action onClosed)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("AdMob SDK is not initialized yet.");
            onClosed?.Invoke();
            return;
        }

        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            Debug.Log("Showing interstitial ad with callback");
            onInterstitialAdClosed = onClosed;
            interstitialAd.Show();
        }
        else
        {
            Debug.LogWarning("Interstitial ad is not ready yet.");
            onClosed?.Invoke();

            // 広告をロード
            LoadInterstitialAd();
        }
    }

    /// <summary>
    /// リワード広告が利用可能かチェック
    /// </summary>
    public bool IsRewardedAdReady()
    {

        return isInitialized && rewardedAd != null && rewardedAd.CanShowAd();
    }

    #endregion

    #region Test Simulation

    /// <summary>
    /// SDK未インストール時の広告表示をシミュレート（テスト用）
    /// </summary>
    private IEnumerator SimulateAdDelay(Action onSuccess, Action onFailed)
    {
        Debug.Log("Simulating ad display... (2 seconds)");

        // Time.timeScale=0でも動くようにWaitForSecondsRealtimeを使用
        yield return new WaitForSecondsRealtime(2f);

        Debug.Log("Simulated ad completed. Granting reward.");
        onSuccess?.Invoke();
    }

    #endregion

    void OnDestroy()
    {

        // 広告を破棄
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
        }

        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
        }

    }
}
