using UnityEngine;
using UnityEngine.UI;

public class VolumeManager : MonoBehaviour
{
    [SerializeField] private Button _volumeButton;
    [SerializeField] private Sprite _normalSprite;
    [SerializeField] private Sprite _allMutedSprite;
    [SerializeField] private Sprite _bgmMutedSprite;
    [SerializeField] private Image _volumeSwitcherImage;

    private const string BgmTag = "BGM";
    private const string VolumeModePrefKey = "AudioVolumeMode";

    private enum VolumeMode
    {
        Normal = 0,
        AllMuted = 1,
        BgmMuted = 2,
    }

    private VolumeMode _volumeMode = VolumeMode.Normal;

    private void Start()
    {
        LoadVolumeSetting();

        if (_volumeButton != null)
        {
            _volumeButton.onClick.AddListener(ToggleVolume);
        }

        UpdateVolumeUI();
    }

    private void LoadVolumeSetting()
    {
        int savedMode = PlayerPrefs.GetInt(VolumeModePrefKey, (int)VolumeMode.Normal);
        _volumeMode = IsValidVolumeMode(savedMode) ? (VolumeMode)savedMode : VolumeMode.Normal;

        ApplyVolumeMode();
        Debug.Log($"[VolumeManager] Loaded setting: {_volumeMode}");
    }

    private void SaveVolumeSetting()
    {
        PlayerPrefs.SetInt(VolumeModePrefKey, (int)_volumeMode);
        PlayerPrefs.Save();

        Debug.Log($"[VolumeManager] Saved setting: {_volumeMode}");
    }

    public void ToggleVolume()
    {
        _volumeMode = _volumeMode switch
        {
            VolumeMode.Normal => VolumeMode.AllMuted,
            VolumeMode.AllMuted => VolumeMode.BgmMuted,
            _ => VolumeMode.Normal,
        };

        ApplyVolumeMode();
        SaveVolumeSetting();
        UpdateVolumeUI();

        Debug.Log($"[VolumeManager] Current mode: {_volumeMode}");
    }

    private void ApplyVolumeMode()
    {
        AudioListener.volume = _volumeMode == VolumeMode.AllMuted ? 0f : 1f;
        SetBgmMuted(_volumeMode == VolumeMode.BgmMuted);
    }

    private void SetBgmMuted(bool isMuted)
    {
        foreach (GameObject bgmObject in GameObject.FindGameObjectsWithTag(BgmTag))
        {
            if (bgmObject.TryGetComponent(out AudioSource audioSource))
            {
                audioSource.mute = isMuted;
            }
        }
    }

    private void UpdateVolumeUI()
    {
        if (_volumeSwitcherImage != null)
        {
            _volumeSwitcherImage.sprite = GetSpriteForCurrentMode();
        }
    }

    private Sprite GetSpriteForCurrentMode()
    {
        return _volumeMode switch
        {
            VolumeMode.Normal => _normalSprite,
            VolumeMode.AllMuted => _allMutedSprite,
            VolumeMode.BgmMuted => _bgmMutedSprite,
            _ => _normalSprite,
        };
    }

    private static bool IsValidVolumeMode(int mode)
    {
        return mode >= (int)VolumeMode.Normal && mode <= (int)VolumeMode.BgmMuted;
    }

    private void OnDestroy()
    {
        if (_volumeButton != null)
        {
            _volumeButton.onClick.RemoveListener(ToggleVolume);
        }
    }
}
