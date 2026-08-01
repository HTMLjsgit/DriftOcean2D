using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class SkinUI : MonoBehaviour
{
    [SerializeField] private int _skinID;
    [SerializeField] private Image _skinImageUI;
    [SerializeField] private Sprite _lockedSprite; // ロック状態のスプライト
    [SerializeField] private Image _outlineImage;
    [SerializeField] private TextMeshProUGUI _newLabelText; // Newラベル
    [SerializeField] private Button _button; // このスキンのボタン
    [SerializeField] private AudioSource _audioSource; // ぷにぷに音再生用
    [SerializeField] private AudioClip _bounceSound; // ぷにぷに音
    private Sprite _originalSprite; // 元のスキンスプライト
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
        bool isNew = _cloudSaveManager.IsSkinNew(_skinID);

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
            // 選択中のスキンをもう一度タップすると詳細を表示
            PlayBounceAnimation();
            SkinInventryManager.instance.ShowSkinDetails(_skinID);
        }
        else
        {
            // 未装備の場合 → 装備する
            await SkinManager.instance.EquipSkin(_skinID);
            Debug.Log($"Equipped Skin ID: {_skinID}");

            // スキンを見た（装備した）としてマーク（Newラベルを消す）
            await _cloudSaveManager.MarkSkinAsSeen(_skinID);

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
        _audioSource.PlayOneShot(_bounceSound);

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
