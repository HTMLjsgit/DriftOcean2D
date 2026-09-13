using UnityEngine;

/// <summary>
/// プレイヤー名の保存・読み込みを管理
/// </summary>
public class PlayerNameManager : MonoBehaviour
{
    public static PlayerNameManager instance;

    private const string KEY_PLAYER_NAME = "PlayerName";
    private const string DEFAULT_PLAYER_NAME = "player";

    [SerializeField] private string _currentPlayerName;
    public string currentPlayerName => _currentPlayerName;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        LoadPlayerName();
    }

    /// <summary>
    /// プレイヤー名をロード
    /// </summary>
    public void LoadPlayerName()
    {
        _currentPlayerName = PlayerPrefs.GetString(KEY_PLAYER_NAME, "");
        Debug.Log($"プレイヤー名をロード: {_currentPlayerName}");
    }

    /// <summary>
    /// プレイヤー名を保存
    /// </summary>
    /// <param name="playerName">保存する名前</param>
    public void SavePlayerName(string playerName)
    {
        if (!PlayerNameFilter.IsAllowed(playerName)) return;
        playerName = playerName?.Trim();
        // 空の場合はデフォルト名を使用
        if (string.IsNullOrEmpty(playerName))
        {
            playerName = DEFAULT_PLAYER_NAME;
        }

        _currentPlayerName = playerName;
        PlayerPrefs.SetString(KEY_PLAYER_NAME, playerName);
        PlayerPrefs.Save();

        Debug.Log($"プレイヤー名を保存: {playerName}");
    }

    /// <summary>
    /// プレイヤー名が設定されているかチェック
    /// </summary>
    /// <returns>名前が設定されていればtrue</returns>
    public bool HasPlayerName()
    {
        bool hasName = !string.IsNullOrEmpty(_currentPlayerName);
        Debug.Log($"HasPlayerName: {hasName}, currentPlayerName='{_currentPlayerName}'");
        return hasName;
    }

    /// <summary>
    /// プレイヤー名を取得（未設定ならデフォルト名）
    /// </summary>
    public string GetPlayerName()
    {
        if (string.IsNullOrEmpty(_currentPlayerName))
        {
            return DEFAULT_PLAYER_NAME;
        }
        return _currentPlayerName;
    }

    /// <summary>
    /// UGSCloudSaveManagerから名前を同期（UGS使用時に呼ぶ）
    /// </summary>
    public void SyncFromUGS(string nameFromUGS)
    {
        if (!string.IsNullOrEmpty(nameFromUGS))
        {
            _currentPlayerName = nameFromUGS;
            PlayerPrefs.SetString(KEY_PLAYER_NAME, nameFromUGS);
            PlayerPrefs.Save();
            Debug.Log($"プレイヤー名をUGSから同期: {nameFromUGS}");
        }
    }
}
