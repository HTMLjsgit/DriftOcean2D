using UnityEngine;
using UnityEngine.UI;

public class TitleController : MonoBehaviour
{
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _skinButton;
    [SerializeField] private Button _rankingButton;
    [SerializeField] private FlowUI _flowUI;
    private SkinInventryManager _startSkinManager;
    private RankingInventryManager _rankingInventryManager;
    private SceneController _sceneController;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _rankingInventryManager = RankingInventryManager.instance;
        _startSkinManager = SkinInventryManager.instance;
        _sceneController = SceneController.instance;
        // _startButton.onClick.AddListener();
        _skinButton.onClick.AddListener(() =>
        {
            _flowUI.SwitchView("Skin");
            _startSkinManager.ApplySkinSprites();
        });
        _rankingButton.onClick.AddListener(() =>
        {
            _flowUI.SwitchView("Ranking");
            _rankingInventryManager.OnRankingViewOpen();
        });
        _startButton.onClick.AddListener(() =>
        {
            _sceneController.SceneLoad("Main");
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
