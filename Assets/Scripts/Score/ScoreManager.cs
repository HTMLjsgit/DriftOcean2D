using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _scoreTextUI;
    [SerializeField] private GameObject _offlinePanel;

    [Header("Settings")]
    [SerializeField] private float _scorePerSecond = 1.0f;
    [SerializeField] private float _timeMultiplier = 1.0f;

    [Header("Status")]
    public float currentScore = 1950.80f;

    [SerializeField] private float _initialScore;

    private bool _scoreMeasureNow;
    private GameManager gameManager;
    private UGSCloudSaveManager _cloudSaveManager;

    public static ScoreManager instance;

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

        _initialScore = currentScore;
    }

    void Start()
    {
        gameManager = GameManager.instance;
        _cloudSaveManager = UGSCloudSaveManager.instance;
        UpdateOfflinePanel();
    }

    void Update()
    {
        if (_scoreMeasureNow)
        {
            float increaseAmount = Time.deltaTime * _timeMultiplier * _timeMultiplier;
            currentScore += increaseAmount;

            if (_scoreTextUI != null)
            {
                _scoreTextUI.SetText(currentScore.ToString("F2"));
            }
        }

        UpdateOfflinePanel();
    }

    public void ScoreMeasureInit()
    {
        currentScore = _initialScore;
        _scoreMeasureNow = false;

        if (_scoreTextUI != null)
        {
            _scoreTextUI.SetText(currentScore.ToString("F2"));
        }
    }

    public void ScoreMeasureStart()
    {
        _scoreMeasureNow = true;
    }

    public void ScoreMeasureStop()
    {
        _scoreMeasureNow = false;
    }

    public void SetTimeMultiplier(float newMultiplier)
    {
        _timeMultiplier = newMultiplier;
    }

    public float getCurrentScore()
    {
        return currentScore;
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
