using System.Collections.Generic;
using UnityEngine;

/// <summary>Optional collectibles and companions. Never changes score or player damage.</summary>
public class OceanLifeManager : MonoBehaviour
{
    public static OceanLifeManager instance { get; private set; }

    [SerializeField] private OceanLifeSettings _settings;
    [SerializeField] private PlanktonPickup _planktonPrefab;
    [SerializeField] private CompanionFish _fishPrefab;
    [SerializeField] private PlayerController _player;
    [SerializeField] private Camera _camera;
    [SerializeField] private AudioSource _pickupAudio;
    [SerializeField] private AudioClip _pickupClip;

    private readonly OceanRunProgress _progress = new OceanRunProgress();
    private readonly List<PlanktonPickup> _activePlankton = new List<PlanktonPickup>();
    private readonly Queue<PlanktonPickup> _pool = new Queue<PlanktonPickup>();
    private readonly List<CompanionFish> _followers = new List<CompanionFish>(3);
    private readonly List<CompanionFish> _allFish = new List<CompanionFish>(6);
    private readonly Vector2[] _history = new Vector2[128];
    private int _historyHead;
    private int _historyCount;
    private float _spawnTimer = 0.3f;
    private float _soundTimer;
    private int _pendingSounds;

    public int PlanktonCount => _progress.PlanktonCount;
    public int FishCount => _followers.Count;
    public OceanLifeSettings Settings => _settings;
    public bool IsPlaying => GameManager.instance != null &&
        GameManager.instance.state == GameManager.GameState.Playing && Time.timeScale > 0f;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        if (_player == null) _player = PlayerController.instance;
        if (_camera == null) _camera = Camera.main;
        if (_settings == null || _planktonPrefab == null || _fishPrefab == null || _player == null || _camera == null)
        {
            Debug.LogError("[OceanLife] Assign the settings, prefabs, player and camera.", this);
            enabled = false;
            return;
        }
        ResetHistory();
    }

    private void Update()
    {
        if (!IsPlaying) return;
        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f)
        {
            SpawnPlanktonGroup();
            float year = ScoreManager.instance != null ? ScoreManager.instance.currentScore : 1950f;
            _spawnTimer = _settings.GetSpawnInterval(year) * Random.Range(0.85f, 1.15f);
        }

        _soundTimer -= Time.deltaTime;
        if (_pendingSounds > 0 && _soundTimer <= 0f)
        {
            _pendingSounds--;
            _soundTimer = 0.055f;
            if (_pickupAudio != null && _pickupClip != null) _pickupAudio.PlayOneShot(_pickupClip, 0.65f);
        }
    }

    private void FixedUpdate()
    {
        if (!IsPlaying || _player == null) return;
        _historyHead = (_historyHead + 1) % _history.Length;
        _history[_historyHead] = _player.transform.position;
        _historyCount = Mathf.Min(_historyCount + 1, _history.Length);
        for (int i = 0; i < _followers.Count; i++)
        {
            _followers[i].Follow(GetFollowPosition(i), Time.fixedDeltaTime);
        }
    }

    private Vector2 GetFollowPosition(int slot)
    {
        float samples = (_settings.followDelay + slot * _settings.additionalDelayPerFish) / Time.fixedDeltaTime;
        samples = Mathf.Clamp(samples, 0f, _historyCount - 1);
        int recent = Mathf.FloorToInt(samples);
        int older = Mathf.Min(recent + 1, _historyCount - 1);
        Vector2 position = Vector2.Lerp(
            _history[(_historyHead - recent + _history.Length) % _history.Length],
            _history[(_historyHead - older + _history.Length) % _history.Length], samples - recent);
        Vector2 offset = _settings.formationOffsets != null && slot < _settings.formationOffsets.Length
            ? _settings.formationOffsets[slot] : new Vector2(-0.45f - slot * 0.15f, 0.3f - slot * 0.3f);
        position += offset;
        Vector2 lower = ViewportPoint(0f, 0.08f);
        Vector2 upper = ViewportPoint(1f, 0.93f);
        position.x = Mathf.Clamp(position.x, lower.x + 0.16f, upper.x - 0.16f);
        position.y = Mathf.Clamp(position.y, lower.y, upper.y);
        return position;
    }

    private void SpawnPlanktonGroup()
    {
        int count = Random.value < _settings.groupChance
            ? Random.Range(Mathf.Max(2, _settings.minGroupSize), Mathf.Max(_settings.minGroupSize, _settings.maxGroupSize) + 1)
            : 1;
        Vector2 start = ViewportPoint(1f, Random.Range(_settings.minViewportY, _settings.maxViewportY));
        start.x += 0.3f;
        float phase = Random.Range(0f, Mathf.PI * 2f);
        float speed = _settings.driftSpeed * (ObstaclesSpawner.instance != null ? ObstaclesSpawner.instance.SpeedMultiplier : 1f);
        float despawnX = ViewportPoint(0f, 0f).x - 0.5f;
        for (int i = 0; i < count && _activePlankton.Count < _settings.maxActivePlankton; i++)
        {
            PlanktonPickup plankton = _pool.Count > 0 ? _pool.Dequeue() : Instantiate(_planktonPrefab, transform);
            Vector2 position = start + new Vector2(i * _settings.groupSpacing, Mathf.Sin(i * 0.65f + phase) * 0.09f);
            plankton.Initialize(this, position, speed, despawnX, phase, _settings.GetPlanktonColor(Random.value));
            _activePlankton.Add(plankton);
        }
    }

    public bool CollectPlankton(PlayerController collector)
    {
        if (!IsPlaying || collector != _player) return false;
        bool addFish = _progress.Collect(_followers.Count, _settings.maxFish, _settings.planktonPerFish);
        _pendingSounds++;
        if (addFish)
        {
            Vector2 target = GetFollowPosition(_followers.Count);
            CompanionFish fish = Instantiate(_fishPrefab, transform);
            fish.Initialize(this, new Vector2(ViewportPoint(0f, 0f).x - 0.4f, target.y));
            _followers.Add(fish);
            _allFish.Add(fish);
        }
        return true;
    }

    public void Recycle(PlanktonPickup plankton)
    {
        if (!_activePlankton.Remove(plankton)) return;
        plankton.gameObject.SetActive(false);
        _pool.Enqueue(plankton);
    }

    public void FishHitObstacle(CompanionFish fish)
    {
        if (!IsPlaying || !_followers.Remove(fish)) return;
        fish.Leave(0f, false);
        // Remaining fish take the earlier slots on the next physics update.
    }

    public void ForgetFish(CompanionFish fish)
    {
        _followers.Remove(fish);
        _allFish.Remove(fish);
    }

    public void EndRun()
    {
        _pendingSounds = 0;
        for (int i = 0; i < _followers.Count; i++)
            _followers[i].Leave(_settings.farewellPause + i * 0.06f, true);
        _followers.Clear();
    }

    public void ResumeRun()
    {
        ClearPlankton();
        ResetHistory();
        _spawnTimer = 0.5f;
    }

    public void ResetRun()
    {
        ClearPlankton();
        foreach (var fish in _allFish.ToArray())
        {
            if (fish != null)
            {
                fish.gameObject.SetActive(false);
                Destroy(fish.gameObject);
            }
        }
        _followers.Clear();
        _allFish.Clear();
        _progress.Reset();
        _pendingSounds = 0;
        _soundTimer = 0f;
        _spawnTimer = 0.3f;
        ResetHistory();
    }

    private void ClearPlankton()
    {
        while (_activePlankton.Count > 0) Recycle(_activePlankton[_activePlankton.Count - 1]);
    }

    private void ResetHistory()
    {
        Vector2 position = _player != null ? (Vector2)_player.transform.position : Vector2.zero;
        for (int i = 0; i < _history.Length; i++) _history[i] = position;
        _historyHead = 0;
        _historyCount = _history.Length;
    }

    public Vector2 ViewportPoint(float x, float y)
    {
        return _camera.ViewportToWorldPoint(new Vector3(x, y, -_camera.transform.position.z));
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
