using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
/// <summary>
/// ゲームの状態管理
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public enum GameState
    {
        Loading,
        Title,
        Playing,
        GameOver
    }
    [SerializeField] private GameState _state;
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

        // シーン開始時の初期状態を設定
        // メインシーン（ゲームプレイシーン）ではLoading状態から始める
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "Main")
        {
            _state = GameState.Loading;
            Debug.Log("[GameManager] Initial state set to Loading");
        }
    }
    public void SetGameState(GameState state)
    {
        _state = state;
    }
}
