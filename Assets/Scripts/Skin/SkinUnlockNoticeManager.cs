using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// タイトル画面でスキン解放をお知らせするマネージャー
/// 新しく解放されたスキンがある場合、タイトルに戻った時にお知らせUIを表示
/// </summary>
public class SkinUnlockNoticeManager : MonoBehaviour
{
    public static SkinUnlockNoticeManager instance;

    [Header("UI References")]
    [SerializeField] private GameObject _noticePanel;
    [SerializeField] private Button _closeButton;

    private UGSCloudSaveManager _cloudSaveManager;
    private List<int> _pendingNotifications = new List<int>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        _cloudSaveManager = UGSCloudSaveManager.instance;

        // 閉じるボタンのリスナー
        _closeButton.onClick.AddListener(OnCloseButtonClicked);

        // パネルを初期非表示
        _noticePanel.SetActive(false);

        // UGSデータロード完了後にお知らせチェック
        _cloudSaveManager.OnDataLoaded += CheckAndShowNotification;

        // 既にロード済みの場合は即座にチェック
        if (_cloudSaveManager.IsDataLoaded)
        {
            CheckAndShowNotification();
        }
    }

    void OnDestroy()
    {
        if (_cloudSaveManager != null)
        {
            _cloudSaveManager.OnDataLoaded -= CheckAndShowNotification;
        }
    }

    /// <summary>
    /// 未通知のスキン解放をチェックしてお知らせを表示
    /// </summary>
    private void CheckAndShowNotification()
    {
        // 未通知のスキンIDを取得
        _pendingNotifications = _cloudSaveManager.GetUnnotifiedSkinIDs();

        if (_pendingNotifications.Count > 0)
        {
            Debug.Log($"[SkinUnlockNoticeManager] Found {_pendingNotifications.Count} unnotified skins: [{string.Join(", ", _pendingNotifications)}]");
            // パネルを表示
            _noticePanel.SetActive(true);
        }
        else
        {
            Debug.Log("[SkinUnlockNoticeManager] No new skin unlocks to notify");
        }
    }

    /// <summary>
    /// 閉じるボタンクリック時の処理
    /// </summary>
    private async void OnCloseButtonClicked()
    {
        // パネルを閉じる
        _noticePanel.SetActive(false);

        // 全てマーク済みにする
        if (_cloudSaveManager != null && _pendingNotifications.Count > 0)
        {
            await _cloudSaveManager.MarkSkinsAsNotified(_pendingNotifications);
            _pendingNotifications.Clear();
        }
    }
}
