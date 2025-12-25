using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public float currentPlayTime;
    private bool _currentPlay;
    public static StageManager instance;
    private ScoreManager _scoreManager;
    private ObstaclesSpawner _obstaclesSpawner;
    private AdsManager _adsManager;
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
        _scoreManager = ScoreManager.instance;
        _obstaclesSpawner = ObstaclesSpawner.instance;
        _adsManager = AdsManager.instance;
        StageStart();

    }
    void Update()
    {
        if (_currentPlay)
        {
            currentPlayTime += Time.deltaTime;
        }
    }
    private void StageStart()
    {
        _scoreManager.ScoreMeasureInit();
        _scoreManager.ScoreMeasureStart();
        _obstaclesSpawner.spawn = true;
        SetCurrentPlay(true);

        // プレイ回数カウント（5回ごとに広告表示）
        _adsManager.OnGamePlayStart();
    }
    public void StageResume()
    {
        // Init（初期化）は呼ばずに、計測だけ再開する
        _scoreManager.ScoreMeasureStart();
        _obstaclesSpawner.spawn = true;
        _obstaclesSpawner.ClearAllObstacles();
        SetCurrentPlay(true);
    }
    public void SetCurrentPlay(bool play)
    {
        _currentPlay = play;
    }
}