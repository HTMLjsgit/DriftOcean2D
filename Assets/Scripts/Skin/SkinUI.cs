using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections.Generic;
using DG.Tweening;
using System.Threading.Tasks;

public class SkinUI : MonoBehaviour
{
    [SerializeField] private int _skinID;
    [SerializeField] private Image _skinImageUI;
    [SerializeField] private Sprite _lockedSprite; // ロック状態のスプライト
    [SerializeField] private Image _outlineImage;
    [SerializeField] private TextMeshProUGUI _newLabelText; // Newラベル
    [SerializeField] private TextMeshProUGUI _tapCountText; // タップ回数表示（TapUnlock用）
    [SerializeField] private Button _button; // このスキンのボタン
    [SerializeField] private AudioSource _audioSource; // ぷにぷに音再生用
    [SerializeField] private AudioClip _bounceSound; // ぷにぷに音
    private Sprite _originalSprite; // 元のスキンスプライト
    private SkinDatabase _skinDatabase;
    private UGSCloudSaveManager _cloudSaveManager;
    public int skinId => _skinID;

    [Header("Animation Settings")]
    [SerializeField] private float _squashDuration = 0.15f; // 縮む時間
    [SerializeField] private float _squashScale = 0.8f; // 縮む大きさ（1.0が元のサイズ、0.8で20%縮む）
    [SerializeField] private Ease _squashEase = Ease.OutQuad; // 縮むときのイージング
    [SerializeField] private Ease _recoverEase = Ease.OutBack; // 戻るときのイージング

    private bool _isAnimating = false; // アニメーション再生中フラグ

    void Start()
    {
        _skinDatabase = SkinDatabase.instance;
        _cloudSaveManager = UGSCloudSaveManager.instance;
    }

    public void Initialize(Sprite sprite)
    {
        _originalSprite = sprite;
        UpdateUIState();
    }

    public void UpdateUIState()
    {
        // 常に最新のインスタンスを取得
        _cloudSaveManager = UGSCloudSaveManager.instance;

        // スキンが解放されているかチェック
        bool isUnlocked = SkinManager.instance.IsUnlocked(_skinID);
        bool isEquipped = SkinManager.instance.currentSkinID == _skinID;
        bool isNew = _cloudSaveManager != null && _cloudSaveManager.IsSkinNew(_skinID);

        Debug.Log($"[SkinUI {_skinID}] UpdateUIState - isUnlocked={isUnlocked}, isEquipped={isEquipped}, isNew={isNew}");

        // 解放されていればオリジナルスプライト、ロック中ならロックスプライトを表示
        if (isUnlocked)
        {
            _skinImageUI.sprite = _originalSprite;
            Debug.Log($"[SkinUI {_skinID}] Setting ORIGINAL sprite");
        }
        else
        {
            _skinImageUI.sprite = _lockedSprite;
            Debug.Log($"[SkinUI {_skinID}] Setting LOCKED sprite");
        }

        // Outlineは装備中の場合のみ表示
        _outlineImage.gameObject.SetActive(isEquipped);

        // Newラベルは解放済みかつ未装備（見ていない）場合のみ表示
        _newLabelText.gameObject.SetActive(isUnlocked && isNew);

        // ボタンの有効/無効を設定（色が薄くならないようにenabledを使用）
        // 解放済みのスキンのみタップ可能（未解放スキンはタップ不可）
        _button.enabled = isUnlocked;

        // タップ回数テキストを更新（TapUnlockタイプの場合のみ表示）
        UpdateTapCountText();
    }

    /// <summary>
    /// このスキンがTapUnlockタイプかチェック
    /// </summary>
    private bool IsTapUnlockType()
    {
        if (_skinDatabase == null) return false;

        var skinData = _skinDatabase.GetAllSkins().FirstOrDefault(s => s.id == _skinID);
        return skinData != null && skinData.unlockType == SkinData.UnlockType.TapUnlock;
    }

    /// <summary>
    /// タップ回数テキストを更新
    /// </summary>
    private void UpdateTapCountText()
    {
        if (_tapCountText == null) return;

        // このスキン自身がTapUnlockかつ未解放の場合にグローバルタップ回数を表示
        bool isUnlocked = SkinManager.instance.IsUnlocked(_skinID);

        if (!isUnlocked && _skinDatabase != null)
        {
            var skinData = _skinDatabase.GetAllSkins().FirstOrDefault(s => s.id == _skinID);

            if (skinData != null && skinData.unlockType == SkinData.UnlockType.TapUnlock)
            {
                int currentCount = _cloudSaveManager.GetGlobalTapCount();
                int requiredCount = (int)skinData.conditionValue;

                _tapCountText.text = $"{currentCount}/{requiredCount}";
                _tapCountText.gameObject.SetActive(true);
                return;
            }
        }

        // それ以外の場合は非表示
        _tapCountText.gameObject.SetActive(false);
    }

