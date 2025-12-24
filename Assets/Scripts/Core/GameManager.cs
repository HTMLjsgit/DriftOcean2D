using Unity.VisualScripting;
using UnityEngine;
/// <summary>
/// ゲームの状態管理
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public enum GameState
    {
        Title,
        Playing,
        GameOver
    }
    [SerializeField] private GameState _state;
    private ObstaclesSpawner _obstaclesSpawner;
    public GameState state => _state;
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
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _obstaclesSpawner = ObstaclesSpawner.instance;
    }

    // Update is called once per frame
    void Update()
    {
        if(GameState.Playing == _state) gamePlaying();
    }

    /// <summary>
    /// ゲームプレイ中動作
    /// </summary>
    public void gamePlaying()
    {
    }

    public void SetGameState(GameState state)
    {
        _state = state;
    }
}
