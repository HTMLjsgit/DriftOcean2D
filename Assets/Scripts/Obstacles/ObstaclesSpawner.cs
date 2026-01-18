using System.Collections.Generic;
using UnityEngine;

public class ObstaclesSpawner : MonoBehaviour
{
    public static ObstaclesSpawner instance;

    [Header("Spawn Settings")]
    [SerializeField] private List<ObstacleData> _currentObstacleDatas = new List<ObstacleData>();

    [SerializeField] private List<GameObject> _spawnedObstacles = new List<GameObject>();
    public List<GameObject> spawnedObstacles => _spawnedObstacles;

    public bool spawn = true;

    [Header("Spawn Position Settings")]
    [SerializeField] private List<Transform> _spawnPositions = new List<Transform>(); // ランダムスポーン位置リスト
    [SerializeField] private GameObject _obstacles; // 後方互換のため残す（使用されない場合はnull可）

    [SerializeField] private float _spawnTime = 2.0f;
    [SerializeField] private float _spawnTimeNow;

    private float _globalSpeedMultiplier = 1.0f;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    void Update()
    {
        if (spawn)
        {
            _spawnTimeNow += Time.deltaTime;
            if (_spawnTimeNow > _spawnTime)
            {
                SpawnObstacle();
                _spawnTimeNow = 0;
            }
        }
    }

    public void SpawnObstacle()
    {
        if (_currentObstacleDatas == null || _currentObstacleDatas.Count == 0) return;

        // スポーン位置を決定（リストからランダム選択、もしくは_obstaclePosフォールバック）
        Transform spawnTransform = GetRandomSpawnPosition();
        if (spawnTransform == null)
        {
            Debug.LogWarning("[ObstaclesSpawner] No spawn position available!");
            return;
        }

        int dataIndex = Random.Range(0, _currentObstacleDatas.Count);
        ObstacleData selectedData = _currentObstacleDatas[dataIndex];

        // _obstaclesの子として生成（スポーン位置そのままを使用）
        GameObject obstacle = Instantiate(selectedData.prefab, spawnTransform.position, spawnTransform.rotation);
        obstacle.transform.SetParent(_obstacles.transform);

        _spawnedObstacles.Add(obstacle);

        ObstacleMover obstacleMover = obstacle.GetComponent<ObstacleMover>();

        // ゴミ固有速度 × 全体倍率
        float finalSpeed = selectedData.baseSpeed * _globalSpeedMultiplier;
        obstacleMover.Move(finalSpeed);
    }

    /// <summary>
    /// ランダムなスポーン位置を取得
    /// </summary>
    /// <returns>選択されたスポーン位置のTransform</returns>
    private Transform GetRandomSpawnPosition()
    {
        // _spawnPositionsリストが有効な場合はランダムに選択
        if ( _spawnPositions.Count > 0)
        {
            int randomIndex = Random.Range(0, _spawnPositions.Count);
            Transform selectedTransform = _spawnPositions[randomIndex];

            if (selectedTransform != null)
            {
                return selectedTransform;
            }
            else
            {
                Debug.LogWarning($"[ObstaclesSpawner] Spawn position at index {randomIndex} is null!");
            }
        }

        return null;
    }

    public void ObstacleListRemove(GameObject key)
    {
        if (_spawnedObstacles.Contains(key))
        {
            _spawnedObstacles.Remove(key);
        }
    }

    /// <summary>
    /// 画面上に出現している全ての障害物を削除する
    /// </summary>
    public void ClearAllObstacles()
    {
        // リスト内の全てのオブジェクトを破壊
        foreach (var obstacle in _spawnedObstacles)
        {
            Destroy(obstacle);
        }
        
        // リストの中身を空にする
        _spawnedObstacles.Clear();
    }
    
    /// <summary>
    /// AddDifficultyStage
    /// </summary>
    /// <param name="clearPrevious">trueなら前の時代のゴミリストを消去して入れ替える</param>
    public void AddDifficultyStage(float interval, float speedMultiplier, List<ObstacleData> newObstacles, bool clearPrevious = true)
    {
        _spawnTime = interval;           
        _globalSpeedMultiplier = speedMultiplier; 
        
        // フラグがtrueなら、リストをクリア（リセット）する
        if (clearPrevious)
        {
            _currentObstacleDatas.Clear();
        }

        // リスト追加処理
        if (newObstacles != null && newObstacles.Count > 0)
        {
            _currentObstacleDatas.AddRange(newObstacles);
        }
        
        Debug.Log($"現在のゴミの種類数: {_currentObstacleDatas.Count}");
    }

    /// <summary>
    /// スポーン位置をGizmosで視覚化（エディタで常に表示）
    /// </summary>
    void OnDrawGizmos()
    {
        // スポーン位置リストの可視化（緑色）
        if (_spawnPositions.Count > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < _spawnPositions.Count; i++)
            {
                Transform spawnPos = _spawnPositions[i];
                // 球体で位置を表示
                Gizmos.DrawWireSphere(spawnPos.position, 0.5f);

                // 番号を表示するために十字マークを追加
                Gizmos.DrawLine(spawnPos.position + Vector3.up * 0.3f, spawnPos.position - Vector3.up * 0.3f);
                Gizmos.DrawLine(spawnPos.position + Vector3.right * 0.3f, spawnPos.position - Vector3.right * 0.3f);
            }
        }
    }
}