using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _scoreTextUI;
    [Header("Settings")]
    // 1秒間に増えるスコアの量（基準値）
    [SerializeField] private float _scorePerSecond = 1.0f; 

    // 時間経過の倍率（1.0なら通常、2.0なら2倍速でスコアが増える）
    // ゲームの難易度調整で「後半は時が経つのが早い」などを表現できます
    [SerializeField] private float _timeMultiplier = 1.0f;

    [Header("Status")]
    // 現在のスコア（初期値はインスペクターまたはInitで設定）
    public float currentScore = 1950.80f;
    
    // リセット時に戻すための初期値を保存しておく変数
    [SerializeField]private float _initialScore;

    private bool _scoreMeasureNow = false;
    private GameManager gameManager;
    public static ScoreManager instance;
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
        // 開始時のスコアを初期値として記憶しておく
        _initialScore = currentScore;
    }
    void Start()
    {
        // GameManagerが存在する場合のみ取得（エラー防止）
        gameManager = GameManager.instance;
    }

    void Update()
    {
        // 計測フラグが true のときだけ加算
        if (_scoreMeasureNow)
        {
            // Time.deltaTime (前のフレームからの経過時間) を使うことで
            // 端末のスペックに関わらず「1秒あたりの増加量」を一定にします。
            
            float increaseAmount = Time.deltaTime * _timeMultiplier * _timeMultiplier;
            currentScore += increaseAmount;
            _scoreTextUI.SetText(currentScore.ToString());
        }
    }

    /// <summary>
    /// スコア計測の初期化（リセット処理）
    /// </summary>
    public void ScoreMeasureInit()
    {
        currentScore = _initialScore; // スコアを最初の値（1950.80など）に戻す
        _scoreMeasureNow = false;     // 計測は止めておく
    }

    /// <summary>
    /// スコアの計測をスタート
    /// </summary>
    public void ScoreMeasureStart()
    {
        _scoreMeasureNow = true;
    }

    /// <summary>
    /// スコア計測を停止（ゲームオーバー時など）
    /// </summary>
    public void ScoreMeasureStop()
    {
        _scoreMeasureNow = false;
    }

    /// <summary>
    /// 外部（GameManagerなど）から、時間の進み具合（倍率）を変更する
    /// </summary>
    public void SetTimeMultiplier(float newMultiplier)
    {
        _timeMultiplier = newMultiplier;
    }

    public float getCurrentScore()
    {
        return currentScore;
    }
}