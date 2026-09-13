using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SeptemberUpdateBuildComparison
{
    internal const string StripKey = "DriftOcean.BuildWithoutOpeningBGM";
    private const string Output = "Builds/SeptemberUpdate";

    // Also usable in a separate batch-mode checkout if the interactive Editor is busy.
    public static void RunBatch()
    {
        Directory.CreateDirectory(Output);
        BuildBoth();
        if (!File.ReadAllText(Path.Combine(Output, "build-status.txt")).StartsWith("Succeeded", StringComparison.Ordinal))
            throw new InvalidOperationException("See Builds/SeptemberUpdate/build-status.txt for the build error.");
    }

    // Capacity measurement can use Unity's debug key when the release password is unavailable.
    public static void RunBatchForSizeComparison()
    {
        bool previousCustomKeystore = PlayerSettings.Android.useCustomKeystore;
        try
        {
            PlayerSettings.Android.useCustomKeystore = false;
            RunBatch();
        }
        finally { PlayerSettings.Android.useCustomKeystore = previousCustomKeystore; }
    }

    [MenuItem("DriftOcean/September Update/Build Android BGM Size Comparison")]
    public static void Begin()
    {
        if (EditorApplication.isPlaying || BuildPipeline.isBuildingPlayer)
            throw new InvalidOperationException("Stop Play mode and wait for any current build.");
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            throw new InvalidOperationException("Select Android before running the comparison.");
        Directory.CreateDirectory(Output);
        File.WriteAllText(Path.Combine(Output, "build-status.txt"), "Queued");
        EditorApplication.delayCall += BuildBoth;
    }

    private static void BuildBoth()
    {
        bool previousBundle = EditorUserBuildSettings.buildAppBundle;
        try
        {
            EditorUserBuildSettings.buildAppBundle = true;
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var full = Build(scenes, false);
            var reduced = Build(scenes, true);
            long difference = full.bytes - reduced.bytes;
            string comparison = "# DriftOcean 2026年9月版 容量比較\n\n" +
                "同じAndroid設定で出力したAABファイルの実測値です。端末へのダウンロード容量・インストール後容量とは異なります。\n\n" +
                (PlayerSettings.Android.useCustomKeystore ? "署名: プロジェクト設定のカスタム署名。\n\n" : "署名: Unityの検証用署名。容量比較用のAABです。ストア公開には本番の署名で再ビルドしてください。\n\n") +
                "| 構成 | AABサイズ | バイト数 |\n| --- | ---: | ---: |\n" +
                $"| BGMをすべて残す完成版 | {full.bytes / 1000000d:F2} MB | {full.bytes:N0} |\n" +
                $"| 序盤BGMだけ除外した比較版 | {reduced.bytes / 1000000d:F2} MB | {reduced.bytes:N0} |\n" +
                $"| 差 | {difference / 1000000d:F2} MB | {difference:N0} |\n\n" +
                "除外対象: `wondrous-waters_3 1.mp3`。通常のプロジェクトと完成版には残しています。比較版はビルド中のシーンコピーから参照を除外し、元のシーン・Prefab・音楽ファイルには変更を加えません。\n\n" +
                $"Unity: {Application.unityVersion}、Android、{PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android)}、{PlayerSettings.Android.targetArchitectures}。\n\n" +
                $"序盤BGMのビルドレポート内データ量: 完成版 {full.openingAudioBytes:N0} bytes、比較版 {reduced.openingAudioBytes:N0} bytes。\n";
            File.WriteAllText(Path.Combine(Output, "容量比較.md"), comparison);
            File.WriteAllText(Path.Combine(Output, "build-status.txt"), "Succeeded\n" + comparison);
            Debug.Log("[SeptemberUpdate] Both Android builds succeeded. " + difference + " bytes saved without opening BGM.");
        }
        catch (Exception exception)
        {
            File.WriteAllText(Path.Combine(Output, "build-status.txt"), "Failed\n" + exception);
            Debug.LogError("[SeptemberUpdate] Build comparison failed: " + exception);
        }
        finally
        {
            SessionState.SetBool(StripKey, false);
            EditorUserBuildSettings.buildAppBundle = previousBundle;
        }
    }

    private static BuildSize Build(string[] scenes, bool strip)
    {
        SessionState.SetBool(StripKey, strip);
        string name = strip ? "DriftOcean-without-opening-bgm.aab" : "DriftOcean.aab";
        File.WriteAllText(Path.Combine(Output, "build-status.txt"), "Building " + name);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(Output, name),
            target = BuildTarget.Android,
            options = BuildOptions.DetailedBuildReport
        });
        string messages = string.Join("\n", report.steps.SelectMany(s => s.messages)
            .Where(m => m.type == LogType.Error || m.type == LogType.Exception || m.type == LogType.Warning)
            .Select(m => m.type + ": " + m.content));
        File.WriteAllText(Path.Combine(Output, name + ".build-log.txt"), report.summary.result + "\n" + messages);
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException(name + ": " + report.summary.result + "\n" + messages);
        ulong audio = 0;
        foreach (var packed in report.packedAssets)
            foreach (var content in packed.contents)
                if (content.sourceAssetPath.EndsWith("wondrous-waters_3 1.mp3", StringComparison.Ordinal)) audio += content.packedSize;
        return new BuildSize { bytes = new FileInfo(Path.Combine(Output, name)).Length, openingAudioBytes = audio };
    }

    private struct BuildSize
    {
        public long bytes;
        public ulong openingAudioBytes;
    }
}

public class SeptemberOpeningBgmBuildProcessor : IProcessSceneWithReport
{
    public int callbackOrder => 1000;

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (report == null || !SessionState.GetBool(SeptemberUpdateBuildComparison.StripKey, false)) return;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var manager in root.GetComponentsInChildren<StageBGMManager>(true))
            {
                var serialized = new SerializedObject(manager);
                var normal = serialized.FindProperty("_normalBGM");
                var clip = normal.objectReferenceValue as AudioClip;
                normal.objectReferenceValue = null;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                foreach (var audio in root.GetComponentsInChildren<AudioSource>(true))
                    if (audio.clip == clip) audio.clip = null;
            }
        }
    }
}
