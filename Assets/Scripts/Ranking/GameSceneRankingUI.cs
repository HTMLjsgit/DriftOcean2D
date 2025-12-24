using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// ゲームシーンでランキングを表示するためのUI管理
/// </summary>
public class GameSceneRankingUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject _rankingPanel; // ランキング表示パネル
    [SerializeField] private Transform _contentTransform; // ScrollViewのContent
    [SerializeField] private GameObject _rankingRowPrefab; // 1行分のプレハブ
    [SerializeField] private Button _closeButton; // 閉じるボタン
    [SerializeField] private Button _changeNameButton; // 名前変更ボタン

    [Header("Your High Score UI")]
    [SerializeField] private GameObject _yourHighScorePanel; // ハイスコア表示パネル
    [SerializeField] private Image _yourSkinImage; // プレイヤーのスキン画像
    [SerializeField] private TextMeshProUGUI _yourNameText; // プレイヤー名
    [SerializeField] private TextMeshProUGUI _yourScoreText; // スコア

    public static GameSceneRankingUI instance;
    private RankingManager _rankingManager;
    private PlayerNameManager _playerNameManager;
    private SkinManager _skinManager;
    private SkinDatabase _skinDatabase;
    private NicknameInputUI _nicknameInputUI;

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

        // 閉じるボタンのリスナー登録
        _closeButton.onClick.AddListener(HideRanking);

        // 名前変更ボタンのリスナー登録
        _changeNameButton.onClick.AddListener(OnChangeNameClicked);

        // 初期状態では非表示
        HideRanking();
    }

    /// <summary>
    /// ランキングパネルを表示
    /// </summary>
    public void ShowRanking()
    {
        _rankingPanel.SetActive(true);
        RefreshRankingList();
        UpdateYourHighScore();
    }

    /// <summary>
    /// ランキングパネルを非表示
    /// </summary>
    public void HideRanking()
    {
        _rankingPanel.SetActive(false);
    }

    /// <summary>
    /// ランキングリストを更新して表示
    /// </summary>
    private void RefreshRankingList()
    {
        // 既存の表示を全削除
        foreach (Transform child in _contentTransform)
        {
            Destroy(child.gameObject);
        }

        // ランキングデータを取得して表示
        var ranking = _rankingManager.GetCurrentRanking();

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
    private void OnNameChanged(string newName)
    {
        _playerNameManager.SavePlayerName(newName);
        // 名前を更新したらYour High Scoreの表示も更新
        UpdateYourHighScore();
    }
}
