using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SeptemberUpdateSetup
{
    [MenuItem("DriftOcean/September Update/Set Up Assets and Main Scene")]
    public static void SetUp()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before setting up assets.");
        Directory.CreateDirectory("Assets/Scriptable/OceanLife");
        Directory.CreateDirectory("Assets/Prefabs/OceanLife");
        AssetDatabase.Refresh();
        ImportSprite("Assets/Sprites/OceanLife/plankton.png");
        ImportSprite("Assets/Sprites/OceanLife/kozakana.png");

        var settings = LoadOrCreate<OceanLifeSettings>("Assets/Scriptable/OceanLife/OceanLifeSettings.asset");
        LoadOrCreate<PlayerNameFilterSettings>("Assets/Resources/PlayerNameFilter.asset");
        SetUpNicknameMessage();
        var material = AssetDatabase.LoadAssetAtPath<Material>(
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");

        var plankton = CreatePlankton(material);
        var fish = CreateFish(material);
        var audioImporter = AssetImporter.GetAtPath("Assets/Audios/plankton_pop.wav") as AudioImporter;
        if (audioImporter != null)
        {
            audioImporter.forceToMono = true;
            var sample = audioImporter.defaultSampleSettings;
            sample.loadType = AudioClipLoadType.DecompressOnLoad;
            sample.compressionFormat = AudioCompressionFormat.PCM;
            sample.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            audioImporter.defaultSampleSettings = sample;
            audioImporter.SaveAndReimport();
        }

        var scene = SceneManager.GetSceneByPath("Assets/Scenes/Main.unity");
        if (!scene.isLoaded) scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Additive);
        var existing = InScene<OceanLifeManager>(scene);
        if (existing == null)
        {
            var root = new GameObject("OceanLifeManager");
            var manager = root.AddComponent<OceanLifeManager>();
            var audio = root.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0f;
            SetObject(manager, "_settings", settings);
            SetObject(manager, "_planktonPrefab", plankton);
            SetObject(manager, "_fishPrefab", fish);
            SetObject(manager, "_pickupAudio", audio);
            SetObject(manager, "_pickupClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audios/plankton_pop.wav"));
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/OceanLife/OceanLifeManager.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            existing = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, scene)).GetComponent<OceanLifeManager>();
        }
        SetObject(existing, "_player", InScene<PlayerController>(scene));
        SetObject(existing, "_camera", InScene<Camera>(scene));

        var gameOver = InScene<GameOverManager>(scene);
        var serialized = new SerializedObject(gameOver);
        var panel = (GameObject)serialized.FindProperty("_gameOverPanel").objectReferenceValue;
        var countTransform = panel.transform.Find("PlanktonCountText");
        TextMeshProUGUI count;
        if (countTransform == null)
        {
            var source = (TextMeshProUGUI)serialized.FindProperty("_scoreText").objectReferenceValue;
            count = UnityEngine.Object.Instantiate(source, panel.transform);
            count.name = "PlanktonCountText";
        }
        else count = countTransform.GetComponent<TextMeshProUGUI>();
        count.text = "PLANKTON × 0";
        var score = (TextMeshProUGUI)serialized.FindProperty("_scoreText").objectReferenceValue;
        score.rectTransform.anchoredPosition = new Vector2(0f, 45f);
        var notice = (GameObject)serialized.FindProperty("_unlockNoticePanel").objectReferenceValue;
        var noticeRect = notice.GetComponent<RectTransform>();
        noticeRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 80f);
        float currentNoticeY = panel.transform.InverseTransformPoint(noticeRect.TransformPoint(noticeRect.rect.center)).y;
        noticeRect.anchoredPosition += new Vector2(0f, 120f - currentNoticeY);
        var noticeText = (TextMeshProUGUI)serialized.FindProperty("_unlockNoticeText").objectReferenceValue;
        noticeText.enableAutoSizing = true;
        noticeText.fontSizeMin = 20f;
        noticeText.fontSizeMax = 24f;
        count.fontSize = 34f;
        count.enableAutoSizing = true;
        count.fontSizeMin = 26f;
        count.fontSizeMax = 34f;
        count.alignment = TextAlignmentOptions.Center;
        count.raycastTarget = false;
        count.rectTransform.anchorMin = count.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        count.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        count.rectTransform.anchoredPosition = new Vector2(0f, 185f);
        count.rectTransform.sizeDelta = new Vector2(520f, 50f);
        SetObject(gameOver, "_planktonCountText", count);
        PrefabUtility.RecordPrefabInstancePropertyModifications(existing);
        PrefabUtility.RecordPrefabInstancePropertyModifications(gameOver);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[SeptemberUpdate] Main scene, sprites, prefabs and filter settings are ready.");
    }

    private static T InScene<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).FirstOrDefault();
    }

    private static void SetUpNicknameMessage()
    {
        const string path = "Assets/Prefabs/NameUI/NicknamePanel.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var label = root.GetComponentsInChildren<TextMeshProUGUI>(true).First(t => t.name == "Text (TMP)");
            label.rectTransform.anchoredPosition = new Vector2(-97f, 70f);
            label.rectTransform.sizeDelta = new Vector2(560f, 90f);
            label.margin = Vector4.zero;
            label.enableAutoSizing = true;
            label.fontSizeMin = 24f;
            label.fontSizeMax = 36f;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }

    private static void ImportSprite(string path)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16f;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static PlanktonPickup CreatePlankton(Material material)
    {
        const string path = "Assets/Prefabs/OceanLife/Plankton.prefab";
        var asset = AssetDatabase.LoadAssetAtPath<PlanktonPickup>(path);
        if (asset != null) return asset;
        var root = new GameObject("Plankton");
        root.transform.localScale = Vector3.one * 0.14f;
        var renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/OceanLife/plankton.png");
        renderer.sharedMaterial = material;
        renderer.sortingOrder = 1;
        var body = root.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        var collider = root.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.5f;
        var pickup = root.AddComponent<PlanktonPickup>();
        var halo = new GameObject("Glow");
        halo.transform.SetParent(root.transform, false);
        halo.transform.localScale = Vector3.one * 1.65f;
        var glow = halo.AddComponent<SpriteRenderer>();
        glow.sprite = renderer.sprite;
        glow.sharedMaterial = material;
        glow.sortingOrder = 0;
        glow.color = new Color(1f, 1f, 1f, 0.16f);
        SetObject(pickup, "_glow", glow);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab.GetComponent<PlanktonPickup>();
    }

    private static CompanionFish CreateFish(Material material)
    {
        const string path = "Assets/Prefabs/OceanLife/CompanionFish.prefab";
        var asset = AssetDatabase.LoadAssetAtPath<CompanionFish>(path);
        if (asset != null) return asset;
        var root = new GameObject("CompanionFish");
        root.transform.localScale = Vector3.one * 0.4f;
        var renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/OceanLife/kozakana.png");
        renderer.sharedMaterial = material;
        renderer.sortingOrder = 0;
        var body = root.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        var collider = root.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.68f, 0.32f);
        collider.offset = new Vector2(0.02f, 0.02f);
        root.AddComponent<CompanionFish>();
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab.GetComponent<CompanionFish>();
    }

    private static void SetObject(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
