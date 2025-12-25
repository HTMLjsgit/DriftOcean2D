using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using Cysharp.Threading.Tasks; // UniTask
using System;

public class SceneController : MonoBehaviour
{
    [SerializeField] private CanvasGroup _sceneLoadCanvasGroup;
    public static SceneController instance;

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

        await _sceneLoadCanvasGroup.DOFade(0f, 1f)
            .SetUpdate(true) // Time.timeScaleに影響されないようにする
            .SetLink(this.gameObject)
            .ToUniTask(cancellationToken: token);

        _sceneLoadCanvasGroup.blocksRaycasts = false;
    }

    public void SceneLoad(string sceneName)
    {
        LoadSceneTask(sceneName).Forget();
    }

    private async UniTaskVoid LoadSceneTask(string sceneName)
    {
        var token = this.GetCancellationTokenOnDestroy();

        // Time.timeScaleを必ず1に戻す（ゲームオーバー時は0になっている可能性があるため）
        Time.timeScale = 1f;

        // --- 1. 暗転 (フェードアウト) ---
        _sceneLoadCanvasGroup.blocksRaycasts = true;

        await _sceneLoadCanvasGroup.DOFade(1f, 1f)
            .SetUpdate(true) // Time.timeScaleに影響されないようにする
            .SetLink(this.gameObject)
            .ToUniTask(cancellationToken: token);

        // --- 2. シーンロード ---
        // 黒画面のままロード待ち
        await SceneManager.LoadSceneAsync(sceneName).ToUniTask(cancellationToken: token);

        // ★ポイント: ロード直後に少しだけ待つ（演出的なタメ）
        // これを入れると「ロード終わった！」という切り替わりが綺麗に見えます
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: token);

        // --- 3. 明転 (フェードイン) ---
        // ここで「新たなシーンで黒→明るく」が実行されます
        await _sceneLoadCanvasGroup.DOFade(0f, 1f)
            .SetUpdate(true) // Time.timeScaleに影響されないようにする
            .SetLink(this.gameObject)
            .ToUniTask(cancellationToken: token);

        _sceneLoadCanvasGroup.blocksRaycasts = false;
    }
}