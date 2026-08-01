using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementEntryUI : MonoBehaviour
{
    [SerializeField] private Image _checkImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;

    public void Initialize(AchievementData data, bool completed)
    {
        _checkImage.gameObject.SetActive(completed);
        _nameText.SetText(data.achievementName);
        _descriptionText.SetText(data.description);
    }
}
