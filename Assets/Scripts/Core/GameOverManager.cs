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
    [SerializeField] private Button _viewRankingButton; // ランキング確認ボタン

    [Header("SNS Share")]
    [SerializeField] private Button _shareButton; // SNSシェアボタン

    public static GameOverManager instance;
    private SkinManager _skinManager;
    private GameManager _gameManager;
    private ScoreManager _scoreManager;
    private StageManager _stageManager;
    private RankingManager _rankingManager;
    private NicknameInputUI _nicknameInputUI;
    private PlayerNameManager _playerNameManager;
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
        _nicknameInputUI = NicknameInputUI.instance;
        _playerNameManager = PlayerNameManager.instance;
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

        // ランキング確認ボタン
        _viewRankingButton.onClick.AddListener(OnViewRankingClicked);

        // SNSシェアボタン
        if (_shareButton != null)
        {
            _shareButton.onClick.AddListener(OnShareButtonClicked);
        }
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

        // コンティニュー済みの場合は即座にボタンを非表示
        if (_hasUsedContinue && _continueButton != null && _continueButton.gameObject != null)
        {
            _continueButton.gameObject.SetActive(false);
        }

        float finalScore = _scoreManager.getCurrentScore();
        float finalTime = _stageManager.currentPlayTime;

        _scoreText.SetText($"Score: {finalScore.ToString("F2")}");
        Time.timeScale = 0;

        try
        {
            // UGSに統計データを保存（スキン解放チェックの前に実行）
            await _cloudSaveManager.UpdateStats(finalScore, finalTime);
            Debug.Log("Stats updated to UGS Cloud Save");

            // 無操作条件の達成チェック
            bool noInputAchieved = _playerController != null && _playerController.NoInputUnlockAchieved;

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
    /// ランキング圏内かチェックし、自動登録
    /// </summary>
    private void CheckAndShowRankingInput(float score)
    {
        Debug.Log("_rankingManager:" + _rankingManager);
        if (_rankingManager.IsRankingEligible(score))
        {
            // ランキング入り！現在のプレイヤー名で自動登録
            string playerName = _playerNameManager.GetPlayerName();
            int currentSkinID = _skinManager.currentSkinID;
            bool success = _rankingManager.TryAddScore(playerName, score, currentSkinID);

            if (success)
            {
                Debug.Log($"ランキング登録成功: {playerName} - {score}");
            }
            else
            {
                Debug.LogWarning("ランキング登録に失敗しました。");
            }
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

        if (SNSShareManager.instance != null)
        {
            SNSShareManager.instance.ShareScore(currentScore);
        }
        else
        {
            Debug.LogWarning("SNSShareManager not found. Cannot share.");
        }
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
                if (_continueButton != null && _continueButton.gameObject != null)
                {
                    _continueButton.gameObject.SetActive(false);
                }

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
            bool adReady = _adsManager != null && _adsManager.IsRewardedAdReady();
            _continueButton.gameObject.SetActive(adReady || _adsManager == null); // テスト用：AdsManagerがない場合は表示

            if (!adReady && _adsManager != null)
            {
                Debug.LogWarning("Rewarded ad is not ready. Continue button hidden.");
            }
        }
    }
}
