using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField]private Button _continueButton;
    [SerializeField] private TextMeshProUGUI _scoreText;
    public static GameOverManager instance;
    private SkinManager _skinManager;
    private GameManager _gameManager;
    private ScoreManager _scoreManager;
    private StageManager _stageManager;
    private float _currentPlayTime;
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
        _continueButton.onClick.AddListener(ContinueGame);
    }
    public void GameOver()
    {
        _gameManager.SetGameState(GameManager.GameState.GameOver);
        _gameOverPanel.SetActive(true);
        _scoreText.SetText($"Score: {_scoreManager.getCurrentScore()}");
        Time.timeScale = 0;
        _skinManager.ReportGameResult(_scoreManager.getCurrentScore(), _stageManager.currentPlayTime);
        _stageManager.SetCurrentPlay(false);
    }
// ★追加: コンティニュー処理
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
