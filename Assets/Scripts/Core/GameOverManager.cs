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

    public static GameOverManager instance;
    private SkinManager _skinManager;
    private GameManager _gameManager;
    private ScoreManager _scoreManager;
    private StageManager _stageManager;
    private RankingManager _rankingManager;
    private NicknameInputUI _nicknameInputUI;
    private PlayerNameManager _playerNameManager;

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

        Debug.Log("OnStart _rankingManager: " + _rankingManager);

        _continueButton.onClick.AddListener(ContinueGame);
        _backToTitleButton.onClick.AddListener(() =>
        {
            Time.timeScale = 1;
            SceneController.instance.SceneLoad("Title");
        });

        // ランキング確認ボタン
        _viewRankingButton.onClick.AddListener(OnViewRankingClicked);
    }
    public void GameOver()
    {
        _gameManager.SetGameState(GameManager.GameState.GameOver);
        _gameOverPanel.SetActive(true);

        float finalScore = _scoreManager.getCurrentScore();
        float finalTime = _stageManager.currentPlayTime;

        _scoreText.SetText($"Score: {finalScore.ToString("F2")}");
        Time.timeScale = 0;

        // スキン解放チェック
        _skinManager.ReportGameResult(finalScore, finalTime);
        _stageManager.SetCurrentPlay(false);

        // ランキング圏内チェック
        CheckAndShowRankingInput(finalScore);
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
    /// コンティニュー処理
    /// </summary>
    private void ContinueGame()
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
}
