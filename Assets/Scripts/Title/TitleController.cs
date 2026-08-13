using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TitleController : MonoBehaviour
{
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _skinButton;
    [SerializeField] private Button _rankingButton;
    [SerializeField] private FlowUI _flowUI;
    [SerializeField] private GameObject _offlinePanel;
    [SerializeField] private TextMeshProUGUI _skinButtonNewLabel; // Skinボタンの「New」ラベル

    private SkinInventryManager _startSkinManager;
    private RankingUIManager _rankingUIManager;
    private SceneController _sceneController;
    private UGSCloudSaveManager _cloudSaveManager;
    private SkinDatabase _skinDatabase;

    void OnDestroy()
    {
        // イベントリスナーを解除
        if (_cloudSaveManager != null)
        {
            _cloudSaveManager.OnDataLoaded -= UpdateSkinButtonNewLabel;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _rankingUIManager = RankingUIManager.instance;
        _startSkinManager = SkinInventryManager.instance;
        _sceneController = SceneController.instance;
        _cloudSaveManager = UGSCloudSaveManager.instance;
        _skinDatabase = SkinDatabase.instance;
        UpdateOfflinePanel();

        _cloudSaveManager.OnDataLoaded += UpdateSkinButtonNewLabel;

        // 既にロード済みの場合は即座に更新
        if (_cloudSaveManager.IsDataLoaded)
        {
            UpdateSkinButtonNewLabel();
        }

        // _startButton.onClick.AddListener();
        _skinButton.onClick.AddListener(async () =>
        {
            // Skinビューを開く前に最新データを再ロード
            if (_cloudSaveManager.IsDataLoaded)
            {
                Debug.Log("[TitleController] Reloading data before showing skins...");
                await _cloudSaveManager.ReloadPlayerData();
            }

            _flowUI.SwitchView("Skin");
            _startSkinManager.ApplySkinSprites();

            // スキン一覧を見たとしてマーク（SkinボタンのNewラベルを消す）
            await _cloudSaveManager.MarkSkinInventoryAsViewed();

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
        UpdateOfflinePanel();
    }

    private void UpdateOfflinePanel()
    {
        if (_offlinePanel == null)
        {
            return;
        }

        _cloudSaveManager = UGSCloudSaveManager.instance;
        bool isOffline = _cloudSaveManager != null && _cloudSaveManager.IsOfflineModeActive();
        _offlinePanel.SetActive(isOffline);
    }
}
