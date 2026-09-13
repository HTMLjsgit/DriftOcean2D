using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class RankingUIManager : MonoBehaviour
{
    private static readonly Color SelectedButtonColor = new Color(0.12f, 0.45f, 0.84f, 0.95f);
    private static readonly Color DisabledButtonColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
    private static readonly Color SelectedTextColor = Color.white;
    private static readonly Color UnselectedTextColor = new Color(0.92f, 0.92f, 0.92f, 1f);

    [Header("UI References")]
    [SerializeField] private GameObject _rankingPanel;
    [SerializeField] private Transform _contentTransform;
    [SerializeField] private GameObject _rankingRowPrefab;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _changeNameButton;

    [Header("Ranking Type Switch")]
    [FormerlySerializedAs("_weeklyButton")]
    [FormerlySerializedAs("_allTimeButton")]
    [SerializeField] private Button _rankingTypeSwitchButton;
    [FormerlySerializedAs("_dailyButton")]
    [SerializeField, HideInInspector] private Button _legacyDailyButton;
    [SerializeField] private TextMeshProUGUI _rankingTypeLabel;

    [Header("Default Ranking Type")]
    [SerializeField] private DefaultRankingType _defaultRankingType = DefaultRankingType.Weekly;

    [Header("Your High Score UI")]
    [SerializeField] private Image _yourSkinImage;
    [SerializeField] private TextMeshProUGUI _yourNameText;
    [SerializeField] private TextMeshProUGUI _yourScoreText;
    [SerializeField] private TextMeshProUGUI _untilRankingText;

    [Header("Optional - For Title Scene")]
    [SerializeField] private FlowUI _flowUI;

    [Header("Offline Mode")]
    [SerializeField] private GameObject _offlinePanel;

    public static RankingUIManager instance;

    private PlayerNameManager _playerNameManager;
    private SkinManager _skinManager;
    private SkinDatabase _skinDatabase;
    private NicknameInputUI _nicknameInputUI;
    private UGSLeaderboardManager _ugsLeaderboardManager;
    private UGSCloudSaveManager _cloudSaveManager;
    private CancellationTokenSource _cancellationTokenSource;
    private UGSRankingEntry _playerWeeklyEntry;
    private UGSRankingEntry _playerDailyEntry;
    private TextMeshProUGUI _rankingTypeSwitchLabel;
    private bool _isChangingName;

    private enum RankingType
    {
        Weekly,
        Daily
    }

    public enum DefaultRankingType
    {
        Weekly,
        Daily
    }

    private RankingType _currentRankingType;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            _cancellationTokenSource = new CancellationTokenSource();
            _currentRankingType = _defaultRankingType == DefaultRankingType.Daily
                ? RankingType.Daily
                : RankingType.Weekly;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        _playerNameManager = PlayerNameManager.instance;
        _skinManager = SkinManager.instance;
        _skinDatabase = SkinDatabase.instance;
        _nicknameInputUI = NicknameInputUI.instance;
        _ugsLeaderboardManager = UGSLeaderboardManager.instance;
        _cloudSaveManager = UGSCloudSaveManager.instance;

        SetUpRankingTypeSwitchButton();
        BindButtons();

        if (_flowUI == null)
        {
            HideRanking();
        }

        RefreshRankingHeader();
        UpdateOfflineModeUI();
    }

    private void SetUpRankingTypeSwitchButton()
    {
        if (_rankingTypeLabel == null && _rankingPanel != null)
        {
            Transform rankingTypeLabelTransform = _rankingPanel.transform.Find("RankingTypeText");
            if (rankingTypeLabelTransform != null)
            {
                _rankingTypeLabel = rankingTypeLabelTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (_rankingTypeSwitchButton == null && _rankingPanel != null)
        {
            Transform existingButtonTransform = _rankingPanel.transform.Find("RankingTypeSwitchButton");
            if (existingButtonTransform != null)
            {
                _rankingTypeSwitchButton = existingButtonTransform.GetComponent<Button>();
            }
        }

        if (_rankingTypeSwitchButton == null && _changeNameButton != null)
        {
            GameObject buttonObject = Instantiate(_changeNameButton.gameObject, _changeNameButton.transform.parent);
            buttonObject.name = "RankingTypeSwitchButton";
            buttonObject.SetActive(true);

            RectTransform sourceRect = _changeNameButton.GetComponent<RectTransform>();
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            if (sourceRect != null && buttonRect != null)
            {
                buttonRect.anchoredPosition = new Vector2(
                    -Mathf.Abs(sourceRect.anchoredPosition.x),
                    sourceRect.anchoredPosition.y);
            }

            _rankingTypeSwitchButton = buttonObject.GetComponent<Button>();
        }

        if (_rankingTypeSwitchButton == null)
        {
            return;
        }

        _rankingTypeSwitchLabel = _rankingTypeSwitchButton.GetComponentInChildren<TextMeshProUGUI>();
        if (_rankingTypeSwitchLabel == null)
        {
            _rankingTypeSwitchLabel = CreateRankingTypeSwitchLabel(_rankingTypeSwitchButton.transform);
        }
    }

    private TextMeshProUGUI CreateRankingTypeSwitchLabel(Transform buttonTransform)
    {
        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonTransform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        if (_rankingTypeLabel != null)
        {
            label.font = _rankingTypeLabel.font;
            label.fontSize = _rankingTypeLabel.fontSize;
            label.fontStyle = _rankingTypeLabel.fontStyle;
        }

        label.alignment = TextAlignmentOptions.Center;
        label.color = SelectedTextColor;
        label.raycastTarget = false;
        return label;
    }

    void Update()
    {
        UpdateOfflineModeUI();
    }

    void OnDestroy()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
    }

    public async void ShowRanking()
    {
        if (_flowUI != null)
        {
            _flowUI.SwitchView("Ranking");
        }
        else if (_rankingPanel != null)
        {
            _rankingPanel.SetActive(true);
        }

        RefreshRankingHeader();
        UpdateOfflineModeUI();
        RefreshRankingList();
        UpdateYourHighScore();

        if (IsOfflineModeActive())
        {
            return;
        }

        try
        {
            var token = _cancellationTokenSource.Token;

            if (_cloudSaveManager != null)
            {
                await _cloudSaveManager.ReloadPlayerData();
            }

            token.ThrowIfCancellationRequested();

            if (_ugsLeaderboardManager != null)
            {
                RankingType initiallyDisplayedType = _currentRankingType;
                RankingType otherType = initiallyDisplayedType == RankingType.Weekly
                    ? RankingType.Daily
                    : RankingType.Weekly;

                Task<bool> otherLeaderboardRefresh = RefreshLeaderboard(otherType);
                await RefreshLeaderboard(initiallyDisplayedType);

                token.ThrowIfCancellationRequested();

                // Render the visible ranking as soon as its first response arrives.
                RefreshRankingList();
                UpdateYourHighScore();

                await otherLeaderboardRefresh;

                token.ThrowIfCancellationRequested();

                RefreshRankingList();

                Task<UGSRankingEntry> weeklyPlayerEntryTask = _ugsLeaderboardManager.GetPlayerWeeklyRank();
                Task<UGSRankingEntry> dailyPlayerEntryTask = _ugsLeaderboardManager.GetPlayerDailyRank();
                _playerWeeklyEntry = await weeklyPlayerEntryTask;
                _playerDailyEntry = await dailyPlayerEntryTask;
            }

            token.ThrowIfCancellationRequested();

            RefreshRankingList();
            UpdateYourHighScore();
        }
        catch (System.OperationCanceledException)
        {
            Debug.Log("[RankingUIManager] ShowRanking was cancelled.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RankingUIManager] Error in ShowRanking: {e.Message}\n{e.StackTrace}");
        }
    }

    public void HideRanking()
    {
        // Rankingを閉じたまま入力パネルだけが次回再表示されないようにする。
        if (_nicknameInputUI != null)
        {
            _nicknameInputUI.HidePanel();
        }

        if (_flowUI != null)
        {
            _flowUI.SwitchView("Start");
        }
        else if (_rankingPanel != null)
        {
            _rankingPanel.SetActive(false);
        }
    }

    private void BindButtons()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(OnCloseClicked);
            _closeButton.onClick.AddListener(OnCloseClicked);
        }

        if (_changeNameButton != null)
        {
            _changeNameButton.onClick.RemoveListener(OnChangeNameClicked);
            _changeNameButton.onClick.AddListener(OnChangeNameClicked);
        }

        if (_rankingTypeSwitchButton != null)
        {
            _rankingTypeSwitchButton.onClick.RemoveAllListeners();
            _rankingTypeSwitchButton.onClick.AddListener(ToggleRankingType);
        }

        if (_legacyDailyButton != null && _legacyDailyButton != _rankingTypeSwitchButton)
        {
            _legacyDailyButton.gameObject.SetActive(false);
        }

        if (_rankingTypeSwitchButton == null || _rankingTypeLabel == null)
        {
            Debug.LogWarning("[RankingUIManager] RankingTypeSwitchButton and RankingTypeLabel must be assigned in the Inspector.");
        }
    }

    private void OnCloseClicked()
    {
        HideRanking();
    }

    private void RefreshRankingList()
    {
        ClearRankingRows();
        RefreshRankingHeader();

        if (IsOfflineModeActive())
        {
            return;
        }

        List<UGSRankingEntry> rankings = GetCurrentRankingEntries();
        if (_contentTransform == null || _rankingRowPrefab == null)
        {
            return;
        }

        foreach (UGSRankingEntry entry in rankings)
        {
            GameObject row = Instantiate(_rankingRowPrefab, _contentTransform);
            RankingUI rowScript = row.GetComponent<RankingUI>();
            if (rowScript != null)
            {
                rowScript.SetData(entry.rank, entry.playerName, entry.score, entry.skinID);
            }
        }
    }

    private void UpdateYourHighScore()
    {
        UGSRankingEntry currentPlayerEntry = IsOfflineModeActive() ? null : GetCurrentPlayerEntry();

        float bestScore = currentPlayerEntry != null
            ? currentPlayerEntry.score
            : (_cloudSaveManager != null ? _cloudSaveManager.GetDisplayBestScore() : 0f);

        // This label describes the current player. Ranking rows retain their recorded names.
        string playerName = _cloudSaveManager != null ? _cloudSaveManager.GetPlayerName()
            : (_playerNameManager != null ? _playerNameManager.GetPlayerName() : PlayerNameFilter.DefaultName);

        int currentSkinID = currentPlayerEntry != null
            ? currentPlayerEntry.skinID
            : (_skinManager != null ? _skinManager.currentSkinID : 1);

        SkinData skinData = _skinDatabase != null ? _skinDatabase.GetSkinById(currentSkinID) : null;

        if (_yourNameText != null)
        {
            _yourNameText.richText = false;
            _yourNameText.text = PlayerNameFilter.DisplayName(playerName);
        }

        if (_yourScoreText != null)
        {
            _yourScoreText.text = bestScore.ToString("F2");
        }

        if (_yourSkinImage != null && skinData != null)
        {
            _yourSkinImage.sprite = skinData.skinSprite;
        }

        UpdateUntilRankingText(bestScore);
    }

    private void UpdateUntilRankingText(float yourScore)
    {
        if (_untilRankingText == null)
        {
            return;
        }

        if (IsOfflineModeActive())
        {
            _untilRankingText.text = "Offline Mode";
            return;
        }

        List<UGSRankingEntry> rankings = GetCurrentRankingEntries();
        if (rankings.Count == 0)
        {
            _untilRankingText.text = "No ranking data";
            return;
        }

        if (GetCurrentPlayerEntry() == null && yourScore <= 0f)
        {
            _untilRankingText.text = "Play once to enter ranking";
            return;
        }

        for (int i = 0; i < rankings.Count; i++)
        {
            if (rankings[i].score <= yourScore)
            {
                _untilRankingText.text = $"Rank {i + 1}";
                return;
            }
        }

        if (rankings.Count >= 10)
        {
            float difference = rankings[9].score - yourScore;
            _untilRankingText.text = $"Need {difference:F2} for Top 10";
            return;
        }

        _untilRankingText.text = "Ranking updates soon";
    }

    private void OnChangeNameClicked()
    {
        if (IsOfflineModeActive())
        {
            Debug.LogWarning("[RankingUIManager] Offline mode active. Name change is disabled.");
            return;
        }

        // NicknameInputUIの初期化順に依存しないよう、クリック時にも取得する。
        if (_nicknameInputUI == null)
        {
            _nicknameInputUI = NicknameInputUI.instance;
        }

        if (_nicknameInputUI == null || _rankingPanel == null)
        {
            Debug.LogError("[RankingUIManager] NicknameInputUI or RankingPanel is not available.");
            return;
        }

        _nicknameInputUI.ShowPanel(
            "What's your name",
            OnNameChanged,
            null,
            _rankingPanel.transform
        );
    }

    private async void OnNameChanged(string newName)
    {
        if (_isChangingName || IsOfflineModeActive() || _cloudSaveManager == null || !PlayerNameFilter.IsAllowed(newName))
        {
            return;
        }

        _isChangingName = true;
        UpdateOfflineModeUI();
        try
        {
            var token = _cancellationTokenSource.Token;
            Task<bool> saving = _cloudSaveManager.SetPlayerName(newName);
            UpdateYourHighScore();
            bool success = await saving;

            token.ThrowIfCancellationRequested();

            if (success && _playerNameManager != null)
            {
                _playerNameManager.SavePlayerName(newName);
            }

            UpdateYourHighScore();
        }
        catch (System.OperationCanceledException)
        {
            Debug.Log("[RankingUIManager] OnNameChanged was cancelled.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RankingUIManager] Error in OnNameChanged: {e.Message}\n{e.StackTrace}");
        }
        finally
        {
            _isChangingName = false;
            if (this != null) UpdateOfflineModeUI();
        }
    }

    private void SwitchRankingType(RankingType type)
    {
        if (_currentRankingType == type)
        {
            return;
        }

        _currentRankingType = type;
        RefreshRankingList();
        UpdateYourHighScore();
    }

    private void ToggleRankingType()
    {
        SwitchRankingType(_currentRankingType == RankingType.Weekly
            ? RankingType.Daily
            : RankingType.Weekly);
    }

    private void ClearRankingRows()
    {
        if (_contentTransform == null)
        {
            return;
        }

        foreach (Transform child in _contentTransform)
        {
            Destroy(child.gameObject);
        }
    }

    private List<UGSRankingEntry> GetCurrentRankingEntries()
    {
        if (_ugsLeaderboardManager == null)
        {
            return new List<UGSRankingEntry>();
        }

        return _currentRankingType == RankingType.Daily
            ? _ugsLeaderboardManager.GetCachedDailyRankings()
            : _ugsLeaderboardManager.GetCachedWeeklyRankings();
    }

    private Task<bool> RefreshLeaderboard(RankingType type)
    {
        return type == RankingType.Daily
            ? _ugsLeaderboardManager.RefreshDailyLeaderboard()
            : _ugsLeaderboardManager.RefreshWeeklyLeaderboard();
    }

    private UGSRankingEntry GetCurrentPlayerEntry()
    {
        return _currentRankingType == RankingType.Daily
            ? _playerDailyEntry
            : _playerWeeklyEntry;
    }

    private void UpdateOfflineModeUI()
    {
        bool isOffline = IsOfflineModeActive();

        if (_offlinePanel != null)
        {
            _offlinePanel.SetActive(isOffline);
        }

        if (_changeNameButton != null)
        {
            _changeNameButton.interactable = !isOffline && !_isChangingName;
        }

        if (_rankingTypeSwitchButton != null)
        {
            _rankingTypeSwitchButton.interactable = !isOffline;
        }

        UpdateRankingTypeButtonVisuals(isOffline);
    }

    private bool IsOfflineModeActive()
    {
        _cloudSaveManager = UGSCloudSaveManager.instance;
        return _cloudSaveManager != null && _cloudSaveManager.IsOfflineModeActive();
    }

    private void RefreshRankingHeader()
    {
        if (_rankingTypeLabel != null)
        {
            _rankingTypeLabel.text = _currentRankingType == RankingType.Daily
                ? "Daily Ranking"
                : "Weekly Ranking";
        }

        if (_rankingTypeSwitchLabel != null)
        {
            _rankingTypeSwitchLabel.text = _currentRankingType == RankingType.Daily
                ? "Weekly Ranking"
                : "Daily Ranking";
        }

        UpdateRankingTypeButtonVisuals(IsOfflineModeActive());
    }

    private void UpdateRankingTypeButtonVisuals(bool isOffline)
    {
        UpdateRankingTypeButtonVisual(_rankingTypeSwitchButton, isOffline);
    }

    private void UpdateRankingTypeButtonVisual(Button button, bool isOffline)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = isOffline
                ? DisabledButtonColor
                : SelectedButtonColor;
        }

        if (_rankingTypeSwitchLabel != null)
        {
            _rankingTypeSwitchLabel.color = isOffline ? UnselectedTextColor : SelectedTextColor;
        }
    }
}
