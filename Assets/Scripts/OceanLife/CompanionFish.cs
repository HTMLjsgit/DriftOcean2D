using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class CompanionFish : MonoBehaviour
{
    private OceanLifeManager _owner;
    private Rigidbody2D _body;
    private Collider2D _collider;
    private SpriteRenderer _renderer;
    private bool _joining = true;
    private bool _leaving;
    private bool _farewell;
    private float _pause, _escapeTime;

    public void Initialize(OceanLifeManager owner, Vector2 position)
    {
        _owner = owner;
        _body = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
        _renderer = GetComponent<SpriteRenderer>();
        transform.position = position;
        _body.position = position;
    }

    public void Follow(Vector2 target, float deltaTime)
    {
        if (_leaving) return;
        Vector2 position = _body.position;
        if (_joining)
        {
            position = Vector2.MoveTowards(position, target, _owner.Settings.joinSpeed * deltaTime);
            if (Vector2.Distance(position, target) < 0.06f) _joining = false;
        }
        else
        {
            float amount = 1f - Mathf.Exp(-deltaTime / Mathf.Max(0.02f, _owner.Settings.followSmoothTime));
            position = Vector2.Lerp(position, target, amount);
        }
        _body.MovePosition(position);
    }

    public void Leave(float pause, bool farewell)
    {
        if (_leaving) return;
        _leaving = true;
        _farewell = farewell;
        _pause = pause;
        _collider.enabled = false;
        _body.simulated = false;
    }

    private void Update()
    {
        if (!_leaving || _owner == null) return;
        // The result/continue screen stops scaled game time.
        float delta = Time.unscaledDeltaTime;
        if (_pause > 0f)
        {
            _pause -= delta;
            return;
        }
        _escapeTime += delta;
        _renderer.flipX = _farewell;
        float direction = _farewell ? -1f : 1f;
        transform.position += new Vector3(direction, 0.12f, 0f) * (_owner.Settings.escapeSpeed * delta);
        if (!_farewell) _renderer.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - _escapeTime / 0.6f));
        float left = _owner.ViewportPoint(0f, 0f).x - 0.6f;
        float right = _owner.ViewportPoint(1f, 0f).x + 0.6f;
        if (transform.position.x < left || transform.position.x > right || _escapeTime > 2.5f)
        {
            _owner.ForgetFish(this);
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_leaving && _owner != null && other.CompareTag("Obstacle")) _owner.FishHitObstacle(this);
    }
}
