/// <summary>One run includes continues; only a retry or a new scene resets it.</summary>
public sealed class OceanRunProgress
{
    public int PlanktonCount { get; private set; }

    public bool Collect(int currentFish, int fishLimit, int planktonPerFish)
    {
        PlanktonCount++;
        return planktonPerFish > 0 && PlanktonCount % planktonPerFish == 0 && currentFish < fishLimit;
    }

    public void Reset()
    {
        PlanktonCount = 0;
    }
}
