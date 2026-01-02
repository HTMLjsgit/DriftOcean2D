using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TitleController : MonoBehaviour
{
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _skinButton;
    [SerializeField] private Button _rankingButton;
    [SerializeField] private FlowUI _flowUI;
    [SerializeField] private TextMeshProUGUI _skinButtonNewLabel; // Skinボタンの「New」ラベル

    private SkinInventryManager _startSkinManager;
    private RankingUIManager _rankingUIManager;
    private SceneController _sceneController;
    private PlayerNameManager _playerNameManager;
    private NicknameInputUI _nicknameInputUI;
    private UGSCloudSaveManager _cloudSaveManager;
    private SkinDatabase _skinDatabase;

    void OnDestroy()
    {
        // イベントリスナーを解除
        if (_cloudSaveManager != null)
        {
            _cloudSaveManager.OnDataLoaded -= CheckAndShowFirstTimeNameInput;
            _cloudSaveManager.OnDataLoaded -= UpdateSkinButtonNewLabel;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _rankingUIManager = RankingUIManager.instance;
        _startSkinManager = SkinInventryManager.instance;
        _sceneController = SceneController.instance;
        _playerNameManager = PlayerNameManager.instance;
        _nicknameInputUI = NicknameInputUI.instance;
        _cloudSaveManager = UGSCloudSaveManager.instance;
        _skinDatabase = SkinDatabase.instance;

        // UGSデータロード完了後に名前入力チェック
        if (_cloudSaveManager != null)
        {
            _cloudSaveManager.OnDataLoaded += CheckAndShowFirstTimeNameInput;
            _cloudSaveManager.OnDataLoaded += UpdateSkinButtonNewLabel;

            // 既にロード済みの場合は即座に更新
            if (_cloudSaveManager.IsDataLoaded)
            {
                UpdateSkinButtonNewLabel();
            }
        }
        else
        {
            // UGS未使用時は即座にチェック
            CheckAndShowFirstTimeNameInput();
        }

        // _startButton.onClick.AddListener();
        _skinButton.onClick.AddListener(async () =>
        {
            // Skinビューを開く前に最新データを再ロード
            if (_cloudSaveManager != null && _cloudSaveManager.IsDataLoaded)
            {
                Debug.Log("[TitleController] Reloading data before showing skins...");
                await _cloudSaveManager.ReloadPlayerData();
            }

            _flowUI.SwitchView("Skin");
            _startSkinManager.ApplySkinSprites();

            // スキン一覧を見たとしてマーク（SkinボタンのNewラベルを消す）
            if (_cloudSaveManager != null)
            {
                await _cloudSaveManager.MarkSkinInventoryAsViewed();
            }

            // Newラベルを更新
            UpdateSkinButtonNewLabel();
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

        if (string.IsNullOrEmpty(currentName))
        {
            Debug.Log("Player name not set, showing input panel");
            // 名前が未設定なら入力画面を表示
            _nicknameInputUI.ShowPanel(
                "What's your name",
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

    /// <summary>
    /// Skinボタンの「New」ラベルを更新
    /// </summary>
    private void UpdateSkinButtonNewLabel()
    {
        if (_skinButtonNewLabel == null)
        {
            return;
        }

        // 新しいスキンが1つでもあればNewラベルを表示
        bool hasNewSkin = HasAnyNewSkin();
        _skinButtonNewLabel.gameObject.SetActive(hasNewSkin);

        Debug.Log($"[TitleController] Skin button New label updated: {hasNewSkin}");
    }

    /// <summary>
    /// 新しいスキンが1つでもあるかチェック（SkinボタンのNewラベル用）
    /// スキン一覧を開いたら消える
    /// </summary>
    private bool HasAnyNewSkin()
    {
        if (_cloudSaveManager == null)
        {
            return false;
        }

        // スキン一覧で未確認の解放済みスキンがあるかチェック
        return _cloudSaveManager.HasNewSkinsInInventory();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
