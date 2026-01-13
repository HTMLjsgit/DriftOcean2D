using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ButtonClickAnimation : MonoBehaviour
{
    [SerializeField] private float _moveDistance = 3f;
    [SerializeField] private float _duration = 0.08f;

    private Button _button;
    private RectTransform _rectTransform;
    private bool _isAnimating = false;

    void Start()
    {
        _button = this.gameObject.GetComponent<Button>();
        _rectTransform = this.gameObject.GetComponent<RectTransform>();

        _button.onClick.AddListener(() =>
        {
            if (_isAnimating) return;
            _isAnimating = true;

            // クリック時の現在位置を取得
            float currentY = _rectTransform.anchoredPosition.y;

            // 下に移動（マイナス方向）
            _rectTransform.DOAnchorPosY(currentY - _moveDistance, _duration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    // 元に戻る
                    _rectTransform.DOAnchorPosY(currentY, _duration)
                        .SetEase(Ease.OutQuad)
                        .OnComplete(() => _isAnimating = false);
                });
        });
    }
}
