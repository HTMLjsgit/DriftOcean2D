using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class FlowUI : MonoBehaviour
{
    // インスペクターで設定するためのクラス
    [System.Serializable]
    public class ViewState
    {
        public string key; // 画面名（例: "Title", "InGame", "Result"）
        public List<GameObject> uiObjects; // その画面で表示するオブジェクトのリスト
        public UnityEvent onOpen;
    }
    [Header("UI設定リスト")]
    [SerializeField] private List<ViewState> _viewStates = new List<ViewState>();

    [Header("初期設定")]
    [SerializeField] private string _startKey = ""; // ゲーム開始時に表示したいキーがあれば入力

    void Start()
    {
        // 開始時に指定されたキーがあれば表示する
        if (!string.IsNullOrEmpty(_startKey))
        {
            SwitchView(_startKey);
        }
    }

    /// <summary>
    /// 指定したキーのUIを表示し、それ以外を全て非表示にする
    /// </summary>
    /// <param name="targetKey">表示したい画面のキー</param>
    public void SwitchView(string targetKey, Action action = null)
    {
        Debug.Log($"FlowUI.SwitchView called: targetKey={targetKey}");
        bool found = false;
        foreach (var state in _viewStates)
        {
            // キーが一致すれば表示(true)、一致しなければ非表示(false)
            bool isActive = state.key == targetKey;

            if (isActive)
            {
                found = true;
                // アクティブになる画面のonOpenイベントだけを呼ぶ
                Debug.Log($"Invoking onOpen for state: {state.key}");
                state.onOpen?.Invoke();
            }

            // リスト内のオブジェクトをすべて設定
            foreach (var obj in state.uiObjects)
            {
                obj.SetActive(isActive);
            }
        }

        if (!found)
        {
            Debug.LogWarning($"ViewState with key '{targetKey}' not found!");
        }
    }

}