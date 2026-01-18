using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField]private Button _continueButton;
    [SerializeField]private Button _backToTitleButton;
    [SerializeField] private TextMeshProUGUI _scoreText;

    [Header("Ranking System")]
    [SerializeField] private Button _rankingButton;
    [Header("SNS Share")]
    [SerializeField] private Button _shareButton; // SNSシェアボタン
    [SerializeField] private Button _retryButton;
    public static GameOverManager instance;
    private SkinManager _skinManager;
    private GameManager _gameManager;
    private ScoreManager _scoreManager;
    private StageManager _stageManager;
    private RankingManager _rankingManager;
    private AdsManager _adsManager;
    private UGSCloudSaveManager _cloudSaveManager;
    private UGSLeaderboardManager _leaderboardManager;
    private PlayerController _playerController;

    // コンティニュー制限
    private const string KEY_CONTINUE_USED = "ContinueUsed";
    private bool _hasUsedContinue = false;

    // 外部からアクセス用
    public bool HasUsedContinue => _hasUsedContinue;

    void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
    void Start()
    {
        _gameManager = GameManager.instance;
        _skinManager = SkinManager.instance;
        _scoreManager = ScoreManager.instance;
        _stageManager = StageManager.instance;
        _rankingManager = RankingManager.instance;
        _adsManager = AdsManager.instance;
        _cloudSaveManager = UGSCloudSaveManager.instance;
        _leaderboardManager = UGSLeaderboardManager.instance;
        _playerController = PlayerController.instance;

        Debug.Log("OnStart _rankingManager: " + _rankingManager);

        _continueButton.onClick.AddListener(OnContinueButtonClicked);
        _backToTitleButton.onClick.AddListener(() =>
        {
            Debug.Log("[GameOverManager] BackToTitle button clicked");
            // Time.timeScaleはSceneControllerで適切なタイミングで設定される
            Debug.Log("[GameOverManager] Calling SceneController.SceneLoad(Title)");
            SceneController.instance.SceneLoad("Title");
        });
        _retryButton.onClick.AddListener(OnRetryButtonClicked);
        // ランキングボタン
        _rankingButton.onClick.AddListener(OnViewRankingClicked);

        // SNSシェアボタン
        _shareButton.onClick.AddListener(OnShareButtonClicked);
    }

    public async void GameOver()
    {
        // 既にGameOver状態の場合は処理をスキップ（多重呼び出し防止）
        if (_gameManager.state == GameManager.GameState.GameOver)
        {
            Debug.LogWarning("[GameOverManager] GameOver already in progress, skipping duplicate call");
            return;
        }

        _gameManager.SetGameState(GameManager.GameState.GameOver);
        _gameOverPanel.SetActive(true);

        // コンティニューボタンの表示/非表示を即座に更新（非同期処理を待たずに）
        UpdateContinueButton();

        float finalScore = _scoreManager.getCurrentScore();
        float finalTime = _stageManager.currentPlayTime;

        _scoreText.SetText($"{finalScore.ToString("F2")}");
        Time.timeScale = 0;

        try
        {
            // UGSに統計データを保存（スキン解放チェックの前に実行）
            await _cloudSaveManager.UpdateStats(finalScore, finalTime);
            Debug.Log("Stats updated to UGS Cloud Save");

            // 無操作条件の達成チェック
            bool noInputAchieved =  _playerController.NoInputUnlockAchieved;

            // スキン解放チェック（更新された統計データを使用）
            await _skinManager.ReportGameResult(finalScore, finalTime, noInputAchieved, _hasUsedContinue);
            _stageManager.SetCurrentPlay(false);

            // UGSリーダーボードにスコアを送信
            bool success = await _leaderboardManager.SubmitScore(finalScore);
            if (success)
            {
                Debug.Log($"Score submitted to UGS Leaderboard: {finalScore}");
            }

            // コンティニューボタンの表示/非表示を更新
            UpdateContinueButton();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameOverManager] Error during GameOver processing: {e.Message}\n{e.StackTrace}");
            // エラーが発生してもUIは更新する
            UpdateContinueButton();
        }
    }

    /// <summary>
    /// ランキング確認ボタンがクリックされたときの処理
    /// </summary>
    private void OnViewRankingClicked()
    {
        // ゲームシーン内でランキングを表示
        RankingUIManager.instance.ShowRanking();
        Debug.Log("ランキング画面を表示します");
    }

    /// <summary>
    /// SNSシェアボタンがクリックされたときの処理
    /// </summary>
    private void OnShareButtonClicked()
    {
        float currentScore = _scoreManager.getCurrentScore();
        SNSShareManager.instance.ShareScore(currentScore);
    }

    /// <summary>
    /// コンティニューボタンクリック時の処理
    /// </summary>
    private void OnContinueButtonClicked()
    {
        // 広告を視聴してコンティニュー
        _adsManager.ShowRewardedAd(
            onSuccess: () =>
            {
                // 広告視聴成功 - コンティニュー実行
                Debug.Log("Rewarded ad success - Continue game");
                _hasUsedContinue = true;

                // コンティニューボタンを即座に非表示
                _continueButton.gameObject.SetActive(false);

                ExecuteContinue();
            },
            onFailed: () =>
            {
                // 広告視聴失敗 - エラーメッセージ表示
                Debug.LogWarning("Rewarded ad failed - Cannot continue");
                // TODO: ユーザーに広告が利用できないことを通知
            }
        );
    }

    /// <summary>
    /// コンティニュー処理を実行
    /// </summary>
    private void ExecuteContinue()
    {
        // 1. 時間を再開させる
        Time.timeScale = 1;

        // 2. ゲームオーバー画面を隠す
        _gameOverPanel.SetActive(false);

        // 3. ゲームステートをプレイ中に戻す
        _gameManager.SetGameState(GameManager.GameState.Playing);

        // 4. ステージ進行を再開（スコアは維持）
        _stageManager.StageResume();
    }

    /// <summary>
    /// コンティニューボタンの表示/非表示を更新
    /// </summary>
    private void UpdateContinueButton()
    {
        // シーン遷移中にボタンが破棄されている可能性があるのでnullチェック
        if (_continueButton == null || _continueButton.gameObject == null)
        {
            return;
        }

        // 1回のプレイで1回のみコンティニュー可能
        if (_hasUsedContinue)
        {
            _continueButton.gameObject.SetActive(false);
        }
        else
        {
            // 広告が利用可能かチェック
            bool adReady = _adsManager.IsRewardedAdReady();
            _continueButton.gameObject.SetActive(adReady || _adsManager == null); // テスト用：AdsManagerがない場合は表示

            if (!adReady)
            {
                Debug.LogWarning("Rewarded ad is not ready. Continue button hidden.");
            }
        }
    }

    /// <summary>
    /// リトライボタンクリック時の処理
    /// 完全に最初からやり直し（プレイ回数としてカウント）
    /// </summary>
    private void OnRetryButtonClicked()
    {
        Debug.Log("[GameOverManager] Retry button clicked");

        // 1. コンティニューフラグをリセット（新しいプレイなので）
        _hasUsedContinue = false;

        // 2. ゲームオーバー画面を閉じる
        _gameOverPanel.SetActive(false);

        // 3. ゲーム状態をプレイ中に戻す
        _gameManager.SetGameState(GameManager.GameState.Playing);

        // 4. PlayerControllerの状態をリセット
        _playerController.ResetForRetry();

        // 5. リトライ処理を実行（StageManager側で広告チェック→timeScale=1→ゲーム開始）
        _stageManager.StageRetry();
    }
}
