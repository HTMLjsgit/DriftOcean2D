using UnityEngine;
using System.Threading.Tasks;

/// <summary>
/// SNSシェア機能を管理するクラス（Social Connector使用）
/// スコアをTwitter/Threads/LINE/Instagramなどにシェアし、スキン解放条件を達成
/// </summary>
public class SNSShareManager : MonoBehaviour
{
    public static SNSShareManager instance;

    [Header("Share Settings")]
    [SerializeField] private string _gameTitle = "Drift Ocean";
    [SerializeField] private string _hashtag = "DriftOcean";
    [SerializeField] private string _appURL = ""; // アプリのストアURL（あれば）
    [SerializeField] private bool _includeScreenshot = false; // スクリーンショット付きでシェア

    private SkinManager _skinManager;
    private string _screenshotPath;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        _skinManager = SkinManager.instance;
        _screenshotPath = Application.persistentDataPath + "/share_screenshot.png";
    }

    /// <summary>
    /// スコアをSNSにシェアする（Social Connector使用）
    /// Twitter, Threads, LINE, Instagram, Facebookなど選択可能
    /// </summary>
    /// <param name="score">シェアするスコア</param>
    public async void ShareScore(float score)
    {
        string shareText = CreateShareText(score);

#if UNITY_ANDROID || UNITY_IOS
        // Android/iOS: Social Connectorでネイティブシェア
        if (_includeScreenshot)
        {
            // スクリーンショット撮影
            ScreenCapture.CaptureScreenshot("share_screenshot.png");
            await Task.Delay(500); // 保存待ち

            // Social Connectorでシェア（画像付き）
            SocialConnector.SocialConnector.Share(shareText, _appURL, _screenshotPath);
        }
        else
        {
            // Social Connectorでシェア（テキストのみ）
            SocialConnector.SocialConnector.Share(shareText, _appURL, null);
        }
        Debug.Log($"SNS share dialog opened with text: {shareText}");
#else
        // Unity Editor / Windows: テスト用フォールバック
        Debug.LogWarning("Social Connector is only available on Android/iOS.");
        Debug.Log($"[TEST MODE] Share Text: {shareText}");
        Debug.Log($"[TEST MODE] App URL: {_appURL}");

        // クリップボードにコピー
        GUIUtility.systemCopyBuffer = shareText + (!string.IsNullOrEmpty(_appURL) ? "\n" + _appURL : "");
        Debug.Log("[TEST MODE] Share text copied to clipboard!");

        // Twitter URLを開く（テスト用）
        string encodedText = UnityEngine.Networking.UnityWebRequest.EscapeURL(shareText);
        string twitterUrl = $"https://twitter.com/intent/tweet?text={encodedText}";
        Application.OpenURL(twitterUrl);
        Debug.Log($"[TEST MODE] Opening Twitter: {twitterUrl}");
#endif

        // スキン解放処理（テスト環境でも動作確認できる）
        await _skinManager.UnlockBySNSShare();
        Debug.Log("SNS share completed - skin unlock triggered");
    }

    /// <summary>
    /// シェアテキストを作成
    /// </summary>
    private string CreateShareText(float score)
    {
        string text = $"{_gameTitle}で{score:F2}点を獲得しました！";

        if (!string.IsNullOrEmpty(_hashtag))
        {
            text += $" #{_hashtag}";
        }

        return text;
    }

    /// <summary>
    /// スクリーンショット付きシェアのON/OFF
    /// </summary>
    public void SetIncludeScreenshot(bool include)
    {
        _includeScreenshot = include;
    }
}
