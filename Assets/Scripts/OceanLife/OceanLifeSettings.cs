using System;
using UnityEngine;

[CreateAssetMenu(fileName = "OceanLifeSettings", menuName = "DriftOcean/Ocean Life Settings")]
public class OceanLifeSettings : ScriptableObject
{
    [Header("Plankton / 出現間隔（横軸は年、縦軸は秒）")]
    public AnimationCurve spawnIntervalByYear = new AnimationCurve(
        new Keyframe(1950f, 0.65f), new Keyframe(1970f, 0.9f),
        new Keyframe(1990f, 1.5f), new Keyframe(2020f, 2.5f), new Keyframe(2050f, 4f));
    [Range(0f, 1f)] public float groupChance = 0.7f;
    [Min(2)] public int minGroupSize = 3;
    [Min(2)] public int maxGroupSize = 7;
    [Min(0.1f)] public float groupSpacing = 0.4f;
    [Min(0.1f)] public float driftSpeed = 1f;
    [Range(0f, 0.4f)] public float verticalDrift = 0.06f;
    [Range(0f, 1f)] public float minViewportY = 0.14f;
    [Range(0f, 1f)] public float maxViewportY = 0.87f;
    [Min(8)] public int maxActivePlankton = 100;

    [Header("Plankton / 色と出現比率")]
    public PlanktonColorWeight[] colors =
    {
        new PlanktonColorWeight("白・透明", new Color(1f, 1f, 1f, 0.85f), 40),
        new PlanktonColorWeight("黄緑", new Color(0.72f, 1f, 0.42f), 20),
        new PlanktonColorWeight("水色", new Color(0.45f, 0.9f, 1f), 15),
        new PlanktonColorWeight("黄色", new Color(1f, 0.94f, 0.35f), 8),
        new PlanktonColorWeight("オレンジ", new Color(1f, 0.65f, 0.3f), 5),
        new PlanktonColorWeight("茶色", new Color(0.7f, 0.47f, 0.27f), 3),
        new PlanktonColorWeight("紫", new Color(0.75f, 0.5f, 1f), 4),
        new PlanktonColorWeight("ピンク", new Color(1f, 0.62f, 0.83f), 5)
    };

    [Header("Companion Fish / 小魚")]
    [Min(1)] public int planktonPerFish = 100;
    [Range(1, 3)] public int maxFish = 3;
    public Vector2[] formationOffsets =
    {
        new Vector2(-0.45f, 0.3f), new Vector2(-0.7f, 0f), new Vector2(-0.48f, -0.3f)
    };
    [Range(0.02f, 0.6f)] public float followDelay = 0.18f;
    [Range(0f, 0.2f)] public float additionalDelayPerFish = 0.08f;
    [Min(0.1f)] public float joinSpeed = 3f;
    [Min(0.02f)] public float followSmoothTime = 0.1f;
    [Min(0f)] public float farewellPause = 0.3f;
    [Min(0.1f)] public float escapeSpeed = 4.5f;

    public float GetSpawnInterval(float year)
    {
        return Mathf.Max(0.15f, spawnIntervalByYear.Evaluate(year));
    }

    public Color GetPlanktonColor(float sample)
    {
        float total = 0f;
        if (colors == null) return Color.white;
        foreach (var entry in colors) total += Mathf.Max(0f, entry.weight);
        if (total <= 0f) return Color.white;
        float remaining = Mathf.Clamp(sample, 0f, 0.999999f) * total;
        foreach (var entry in colors)
        {
            remaining -= Mathf.Max(0f, entry.weight);
            if (remaining < 0f) return entry.color;
        }
        return Color.white;
    }
}

[Serializable]
public struct PlanktonColorWeight
{
    public string label;
    public Color color;
    [Min(0f)] public float weight;

    public PlanktonColorWeight(string label, Color color, float weight)
    {
        this.label = label;
        this.color = color;
        this.weight = weight;
    }
}
