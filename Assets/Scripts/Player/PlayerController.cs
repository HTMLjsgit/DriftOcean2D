using UnityEngine;
using DG.Tweening;
/// <summary>
/// プレイヤーの動き、値保持
/// </summary>
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private AudioSource _jumpAudioSource;
    private Rigidbody2D _rigidbody2D;
    private GameManager _gameManager;
    private GameOverManager _gameOverManager;
    public static PlayerController instance;

    // 初期位置（リトライ時にリセットするため）
    private Vector3 _initialPosition;
    private Vector3 _defaultLocalScale;
    // 無操作トラッキング用
    [Header("No Input Tracking")]
    [SerializeField] private float _noInputThreshold = 30f; // 無操作判定時間（秒）
    private float _lastInputTime;
    private bool _noInputUnlockTriggered = false;

    // ぷにぷにアニメーション用
    [Header("Bounce Animation Settings")]
    [SerializeField] private float _squashDuration = 0.15f; // 縮む時間
    [SerializeField] private float _squashScale = 0.8f; // 縮む大きさ（1.0が元のサイズ、0.8で20%縮む）
    [SerializeField] private Ease _squashEase = Ease.OutQuad; // 縮むときのイージング
    [SerializeField] private Ease _recoverEase = Ease.OutBack; // 戻るときのイージング
    private SpriteRenderer _spriteRenderer;
    private bool _isAnimating = false; // アニメーション再生中フラグ

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

        // 初期位置を保存
        _initialPosition = transform.position;

        // Rigidbody2Dを取得してgravityScaleを0に設定（シーン開始時は落下させない）
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _rigidbody2D.gravityScale = 0;
        Debug.Log("[PlayerController] gravityScale set to 0 in Awake");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // _rigidbody2DはAwakeで既に取得済み
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _gameManager = GameManager.instance;
        _gameOverManager = GameOverManager.instance;
        _lastInputTime = Time.time;
        _defaultLocalScale = this.gameObject.transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        if(_gameManager.state == GameManager.GameState.GameOver) return;

        // 無操作チェック
        CheckNoInput();
    }

    /// <summary>
    /// UI Panelからタップで呼ばれるジャンプ処理
    /// EventTrigger経由で呼び出される
    /// </summary>
    public void OnTapJump()
    {
        if(_gameManager.state == GameManager.GameState.GameOver) return;

        // gravityScaleが0の場合はジャンプできない（ゲーム開始前）
        if(_rigidbody2D.gravityScale == 0)
        {
            Debug.Log("[PlayerController] Jump blocked - gravityScale is 0");
            return;
        }

        Jump();
        _lastInputTime = Time.time; // 入力時間をリセット
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
        PlayBounceAnimation();
    }

    /// <summary>
    /// ぷにぷにバウンスアニメーション再生（ScaleYを縮めて戻す）
    /// </summary>
    private void PlayBounceAnimation()
    {
        // SpriteRendererが無い場合は何もしない
        if (_spriteRenderer == null) return;

        // アニメーション再生中は新しいアニメーションを開始しない
        if (_isAnimating) return;

        _isAnimating = true;

        // 既に再生中のアニメーションがあればキル
        _spriteRenderer.transform.DOKill();

        // スケールをデフォルトに戻してからアニメーション開始
        _spriteRenderer.transform.localScale = _defaultLocalScale;

        // シーケンスを作成
        Sequence sequence = DOTween.Sequence();

        // 1. ScaleYをデフォルトから縮める（デフォルトスケールの_squashScale倍）
        float shrunkY = _defaultLocalScale.y * _squashScale;
        sequence.Append(_spriteRenderer.transform.DOScaleY(shrunkY, _squashDuration).SetEase(_squashEase));

        // 2. ScaleYをデフォルトに戻す
        sequence.Append(_spriteRenderer.transform.DOScaleY(_defaultLocalScale.y, _squashDuration).SetEase(_recoverEase));

        // アニメーション終了時にフラグをリセット
        sequence.OnComplete(() =>
        {
            _isAnimating = false;
        });

        // シーケンス再生
        sequence.Play();
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

    /// <summary>
    /// ゲーム開始時に呼ばれる
    /// SceneControllerのロード完了1秒後に呼ばれる
    /// </summary>
    public void StartGame()
    {
        Debug.Log("[PlayerController] Game started");
    }
    public void SetGravityScale(float gravityScale)
    {
        _rigidbody2D.gravityScale = gravityScale;
    }
    /// <summary>
    /// リトライ時にプレイヤーの状態をリセット
    /// </summary>
    public void ResetForRetry()
    {
        // 位置を初期位置に戻す
        transform.position = _initialPosition;

        // 速度をリセット
        _rigidbody2D.linearVelocity = Vector2.zero;

        // 無操作トラッキングをリセット
        _lastInputTime = Time.time;
        _noInputUnlockTriggered = false;
        NoInputUnlockAchieved = false;

        Debug.Log("[PlayerController] Reset for retry");
    }
}
