using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
public class PlanktonPickup : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _glow;
    private OceanLifeManager _owner;
    private Rigidbody2D _body;
    private CircleCollider2D _collider;
    private SpriteRenderer _renderer;
    private Vector3 _initialScale;
    private Color _color;
    private float _speed, _despawnX, _baseY, _phase, _age, _collectionTime;
    private bool _collected;
    private bool _attracted;
    private float _attractionSpeed;

    public void Initialize(OceanLifeManager owner, Vector2 position, float speed, float despawnX, float phase, Color color)
    {
        if (_body == null)
        {
            _body = GetComponent<Rigidbody2D>();
            _collider = GetComponent<CircleCollider2D>();
            _renderer = GetComponent<SpriteRenderer>();
            _initialScale = transform.localScale;
        }
        _owner = owner;
        _speed = speed;
        _despawnX = despawnX;
        _baseY = position.y;
        _phase = phase;
        _age = _collectionTime = 0f;
        _collected = false;
        _attracted = false;
        _attractionSpeed = 0f;
        _color = color;
        transform.localScale = _initialScale;
        transform.position = position;
        _body.position = position;
        _body.linearVelocity = Vector2.zero;
        _body.simulated = true;
        _collider.enabled = true;
        _renderer.color = color;
        if (_glow != null) _glow.color = new Color(color.r, color.g, color.b, color.a * 0.16f);
        gameObject.SetActive(true);
    }

    private void FixedUpdate()
    {
        if (_collected || _owner == null || !_owner.IsPlaying) return;
        _age += Time.fixedDeltaTime;
        Vector2 position = _body.position;
        var settings = _owner.Settings;
        var player = _owner.Player;
        if (player != null && player.gameObject.activeInHierarchy)
        {
            Vector2 target = player.transform.position;
            if (!_attracted && settings.attractionRadius > 0f &&
                (target - position).sqrMagnitude <= settings.attractionRadius * settings.attractionRadius)
                _attracted = true;

            if (_attracted)
            {
                // Keep moving visibly toward the player; the normal trigger awards the pickup once.
                _attractionSpeed = Mathf.MoveTowards(_attractionSpeed, settings.attractionSpeed,
                    settings.attractionAcceleration * Time.fixedDeltaTime);
                _body.MovePosition(Vector2.MoveTowards(position, target, _attractionSpeed * Time.fixedDeltaTime));
                return;
            }
        }
        position.x -= _speed * Time.fixedDeltaTime;
        // At the cutoff, keep the current height so stopping the sway does not cause a jump.
        if (_owner.VerticalDriftEnabled)
            position.y = _baseY + Mathf.Sin(_age * 1.4f + _phase) * settings.verticalDrift;
        _body.MovePosition(position);
        if (position.x < _despawnX) _owner.Recycle(this);
    }

    private void Update()
    {
        if (!_collected) return;
        _collectionTime += Time.deltaTime;
        float remaining = 1f - Mathf.Clamp01(_collectionTime / 0.12f);
        transform.localScale = _initialScale * remaining;
        _renderer.color = new Color(_color.r, _color.g, _color.b, _color.a * remaining);
        if (remaining <= 0f) _owner.Recycle(this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_collected || _owner == null) return;
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || !_owner.CollectPlankton(player)) return;
        _collected = true;
        _collider.enabled = false;
        _body.simulated = false;
    }
}
