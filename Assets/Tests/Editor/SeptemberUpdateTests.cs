using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class SeptemberUpdateTests
{
    [Test]
    public void FishArrivesOnlyAtHundredsAndNeverBanksRewardsAtCapacity()
    {
        var run = new OceanRunProgress();
        for (int i = 1; i < 100; i++) Assert.IsFalse(run.Collect(0, 3, 100));
        Assert.IsTrue(run.Collect(0, 3, 100));
        for (int i = 101; i <= 199; i++) Assert.IsFalse(run.Collect(1, 3, 100));
        Assert.IsTrue(run.Collect(1, 3, 100));
        for (int i = 201; i <= 299; i++) Assert.IsFalse(run.Collect(2, 3, 100));
        Assert.IsTrue(run.Collect(2, 3, 100));
        for (int i = 301; i <= 450; i++) Assert.IsFalse(run.Collect(3, 3, 100));
        // Losing a companion at 450 does not cash in the missed reward at 400.
        for (int i = 451; i < 500; i++) Assert.IsFalse(run.Collect(2, 3, 100));
        Assert.IsTrue(run.Collect(2, 3, 100));
        Assert.AreEqual(500, run.PlanktonCount);
        run.Reset();
        Assert.AreEqual(0, run.PlanktonCount);
        Assert.IsFalse(run.Collect(0, 3, 100));
    }

    [Test]
    public void PlanktonPaletteMatchesRequestedPercentagesAndDeclinesLater()
    {
        var settings = AssetDatabase.LoadAssetAtPath<OceanLifeSettings>("Assets/Scriptable/OceanLife/OceanLifeSettings.asset");
        Assert.IsNotNull(settings);
        int[] expected = { 40, 20, 15, 8, 5, 3, 4, 5 };
        int[] counts = new int[expected.Length];
        for (int i = 0; i < 100; i++)
        {
            Color color = settings.GetPlanktonColor((i + 0.5f) / 100f);
            int index = System.Array.FindIndex(settings.colors, entry => entry.color == color);
            Assert.GreaterOrEqual(index, 0);
            counts[index]++;
        }
        CollectionAssert.AreEqual(expected, counts);
        Assert.Less(settings.GetSpawnInterval(1950), settings.GetSpawnInterval(1990));
        Assert.Less(settings.GetSpawnInterval(1990), settings.GetSpawnInterval(2050));
        Assert.AreEqual(100, settings.planktonPerFish);
        Assert.AreEqual(3, settings.maxFish);
    }

    [Test]
    public void NameFilterCoversEverySuppliedWordAndEditableAdditions()
    {
        var settings = Resources.Load<PlayerNameFilterSettings>("PlayerNameFilter");
        Assert.IsNotNull(settings);
        Assert.AreEqual(15, settings.blockedWords.Length);
        foreach (string word in settings.blockedWords)
        {
            Assert.IsTrue(PlayerNameFilter.ContainsBlockedWord(word, settings), word);
            Assert.IsTrue(PlayerNameFilter.ContainsBlockedWord(word.ToUpperInvariant(), settings), word);
            Assert.IsTrue(PlayerNameFilter.ContainsBlockedWord(string.Join(".", word.ToCharArray()), settings), word);
        }
        Assert.IsTrue(PlayerNameFilter.ContainsBlockedWord("ＰＯＲＮ", settings));
        Assert.IsTrue(PlayerNameFilter.ContainsBlockedWord("p\u200bo\u200brn", settings));
        foreach (string name in new[] { "OceanBlue", "くらげ", "魚太郎123", "player", "ぷよぷよ" })
            Assert.IsFalse(PlayerNameFilter.ContainsBlockedWord(name, settings), name);
        var copy = Object.Instantiate(settings);
        try
        {
            copy.blockedWords = copy.blockedWords.Concat(new[] { "追加禁止語" }).ToArray();
            Assert.IsTrue(PlayerNameFilter.ContainsBlockedWord("追加禁止語", copy));
        }
        finally { Object.DestroyImmediate(copy); }
    }

    [Test]
    public void OptionalWholeWordModeAllowsInnocentSubstringsAndStillRejectsSeparatedWords()
    {
        var settings = ScriptableObject.CreateInstance<PlayerNameFilterSettings>();
        try
        {
            settings.matchInsideEnglishWords = false;
            foreach (string name in new[] { "peacock", "Scunthorpe", "spice", "grape" })
                Assert.IsFalse(PlayerNameFilter.ContainsBlockedWord(name, settings), name);
            foreach (string name in new[] { "hello porn", "p.o.r.n", "ＰＯＲＮ123", "死ね太郎" })
                Assert.IsTrue(PlayerNameFilter.ContainsBlockedWord(name, settings), name);
        }
        finally { Object.DestroyImmediate(settings); }
    }

    [Test]
    public void PrefabsUseSuppliedSpritesAndHarmlessTriggerColliders()
    {
        var plankton = AssetDatabase.LoadAssetAtPath<PlanktonPickup>("Assets/Prefabs/OceanLife/Plankton.prefab");
        var fish = AssetDatabase.LoadAssetAtPath<CompanionFish>("Assets/Prefabs/OceanLife/CompanionFish.prefab");
        Assert.IsNotNull(plankton);
        Assert.IsNotNull(fish);
        Assert.IsTrue(plankton.GetComponent<Collider2D>().isTrigger);
        Assert.IsTrue(fish.GetComponent<Collider2D>().isTrigger);
        Assert.IsFalse(fish.CompareTag("Obstacle"));
        Assert.AreEqual("Assets/Sprites/OceanLife/plankton.png", AssetDatabase.GetAssetPath(plankton.GetComponent<SpriteRenderer>().sprite));
        Assert.AreEqual("Assets/Sprites/OceanLife/kozakana.png", AssetDatabase.GetAssetPath(fish.GetComponent<SpriteRenderer>().sprite));
        foreach (var sprite in new[] { plankton.GetComponent<SpriteRenderer>().sprite, fish.GetComponent<SpriteRenderer>().sprite })
        {
            Assert.AreEqual(new Vector2(16, 16), sprite.rect.size);
            Assert.AreEqual(FilterMode.Point, sprite.texture.filterMode);
        }
    }
}
