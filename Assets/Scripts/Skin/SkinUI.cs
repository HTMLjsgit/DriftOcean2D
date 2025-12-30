using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class SkinUI : MonoBehaviour
{
    [SerializeField] private int _skinID;
    [SerializeField] private Image _skinImageUI;
    [SerializeField] private Sprite _lockedSprite; // ロック状態のスプライト
    [SerializeField] private Image _outlineImage;
    [SerializeField] private TextMeshProUGUI _newLabelText; // Newラベル
    private Sprite _originalSprite; // 元のスキンスプライト
    private SkinDatabase _skinDatabase;
    private UGSCloudSaveManager _cloudSaveManager;
    public int skinId => _skinID;

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
        if (_newLabelText != null)
        {
            _newLabelText.gameObject.SetActive(isUnlocked && isNew);
        }
    }

    public async void OnClickedSkinUI()
    {
        // ロックされている場合
        if (!SkinManager.instance.IsUnlocked(_skinID))
        {
            Debug.Log($"Skin {_skinID} is locked!");

            // スキンデータを取得
            if (_skinDatabase != null)
            {
                var skinData = _skinDatabase.GetAllSkins().FirstOrDefault(s => s.id == _skinID);
                if (skinData != null && skinData.unlockType == SkinData.UnlockType.AdWatch)
                {
                    // AdWatchタイプの場合は広告を表示してスキン解放
                    Debug.Log($"Attempting to unlock skin {_skinID} by watching ad...");
                    SkinManager.instance.UnlockSkinByAd(_skinID,
                        onSuccess: () =>
                        {
                            Debug.Log($"Successfully unlocked skin {_skinID} by ad!");
                            // UI更新
                            SkinInventryManager.instance.RefreshAllSlots();
                        },
                        onFailed: () =>
                        {
                            Debug.LogWarning($"Failed to unlock skin {_skinID} by ad.");
                            // TODO: ユーザーに広告視聴失敗を通知
                        }
                    );
                }
                else
                {
                    // AdWatch以外の条件のスキンはクリックしても何もしない
                    Debug.Log($"Skin {_skinID} requires: {skinData?.unlockType}");
                }
            }
            return;
        }

        // 解放済みの場合は装備
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