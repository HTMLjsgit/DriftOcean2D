using UnityEngine;
using UnityEngine.UI; // Buttonを扱うために必要

public class VolumeManager : MonoBehaviour
{
    [SerializeField] private Button _volumeButton; // インスペクターでボタンをアサイン
    private bool isMuted = false;

    [SerializeField] private Sprite _onSprite; // 音がONの時のスプライト
    [SerializeField] private Sprite _offSprite; // 音がOFFの時のスプライト

    [SerializeField] private Image _volumeSwitcherImage;

    private const string VOLUME_PREF_KEY = "AudioMuted";

    void Start()
    {
        // 保存された設定を読み込む
        LoadVolumeSetting();

        // ボタンがセットされているか確認し、クリックイベントを登録
        _volumeButton.onClick.AddListener(ToggleVolume);

        // 初期スプライトを設定
        UpdateVolumeUI();
    }

    /// <summary>
    /// 保存された音量設定を読み込む
    /// </summary>
    private void LoadVolumeSetting()
    {
        // PlayerPrefsから読み込み（0 = OFF, 1 = ON）
        isMuted = PlayerPrefs.GetInt(VOLUME_PREF_KEY, 0) == 1;

        // AudioListenerに反映
        AudioListener.volume = isMuted ? 0 : 1;

        Debug.Log($"[VolumeManager] Loaded setting: {(isMuted ? "Muted" : "Unmuted")}");
    }

    /// <summary>
    /// 音量設定を保存
    /// </summary>
    private void SaveVolumeSetting()
    {
        // PlayerPrefsに保存（0 = OFF, 1 = ON）
        PlayerPrefs.SetInt(VOLUME_PREF_KEY, isMuted ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log($"[VolumeManager] Saved setting: {(isMuted ? "Muted" : "Unmuted")}");
    }

    /// <summary>
    /// 音のON/OFFを切り替えるメソッド
    /// </summary>
    public void ToggleVolume()
    {
        isMuted = !isMuted;

        // isMutedがtrueなら音量を0に、falseなら1にする
        AudioListener.volume = isMuted ? 0 : 1;

        // 設定を保存
        SaveVolumeSetting();

        // UIを更新
        UpdateVolumeUI();

        Debug.Log(isMuted ? "Muted" : "Unmuted");
    }

    /// <summary>
    /// ボリュームボタンのスプライトを更新
    /// </summary>
    private void UpdateVolumeUI()
    {
        // ミュート中（音OFF）→ OFFスプライト、ミュート解除（音ON）→ ONスプライト
        _volumeSwitcherImage.sprite = isMuted ? _offSprite : _onSprite;
    }

    // スクリプトが破棄されるときにリスナーを解除（メモリリーク防止のベストプラクティス）
    void OnDestroy()
    {
        if (_volumeButton != null)
        {
            _volumeButton.onClick.RemoveListener(ToggleVolume);
        }
    }
}