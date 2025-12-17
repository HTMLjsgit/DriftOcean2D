using UnityEngine;
using UnityEngine.UI;

public class TitleController : MonoBehaviour
{
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _skinButton;
    [SerializeField] private Button _rankingButton;
    [SerializeField] private FlowUI _flowUI;
    private StartSkinManager _startSkinManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _startSkinManager = StartSkinManager.instance;
        // _startButton.onClick.AddListener();
        _skinButton.onClick.AddListener(() =>
        {
            _flowUI.SwitchView("Skin");
            // _startSkinManager.OnOpen();
        });
        _rankingButton.onClick.AddListener(() =>
        {
            _flowUI.SwitchView("Ranking");
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
