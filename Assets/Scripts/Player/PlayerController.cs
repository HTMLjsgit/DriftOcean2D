using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
/// <summary>
/// プレイヤーの動き、値保持
/// </summary>
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private PlayerInput playerInput;
    private Rigidbody2D _rigidbody2D;
    
    private InputAction _jumpAction;
    private GameManager _gameManager;
    private GameOverManager _gameOverManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _jumpAction = playerInput.actions.FindAction("Jump");
        _gameManager = GameManager.instance;
        _gameOverManager = GameOverManager.instance;
    }

    // Update is called once per frame
    void Update()
    {
        if (_jumpAction.triggered)
        {
            Jump();
        }
    }

    public void Jump(){
        _rigidbody2D.linearVelocityY = _jumpForce;
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
