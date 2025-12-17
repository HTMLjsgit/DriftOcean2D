using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; // Listを使うために必要

public class StartSkinManager : MonoBehaviour
{
    // --- 追加部分: Inspectorで設定するためのクラス定義 ---
    [System.Serializable]
    public class SkinUISlot
    {
        public int skinId;       // 表示したいスキンのID
        public Image targetImage; // その画像を表示するUIのImageコンポーネント
    }

    [Header("UI References")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private FlowUI _flowUI;
    private SkinDatabase _skinDatabase;
    
    [Header("Skin List Settings")]
    // ここにインスペクターでIDとImageを登録していく
    [SerializeField] private List<SkinUISlot> _skinSlots; 

    public static StartSkinManager instance;

    void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    void Start()
    {
        _skinDatabase = SkinDatabase.instance;
        // 閉じるボタンの処理
        _closeButton.onClick.AddListener(() =>
        {
            _flowUI.SwitchView("Start");
        });

        // ★追加: スキン画像の適用処理を実行
        ApplySkinSprites();
    }

    /// <summary>
    /// 登録されたリストに基づいて、ImageのSpriteを書き換える
    /// </summary>
    private void ApplySkinSprites()
    {
        // データベースがない場合はエラーになるのでガード
        if (_skinDatabase == null)
        {
            Debug.LogError("SkinDatabase instance is null!");
            return;
        }

        foreach (var slot in _skinSlots)
        {
            // IDを使ってデータベースからSkinDataを取得
            // (前回のSkinDatabaseの実装にある GetSkinById を使用)
            SkinData data = _skinDatabase.GetSkinById(slot.skinId);

            if (data != null && slot.targetImage != null)
            {
                // 画像を適用
                slot.targetImage.sprite = data.skinSprite;
                
                // 画像が潰れないようにアスペクト比を維持する設定（お好みで）
                // slot.targetImage.preserveAspect = true; 
            }
        }
    }
}