using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using Cysharp.Threading.Tasks; // UniTask
using System;

public class SceneController : MonoBehaviour
{
    [SerializeField] private CanvasGroup _sceneLoadCanvasGroup;
    public static SceneController instance;

    private bool isLoading = false; // ロード中フラグ

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    void Start()
    {
        // 初回起動時のフェードイン
        _sceneLoadCanvasGroup.alpha = 1f;
        _sceneLoadCanvasGroup.blocksRaycasts = true;
        StartFadeIn().Forget();
    }
    /// <summary>
    /// 起動時のフェードイン処理
    /// </summary>
    private async UniTaskVoid StartFadeIn()
    {
        var token = this.GetCancellationTokenOnDestroy();

        // 0.5秒待ってからフェードイン（いきなり始まると慌ただしいので）
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: token);

        // フェードイン開始と同時に入力を有効化
        _sceneLoadCanvasGroup.blocksRaycasts = false;

        await _sceneLoadCanvasGroup.DOFade(0f, 1f)
            .SetUpdate(true) // Time.timeScaleに影響されないようにする
            .SetLink(this.gameObject)
            .ToUniTask(cancellationToken: token);
    }

    public void SceneLoad(string sceneName)
    {
        // 既にロード中の場合は無視
        if (isLoading)
        {
            Debug.LogWarning($"Scene load already in progress. Ignoring request to load '{sceneName}'");
            return;
        }

        LoadSceneTask(sceneName).Forget();
    }

    private async UniTaskVoid LoadSceneTask(string sceneName)
    {
        isLoading = true;

        try
        {
            var token = this.GetCancellationTokenOnDestroy();

            // 既存のフェードアニメーションを強制停止（競合を防ぐ）
            _sceneLoadCanvasGroup.DOKill();
            Debug.Log("[SceneController] Killed existing DOTween animations on CanvasGroup");

            // --- 1. 暗転 (フェードアウト) ---
            _sceneLoadCanvasGroup.blocksRaycasts = true;

            await _sceneLoadCanvasGroup.DOFade(1f, 1f)
                .SetUpdate(true) // Time.timeScaleに影響されないようにする
                .SetLink(this.gameObject)
                .ToUniTask(cancellationToken: token);

            // フェードアウト完了後にTime.timeScaleを1に戻す（プレイヤーが見えない状態で戻す）
            Time.timeScale = 1f;
            Debug.Log("[SceneController] Time.timeScale reset to 1 after fade out");

            // --- 2. シーンロード ---
            // 黒画面のままロード待ち
            await SceneManager.LoadSceneAsync(sceneName).ToUniTask(cancellationToken: token);

            // ★ポイント: UGSデータのロード完了を待つ（スキン等の読み込みを黒画面中に完了させる）
            await WaitForUGSDataLoaded(token);

            // ★ポイント: ロード直後に少しだけ待つ（演出的なタメ）
            // これを入れると「ロード終わった！」という切り替わりが綺麗に見えます
            await UniTask.Delay(TimeSpan.FromSeconds(0.3f), cancellationToken: token);

            // シーンロード完了後、1秒待ってからゲームを開始（Mainシーンの場合）
            if(sceneName == "Main")
            {
                StartGameAfterLoad(token).Forget();
            }

            // --- 3. 明転 (フェードイン) ---
            // ここで「新たなシーンで黒→明るく」が実行されます
            Debug.Log($"[SceneController] Starting fade in... alpha={_sceneLoadCanvasGroup.alpha}");

            // フェードイン開始と同時に入力を有効化（すぐに操作できるようにする）
            _sceneLoadCanvasGroup.blocksRaycasts = false;

            await _sceneLoadCanvasGroup.DOFade(0f, 1f)
                .SetUpdate(true) // Time.timeScaleに影響されないようにする
                .SetLink(this.gameObject)
                .ToUniTask(cancellationToken: token);

            Debug.Log($"[SceneController] Fade in complete. alpha={_sceneLoadCanvasGroup.alpha}");
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning("[SceneController] Scene load was cancelled.");
            // キャンセルされた場合でもフェードインを完了させる
            ForceCompleteFadeIn();
            // ゲーム開始処理を実行（エラー時は即座に開始）
            StartGameImmediately();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SceneController] Error during scene load: {e.Message}\n{e.StackTrace}");
            // エラーが発生した場合でもフェードインを完了させる
            ForceCompleteFadeIn();
            // ゲーム開始処理を実行（エラー時は即座に開始）
            StartGameImmediately();
        }
        finally
        {
            isLoading = false;
        }
    }

    /// <summary>
    /// エラー時に強制的にフェードインを完了させる
    /// </summary>
    private void ForceCompleteFadeIn()
    {
        Debug.Log("[SceneController] Force completing fade in...");

        // 実行中のDOTweenアニメーションを停止
        _sceneLoadCanvasGroup.DOKill();

        // 強制的にalphaを0にする
        _sceneLoadCanvasGroup.alpha = 0f;
        _sceneLoadCanvasGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// UGSデータのロード完了を待つ（スキン等の読み込みを黒画面中に完了させる）
    /// 最大5秒でタイムアウト
    /// </summary>
    private async UniTask WaitForUGSDataLoaded(System.Threading.CancellationToken token)
    {
        var cloudSaveManager = UGSCloudSaveManager.instance;

        if (cloudSaveManager == null)
        {
            Debug.LogWarning("[SceneController] UGSCloudSaveManager not found, skipping wait.");
            return;
        }

        // 既にロード済みの場合は即座に戻る
        if (cloudSaveManager.IsDataLoaded)
        {
            Debug.Log("[SceneController] UGS data already loaded.");
            return;
        }

        Debug.Log("[SceneController] Waiting for UGS data to load...");

        // 最大5秒待機（タイムアウト付き）
        float elapsed = 0f;
        const float timeout = 5f;

        while (!cloudSaveManager.IsDataLoaded && elapsed < timeout)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.1f), cancellationToken: token);
            elapsed += 0.1f;
        }

        if (cloudSaveManager.IsDataLoaded)
        {
            Debug.Log($"[SceneController] UGS data loaded after {elapsed:F1}s");
        }
        else
        {
            Debug.LogWarning($"[SceneController] UGS data load timed out after {timeout}s");
        }
    }

    /// <summary>
    /// シーンロード完了後、1秒待ってからゲームを開始する
    /// GameManagerの状態をPlayingに変更し、PlayerControllerのgravityScaleを1に設定
    /// </summary>
    private async UniTaskVoid StartGameAfterLoad(System.Threading.CancellationToken token)
    {
        // 1秒待機
        Debug.Log("[SceneController] Waiting 1 second before starting game...");
        await UniTask.Delay(TimeSpan.FromSeconds(0.01f), cancellationToken: token);

        var gameManager = GameManager.instance;
        gameManager.SetGameState(GameManager.GameState.Playing);
        Debug.Log("[SceneController] Game state set to Playing");

        var playerController = PlayerController.instance;
        playerController.StartGame();
        playerController.SetGravityScale(1);
        Debug.Log("[SceneController] Player gravityScale set to 1 - Game started");
    }

    /// <summary>
    /// エラー時に即座にゲームを開始する（待機なし）
    /// </summary>
    private void StartGameImmediately()
    {
        var gameManager = GameManager.instance;
        gameManager.SetGameState(GameManager.GameState.Playing);
        Debug.Log("[SceneController] Game state set to Playing (immediate)");

        var playerController = PlayerController.instance;
        playerController.StartGame();
        Debug.Log("[SceneController] Player game started (immediate)");
    }
}