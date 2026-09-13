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
    private DifficultyManager _difficultyManager;
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
        _difficultyManager = DifficultyManager.instance;
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
    /// <summary>
    /// リトライ処理（完全に最初からやり直し）
    /// プレイ回数としてカウントし、5回ごとに広告を表示
    /// </summary>
    public void StageRetry()
    {
        // 1. まずリセット処理を行う（Time.timeScale = 0 のままでOK）
        currentPlayTime = 0;
        _scoreManager.ScoreMeasureInit();
        if (OceanLifeManager.instance != null) OceanLifeManager.instance.ResetRun();
        _obstaclesSpawner.ClearAllObstacles();
        _obstaclesSpawner.spawn = false;

        // 難易度を最初の状態にリセット
        if (_difficultyManager != null)
        {
            _difficultyManager.ResetDifficulty();
        }

        // 2. プレイ回数カウント＆広告チェック（5回ごとに広告表示）
        //    広告表示後（または広告なしの場合は即座に）ゲームを開始
        _adsManager.OnGamePlayStart(onComplete: () =>
        {
            // 3. 広告が終わってからtimeScale = 1にしてゲーム開始
            Time.timeScale = 1;
            _scoreManager.ScoreMeasureStart();
            _obstaclesSpawner.spawn = true;
            SetCurrentPlay(true);
        });
    }
    public void StageResume()
    {
        if (OceanLifeManager.instance != null) OceanLifeManager.instance.ResumeRun();
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
