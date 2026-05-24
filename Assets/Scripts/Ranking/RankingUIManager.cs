using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RankingUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject _rankingPanel;
    [SerializeField] private Transform _contentTransform;
    [SerializeField] private GameObject _rankingRowPrefab;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _changeNameButton;

    [Header("Ranking Type Switch")]
    [SerializeField] private Button _allTimeButton;
    [SerializeField] private Button _dailyButton;
    [SerializeField] private TextMeshProUGUI _rankingTypeLabel;

    [Header("Default Ranking Type")]
    [SerializeField] private DefaultRankingType _defaultRankingType = DefaultRankingType.AllTime;

    [Header("Your High Score UI")]
    [SerializeField] private Image _yourSkinImage;
    [SerializeField] private TextMeshProUGUI _yourNameText;
    [SerializeField] private TextMeshProUGUI _yourScoreText;
    [SerializeField] private TextMeshProUGUI _untilRankingText;

    [Header("Optional - For Title Scene")]
    [SerializeField] private FlowUI _flowUI;

    [Header("Offline Mode")]
    [SerializeField] private TextMeshProUGUI _offlineModeText;

    public static RankingUIManager instance;

    private PlayerNameManager _playerNameManager;
    private SkinManager _skinManager;
    private SkinDatabase _skinDatabase;
    private NicknameInputUI _nicknameInputUI;
    private UGSLeaderboardManager _ugsLeaderboardManager;
    private UGSCloudSaveManager _cloudSaveManager;
    private CancellationTokenSource _cancellationTokenSource;

    private enum RankingType
    {
        AllTime,
        Daily
    }

    public enum DefaultRankingType
    {
        AllTime,
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
                : RankingType.AllTime;
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

        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(OnCloseClicked);
        }

        if (_changeNameButton != null)
        {
            _changeNameButton.onClick.AddListener(OnChangeNameClicked);
        }

        if (_allTimeButton != null)
        {
            _allTimeButton.onClick.AddListener(() => SwitchRankingType(RankingType.AllTime));
        }

        if (_dailyButton != null)
        {
            _dailyButton.onClick.AddListener(() => SwitchRankingType(RankingType.Daily));
        }

        if (_flowUI == null)
        {
            HideRanking();
        }

        UpdateOfflineModeUI();
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

        UpdateOfflineModeUI();

        if (IsOfflineModeActive())
        {
            RefreshRankingList();
            UpdateYourHighScore();
            return;
        }

        try
        {
            var token = _cancellationTokenSource.Token;

            if (_cloudSaveManager != null)
            {
                Debug.Log("[RankingUIManager] Reloading player data from cloud...");
                await _cloudSaveManager.ReloadPlayerData();
            }

            token.ThrowIfCancellationRequested();

            if (_ugsLeaderboardManager != null)
            {
                await _ugsLeaderboardManager.RefreshLeaderboard();
                await _ugsLeaderboardManager.RefreshDailyLeaderboard();
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
        if (_flowUI != null)
        {
            _flowUI.SwitchView("Start");
        }
        else if (_rankingPanel != null)
        {
            _rankingPanel.SetActive(false);
        }
    }

    private void OnCloseClicked()
    {
        HideRanking();
    }

    private void RefreshRankingList()
    {
        ClearRankingRows();

        if (IsOfflineModeActive())
        {
            if (_rankingTypeLabel != null)
            {
                _rankingTypeLabel.text = "Offline Mode";
            }

            return;
        }

        List<UGSRankingEntry> rankings = GetCurrentRankingEntries();

        if (_rankingTypeLabel != null)
        {
            _rankingTypeLabel.text = _currentRankingType == RankingType.Daily
                ? "Daily Ranking"
                : "All Score Ranking";
        }

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
        float bestScore = _cloudSaveManager != null ? _cloudSaveManager.GetDisplayBestScore() : 0f;
        string playerName = _cloudSaveManager != null ? _cloudSaveManager.GetPlayerName() : "Player";
        int currentSkinID = _skinManager != null ? _skinManager.currentSkinID : 1;
        SkinData skinData = _skinDatabase != null ? _skinDatabase.GetSkinById(currentSkinID) : null;

        if (_yourNameText != null)
        {
            _yourNameText.text = playerName;
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

        if (_nicknameInputUI == null)
        {
            return;
        }

        _nicknameInputUI.ShowPanel(
            "What's your name",
            OnNameChanged,
            null
        );
    }

    private async void OnNameChanged(string newName)
    {
        if (IsOfflineModeActive() || _cloudSaveManager == null)
        {
            return;
        }

        try
        {
            var token = _cancellationTokenSource.Token;
            bool success = await _cloudSaveManager.SetPlayerName(newName);

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
            : _ugsLeaderboardManager.GetCachedRankings();
    }

    private void UpdateOfflineModeUI()
    {
        bool isOffline = IsOfflineModeActive();

        if (_offlineModeText != null)
        {
            _offlineModeText.gameObject.SetActive(isOffline);
            _offlineModeText.text = "Offline Mode";
        }

        if (_changeNameButton != null)
        {
            _changeNameButton.interactable = !isOffline;
        }

        if (_allTimeButton != null)
        {
            _allTimeButton.interactable = !isOffline;
        }

        if (_dailyButton != null)
        {
            _dailyButton.interactable = !isOffline;
        }

        if (isOffline)
        {
            if (_rankingTypeLabel != null)
            {
                _rankingTypeLabel.text = "Offline Mode";
            }

            if (_untilRankingText != null)
            {
                _untilRankingText.text = "Offline Mode";
            }
        }
    }

    private bool IsOfflineModeActive()
    {
        _cloudSaveManager = UGSCloudSaveManager.instance;
        return _cloudSaveManager != null && _cloudSaveManager.IsOfflineModeActive();
    }
}
