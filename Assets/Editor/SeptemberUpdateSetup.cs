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
        var countTransform = panel.transform.Find("ScoreResult/PlanktonCountText") ?? panel.transform.Find("PlanktonCountText");
        TextMeshProUGUI count;
        if (countTransform == null)
        {
            var source = (TextMeshProUGUI)serialized.FindProperty("_scoreText").objectReferenceValue;
            count = UnityEngine.Object.Instantiate(source, panel.transform);
            count.name = "PlanktonCountText";
        }
        else count = countTransform.GetComponent<TextMeshProUGUI>();
        count.text = "PLANKTON × 0";
        SetObject(gameOver, "_planktonCountText", count);
        ConfigureResultLayout(gameOver, count);
        PrefabUtility.RecordPrefabInstancePropertyModifications(existing);
        PrefabUtility.RecordPrefabInstancePropertyModifications(gameOver);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[SeptemberUpdate] Main scene, sprites, prefabs and filter settings are ready.");
    }

    [MenuItem("DriftOcean/September Update/Apply Feedback Layout")]
    public static void ApplyFeedbackLayout()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before changing scene assets.");
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/Main.unity");
        if (!scene.isLoaded) scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Additive);
        var gameOver = InScene<GameOverManager>(scene);
        var serialized = new SerializedObject(gameOver);
        var count = (TextMeshProUGUI)serialized.FindProperty("_planktonCountText").objectReferenceValue;
        ConfigureResultLayout(gameOver, count);
        ConnectOceanLogDetailLabels();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureResultLayout(GameOverManager gameOver, TextMeshProUGUI count)
    {
        var serialized = new SerializedObject(gameOver);
        var score = (TextMeshProUGUI)serialized.FindProperty("_scoreText").objectReferenceValue;
        var card = (RectTransform)score.transform.parent;
        CenterRect(score.rectTransform, new Vector2(0f, 0f), new Vector2(520f, 90f));
        score.margin = Vector4.zero;
        score.alignment = TextAlignmentOptions.Center;
        score.enableAutoSizing = true;
        score.fontSizeMin = 38f;
        score.fontSizeMax = 76f;
        score.textWrappingMode = TextWrappingModes.NoWrap;
        score.text = "2000.39年";

        count.rectTransform.SetParent(card, false);
        CenterRect(count.rectTransform, new Vector2(0f, -90f), new Vector2(520f, 44f));
        count.margin = Vector4.zero;
        count.enableAutoSizing = true;
        count.fontSizeMin = 26f;
        count.fontSizeMax = 34f;
        count.alignment = TextAlignmentOptions.Center;
        count.textWrappingMode = TextWrappingModes.NoWrap;
        count.raycastTarget = false;

        var notice = (GameObject)serialized.FindProperty("_unlockNoticePanel").objectReferenceValue;
        CenterRect(notice.GetComponent<RectTransform>(), new Vector2(0f, -142f), new Vector2(520f, 48f));
        var noticeText = (TextMeshProUGUI)serialized.FindProperty("_unlockNoticeText").objectReferenceValue;
        noticeText.margin = Vector4.zero;
        noticeText.rectTransform.offsetMin = new Vector2(38f, 0f);
        noticeText.rectTransform.offsetMax = new Vector2(-10f, 0f);
        noticeText.enableAutoSizing = true;
        noticeText.fontSizeMin = 20f;
        noticeText.fontSizeMax = 24f;
        var icon = notice.transform.Find("UnlockImage") as RectTransform;
        if (icon != null) CenterRect(icon, new Vector2(-240f, 0f), new Vector2(24f, 24f));

        foreach (var target in new UnityEngine.Object[] { score, score.rectTransform, count, count.rectTransform,
            notice.GetComponent<RectTransform>(), noticeText, noticeText.rectTransform, icon })
        {
            if (target == null) continue;
            EditorUtility.SetDirty(target);
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
    }

    private static void CenterRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void ConnectOceanLogDetailLabels()
    {
        const string path = "Assets/Prefabs/UI/OceanLogFeature.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var ui = root.GetComponent<OceanLogUI>();
            var serialized = new SerializedObject(ui);
            var panel = (GameObject)serialized.FindProperty("_detailPanel").objectReferenceValue;
            var labels = panel.GetComponentsInChildren<TextMeshProUGUI>(true);
            SetObject(ui, "_detailDecompositionLabel", labels.First(t => t.transform.parent.name == "DecompositionHeader"));
            SetObject(ui, "_detailMaterialsLabel", labels.First(t => t.transform.parent.name == "MaterialsHeader"));
            SetObject(ui, "_detailSourcesLabel", labels.First(t => t.transform.parent.name == "SourcesHeader"));
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
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
