using System.Collections.Generic;
using UnityEngine;

public class SkinDatabase : Database
{
    public static SkinDatabase instance;
    [Header("Data Storage")]
    // InspectorでSkinDataを登録するためのリスト
    [SerializeField] private List<SkinData> _skinList = new List<SkinData>();

    // 検索を高速化するための辞書（ID検索用）
    private Dictionary<int, SkinData> _skinMap = new Dictionary<int, SkinData>();

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
        Initialize();
    }

    public override void Initialize()
    {
        base.Initialize();
        _skinMap.Clear();
        foreach (var skin in _skinList)
        {
            if (skin == null) continue;

            if (!_skinMap.ContainsKey(skin.id))
            {
                _skinMap.Add(skin.id, skin);
            }
            else
            {
                Debug.LogWarning($"Skin ID重複警告: ID {skin.id} が重複しています。");
            }
        }
        Debug.Log($"SkinDatabase Initialized. Loaded {_skinMap.Count} skins.");
    }
    void Start()
    {
    }
    /// <summary>
    /// IDを指定してSkinDataを取得する (最速)
    /// </summary>
    public SkinData GetSkinById(int id)
    {
        Debug.Log("_skinMap: " + _skinMap.Count);
        Debug.Log("id: " + id);
        if (_skinMap.TryGetValue(id, out SkinData skin))
        {
            return skin;
        }
        
        Debug.LogError($"Skin ID: {id} が見つかりません。");
        return null;
    }
    /// <summary>
    /// スキンの総数を取得
    /// </summary>
    public int GetSkinCount()
    {
        return _skinList.Count;
    }
    /// <summary>
    /// 全てのスキンリストを取得する
    /// </summary>
    public List<SkinData> GetAllSkins()
    {
        return _skinList;
    }
}
