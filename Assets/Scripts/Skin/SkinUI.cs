using UnityEngine;
using UnityEngine.UI;

public class SkinUI : MonoBehaviour
{
    [SerializeField] private GameObject _checkedImageObject;
    [SerializeField] private int _skinID;
    [SerializeField] private Image _skinImageUI;
    [SerializeField] private Sprite _lockedSprite; // ロック状態のスプライト

    private Sprite _originalSprite; // 元のスキンスプライト
    public int skinId => _skinID;

    public void Initialize(Sprite sprite)
    {
        _originalSprite = sprite;
        UpdateUIState();
    }

    public void UpdateUIState()
    {
        // スキンが解放されているかチェック
        bool isUnlocked = SkinManager.instance.IsUnlocked(_skinID);
        bool isEquipped = SkinManager.instance.currentSkinID == _skinID;

        // 解放されていればオリジナルスプライト、ロック中ならロックスプライトを表示
        if (isUnlocked)
        {
            _skinImageUI.sprite = _originalSprite;
        }
        else
        {
            _skinImageUI.sprite = _lockedSprite;
        }

        // チェックマークは装備中かつ解放済みの場合のみ表示
        _checkedImageObject.SetActive(isEquipped && isUnlocked);

        Debug.Log($"SkinUI {_skinID}: UpdateUIState - currentSkinID={SkinManager.instance.currentSkinID}, isUnlocked={isUnlocked}, isEquipped={isEquipped}");
    }

    public void OnClickedSkinUI()
    {
        // ロックされている場合は何もしない
        if (!SkinManager.instance.IsUnlocked(_skinID))
        {
            Debug.Log($"Skin {_skinID} is locked!");
            return;
        }

        SkinManager.instance.EquipSkin(_skinID);
        Debug.Log($"Equipped Skin ID: {_skinID}");

        SkinInventryManager.instance.RefreshAllSlots();
    }
}