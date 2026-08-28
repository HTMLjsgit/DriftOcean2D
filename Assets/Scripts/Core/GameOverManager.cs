using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private Button _continueButton;
    [SerializeField] private TextMeshProUGUI _noAddText;
    [SerializeField] private Button _backToTitleButton;
    [SerializeField] private TextMeshProUGUI _scoreText;

    [Header("Result Summary")]
    [SerializeField] private TextMeshProUGUI _resultCommentText;
    [SerializeField] private GameObject _unlockNoticePanel;
    [SerializeField] private TextMeshProUGUI _unlockNoticeText;

    [Header("Ranking System")]
    [SerializeField] private Button _rankingButton;
    [SerializeField] private Button _oceanLogButton;
    [SerializeField] private OceanLogUI _oceanLogUI;

    [Header("SNS Share")]
    [SerializeField] private Button _shareButton;
    [SerializeField] private Button _retryButton;

    public static GameOverManager instance;

    private SkinManager _skinManager;
    private GameManager _gameManager;
    private ScoreManager _scoreManager;
    private StageManager _stageManager;
    private AdsManager _adsManager;
    private UGSCloudSaveManager _cloudSaveManager;
    private UGSLeaderboardManager _leaderboardManager;
    private PlayerController _playerController;

    private bool _hasUsedContinue;

    public bool HasUsedContinue => _hasUsedContinue;

    void Awake()
    {
        if (_gameOverPanel != null)
        {
            _gameOverPanel.SetActive(false);
        }

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
        _gameManager = GameManager.instance;
        _skinManager = SkinManager.instance;
        _scoreManager = ScoreManager.instance;
        _stageManager = StageManager.instance;
        _adsManager = AdsManager.instance;
        _cloudSaveManager = UGSCloudSaveManager.instance;
        _leaderboardManager = UGSLeaderboardManager.instance;
        _playerController = PlayerController.instance;

        if (_continueButton != null)
        {
            _continueButton.onClick.AddListener(OnContinueButtonClicked);
        }

        if (_backToTitleButton != null)
        {
            _backToTitleButton.onClick.AddListener(() => SceneController.instance.SceneLoad("Title"));
        }

        if (_retryButton != null)
        {
            _retryButton.onClick.AddListener(OnRetryButtonClicked);
        }

        if (_rankingButton != null)
        {
            _rankingButton.onClick.AddListener(OnViewRankingClicked);
        }

        _oceanLogButton.onClick.AddListener(_oceanLogUI.Open);

        if (_shareButton != null)
        {
            _shareButton.onClick.AddListener(OnShareButtonClicked);
        }
    }

    public async void GameOver()
    {
        if (_gameManager != null && _gameManager.state == GameManager.GameState.GameOver)
        {
            Debug.LogWarning("[GameOverManager] GameOver already in progress.");
            return;
        }

        if (_gameManager != null)
        {
            _gameManager.SetGameState(GameManager.GameState.GameOver);
        }

        if (_gameOverPanel != null)
        {
            _gameOverPanel.SetActive(true);
        }

        UpdateContinueButton();

        float finalScore = _scoreManager != null ? _scoreManager.getCurrentScore() : 0f;
        float finalTime = _stageManager != null ? _stageManager.currentPlayTime : 0f;

        if (_scoreText != null)
        {
            _scoreText.SetText(finalScore.ToString("F2"));
        }

        int resultYear = Mathf.FloorToInt(finalScore);
        string resultComment = AchievementManager.instance.GetResultComment(finalScore);
        
        _resultCommentText.SetText(
        Application.systemLanguage == SystemLanguage.Japanese
        ? $"{resultYear}年まで漂った！\n{resultComment}"
        : $"{resultYear} years drifted!\n{resultComment}"
        );

        _unlockNoticePanel.SetActive(false);

        Time.timeScale = 0f;

        if (_cloudSaveManager != null && _cloudSaveManager.IsOfflineModeActive())
        {
            _cloudSaveManager.RecordOfflineBestScore(finalScore);

            if (_stageManager != null)
            {
                _stageManager.SetCurrentPlay(false);
            }

            UpdateContinueButton();
            return;
        }

        List<int> previousSkinIDs = _cloudSaveManager.GetUnlockedSkinIDs().ToList();
        List<int> previousAchievementIDs = _cloudSaveManager.GetUnlockedAchievementIDs();
        int bounceCount = _playerController.ConsumePendingBounceCount();
        bool tapUnlockAchieved = _playerController.ConsumeTapUnlockCondition();
        int collidedObstacleID = _playerController.ConsumeCollidedObstacleID();
        bool reachedHardMode = DifficultyManager.instance.maxDifficultyMode;

        if (_cloudSaveManager != null)
        {
            await _cloudSaveManager.UpdateStats(finalScore, finalTime, bounceCount, reachedHardMode);
            await _cloudSaveManager.DiscoverObstacle(collidedObstacleID);
        }

        bool noInputAchieved = _playerController != null && _playerController.NoInputUnlockAchieved;

        if (_skinManager != null)
        {
            await _skinManager.ReportGameResult(
                finalScore,
                finalTime,
                noInputAchieved,
                _hasUsedContinue,
                tapUnlockAchieved);
        }

        await AchievementManager.instance.EvaluateAchievements();

        ShowUnlockNotices(
            previousSkinIDs,
            previousAchievementIDs);

        if (_stageManager != null)
        {
            _stageManager.SetCurrentPlay(false);
        }

        if (_leaderboardManager != null)
        {
            await _leaderboardManager.SubmitScore(finalScore);
        }

        UpdateContinueButton();
    }

    private void ShowUnlockNotices(
        List<int> previousSkinIDs,
        List<int> previousAchievementIDs)
    {
        List<int> newSkinIDs = _cloudSaveManager.GetUnlockedSkinIDs()
            .Where(id => !previousSkinIDs.Contains(id))
            .ToList();
        List<int> newAchievementIDs = _cloudSaveManager.GetUnlockedAchievementIDs()
            .Where(id => !previousAchievementIDs.Contains(id))
            .ToList();
        List<string> messages = new List<string>();
        if (newAchievementIDs.Count > 0)
        {
            messages.Add("Achievement Unlocked!");
        }

        if (newSkinIDs.Count > 0)
        {
            messages.Add("Skin Unlocked!");
        }

        if (messages.Count > 0)
        {
            _unlockNoticeText.SetText(string.Join("\n", messages));
            _unlockNoticePanel.SetActive(true);
        }
    }

    private void OnViewRankingClicked()
    {
        if (RankingUIManager.instance != null)
        {
            RankingUIManager.instance.ShowRanking();
        }
    }

    private void OnShareButtonClicked()
    {
        if (SNSShareManager.instance != null && _scoreManager != null)
        {
            SNSShareManager.instance.ShareScore(_scoreManager.getCurrentScore());
        }
    }

    private void OnContinueButtonClicked()
    {
        if (_adsManager == null)
        {
            ExecuteContinue();
            return;
        }

        _adsManager.ShowRewardedAd(
            onSuccess: () =>
            {
                _hasUsedContinue = true;

                if (_noAddText != null)
                {
                    _noAddText.gameObject.SetActive(false);
                }

                if (_continueButton != null)
                {
                    _continueButton.gameObject.SetActive(false);
                }

                ExecuteContinue();
            },
            onFailed: () =>
            {
                if (_noAddText != null)
                {
                    _noAddText.gameObject.SetActive(true);
                }

                ExecuteContinue();
            }
        );
    }

    private void ExecuteContinue()
    {
        Time.timeScale = 1f;

        if (_gameOverPanel != null)
        {
            _gameOverPanel.SetActive(false);
        }

        if (_gameManager != null)
        {
            _gameManager.SetGameState(GameManager.GameState.Playing);
        }

        if (_stageManager != null)
        {
            _stageManager.StageResume();
        }
    }

    private void UpdateContinueButton()
    {
        if (_continueButton == null)
        {
            return;
        }

        if (_hasUsedContinue)
        {
            _continueButton.gameObject.SetActive(false);
            return;
        }

        bool adReady = _adsManager != null && _adsManager.IsRewardedAdReady();
        _continueButton.gameObject.SetActive(true);

        if (!adReady)
        {
            Debug.LogWarning("Rewarded ad is not ready.");
        }
    }

    private void OnRetryButtonClicked()
    {
        _hasUsedContinue = false;

        if (_gameOverPanel != null)
        {
            _gameOverPanel.SetActive(false);
        }

        if (_gameManager != null)
        {
            _gameManager.SetGameState(GameManager.GameState.Playing);
        }

        if (_playerController != null)
        {
            _playerController.ResetForRetry();
        }

        if (_stageManager != null)
        {
            _stageManager.StageRetry();
        }
    }
}
