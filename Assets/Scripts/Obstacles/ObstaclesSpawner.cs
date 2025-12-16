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
    [SerializeField] private GameObject _obstaclePos; 

    [SerializeField] private float _spawnTime = 2.0f;
    [SerializeField]private float _spawnTimeNow;
    
    private float _globalSpeedMultiplier = 1.0f;

    [SerializeField] private int _randomSpawnYRange = 5;

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

        int dataIndex = Random.Range(0, _currentObstacleDatas.Count);
        ObstacleData selectedData = _currentObstacleDatas[dataIndex];

        GameObject obstacle = Instantiate(selectedData.prefab, _obstaclePos.transform);
        
        float r = Random.Range(-1f * _randomSpawnYRange, (float)_randomSpawnYRange);
        obstacle.transform.position = new Vector2(obstacle.transform.position.x, obstacle.transform.position.y + r);

        _spawnedObstacles.Add(obstacle);

        ObstacleMover obstacleMover = obstacle.GetComponent<ObstacleMover>();
        
        // ゴミ固有速度 × 全体倍率
        float finalSpeed = selectedData.baseSpeed * _globalSpeedMultiplier;
        obstacleMover.Move(finalSpeed);
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
            // 念のためnullチェック（既に破壊されている場合などを考慮）
            if (obstacle != null)
            {
                Destroy(obstacle);
            }
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
}