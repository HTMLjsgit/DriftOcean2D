using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "ObstacleData", menuName = "ScriptableObject/ObstacleData")]
public class ObstacleData : ScriptableObject
{
    public int id;

    [FormerlySerializedAs("name")]
    public string displayName;
    public GameObject prefab;
    public float baseSpeed;

    [Header("Ocean Log")]
    public Sprite encyclopediaSprite;
    [TextArea(2, 5)] public string description;
    [TextArea(2, 5)] public string decomposition;
    [TextArea(2, 6)] public string materials;
    [TextArea(2, 6)] public string sources;
    [TextArea(2, 5)] public string trivia;
    [TextArea(1, 3)] public string era;

    [Header("Ocean Log English")]
    public string displayNameEnglish;
    [TextArea(2, 5)] public string descriptionEnglish;
    [TextArea(2, 5)] public string decompositionEnglish;
    [TextArea(2, 6)] public string materialsEnglish;
    [TextArea(2, 6)] public string sourcesEnglish;
    [TextArea(2, 5)] public string triviaEnglish;
    [TextArea(1, 3)] public string eraEnglish;

    public Sprite GetEncyclopediaSprite()
    {
        if (encyclopediaSprite != null)
        {
            return encyclopediaSprite;
        }

        return prefab != null ? prefab.GetComponentInChildren<SpriteRenderer>()?.sprite : null;
    }
}
