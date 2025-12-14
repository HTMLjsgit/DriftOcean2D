using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager instance;
    private ScoreManager _scoreManager;
    private ObstaclesSpawner _obstaclesSpawner;
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
        StageStart();

    }

    private void StageStart()
    {
        _scoreManager.ScoreMeasureInit();
        _scoreManager.ScoreMeasureStart();
        _obstaclesSpawner.spawn = true;
    }
}