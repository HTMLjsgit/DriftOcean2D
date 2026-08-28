using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OceanLogGarbageEntryUI : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private Image _garbageImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _eraText;
    [SerializeField] private TextMeshProUGUI _newText;

    private OceanLogUI _owner;
    private ObstacleData _data;

    public void Initialize(OceanLogUI owner, ObstacleData data, bool discovered, bool isNew)
    {
        _owner = owner;
        _data = data;

        _garbageImage.sprite = data.GetEncyclopediaSprite();
        _garbageImage.color = discovered ? Color.white : new Color(0.2f, 0.2f, 0.2f, 0.65f);
        
        if (Application.systemLanguage == SystemLanguage.Japanese)
        {
            _nameText.SetText(discovered ? data.displayName : "???");
            _eraText.SetText(discovered ? data.era : "???");
        }
        else
        {
            _nameText.SetText(discovered ? data.displayNameEnglish : "???");
            _eraText.SetText(discovered ? data.eraEnglish : "???");
        }
        
        _newText.gameObject.SetActive(isNew);
        _button.interactable = discovered;

        _button.onClick.RemoveListener(OnClicked);
        _button.onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        _owner.ShowGarbageDetails(_data);
    }
}
