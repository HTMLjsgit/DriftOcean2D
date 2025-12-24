using UnityEngine;
using UnityEngine.UI; // Buttonを扱うために必要

public class VolumeManager : MonoBehaviour
{
    [SerializeField] private Button _volumeButton; // インスペクターでボタンをアサイン
    private bool isMuted = false;

    [SerializeField] private Sprite _onSprite;
    [SerializeField] private Sprite _offSprite;

    [SerializeField] private Image _volumeSwitcherImage;
    void Start()
    {
        // ボタンがセットされているか確認し、クリックイベントを登録
        _volumeButton.onClick.AddListener(ToggleVolume);
    }

    // 音のON/OFFを切り替えるメソッド
    public void ToggleVolume()
    {
        isMuted = !isMuted;
        
        // isMutedがtrueなら音量を0に、falseなら1にする
        AudioListener.volume = isMuted ? 0 : 1;

        // デバッグログで状態を確認（任意）
        Debug.Log(isMuted ? "Muted" : "Unmuted");
        Sprite s = isMuted ? _onSprite : _offSprite;
        _volumeSwitcherImage.sprite = s;
    }

    // スクリプトが破棄されるときにリスナーを解除（メモリリーク防止のベストプラクティス）
    void OnDestroy()
    {
        _volumeButton.onClick.RemoveListener(ToggleVolume);
    }
}