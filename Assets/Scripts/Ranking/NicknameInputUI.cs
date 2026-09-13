using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ニックネーム入力UIの管理
/// </summary>
public class NicknameInputUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject _inputPanel;
    [SerializeField] private TMP_InputField _nameInputField;
    [SerializeField] private Button _submitButton;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private TextMeshProUGUI _messageText;
    private PlayerNameManager _playerNameManager;
    private UGSCloudSaveManager _cloudSaveManager;
    [Header("Settings")]
    [SerializeField] private int _maxNameLength = 10;
    [SerializeField] private string _defaultName = "player";

    private Action<string> _onSubmitCallback;
    private Action _onCancelCallback;

    public static NicknameInputUI instance;

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
        // InputFieldの最大文字数を設定
        _nameInputField.characterLimit = _maxNameLength;
        _playerNameManager = PlayerNameManager.instance;
        _cloudSaveManager = UGSCloudSaveManager.instance;
        // ボタンのイベント登録
        _submitButton.onClick.AddListener(OnSubmitClicked);
        _cancelButton.onClick.AddListener(OnCancelClicked);

        // 初期状態では非表示
        HidePanel();
    }

    /// <summary>
    /// ニックネーム入力パネルを表示
    /// </summary>
    /// <param name="message">表示するメッセージ（例：「ランキング入り！名前を入力してください」）</param>
    /// <param name="onSubmit">送信時のコールバック</param>
    /// <param name="onCancel">キャンセル時のコールバック</param>
    public void ShowPanel(string message, Action<string> onSubmit, Action onCancel = null)
    {
        ShowPanel(message, onSubmit, onCancel, null);
    }

    /// <summary>
    /// 指定した画面の最前面にニックネーム入力パネルを表示
    /// </summary>
    public void ShowPanel(string message, Action<string> onSubmit, Action onCancel, Transform presentationParent)
    {
        Debug.Log($"NicknameInputUI.ShowPanel called: message={message}");

        if (presentationParent != null)
        {
            // Rankingなど別Canvasの裏側に隠れないよう、表示先の配下へ移動する。
            _inputPanel.transform.SetParent(presentationParent, true);
            _inputPanel.transform.SetAsLastSibling();
        }

        _inputPanel.SetActive(true);

        // メッセージ設定
        // Older scene/prefab overrides contain large negative margins. Keep validation text inside the panel.
        _messageText.rectTransform.anchoredPosition = new Vector2(-97f, 70f);
        _messageText.rectTransform.sizeDelta = new Vector2(560f, 90f);
        _messageText.margin = Vector4.zero;
        _messageText.enableAutoSizing = true;
        _messageText.fontSizeMin = 24f;
        _messageText.fontSizeMax = 36f;
        _messageText.alignment = TextAlignmentOptions.Center;
        _messageText.textWrappingMode = TextWrappingModes.Normal;
        _messageText.text = message;

        // InputFieldに現在の名前を設定（UGSから取得、常に使用）
        string currentName = _cloudSaveManager.GetPlayerName();
        _nameInputField.text = currentName;
        _nameInputField.Select();
        _nameInputField.ActivateInputField();

        // コールバックを保存
        _onSubmitCallback = onSubmit;
        _onCancelCallback = onCancel;
    }

    /// <summary>
    /// パネルを非表示
    /// </summary>
    public void HidePanel()
    {
        _inputPanel.SetActive(false);

        _onSubmitCallback = null;
        _onCancelCallback = null;
    }

    /// <summary>
    /// 送信ボタンが押されたときの処理
    /// </summary>
    private void OnSubmitClicked()
    {
        string inputName = _nameInputField.text.Trim();

        // 空欄の場合はデフォルト名を使用
        if (string.IsNullOrEmpty(inputName))
        {
            inputName = _defaultName;
        }

        if (!PlayerNameFilter.IsAllowed(inputName))
        {
            _messageText.text = Application.systemLanguage == SystemLanguage.Japanese
                ? "この名前には使用できない言葉が含まれています。別の名前を入力してください。"
                : "This name contains a word that cannot be used. Please choose another name.";
            _nameInputField.Select();
            _nameInputField.ActivateInputField();
            return;
        }

        // コールバック実行
        _onSubmitCallback?.Invoke(inputName);

        // パネルを閉じる
        HidePanel();
    }

    /// <summary>
    /// キャンセルボタンが押されたときの処理
    /// </summary>
    private void OnCancelClicked()
    {
        // コールバック実行
        _onCancelCallback?.Invoke();

        // パネルを閉じる
        HidePanel();
    }

    void OnDestroy()
    {
        _submitButton.onClick.RemoveListener(OnSubmitClicked);
        _cancelButton.onClick.RemoveListener(OnCancelClicked);
    }
}
