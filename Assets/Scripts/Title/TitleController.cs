using UnityEngine;
using UnityEngine.UI;

public class TitleController : MonoBehaviour
{
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _skinButton;
    [SerializeField] private Button _rankingButton;
    [SerializeField] private FlowUI _flowUI;

    private SkinInventryManager _startSkinManager;
    private RankingUIManager _rankingUIManager;
    private SceneController _sceneController;
    private PlayerNameManager _playerNameManager;
    private NicknameInputUI _nicknameInputUI;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _rankingUIManager = RankingUIManager.instance;
        _startSkinManager = SkinInventryManager.instance;
        _sceneController = SceneController.instance;
        _playerNameManager = PlayerNameManager.instance;
        _nicknameInputUI = NicknameInputUI.instance;

        // 初回起動時に名前入力を促す
        CheckAndShowFirstTimeNameInput();

        // _startButton.onClick.AddListener();
        _skinButton.onClick.AddListener(() =>
        {
            _flowUI.SwitchView("Skin");
            _startSkinManager.ApplySkinSprites();
        });
        _rankingButton.onClick.AddListener(() =>
        {
            _rankingUIManager.ShowRanking();
        });
        _startButton.onClick.AddListener(() =>
        {
            _sceneController.SceneLoad("Main");
        });
    }

    /// <summary>
    /// 初回起動時に名前入力画面を表示
    /// </summary>
    private void CheckAndShowFirstTimeNameInput()
    {
        Debug.Log($"CheckAndShowFirstTimeNameInput: _playerNameManager={_playerNameManager}, _nicknameInputUI={_nicknameInputUI}");

        if (!_playerNameManager.HasPlayerName())
        {
            Debug.Log("Player name not set, showing input panel");
            // 名前が未設定なら入力画面を表示
            _nicknameInputUI.ShowPanel(
                "ようこそ！\nあなたの名前を入力してください",
                OnFirstTimeNameSubmitted,
                null // キャンセルなし
            );
        }
        else
        {
            Debug.Log("Player name already set");
        }
    }

    /// <summary>
    /// 初回名前入力完了時の処理
    /// </summary>
    private void OnFirstTimeNameSubmitted(string playerName)
    {
        _playerNameManager.SavePlayerName(playerName);
        Debug.Log($"プレイヤー名を設定しました: {playerName}");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
