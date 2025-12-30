using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RankingInventryManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform _contentTransform; // ScrollViewのContent
    [SerializeField] private GameObject _rankingRowPrefab; // 1行分のプレハブ
    [SerializeField] private Button _backButton;
    [SerializeField] private Button _changeNameButton; // 名前変更ボタン
    [SerializeField] private FlowUI _flowUI;

    [Header("Your High Score UI")]
    [SerializeField] private GameObject _yourHighScorePanel; // ハイスコア表示パネル
    [SerializeField] private Image _yourSkinImage; // プレイヤーのスキン画像
    [SerializeField] private TMPro.TextMeshProUGUI _yourNameText; // プレイヤー名
    [SerializeField] private TMPro.TextMeshProUGUI _yourScoreText; // スコア
    [SerializeField] private TMPro.TextMeshProUGUI _untilRankingText; // ランキング入りまでの差分

    public static RankingInventryManager instance;
    private PlayerNameManager _playerNameManager;
    private NicknameInputUI _nicknameInputUI;
    private SkinManager _skinManager;
    private SkinDatabase _skinDatabase;
    void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
    void Start()
    {
        _playerNameManager = PlayerNameManager.instance;
        _nicknameInputUI = NicknameInputUI.instance;
        _skinManager = SkinManager.instance;
        _skinDatabase = SkinDatabase.instance;

        _backButton.onClick.AddListener(() =>
        {
            _flowUI.SwitchView("Start"); // タイトルに戻る
        });

        // 名前変更ボタンのリスナー登録
        _changeNameButton.onClick.AddListener(OnChangeNameClicked);
    }

    // FlowUIのイベントで呼ぶ（OnOpen）
    public void OnRankingViewOpen()
    {
        RefreshRankingList();
        UpdateYourHighScore();
    }

    private void RefreshRankingList()
    {
        Debug.Log("RefreshRankingList called");

        // 一旦今ある表示を全消し
        foreach (Transform child in _contentTransform)
        {
            Destroy(child.gameObject);
        }

        // マネージャーからデータを取得して生成
        var ranking = RankingManager.instance.currentRanking;
        Debug.Log($"Ranking count: {ranking.Count}");

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

    /// <summary>
    /// 名前変更ボタンがクリックされたときの処理
    /// </summary>
    private void OnChangeNameClicked()
    {
        string currentName = _playerNameManager.GetPlayerName();
        _nicknameInputUI.ShowPanel(
            "What's your name",
            OnNameChanged,
            null // キャンセル時は何もしない
        );
    }

    /// <summary>
    /// 名前が変更されたときの処理
    /// </summary>
    private void OnNameChanged(string newName)
    {
        _playerNameManager.SavePlayerName(newName);
        // 名前を更新したらYour High Scoreの表示も更新
        UpdateYourHighScore();
        Debug.Log($"プレイヤー名を変更しました: {newName}");
    }

    /// <summary>
    /// Your High Score情報を更新
    /// </summary>
    private void UpdateYourHighScore()
    {
        // ベストスコアを取得
        float bestScore = PlayerPrefs.GetFloat("Stats_BestScore", 0f);

        // プレイヤー名を取得
        string playerName = _playerNameManager.GetPlayerName();

        // 現在装備中のスキンを取得
        int currentSkinID = _skinManager.currentSkinID;
        SkinData skinData = _skinDatabase.GetSkinById(currentSkinID);

        // UIに反映
        _yourNameText.text = playerName;
        _yourScoreText.text = bestScore.ToString("F2");
        _yourSkinImage.sprite = skinData.skinSprite;

        // ランキング入りまでの差分を計算
        UpdateUntilRankingText(bestScore);
    }

    /// <summary>
    /// ランキング入りまでの差分を計算して表示
    /// </summary>
    private void UpdateUntilRankingText(float yourScore)
    {
        var ranking = RankingManager.instance.GetCurrentRanking();

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