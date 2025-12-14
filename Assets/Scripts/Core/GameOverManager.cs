using UnityEngine;

public class GameOverManager : MonoBehaviour
{
    [SerializeField] private GameObject _gameOverPanel;
    public static GameOverManager instance;
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
    public void GameOver()
    {
        _gameOverPanel.SetActive(true);
        Time.timeScale = 0;
    }
}
