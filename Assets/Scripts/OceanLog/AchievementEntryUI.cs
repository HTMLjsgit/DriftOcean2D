using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementEntryUI : MonoBehaviour
{
    [SerializeField] private Image _checkBackground;
    [SerializeField] private TextMeshProUGUI _checkText;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;

    public void Initialize(AchievementData data, bool completed)
    {
        _checkBackground.color = completed
            ? new Color(0.08f, 0.67f, 0.45f, 1f)
            : new Color(0f, 0f, 0f, 0f);
        _checkText.SetText(completed ? "✓" : "□");
        _nameText.SetText(data.achievementName);
        _descriptionText.SetText(data.description);
    }
}
