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

    [Header("Stages")]
    [SerializeField] private List<DifficultyProfile> difficultyStages;

    private List<DifficultyProfile> _pendingStages;
    private bool _isChangingDifficulty = false;

    void Start()
    {
        _scoreManager = ScoreManager.instance;
        _obstacleSpawner = ObstaclesSpawner.instance;
        _pendingStages = difficultyStages.OrderBy(x => x.thresholdYear).ToList();
    }

    void Update()
    {
        // ★修正1: 処理中(_isChangingDifficulty)なら何もしないで帰る
        if (_pendingStages.Count == 0 || _isChangingDifficulty) return;

        DifficultyProfile nextStage = _pendingStages[0];

        if (_scoreManager.currentScore >= nextStage.thresholdYear)
        {
            ApplyDifficulty(nextStage).Forget();

            // ★修正2: 実行したらリストから削除して、次は「次の年代」を見るようにする
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
    }
}