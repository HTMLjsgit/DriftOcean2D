using UnityEngine;
using TMPro;
using UnityEngine.UI; // TextMeshProを使う場合

public class RankingUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _rankText;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private Image skinIcon;
    // [SerializeField] private TextMeshProUGUI _dateText;
    public int skinID;
    public void SetData(int rank, string name, float score, int skinID)
    {
        _rankText.text = rank.ToString() + "位";
        _nameText.text = name;
        _scoreText.text = score.ToString("F2"); // 小数点2桁まで
        skinIcon.sprite = SkinDatabase.instance.GetSkinById(skinID).skinSprite;
        // _dateText.text = date;
    }
}