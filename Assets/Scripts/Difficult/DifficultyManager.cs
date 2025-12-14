using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using System; // UniTaskを使うために必要
public class DifficultyManager : MonoBehaviour
{
    [Header("References")]
    private ScoreManager _scoreManager;
    private ObstaclesSpawner _obstacleSpawner;

    [Header("Stages")]
    [SerializeField] private List<DifficultyProfile> difficultyStages;

    private List<DifficultyProfile> _pendingStages;
    // 現在難易度切り替え処理中かどうかを防ぐフラグ
    private bool _isChangingDifficulty = false;
    void Start()
    {
        _scoreManager = ScoreManager.instance;
        _obstacleSpawner = ObstaclesSpawner.instance;
        _pendingStages = difficultyStages.OrderBy(x => x.thresholdYear).ToList();
    }

    void Update()
    {
        if (_pendingStages.Count == 0) return;

        DifficultyProfile nextStage = _pendingStages[0];

        if (_scoreManager.currentScore >= nextStage.thresholdYear)
        {
            ApplyDifficulty(nextStage).Forget();
        }
    }

    // void ApplyDifficulty(DifficultyProfile profile)
    // {
    //     Debug.Log($"年数が {profile.thresholdYear} を超えました。難易度を更新します。");

    //     // [修正] Spawner側の修正したメソッドを呼び出します
    //     _obstacleSpawner.AddDifficultyStage(profile.spawnInterval, profile.speedMultiplier, profile.newObstaclesToAdd);

    //     _pendingStages.RemoveAt(0);
    // }
    private async UniTaskVoid ApplyDifficulty(DifficultyProfile profile)
    {
        _isChangingDifficulty = true;
        var token = this.GetCancellationTokenOnDestroy();
        if(profile.restDuration > 0f)
        {
            Debug.Log($"<color=cyan>休憩タイム突入！ {profile.restDuration}秒間 敵が出ません</color>");
            _obstacleSpawner.spawn = false;
            await UniTask.Delay(TimeSpan.FromSeconds(profile.restDuration), cancellationToken: token);

        }
        Debug.Log($"年数が {profile.thresholdYear} を超えました。難易度を更新します。");
        _obstacleSpawner.AddDifficultyStage(profile.spawnInterval, profile.speedMultiplier, profile.newObstaclesToAdd, true);
        _obstacleSpawner.spawn = true;
        _isChangingDifficulty = false;
    }
}