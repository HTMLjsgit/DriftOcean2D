using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkinInventryManager : MonoBehaviour
{
    public static SkinInventryManager instance;

    [Header("UI References")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private FlowUI _flowUI;

    [Header("Skin List Settings")]
    [SerializeField] private List<SkinUI> _skinSlots;

    [Header("Offline Mode")]
    [SerializeField] private GameObject _offlinePanel;

    private UGSCloudSaveManager _cloudSaveManager;
    private bool _isInitialized;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        UpdateOfflinePanel();
    }

    void Update()
    {
        UpdateOfflinePanel();
    }

    void Start()
    {
        if (_closeButton != null && _flowUI != null)
        {
            _closeButton.onClick.AddListener(() => _flowUI.SwitchView("Start"));
        }

        _cloudSaveManager = UGSCloudSaveManager.instance;

        if (_cloudSaveManager != null)
        {
            _cloudSaveManager.OnDataLoaded += OnUGSDataLoaded;

            if (_cloudSaveManager.IsDataLoaded)
            {
                OnUGSDataLoaded();
            }
        }
    }

    void OnDestroy()
    {
        if (_cloudSaveManager != null)
        {
            _cloudSaveManager.OnDataLoaded -= OnUGSDataLoaded;
        }
    }

    private void OnUGSDataLoaded()
    {
        _isInitialized = true;
        RefreshAllSlots();
        UpdateOfflinePanel();
    }

    public void ApplySkinSprites()
    {
        _cloudSaveManager = UGSCloudSaveManager.instance;

        foreach (SkinUI slot in _skinSlots)
        {
            SkinData data = SkinDatabase.instance.GetSkinById(slot.skinId);
            if (data != null)
            {
                slot.Initialize(data.skinSprite);
            }
        }

        RefreshAllSlots();
    }

    public void RefreshAllSlots()
    {
        foreach (SkinUI slot in _skinSlots)
        {
            slot.UpdateUIState();
        }

        if (!_isInitialized)
        {
            return;
        }

        UpdateOfflinePanel();
    }

    private void UpdateOfflinePanel()
    {
        if (_offlinePanel == null)
        {
            return;
        }

        bool isOffline = IsOfflineModeActive();
        _offlinePanel.SetActive(isOffline);
    }

    private bool IsOfflineModeActive()
    {
        _cloudSaveManager = UGSCloudSaveManager.instance;
        return _cloudSaveManager != null && _cloudSaveManager.IsOfflineModeActive();
    }
}
