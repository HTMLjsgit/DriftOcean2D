using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using DG.Tweening;

/// <summary>
/// BGMを管理するマネージャー
/// 難易度に応じてBGMを切り替え、クロスフェード処理を行う
/// </summary>
public class BGMManager : MonoBehaviour
{
    public static BGMManager instance;

    [Header("BGM Settings")]
    [SerializeField] private AudioClip _normalBGM;           // 通常BGM（開始時）
    [SerializeField] private AudioClip _midDifficultyBGM;    // 中間難易度BGM（3分経過）
    [SerializeField] private AudioClip _maxDifficultyBGM;    // 最高難易度BGM（鬼畜ステージ）

    [Header("Audio Source")]
    [SerializeField] private AudioSource _currentAudioSource;
    [SerializeField] private AudioSource _nextAudioSource;

    [Header("Crossfade Settings")]
    [SerializeField] private float _crossfadeDuration = 2f;  // クロスフェード時間（秒）

    private bool _isCrossfading = false;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        // AudioSourceが未設定の場合は自動追加
        _currentAudioSource = gameObject.AddComponent<AudioSource>();
        _currentAudioSource.loop = true;
        _currentAudioSource.playOnAwake = false;
        _nextAudioSource = gameObject.AddComponent<AudioSource>();
        _nextAudioSource.loop = true;
        _nextAudioSource.playOnAwake = false;
        _nextAudioSource.volume = 0f;
    }

    void Start()
    {
        // ゲーム開始時に通常BGMを再生
        PlayNormalBGM();
    }

    /// <summary>
    /// 通常BGMを再生（ゲーム開始時）
    /// </summary>
    public void PlayNormalBGM()
    {
        PlayBGMImmediate(_normalBGM);
    }

    /// <summary>
    /// 中間難易度BGMに切り替え（クロスフェード）
    /// </summary>
    public async UniTaskVoid SwitchToMidDifficultyBGM()
    {
        await CrossfadeTo(_midDifficultyBGM);
    }

    /// <summary>
    /// 最高難易度BGMに切り替え（クロスフェード）
    /// </summary>
    public async UniTaskVoid SwitchToMaxDifficultyBGM()
    {
        await CrossfadeTo(_maxDifficultyBGM);
    }

    /// <summary>
    /// BGMを即座に切り替え（フェードなし）
    /// </summary>
    /// <param name="clip">再生するBGM</param>
    private void PlayBGMImmediate(AudioClip clip)
    {
        if (_currentAudioSource.clip == clip && _currentAudioSource.isPlaying)
        {
            return; // 既に同じBGMが再生中
        }

        _currentAudioSource.Stop();
        _currentAudioSource.clip = clip;
        _currentAudioSource.volume = 1f;
        _currentAudioSource.Play();

        Debug.Log($"[BGMManager] Playing BGM immediately: {clip.name}");
    }

    /// <summary>
    /// BGMをクロスフェードで切り替え
    /// </summary>
    /// <param name="newClip">次に再生するBGM</param>
    private async UniTask CrossfadeTo(AudioClip newClip)
    {
        if (_isCrossfading)
        {
            Debug.LogWarning("[BGMManager] Already crossfading, ignoring new request");
            return;
        }

        if (_currentAudioSource.clip == newClip && _currentAudioSource.isPlaying)
        {
            Debug.Log($"[BGMManager] Already playing {newClip.name}, skipping crossfade");
            return;
        }

        _isCrossfading = true;

        Debug.Log($"[BGMManager] Starting crossfade to: {newClip.name}");

        // 次のBGMをセットアップ
        _nextAudioSource.clip = newClip;
        _nextAudioSource.volume = 0f;
        _nextAudioSource.Play();

        // DOTweenでクロスフェード
        var token = this.GetCancellationTokenOnDestroy();

        try
        {
            // 同時に実行：現在のBGMをフェードアウト、次のBGMをフェードイン
            await UniTask.WhenAll(
                _currentAudioSource.DOFade(0f, _crossfadeDuration).SetEase(Ease.InOutQuad).ToUniTask(cancellationToken: token),
                _nextAudioSource.DOFade(1f, _crossfadeDuration).SetEase(Ease.InOutQuad).ToUniTask(cancellationToken: token)
            );

            // AudioSourceを入れ替え
            _currentAudioSource.Stop();
            var temp = _currentAudioSource;
            _currentAudioSource = _nextAudioSource;
            _nextAudioSource = temp;

            Debug.Log($"[BGMManager] Crossfade completed: {newClip.name}");
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning("[BGMManager] Crossfade cancelled");
        }
        finally
        {
            _isCrossfading = false;
        }
    }

    /// <summary>
    /// BGMを停止
    /// </summary>
    public void StopBGM()
    {
        _currentAudioSource.Stop();
        _nextAudioSource.Stop();
        Debug.Log("[BGMManager] BGM stopped");
    }

    /// <summary>
    /// BGMをリセット（リトライ時など）
    /// </summary>
    public void ResetBGM()
    {
        StopBGM();
        PlayNormalBGM();
        Debug.Log("[BGMManager] BGM reset to normal");
    }
}