    public async void OnClickedSkinUI()
    {
        bool isUnlocked = SkinManager.instance.IsUnlocked(_skinID);

        // ロックされている場合は何もしない
        if (!isUnlocked)
        {
            Debug.Log($"Skin {_skinID} is locked!");
            return;
        }

        // 解放済みの場合
        bool isAlreadyEquipped = SkinManager.instance.currentSkinID == _skinID;

        if (isAlreadyEquipped)
        {
            // 既に装備中の場合 → ぷにぷにアニメーション再生
            PlayBounceAnimation();
            Debug.Log($"[SkinUI {_skinID}] Already equipped - playing bounce animation");

            // グローバルタップカウントを増やす
            await HandleGlobalTapUnlock();
        }
        else
        {
            // 未装備の場合 → 装備する
            await SkinManager.instance.EquipSkin(_skinID);
            Debug.Log($"Equipped Skin ID: {_skinID}");

            // スキンを見た（装備した）としてマーク（Newラベルを消す）
            if (_cloudSaveManager != null)
            {
                await _cloudSaveManager.MarkSkinAsSeen(_skinID);
            }

            SkinInventryManager.instance.RefreshAllSlots();
        }
    }

    /// <summary>
    /// グローバルタップアンロック処理（解放済みスキンをタップした時）
    /// </summary>
    private async Task HandleGlobalTapUnlock()
    {
        if (_cloudSaveManager == null || _skinDatabase == null) return;

        // グローバルタップ回数を増やす
        int newCount = await _cloudSaveManager.IncrementGlobalTapCount();

        Debug.Log($"[SkinUI] Global tap count: {newCount}");

        // 全てのTapUnlockタイプの未解放スキンをチェック
        var allSkins = _skinDatabase.GetAllSkins();
        bool anyUnlocked = false;
        List<int> unlockedSkinIDs = new List<int>();

        foreach (var skinData in allSkins)
        {
            // TapUnlockタイプかつ未解放のスキンをチェック
            if (skinData.unlockType == SkinData.UnlockType.TapUnlock &&
                !SkinManager.instance.IsUnlocked(skinData.id))
            {
                int requiredCount = (int)skinData.conditionValue;

                // 必要回数に達したら解放
                if (newCount >= requiredCount)
                {
                    Debug.Log($"[SkinUI] Tap unlock achieved! Unlocking skin {skinData.id}...");

                    // スキンを解放
                    await SkinManager.instance.UnlockSkin(skinData.id);
                    anyUnlocked = true;
                    unlockedSkinIDs.Add(skinData.id);

                    Debug.Log($"[SkinUI] Skin {skinData.id} unlocked by tap!");
                }
            }
        }

        // スキンが解放された場合
        if (anyUnlocked)
        {
            // グローバルタップ回数をリセット
            await _cloudSaveManager.ResetGlobalTapCount();

            // スキン一覧画面で解放したので、通知済みとしてマーク（タイトル画面でお知らせを出さない）
            await _cloudSaveManager.MarkSkinsAsNotified(unlockedSkinIDs);

            // UI更新
            SkinInventryManager.instance.RefreshAllSlots();
        }
    }

    /// <summary>
    /// ぷにぷにバウンスアニメーション再生（ScaleYを縮めて戻す）
    /// </summary>
    private void PlayBounceAnimation()
    {
        // アニメーション再生中は新しいアニメーションを開始しない
        if (_isAnimating)
        {
            Debug.Log($"[SkinUI {_skinID}] Animation already playing, skipping");
            return;
        }

        Debug.Log($"[SkinUI {_skinID}] Starting bounce animation");
        _isAnimating = true;

        // ぷにぷに音を再生
        if (_audioSource != null && _bounceSound != null)
        {
            _audioSource.PlayOneShot(_bounceSound);
        }

        // 既に再生中のアニメーションがあればキル
        _skinImageUI.transform.DOKill();

        // スケールを元に戻してからアニメーション開始
        _skinImageUI.transform.localScale = Vector3.one;

        // シーケンスを作成
        Sequence sequence = DOTween.Sequence();

        // 1. ScaleYを縮める
        sequence.Append(_skinImageUI.transform.DOScaleY(_squashScale, _squashDuration).SetEase(_squashEase));

        // 2. ScaleYを元に戻す
        sequence.Append(_skinImageUI.transform.DOScaleY(1f, _squashDuration).SetEase(_recoverEase));

        // アニメーション終了時にフラグをリセット
        sequence.OnComplete(() =>
        {
            Debug.Log($"[SkinUI {_skinID}] Animation completed, resetting flag");
            _isAnimating = false;
        });

        // シーケンス再生
        sequence.Play();
    }
}