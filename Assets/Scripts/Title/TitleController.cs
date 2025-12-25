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
    private UGSCloudSaveManager _cloudSaveManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _rankingUIManager = RankingUIManager.instance;
        _startSkinManager = SkinInventryManager.instance;
        _sceneController = SceneController.instance;
        _playerNameManager = PlayerNameManager.instance;
        _nicknameInputUI = NicknameInputUI.instance;
        _cloudSaveManager = UGSCloudSaveManager.instance;

        // UGSデータロード完了後に名前入力チェック
        if (_cloudSaveManager != null)
        {
            _cloudSaveManager.OnDataLoaded += CheckAndShowFirstTimeNameInput;
        }
        else
        {
            // UGS未使用時は即座にチェック
            CheckAndShowFirstTimeNameInput();
        }

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
        Debug.Log($"CheckAndShowFirstTimeNameInput");

        // UGS使用時はCloudSaveから名前を取得
        string currentName = "";
        if (_cloudSaveManager != null)
        {
            currentName = _cloudSaveManager.GetPlayerName();
        }
        else if (_playerNameManager != null)
        {
            currentName = _playerNameManager.GetPlayerName();
        }

        // デフォルト名の場合は名前入力を促す
        if (string.IsNullOrEmpty(currentName) || currentName == "プレイヤー")
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
            Debug.Log($"Player name already set: {currentName}");
        }
    }

    /// <summary>
    /// 初回名前入力完了時の処理
    /// </summary>
    private async void OnFirstTimeNameSubmitted(string playerName)
    {
        Debug.Log($"プレイヤー名を設定します: {playerName}");

        // UGS使用時はCloudSaveに保存
        if (_cloudSaveManager != null)
        {
            bool success = await _cloudSaveManager.SetPlayerName(playerName);
            if (success)
            {
                Debug.Log($"プレイヤー名をUGSに保存しました: {playerName}");
            }
            else
            {
                Debug.LogWarning("Failed to save player name to UGS. Falling back to local save.");
                _playerNameManager?.SavePlayerName(playerName);
            }
        }
        else
        {
            // UGS未使用時はPlayerPrefsに保存
            _playerNameManager?.SavePlayerName(playerName);
            Debug.Log($"プレイヤー名をローカルに保存しました: {playerName}");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
