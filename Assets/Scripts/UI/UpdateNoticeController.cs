using System;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class UpdateNoticeController : MonoBehaviour
{
    private const string LastSeenVersionCodeKey = "UpdateNotice.LastSeenVersionCode";

    private static UpdateNoticeController _instance;

    [Header("Notice")]
    [SerializeField] private bool _showOnFirstLaunch = false;

    [Header("Debug")]
    [SerializeField] private bool _showForDebug = false;

    [Header("Prefab")]
    [SerializeField] private GameObject _noticePrefab;
    [SerializeField] private Transform _noticeParent;
    [SerializeField] private string _closeButtonName = "CloseButton";

    private GameObject _noticeInstance;
    private Button _closeButton;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    private void Start()
    {
        if (_showForDebug)
        {
            ShowNotice();
            return;
        }

        CheckAndShowUpdateNotice();
    }

    private void OnDestroy()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(HideNotice);
        }

        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void CheckAndShowUpdateNotice()
    {
        int currentVersionCode = GetCurrentVersionCode();
        if (currentVersionCode <= 0)
        {
            Debug.LogWarning("[UpdateNoticeController] Could not read current versionCode.");
            return;
        }

        int previousVersionCode = PlayerPrefs.GetInt(LastSeenVersionCodeKey, -1);
        bool isFirstLaunch = previousVersionCode < 0;
        bool shouldShowNotice = isFirstLaunch ? _showOnFirstLaunch : currentVersionCode > previousVersionCode;

        PlayerPrefs.SetInt(LastSeenVersionCodeKey, currentVersionCode);
        PlayerPrefs.Save();

        Debug.Log($"[UpdateNoticeController] currentVersionCode={currentVersionCode}, previousVersionCode={previousVersionCode}, shouldShowNotice={shouldShowNotice}");

        if (shouldShowNotice)
        {
            ShowNotice();
        }
    }

    private int GetCurrentVersionCode()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            using AndroidJavaObject packageManager = activity.Call<AndroidJavaObject>("getPackageManager");

            string packageName = activity.Call<string>("getPackageName");
            using AndroidJavaObject packageInfo = packageManager.Call<AndroidJavaObject>("getPackageInfo", packageName, 0);

            using AndroidJavaClass versionClass = new AndroidJavaClass("android.os.Build$VERSION");
            int sdkInt = versionClass.GetStatic<int>("SDK_INT");
            if (sdkInt >= 28)
            {
                long longVersionCode = packageInfo.Call<long>("getLongVersionCode");
                return longVersionCode > int.MaxValue ? int.MaxValue : (int)longVersionCode;
            }

            return packageInfo.Get<int>("versionCode");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UpdateNoticeController] Failed to read Android versionCode: {e.Message}");
            return 0;
        }
#elif UNITY_IOS && !UNITY_EDITOR
        return ParseVersionToCode(Application.version);
#elif UNITY_EDITOR
        return PlayerSettings.Android.bundleVersionCode;
#else
        return 0;
#endif
    }

    /// <summary>
    /// マーケティングバージョン文字列（例 "1.2.3"）を単調増加する整数コードに変換する。
    /// 各要素 &lt; 1000 を前提に major*1_000_000 + minor*1_000 + build で畳む。
    /// パース不能な場合は 0 を返す（更新通知はスキップされる）。
    /// </summary>
    private int ParseVersionToCode(string version)
    {
        try
        {
            Version v = new Version(version);
            int major = Mathf.Max(v.Major, 0);
            int minor = Mathf.Max(v.Minor, 0);
            int build = Mathf.Max(v.Build, 0); // 未指定(-1)は0扱い
            return major * 1_000_000 + minor * 1_000 + build;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[UpdateNoticeController] Failed to parse iOS version '{version}': {e.Message}");
            return 0;
        }
    }

    private void ShowNotice()
    {
        if (_noticeInstance != null)
        {
            _noticeInstance.SetActive(true);
            return;
        }

        if (_noticePrefab == null)
        {
            Debug.LogWarning("[UpdateNoticeController] Notice prefab is not assigned.");
            return;
        }

        _noticeInstance = Instantiate(_noticePrefab, _noticeParent);
        _noticeInstance.SetActive(true);
        BindCloseButton();
    }

    private void HideNotice()
    {
        if (_noticeInstance != null)
        {
            _noticeInstance.SetActive(false);
        }
    }

    private void BindCloseButton()
    {
        Transform closeButtonTransform = FindChildByName(_noticeInstance.transform, _closeButtonName);
        if (closeButtonTransform != null)
        {
            _closeButton = closeButtonTransform.GetComponent<Button>();
        }

        if (_closeButton == null)
        {
            _closeButton = _noticeInstance.GetComponentInChildren<Button>(true);
        }

        if (_closeButton == null)
        {
            Debug.LogWarning("[UpdateNoticeController] Close button was not found in notice prefab.");
            return;
        }

        _closeButton.onClick.AddListener(HideNotice);
    }

    private static Transform FindChildByName(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrEmpty(childName))
        {
            return null;
        }

        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }

            Transform result = FindChildByName(child, childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
