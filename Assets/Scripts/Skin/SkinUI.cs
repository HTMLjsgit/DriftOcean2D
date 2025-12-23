using UnityEngine;
using UnityEngine.UI;

public class SkinUI : MonoBehaviour
{
    [SerializeField] private GameObject _checkedImageObject;
    [SerializeField] private int _skinID;
    [SerializeField] private Image _skinImageUI;
    
    public int skinId => _skinID;

    public void Initialize(Sprite sprite)
    {
        _skinImageUI.sprite = sprite;
        UpdateUIState();
    }

    public void UpdateUIState()
    {
        bool isEquipped = SkinManager.instance.currentSkinID == _skinID;
        _checkedImageObject.SetActive(isEquipped);
    }

    public void OnClickedSkinUI()
    {
        SkinManager.instance.EquipSkin(_skinID);
        Debug.Log($"Equipped Skin ID: {_skinID}");

        SkinInventryManager.instance.RefreshAllSlots();
    }
}