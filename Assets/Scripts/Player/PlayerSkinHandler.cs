using UnityEngine;

public class PlayerSkinHandler : MonoBehaviour
{
    private Sprite _currentSkinSprite;
    public Sprite currentSkinSprite => _currentSkinSprite;
    private SpriteRenderer _spriteRenderer;
    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }
    public void SetSkin(Sprite skin)
    {
        _currentSkinSprite = skin;
        _spriteRenderer.sprite = _currentSkinSprite;
    }
    
}