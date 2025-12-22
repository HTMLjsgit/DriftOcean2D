using UnityEngine;

public class PlayerSkinHandler : MonoBehaviour
{
    private Sprite _currentSkinSprite;
    public Sprite currentSkinSprite => _currentSkinSprite;
    private SpriteRenderer _spriteRenderer;
    private SkinManager _skinManager;
    private SkinDatabase _skinDatabase;
    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _skinManager = SkinManager.instance;
        _skinDatabase = SkinDatabase.instance;
        Sprite skinSprite = _skinDatabase.GetSkinById(_skinManager.currentSkinID).skinSprite;
        this.SetSkin(skinSprite);
    }
    public void SetSkin(Sprite skin)
    {
        _currentSkinSprite = skin;
        _spriteRenderer.sprite = _currentSkinSprite;
        
    }
    
}