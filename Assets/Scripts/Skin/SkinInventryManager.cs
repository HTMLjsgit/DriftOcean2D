using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SkinInventryManager : MonoBehaviour
{
    public static SkinInventryManager instance;

    [Header("UI References")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private FlowUI _flowUI;

    [Header("Skin List Settings")]
    [SerializeField] private List<SkinUI> _skinSlots;

    private UGSCloudSaveManager _cloudSaveManager;
    private bool _isInitialized = false;

    void Awake()
    {
        if(instance == null) instance = this;
        else Destroy(this.gameObject);
    }

    void Start()
    {
        _closeButton.onClick.AddListener(() => _flowUI.SwitchView("Start"));
        // UGSCloudSaveManagerのデータロード完了を待つ
        _cloudSaveManager = UGSCloudSaveManager.instance;
        // OnDataLoadedイベントに登録
        _cloudSaveManager.OnDataLoaded += OnUGSDataLoaded;

        // 既にロード済みの場合は即座に初期化
        if (_cloudSaveManager.IsDataLoaded)
        {
            Debug.Log("[SkinInventryManager] UGS data already loaded, initializing immediately");
            OnUGSDataLoaded();
        }
        else
        {
            Debug.Log("[SkinInventryManager] Waiting for UGS data to load...");
        }

    }

    void OnDestroy()
    {
        // イベントリスナーを解除
        if (_cloudSaveManager != null)
        {
            _cloudSaveManager.OnDataLoaded -= OnUGSDataLoaded;
        }
    }

    /// <summary>
    /// UGSデータロード完了時の処理
    /// </summary>
    private void OnUGSDataLoaded()
    {
        Debug.Log("[SkinInventryManager] UGS data loaded, refreshing skin UI");
        _isInitialized = true;

        // スキンUIを更新
        RefreshAllSlots();
    }

    public void ApplySkinSprites()
    {
        Debug.Log("[SkinInventryManager] ApplySkinSprites called");

        // 最新のUGSCloudSaveManagerインスタンスを取得
        _cloudSaveManager = UGSCloudSaveManager.instance;

        // スキンスプライトを初期化
        foreach (var slot in _skinSlots)
        {
            Debug.Log("skinDatabase: " + SkinDatabase.instance);
            Debug.Log("slot: "+ slot);
            var data = SkinDatabase.instance.GetSkinById(slot.skinId);
            slot.Initialize(data.skinSprite);
        }

        // ロック状態を更新（データロード状態に関わらず実行）
        Debug.Log($"[SkinInventryManager] Refreshing lock states. IsDataLoaded={_cloudSaveManager?.IsDataLoaded}, _isInitialized={_isInitialized}");
        RefreshAllSlots();
    }

    public void RefreshAllSlots()
    {
        Debug.Log("[SkinInventryManager] RefreshAllSlots called");
        _skinSlots.ForEach(slot => slot.UpdateUIState());
    }
}