using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
/// <summary>
/// プレイヤーの動き、値保持
/// </summary>
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private PlayerInput _playerInput;
    [SerializeField] private AudioSource _jumpAudioSource;
    private Rigidbody2D _rigidbody2D;

    private InputAction _jumpAction;
    private GameManager _gameManager;
    private GameOverManager _gameOverManager;
    public static PlayerController instance;
    // 無操作トラッキング用
    [Header("No Input Tracking")]
    [SerializeField] private float _noInputThreshold = 30f; // 無操作判定時間（秒）
    private float _lastInputTime;
    private bool _noInputUnlockTriggered = false;

    // 無操作解放が達成されたかのフラグ（外部からアクセス用）
    public bool NoInputUnlockAchieved { get; private set; } = false;
    void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _jumpAction = _playerInput.actions.FindAction("Jump");
        _gameManager = GameManager.instance;
        _gameOverManager = GameOverManager.instance;
        _lastInputTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        if(_gameManager.state == GameManager.GameState.GameOver) return;

        if (_jumpAction.triggered)
        {
            Jump();
            _lastInputTime = Time.time; // 入力時間をリセット
        }

        // 無操作チェック
        CheckNoInput();
    }

    private void CheckNoInput()
    {
        if (_noInputUnlockTriggered) return;

        float timeSinceLastInput = Time.time - _lastInputTime;
        if (timeSinceLastInput >= _noInputThreshold)
        {
            _noInputUnlockTriggered = true;
            NoInputUnlockAchieved = true;
            Debug.Log($"No input for {_noInputThreshold} seconds - Unlock condition met!");
        }
    }

    public void Jump(){
        _rigidbody2D.linearVelocityY = _jumpForce;
        _jumpAudioSource.Play();
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        //ゲームオーバー
        if(collider.gameObject.tag == "Obstacle")
        {
            _gameOverManager.GameOver();
        }
    }
    void OnTriggerExit2D(Collider2D collider)
    {
        
    }
}
