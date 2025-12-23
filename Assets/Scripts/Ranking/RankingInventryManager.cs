using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RankingInventryManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform _contentTransform; // ScrollViewのContent
    [SerializeField] private GameObject _rankingRowPrefab; // 1行分のプレハブ
    [SerializeField] private Button _backButton;
    [SerializeField] private FlowUI _flowUI;
    public static RankingInventryManager instance;
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
        _backButton.onClick.AddListener(() =>
        {
            _flowUI.SwitchView("Start"); // タイトルに戻る
        });
    }

    // FlowUIのイベントで呼ぶ（OnOpen）
    public void OnRankingViewOpen()
    {
        RefreshRankingList();
    }

    private void RefreshRankingList()
    {
        // 一旦今ある表示を全消し
        foreach (Transform child in _contentTransform)
        {
            Destroy(child.gameObject);
        }

        // マネージャーからデータを取得して生成
        var ranking = RankingManager.instance.currentRanking;

        for (int i = 0; i < ranking.Count; i++)
        {
            var entry = ranking[i];
            
            // プレハブ生成
            GameObject row = Instantiate(_rankingRowPrefab, _contentTransform);
            
            // テキスト設定（プレハブにRankingRowスクリプトがついている想定）
            // もしスクリプトを作るのが面倒なら、FindやGetChildでTextを探してもOKです
            RankingUI rowScript = row.GetComponent<RankingUI>();
            rowScript.SetData(i + 1, entry.playerName, entry.score, entry.date);
        }
    }
}