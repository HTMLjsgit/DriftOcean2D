using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeArea :  MonoBehaviour
{
    private void Start()
    {
        // セーフエリアを反映
        Apply(Screen.safeArea);
    }

    private void Apply(Rect safeArea)
    {
        var screenWidth = Screen.width;
        var screenHeight = Screen.height;
        
        var resolution = new Vector2Int(screenWidth, screenHeight);
        var normalizedMin = new Vector2(safeArea.xMin / resolution.x, safeArea.yMin / resolution.y);
        var normalizedMax = new Vector2(safeArea.xMax / resolution.x, safeArea.yMax / resolution.y);

        if (transform is RectTransform rectTransform)
        {
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchorMin = normalizedMin;
            rectTransform.anchorMax = normalizedMax;
        }
    }
}