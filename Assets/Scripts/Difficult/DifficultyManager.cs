using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using System;

public class DifficultyManager : MonoBehaviour
{
    [Header("References")]
    private ScoreManager _scoreManager;
    private ObstaclesSpawner _obstacleSpawner;
    private StageBGMManager _bgmStageManager;

    [Header("Stages")]
    [SerializeField] private List<DifficultyProfile> difficultyStages;
    public bool maxDifficultyMode;
    private List<DifficultyProfile> _pendingStages;
    private bool _isChangingDifficulty = false;
    public static DifficultyManager instance;

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
        _scoreManager = ScoreManager.instance;
        _obstacleSpawner = ObstaclesSpawner.instance;
        _bgmStageManager = StageBGMManager.instance;
        InitializePendingStages();
    }

    /// <summary>
    /// ペンディングステージリストを初期化
    /// </summary>
    private void InitializePendingStages()
    {
        _pendingStages = difficultyStages.OrderBy(x => x.thresholdYear).ToList();
        Debug.Log($"[DifficultyManager] Initialized {_pendingStages.Count} difficulty stages");
    }

    /// <summary>
    /// リトライ時に難易度を最初の状態に戻す
    /// </summary>
    public void ResetDifficulty()
    {
        Debug.Log("[DifficultyManager] Resetting difficulty to initial state");

        // ペンディングステージを再初期化
        InitializePendingStages();

        // 処理中フラグをリセット
        _isChangingDifficulty = false;

        // 最高難易度モードをリセット
        maxDifficultyMode = false;

        // BGMを通常に戻す
        if (_bgmStageManager != null)
        {
            _bgmStageManager.ResetBGM();
        }

        // 最初の難易度プロファイルを適用（1960年の状態）
        if (difficultyStages.Count > 0)
        {
            var firstStage = difficultyStages.OrderBy(x => x.thresholdYear).FirstOrDefault();
            if (firstStage != null)
            {
                _obstacleSpawner.AddDifficultyStage(
                    firstStage.spawnInterval,
                    firstStage.speedMultiplier,
                    firstStage.newObstaclesToAdd,
                    true
                );
                Debug.Log($"[DifficultyManager] Applied initial difficulty: {firstStage.thresholdYear}");
            }
        }
    }

    void Update()
    {
        //  処理中(_isChangingDifficulty)なら何もしないで帰る
        if (_pendingStages.Count == 0 || _isChangingDifficulty) return;

        DifficultyProfile nextStage = _pendingStages[0];

        if (_scoreManager.currentScore >= nextStage.thresholdYear)
        {
            ApplyDifficulty(nextStage).Forget();

            //  実行したらリストから削除して、次は「次の年代」を見るようにする
            _pendingStages.RemoveAt(0);
        }
    }

    private async UniTaskVoid ApplyDifficulty(DifficultyProfile profile)
    {
        _isChangingDifficulty = true;
        
        var token = this.GetCancellationTokenOnDestroy();

        // 休憩がある場合
        if(profile.restDuration > 0f)
        {
            Debug.Log($"<color=cyan>休憩タイム突入！ {profile.restDuration}秒間 敵が出ません</color>");
            _obstacleSpawner.spawn = false;
            
            // 休憩中も他の処理（ゲームオーバーなど）でオブジェクトが消えるとエラーになるのでTokenを渡す
            await UniTask.Delay(TimeSpan.FromSeconds(profile.restDuration), cancellationToken: token);
        }

        Debug.Log($"年数が {profile.thresholdYear} を超えました。難易度を更新します。");
        
        // trueを渡しているので、以前のゴミリストは消去されて新しいものに入れ替わる
        _obstacleSpawner.AddDifficultyStage(profile.spawnInterval, profile.speedMultiplier, profile.newObstaclesToAdd, true);
        
        _obstacleSpawner.spawn = true;
        _isChangingDifficulty = false;

        if (profile.maxDifficulty)
        {
            // 最高難易度ならば
            this.maxDifficultyMode = true;
            Debug.Log("[DifficultyManager] 最高難易度（鬼畜ステージ）に到達！");

            // BGMを最高難易度用に切り替え
            _bgmStageManager.SwitchToMaxDifficultyBGM().Forget();
        }
        else if (profile.middleDifficulty)
        {
            // 中間難易度ならば
            Debug.Log("[DifficultyManager] 中間難易度に到達！");

            // BGMを中間難易度用に切り替え
            _bgmStageManager.SwitchToMidDifficultyBGM().Forget();
        }
        else
        {
            this.maxDifficultyMode = false;
        }
    }
}