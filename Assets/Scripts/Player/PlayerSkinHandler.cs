using UnityEngine;

public class PlayerSkinHandler : MonoBehaviour
{
    private Sprite _currentSkinSprite;
    public Sprite currentSkinSprite => _currentSkinSprite;
    private SpriteRenderer _spriteRenderer;
    private SkinManager _skinManager;
    private SkinDatabase _skinDatabase;
    private UGSCloudSaveManager _cloudSaveManager;

    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _skinManager = SkinManager.instance;
        _skinDatabase = SkinDatabase.instance;
        _cloudSaveManager = UGSCloudSaveManager.instance;

        if (_cloudSaveManager != null)
        {
            // データが既にロード済みの場合は即座に適用
            if (_cloudSaveManager.IsDataLoaded)
            {
                ApplySkin();
            }
            else
            {
                // データロード完了を待つ
                _cloudSaveManager.OnDataLoaded += OnDataLoaded;
            }
        }
        else
        {
            // UGSCloudSaveManagerがない場合はフォールバック
            Debug.LogWarning("[PlayerSkinHandler] UGSCloudSaveManager not found. Using default skin.");
            ApplySkin();
        }
    }

    void OnDestroy()
    {
        // イベント購読を解除
        if (_cloudSaveManager != null)
        {
            _cloudSaveManager.OnDataLoaded -= OnDataLoaded;
        }
    }

    /// <summary>
    /// UGSデータロード完了時のコールバック
    /// </summary>
    private void OnDataLoaded()
    {
        Debug.Log("[PlayerSkinHandler] UGS data loaded, applying skin...");
        ApplySkin();
    }

    /// <summary>
    /// スキンを適用する
    /// </summary>
    private void ApplySkin()
    {
        // 最新の参照を取得（シーン遷移対策）
        _skinManager = SkinManager.instance;
        _skinDatabase = SkinDatabase.instance;

        if (_skinManager == null || _skinDatabase == null)
        {
            Debug.LogError("[PlayerSkinHandler] SkinManager or SkinDatabase is null!");
            return;
        }

        int skinID = _skinManager.currentSkinID;
        SkinData skinData = _skinDatabase.GetSkinById(skinID);

        if (skinData != null && skinData.skinSprite != null)
        {
            SetSkin(skinData.skinSprite);
            Debug.Log($"[PlayerSkinHandler] Skin applied: ID={skinID}");
        }
        else
        {
            Debug.LogWarning($"[PlayerSkinHandler] Could not find skin with ID={skinID}, using fallback.");
            // フォールバック：初期スキン（ID=1）を使用
            skinData = _skinDatabase.GetSkinById(1);
            if (skinData != null && skinData.skinSprite != null)
            {
                SetSkin(skinData.skinSprite);
            }
        }
    }

    public void SetSkin(Sprite skin)
    {
        _currentSkinSprite = skin;
        if (_spriteRenderer != null)
        {
            _spriteRenderer.sprite = _currentSkinSprite;
        }
    }
}