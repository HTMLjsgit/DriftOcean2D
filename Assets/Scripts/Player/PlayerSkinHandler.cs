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

        if (_spriteRenderer == null)
        {
            Debug.LogError("[PlayerSkinHandler] SpriteRenderer component not found on this GameObject!");
            return;
        }

        _skinManager = SkinManager.instance;
        _skinDatabase = SkinDatabase.instance;
        _cloudSaveManager = UGSCloudSaveManager.instance;
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
        // SpriteRendererの再確認（シーン遷移後に消えている可能性対策）
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                Debug.LogError("[PlayerSkinHandler] SpriteRenderer is null in ApplySkin! Cannot apply skin.");
                return;
            }
        }

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
            else
            {
                Debug.LogError("[PlayerSkinHandler] Fallback skin (ID=1) not found! Player will have no sprite.");
            }
        }
    }

    public void SetSkin(Sprite skin)
    {
        _currentSkinSprite = skin;
        _spriteRenderer.sprite = _currentSkinSprite;
    }
}