using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// ランキング表示UI管理（TitleシーンとMainシーン共通）
/// </summary>
public class RankingUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject _rankingPanel; // ランキング表示パネル
    [SerializeField] private Transform _contentTransform; // ScrollViewのContent
    [SerializeField] private GameObject _rankingRowPrefab; // 1行分のプレハブ
    [SerializeField] private Button _closeButton; // 閉じるボタン
    [SerializeField] private Button _changeNameButton; // 名前変更ボタン

    [Header("Your High Score UI")]
    [SerializeField] private Image _yourSkinImage; // プレイヤーのスキン画像
    [SerializeField] private TextMeshProUGUI _yourNameText; // プレイヤー名
    [SerializeField] private TextMeshProUGUI _yourScoreText; // スコア
    [SerializeField] private TextMeshProUGUI _untilRankingText; // ランキング入りまでの差分（Titleシーンのみ）

    [Header("Optional - For Title Scene")]
    [SerializeField] private FlowUI _flowUI; // Titleシーンのみ使用

    public static RankingUIManager instance;
    private RankingManager _rankingManager;
    private PlayerNameManager _playerNameManager;
    private SkinManager _skinManager;
    private SkinDatabase _skinDatabase;
    private NicknameInputUI _nicknameInputUI;
    private UGSLeaderboardManager _ugsLeaderboardManager;
    private UGSCloudSaveManager _cloudSaveManager;

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
        _rankingManager = RankingManager.instance;
        _playerNameManager = PlayerNameManager.instance;
        _skinManager = SkinManager.instance;
        _skinDatabase = SkinDatabase.instance;
        _nicknameInputUI = NicknameInputUI.instance;
        _ugsLeaderboardManager = UGSLeaderboardManager.instance;
        _cloudSaveManager = UGSCloudSaveManager.instance;

        // 閉じるボタンのリスナー登録
        _closeButton.onClick.AddListener(OnCloseClicked);

        // 名前変更ボタンのリスナー登録
        _changeNameButton.onClick.AddListener(OnChangeNameClicked);

        // Mainシーンの場合は初期状態で非表示
        if (_flowUI == null)
        {
            HideRanking();
        }
    }

    /// <summary>
    /// ランキングビューを開く（FlowUIまたはパネルで表示）
    /// </summary>
    public async void ShowRanking()
    {
        if (_flowUI != null)
        {
            // Titleシーン: FlowUIを使用
            _flowUI.SwitchView("Ranking");
        }
        else
        {
            // Mainシーン: パネルを直接表示
            _rankingPanel.SetActive(true);
        }

        // UGS使用時はリーダーボードを更新
        if (_ugsLeaderboardManager != null)
        {
            await _ugsLeaderboardManager.RefreshLeaderboard();
        }

        RefreshRankingList();
        UpdateYourHighScore();
    }

    /// <summary>
    /// ランキングビューを閉じる
    /// </summary>
    public void HideRanking()
    {
        if (_flowUI != null)
        {
            // Titleシーン: FlowUIでStartビューに戻る
            _flowUI.SwitchView("Start");
        }
        else
        {
            // Mainシーン: パネルを非表示
            _rankingPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 閉じるボタンがクリックされたときの処理
    /// </summary>
    private void OnCloseClicked()
    {
        HideRanking();
    }

    /// <summary>
    /// ランキングリストを更新して表示
    /// </summary>
    private void RefreshRankingList()
    {
        Debug.Log("RefreshRankingList called");

        // 既存の表示を全削除
        foreach (Transform child in _contentTransform)
        {
            Destroy(child.gameObject);
        }

        // UGS使用時はUGSランキングを表示
        if (_ugsLeaderboardManager != null)
        {
            var ugsRankings = _ugsLeaderboardManager.GetCachedRankings();
            Debug.Log($"UGS Ranking count: {ugsRankings.Count}");

            for (int i = 0; i < ugsRankings.Count; i++)
            {
                var entry = ugsRankings[i];

                // プレハブ生成
                GameObject row = Instantiate(_rankingRowPrefab, _contentTransform);

                // テキスト設定
                RankingUI rowScript = row.GetComponent<RankingUI>();
                rowScript.SetData(entry.rank, entry.playerName, entry.score, entry.skinID);
            }
        }
        else
        {
            // UGS未使用時はローカルランキングを表示
            var ranking = _rankingManager.GetCurrentRanking();
            Debug.Log($"Local Ranking count: {ranking.Count}");

            for (int i = 0; i < ranking.Count; i++)
            {
                var entry = ranking[i];

                // プレハブ生成
                GameObject row = Instantiate(_rankingRowPrefab, _contentTransform);

                // テキスト設定
                RankingUI rowScript = row.GetComponent<RankingUI>();
                rowScript.SetData(i + 1, entry.playerName, entry.score, entry.skinID);
            }
        }
    }

    /// <summary>
    /// Your High Score情報を更新
    /// </summary>
    private void UpdateYourHighScore()
    {
        // ベストスコアを取得（UGS優先、フォールバックはPlayerPrefs）
        float bestScore = 0f;
        string playerName = "";

        if (_cloudSaveManager != null)
        {
            // UGS使用時はCloudSaveから取得
            bestScore = _cloudSaveManager.GetStats().bestScore;
            playerName = _cloudSaveManager.GetPlayerName();
        }
        else
        {
            // UGS未使用時はPlayerPrefsから取得
            bestScore = PlayerPrefs.GetFloat("Stats_BestScore", 0f);
            playerName = _playerNameManager.GetPlayerName();
        }

        // 現在装備中のスキンを取得
        int currentSkinID = _skinManager.currentSkinID;
        SkinData skinData = _skinDatabase.GetSkinById(currentSkinID);

        // UIに反映
        _yourNameText.text = playerName;
        _yourScoreText.text = bestScore.ToString("F2");
        _yourSkinImage.sprite = skinData.skinSprite;

        // ランキング入りまでの差分を計算（Titleシーンのみ）
        if (_untilRankingText != null)
        {
            UpdateUntilRankingText(bestScore);
        }
    }

    /// <summary>
    /// ランキング入りまでの差分を計算して表示
    /// </summary>
    private void UpdateUntilRankingText(float yourScore)
    {
        // UGS使用時はUGSランキングから計算
        if (_ugsLeaderboardManager != null)
        {
            var ugsRankings = _ugsLeaderboardManager.GetCachedRankings();

            if (ugsRankings.Count == 0)
            {
                _untilRankingText.text = "まだランキングがありません";
                return;
            }

            // すでにランキング入りしているかチェック
            bool isInRanking = false;
            int yourRank = -1;

            for (int i = 0; i < ugsRankings.Count; i++)
            {
                if (ugsRankings[i].score <= yourScore)
                {
                    isInRanking = true;
                    yourRank = i + 1;
                    break;
                }
            }

            if (isInRanking)
            {
                _untilRankingText.text = $"現在 {yourRank}位！";
            }
            else
            {
                // ランキング圏外の場合、10位との差を表示
                if (ugsRankings.Count >= 10)
                {
                    float tenthScore = ugsRankings[9].score;
                    float difference = tenthScore - yourScore;
                    _untilRankingText.text = $"ランキング入りまで あと {difference:F2}";
                }
                else
                {
                    // ランキングが10件未満の場合は必ずランキング入りできる
                    _untilRankingText.text = "次回プレイでランキング入り確定！";
                }
            }
        }
        else
        {
            // UGS未使用時はローカルランキングから計算
            var ranking = _rankingManager.GetCurrentRanking();

            if (ranking.Count == 0)
            {
                _untilRankingText.text = "まだランキングがありません";
                return;
            }

            // すでにランキング入りしているかチェック
            bool isInRanking = false;
            int yourRank = -1;

            for (int i = 0; i < ranking.Count; i++)
            {
                if (ranking[i].score <= yourScore)
                {
                    isInRanking = true;
                    yourRank = i + 1;
                    break;
                }
            }

            if (isInRanking)
            {
                _untilRankingText.text = $"現在 {yourRank}位！";
            }
            else
            {
                // ランキング圏外の場合、10位との差を表示
                if (ranking.Count >= 10)
                {
                    float tenthScore = ranking[9].score;
                    float difference = tenthScore - yourScore;
                    _untilRankingText.text = $"ランキング入りまで あと {difference:F2}";
                }
                else
                {
                    // ランキングが10件未満の場合は必ずランキング入りできる
                    _untilRankingText.text = "次回プレイでランキング入り確定！";
                }
            }
        }
    }

    /// <summary>
    /// 名前変更ボタンがクリックされたときの処理
    /// </summary>
    private void OnChangeNameClicked()
    {
        string currentName = _playerNameManager.GetPlayerName();
        _nicknameInputUI.ShowPanel(
            $"現在の名前: {currentName}\n新しい名前を入力してください",
            OnNameChanged,
            null
        );
    }

    /// <summary>
    /// 名前が変更されたときの処理
    /// </summary>
    private async void OnNameChanged(string newName)
    {
        // UGS使用時はCloudSaveに保存
        if (_cloudSaveManager != null)
        {
            bool success = await _cloudSaveManager.SetPlayerName(newName);
            if (success)
            {
                Debug.Log($"プレイヤー名をUGSに保存しました: {newName}");
            }
            else
            {
                Debug.LogWarning("Failed to save player name to UGS. Falling back to local save.");
                _playerNameManager?.SavePlayerName(newName);
            }
        }
        else
        {
            // UGS未使用時はPlayerPrefsに保存
            _playerNameManager.SavePlayerName(newName);
            Debug.Log($"プレイヤー名をローカルに保存しました: {newName}");
        }

        // 名前を更新したらYour High Scoreの表示も更新
        UpdateYourHighScore();
    }
}
