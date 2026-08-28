using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class OceanLogUI : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private Button _openButton;
    [SerializeField] private TextMeshProUGUI _openButtonNewText;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _garbageTabButton;
    [SerializeField] private Button _achievementTabButton;
    [SerializeField] private TextMeshProUGUI _garbageTabLabel;
    [SerializeField] private Image _garbageTabIcon;
    [SerializeField] private TextMeshProUGUI _achievementTabLabel;

    [Header("Pages")]
    [SerializeField] private GameObject _rootPanel;
    [SerializeField] private GameObject _garbagePage;
    [SerializeField] private GameObject _achievementPage;
    [SerializeField] private TextMeshProUGUI _garbageCountText;
    [SerializeField] private TextMeshProUGUI _completeMessageText;
    [SerializeField] private TextMeshProUGUI _notesText;
    [SerializeField] private List<OceanLogGarbageEntryUI> _garbageEntries;
    [SerializeField] private List<AchievementEntryUI> _achievementEntries;

    [Header("Garbage Details")]
    [SerializeField] private GameObject _detailPanel;
    [SerializeField] private Button _detailCloseButton;
    [SerializeField] private Image _detailImage;
    [SerializeField] private TextMeshProUGUI _detailNameText;
    [SerializeField] private TextMeshProUGUI _detailDescriptionText;
    [SerializeField] private TextMeshProUGUI _detailDecompositionText;
    [SerializeField] private TextMeshProUGUI _detailDecompositionLabel;
    [SerializeField] private TextMeshProUGUI _detailMaterialsText;
    [SerializeField] private TextMeshProUGUI _detailMaterialsLabel;
    [SerializeField] private TextMeshProUGUI _detailSourcesText;
    [SerializeField] private TextMeshProUGUI _detailSourcesLabel;
    [SerializeField] private TextMeshProUGUI _detailTriviaText;
    [SerializeField] private TextMeshProUGUI _detailEraText;

    private UGSCloudSaveManager _cloudSaveManager;
    private OceanLogCatalog _catalog;
    private bool _isSubscribedToDataLoaded;

    private static readonly Color32 SelectedTabTextColor = new Color32(15, 61, 105, 255);
    private static readonly Color32 UnselectedTabColor = new Color32(11, 27, 56, 255);

    void Awake()
    {
        if (_openButton != null) _openButton.onClick.AddListener(Open);
        if (_closeButton != null) _closeButton.onClick.AddListener(Close);
        if (_garbageTabButton != null) _garbageTabButton.onClick.AddListener(ShowGarbagePage);
        if (_achievementTabButton != null) _achievementTabButton.onClick.AddListener(ShowAchievementPage);
        if (_detailCloseButton != null)
        {
            _detailCloseButton.onClick.AddListener(() => _detailPanel.SetActive(false));
        }

        if (_rootPanel != null) _rootPanel.SetActive(false);
        if (_detailPanel != null) _detailPanel.SetActive(false);
    }

    void Start()
    {
        EnsureDependencies();
        UpdateOpenButtonNewLabel();
        UpdateTabLabels();
        UpdateCompleteMessage();
        UpdateNotesText();
    }

    private void UpdateCompleteMessage()

    {
        if (_completeMessageText == null)
        {
            return;
        }

        if (Application.systemLanguage == SystemLanguage.Japanese)
        {
        _completeMessageText.SetText("すべての漂流物を記録しました。 by Drift Ocean");
        }
        else
        {
        _completeMessageText.SetText("All drifting objects have been recorded. by Drift Ocean");
        }
   }
   private void UpdateNotesText()
   {
       if (_notesText == null)
       {
           return;
       }
       if (Application.systemLanguage == SystemLanguage.Japanese)
       {
           _notesText.SetText(

        "【Ocean Logについて】\n" +

        "※ この図鑑に収録されている漂流物は一例です。海には、ほかにもさまざまな種類のものが存在します。\n" +

        "※ 分解までの年数は、国内外の教育・研究機関で広く用いられている一般的な目安を参考にしています。素材や大きさ、海水温、紫外線などの環境によって大きく異なる場合があります。\n" +

        "※ 「広く使われ始めた年代」または「海のごみ問題として注目された年代」は、国内外の資料をもとにした目安です。地域や時代背景によって異なります。\n" +

        "※ 世界の海洋プラスチックごみの約8割は、陸域から発生しているとも言われています。街で捨てられたごみは、風や雨によって側溝や川へ流れ、やがて海へたどり着きます。（環境省資料より）"
       );
       }
       else
       {
          _notesText.SetText(
        "About the Ocean Log\n" +

        "The objects shown here are only a selection; many other types of debris can be found in the ocean.\n" +

        "Decomposition times and historical dates are approximate and may vary by source and environmental conditions.\n" +

        "An estimated 80% of marine plastic pollution comes from land, carried to the ocean by wind, rain, drains, and rivers.\n" +

        "(Source: Ministry of the Environment, Government of Japan)"

       );
       }
   }

    private void UpdateTabLabels()
    {
        if (Application.systemLanguage != SystemLanguage.Japanese)
        {
        _garbageTabLabel.SetText("Ocean Log");
        _achievementTabLabel.SetText("Achievements");
        }
    }
    
    void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        if (_detailPanel.activeSelf)
        {
            _detailPanel.SetActive(false);
        }
        else if (_rootPanel.activeSelf)
        {
            Close();
        }
    }

    void OnDestroy()
    {
        if (_isSubscribedToDataLoaded && _cloudSaveManager != null)
        {
            _cloudSaveManager.OnDataLoaded -= UpdateOpenButtonNewLabel;
            _cloudSaveManager.OnOceanLogChanged -= OnOceanLogChanged;
        }
    }

    public void Open()
    {
        if (!EnsureDependencies())
        {
            Debug.LogError("[OceanLogUI] Cannot open because the data managers are not ready.");
            return;
        }

        _rootPanel.SetActive(true);
        _detailPanel.SetActive(false);
        ShowGarbagePage();
        Refresh();
        UpdateOpenButtonNewLabel();
    }

    public void Close()
    {
        _rootPanel.SetActive(false);
    }

    public void ShowGarbagePage()
    {
        _garbagePage.SetActive(true);
        _achievementPage.SetActive(false);
        _detailPanel.SetActive(false);
        UpdateTabVisuals();
    }

    public void ShowAchievementPage()
    {
        _garbagePage.SetActive(false);
        _achievementPage.SetActive(true);
        _detailPanel.SetActive(false);
        UpdateTabVisuals();
    }

    private void UpdateTabVisuals()
    {
        _garbageTabButton.image.color = Color.white;
        _achievementTabButton.image.color = UnselectedTabColor;
        _garbageTabLabel.color = SelectedTabTextColor;
        _garbageTabIcon.color = SelectedTabTextColor;
        _achievementTabLabel.color = Color.white;
    }

    public void Refresh()
    {
        if (!EnsureDependencies())
        {
            Debug.LogWarning("[OceanLogUI] Refresh skipped because the data managers are not ready.");
            return;
        }

        List<int> discoveredIDs = _cloudSaveManager.GetDiscoveredObstacleIDs();
        for (int i = 0; i < _catalog.garbageEntries.Count; i++)
        {
            ObstacleData data = _catalog.garbageEntries[i];
            bool discovered = discoveredIDs.Contains(data.id);
            _garbageEntries[i].Initialize(
                this,
                data,
                discovered,
                discovered && _cloudSaveManager.IsObstacleEntryNew(data.id));
        }

        _garbageCountText.SetText(
            $"Objects：<color=#0966AD>{discoveredIDs.Count}</color>/30");
        _completeMessageText.gameObject.SetActive(discoveredIDs.Count >= 30);

        for (int i = 0; i < _catalog.achievements.Count; i++)
        {
            AchievementData data = _catalog.achievements[i];
            bool completed = _cloudSaveManager.IsAchievementUnlocked(data.id);
            _achievementEntries[i].gameObject.SetActive(!data.hiddenUntilCompleted || completed);
            _achievementEntries[i].Initialize(data, completed);
        }
    }

   public void ShowGarbageDetails(ObstacleData data)
   {
       _detailImage.sprite = data.GetEncyclopediaSprite();

       if (Application.systemLanguage == SystemLanguage.Japanese)
       {
           _detailNameText.SetText(data.displayName);
           _detailDescriptionText.SetText(data.description);
           _detailDecompositionText.SetText(data.decomposition);
           _detailDecompositionLabel.SetText("分解まで");
           _detailMaterialsText.SetText(data.materials);
           _detailMaterialsLabel.SetText("素材");
           _detailSourcesText.SetText($"・{data.sources.Replace("\n", "\n・")}");
           _detailTriviaText.SetText($"！ {data.trivia}");
           _detailSourcesLabel.SetText("主な由来");
           _detailEraText.SetText(data.era);
        }
        else
       {
           _detailNameText.SetText(data.displayNameEnglish);
           _detailDescriptionText.SetText(data.descriptionEnglish);
           _detailDecompositionText.SetText(data.decompositionEnglish);
           _detailDecompositionLabel.SetText("Decomposition Time");
           _detailMaterialsText.SetText(data.materialsEnglish);
           _detailMaterialsLabel.SetText("Materials");
           _detailSourcesText.SetText($"・{data.sourcesEnglish.Replace("\n", "\n・")}");
           _detailTriviaText.SetText($"! {data.triviaEnglish}");
           _detailSourcesLabel.SetText("Common Sources");
           _detailEraText.SetText(data.eraEnglish);
        }

        _detailPanel.SetActive(true);

        MarkGarbageDetailAsViewed(data.id);

    }

    private async void MarkGarbageDetailAsViewed(int obstacleID)
    {
        if (!EnsureDependencies() || !_cloudSaveManager.IsObstacleEntryNew(obstacleID))
        {
            return;
        }

        await _cloudSaveManager.MarkObstacleEntryAsViewed(obstacleID);
    }

    private void OnOceanLogChanged()
    {
        UpdateOpenButtonNewLabel();

        if (_rootPanel != null && _rootPanel.activeSelf)
        {
            Refresh();
        }
    }

    private void UpdateOpenButtonNewLabel()
    {
        if (_openButtonNewText == null)
        {
            return;
        }

        bool hasNewEntries = EnsureDependencies() && _cloudSaveManager.HasNewObstacleEntries();
        _openButtonNewText.gameObject.SetActive(hasNewEntries);
    }

    private bool EnsureDependencies()
    {
        if (_cloudSaveManager == null)
        {
            _cloudSaveManager = UGSCloudSaveManager.instance;
        }

        if (_catalog == null && AchievementManager.instance != null)
        {
            _catalog = AchievementManager.instance.Catalog;
        }

        if (!_isSubscribedToDataLoaded && _cloudSaveManager != null)
        {
            _cloudSaveManager.OnDataLoaded += UpdateOpenButtonNewLabel;
            _cloudSaveManager.OnOceanLogChanged += OnOceanLogChanged;
            _isSubscribedToDataLoaded = true;
        }

        return _cloudSaveManager != null && _catalog != null;
    }
}
