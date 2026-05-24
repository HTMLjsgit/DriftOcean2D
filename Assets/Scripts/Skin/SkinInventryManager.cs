using System.Collections.Generic;
using TMPro;
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
    [SerializeField] private TextMeshProUGUI _offlineModeText;

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

        UpdateOfflineModeUI();
    }

    void Update()
    {
        UpdateOfflineModeUI();
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
        UpdateOfflineModeUI();
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

        UpdateOfflineModeUI();
    }

    private void UpdateOfflineModeUI()
    {
        if (_offlineModeText == null)
        {
            return;
        }

        bool isOffline = IsOfflineModeActive();
        _offlineModeText.gameObject.SetActive(isOffline);
        _offlineModeText.text = "Offline Mode";
    }

    private bool IsOfflineModeActive()
    {
        _cloudSaveManager = UGSCloudSaveManager.instance;
        return _cloudSaveManager != null && _cloudSaveManager.IsOfflineModeActive();
    }
}
