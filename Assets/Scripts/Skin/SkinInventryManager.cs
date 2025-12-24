using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SkinInventryManager : MonoBehaviour
{
    public static SkinInventryManager instance;

    [Header("UI References")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private FlowUI _flowUI;
    
    [Header("Skin List Settings")]
    [SerializeField] private List<SkinUI> _skinSlots; 

    void Awake()
    {
        if(instance == null) instance = this;
        else Destroy(this.gameObject);
    }

    void Start()
    {
        _closeButton.onClick.AddListener(() => _flowUI.SwitchView("Start"));
    }

    public void ApplySkinSprites()
    {
        Debug.Log("ApplySkinSprites called");
        foreach (var slot in _skinSlots)
        {
            var data = SkinDatabase.instance.GetSkinById(slot.skinId);
            slot.Initialize(data.skinSprite);
        }
    }

    public void RefreshAllSlots()
    {
        Debug.Log("RefreshAllSlots called");
        _skinSlots.ForEach(slot => slot.UpdateUIState());
    }
}