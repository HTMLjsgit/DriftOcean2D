using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Threading;

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

    // 非同期処理のキャンセル用
    private CancellationTokenSource _cancellationTokenSource;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            _cancellationTokenSource = new CancellationTokenSource();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        // 非同期処理をキャンセル
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
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

        try
        {
            // キャンセルトークンを取得
            var token = _cancellationTokenSource.Token;

            // クラウドから最新のプレイヤーデータを再ロード
            Debug.Log("[RankingUIManager] Reloading player data from cloud...");
            await _cloudSaveManager.ReloadPlayerData();

            // キャンセルチェック
            token.ThrowIfCancellationRequested();

            // リーダーボードを更新（常に使用）
            await _ugsLeaderboardManager.RefreshLeaderboard();

            // キャンセルチェック
            token.ThrowIfCancellationRequested();

            RefreshRankingList();
            UpdateYourHighScore();
        }
        catch (System.OperationCanceledException)
        {
            Debug.Log("[RankingUIManager] ShowRanking was cancelled (scene transition or destroy)");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RankingUIManager] Error in ShowRanking: {e.Message}\n{e.StackTrace}");
        }
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

        // UGSランキングを表示（常に使用）
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

    /// <summary>
    /// Your High Score情報を更新
    /// </summary>
    private void UpdateYourHighScore()
    {
        // UGSから取得（常に使用）
        float bestScore = _cloudSaveManager.GetStats().bestScore;
        string playerName = _cloudSaveManager.GetPlayerName();

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
        // UGSランキングから計算（常に使用）
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

    /// <summary>
    /// 名前変更ボタンがクリックされたときの処理
    /// </summary>
    private void OnChangeNameClicked()
    {
        // UGSから名前を取得（常に使用）
        string currentName = _cloudSaveManager.GetPlayerName();

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
        try
        {
            // キャンセルトークンを取得
            var token = _cancellationTokenSource.Token;

            // UGSに保存（常に使用）
            bool success = await _cloudSaveManager.SetPlayerName(newName);

            // キャンセルチェック
            token.ThrowIfCancellationRequested();

            if (success)
            {
                Debug.Log($"プレイヤー名をUGSに保存しました: {newName}");

                // PlayerNameManagerに同期保存（InputField用）
                _playerNameManager.SavePlayerName(newName);
            }
            else
            {
                Debug.LogError("Failed to save player name to UGS.");
            }

            // 名前を更新したらYour High Scoreの表示も更新
            UpdateYourHighScore();
        }
        catch (System.OperationCanceledException)
        {
            Debug.Log("[RankingUIManager] OnNameChanged was cancelled (scene transition or destroy)");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RankingUIManager] Error in OnNameChanged: {e.Message}\n{e.StackTrace}");
        }
    }
}
