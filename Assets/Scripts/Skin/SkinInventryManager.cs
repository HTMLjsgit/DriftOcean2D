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

    [Header("Skin Details")]
    [SerializeField] private GameObject _detailPanel;
    [SerializeField] private Button _detailCloseButton;
    [SerializeField] private Image _detailSkinImage;
    [SerializeField] private Image _detailSkinBackgroundImage;
    [SerializeField] private TextMeshProUGUI _detailSkinName;
    [SerializeField] private TextMeshProUGUI _detailDescription;

    [Header("Offline Mode")]
    [SerializeField] private GameObject _offlinePanel;

    private UGSCloudSaveManager _cloudSaveManager;
    private bool _isInitialized;
    private Color _defaultDetailSkinBackgroundColor = Color.white;

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

        if (_detailSkinBackgroundImage != null)
        {
            _defaultDetailSkinBackgroundColor = _detailSkinBackgroundImage.color;
        }
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

        _detailCloseButton.onClick.AddListener(() => _detailPanel.SetActive(false));
        _detailPanel.SetActive(false);

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
                bool visible = !data.hiddenUntilUnlocked || SkinManager.instance.IsUnlocked(data.id);
                slot.gameObject.SetActive(visible);
                slot.Initialize(data.GetDisplaySprite());
            }
        }

        RefreshAllSlots();
    }

    public void RefreshAllSlots()
    {
        foreach (SkinUI slot in _skinSlots)
        {
            SkinData data = SkinDatabase.instance.GetSkinById(slot.skinId);
            slot.gameObject.SetActive(!data.hiddenUntilUnlocked || SkinManager.instance.IsUnlocked(data.id));
            slot.UpdateUIState();
        }

        if (!_isInitialized)
        {
            return;
        }

        UpdateOfflinePanel();
    }

    public void ShowSkinDetails(int skinID)
    {
        SkinData data = SkinDatabase.instance.GetSkinById(skinID);
     _detailSkinName.SetText(Application.systemLanguage == SystemLanguage.Japanese
            ? data.skinName
            : data.englishSkinName);
     _detailDescription.SetText(Application.systemLanguage == SystemLanguage.Japanese
            ? data.description
            : data.englishDescription);
     _detailSkinImage.sprite = data.GetDisplaySprite();

        if (_detailSkinBackgroundImage != null)
        {
            _detailSkinBackgroundImage.color = data.useCustomDetailBackground
                ? data.detailBackgroundColor
                : _defaultDetailSkinBackgroundColor;
        }

        _detailPanel.SetActive(true);
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
