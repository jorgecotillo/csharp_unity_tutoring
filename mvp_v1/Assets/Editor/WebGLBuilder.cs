using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GoblinSiege.EditorTools
{
    /// <summary>
    /// Headless WebGL build entry point for Goblin Siege.
    ///
    /// The game has no authored scenes: <c>RaidBootstrap</c> spawns the entire game in
    /// code via <c>[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]</c>. So this builder
    /// only needs ONE empty scene registered in the build so the player has something to
    /// load; the bootstrap then assembles the game at runtime.
    ///
    /// Invoke headless:
    ///   Unity.exe -batchmode -nographics -projectPath "&lt;mvp_v1&gt;" \
    ///     -buildTarget WebGL \
    ///     -executeMethod GoblinSiege.EditorTools.WebGLBuilder.Build \
    ///     -logFile - [-buildOutput "&lt;abs path&gt;"]
    ///
    /// Do NOT pass -quit; this method calls EditorApplication.Exit itself with the build
    /// result code (0 = success, 1 = failure).
    /// </summary>
    public static class WebGLBuilder
    {
        private const string ScenesDir = "Assets/Scenes";
        private const string BootstrapScenePath = "Assets/Scenes/Main.unity";

        public static void Build()
        {
            try
            {
                string outputDir = ResolveOutputDir();
                string scenePath = EnsureBootstrapScene();

                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(scenePath, true)
                };

                // Phase 0: simplest possible WebGL hosting. No build-time compression so the
                // files serve correctly from a plain static host without special headers.
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
                PlayerSettings.WebGL.decompressionFallback = true;

                Directory.CreateDirectory(outputDir);

                var options = new BuildPlayerOptions
                {
                    scenes = new[] { scenePath },
                    locationPathName = outputDir,
                    target = BuildTarget.WebGL,
                    targetGroup = BuildTargetGroup.WebGL,
                    options = BuildOptions.None
                };

                Log($"Starting WebGL build → {outputDir}");
                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildSummary summary = report.summary;

                if (summary.result == BuildResult.Succeeded)
                {
                    Log($"WebGL build SUCCEEDED: {summary.totalSize} bytes, {summary.totalTime}");
                    EditorApplication.Exit(0);
                }
                else
                {
                    LogError($"WebGL build FAILED: result={summary.result}, errors={summary.totalErrors}");
                    EditorApplication.Exit(1);
                }
            }
            catch (Exception ex)
            {
                LogError($"WebGL build threw an exception: {ex}");
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Ensures a single empty scene exists at <see cref="BootstrapScenePath"/> and is
        /// saved to disk. Returns the scene asset path.
        /// </summary>
        private static string EnsureBootstrapScene()
        {
            if (File.Exists(BootstrapScenePath))
            {
                Log($"Bootstrap scene already exists: {BootstrapScenePath}");
                return BootstrapScenePath;
            }

            if (!Directory.Exists(ScenesDir))
            {
                Directory.CreateDirectory(ScenesDir);
            }

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.DefaultGameObjects,
                NewSceneMode.Single);

            bool saved = EditorSceneManager.SaveScene(scene, BootstrapScenePath);
            if (!saved)
            {
                throw new Exception($"Failed to save bootstrap scene to {BootstrapScenePath}");
            }

            AssetDatabase.Refresh();
            Log($"Created bootstrap scene: {BootstrapScenePath}");
            return BootstrapScenePath;
        }

        /// <summary>
        /// Resolves the output directory. Honors an optional "-buildOutput &lt;abs path&gt;"
        /// command line argument; otherwise defaults to &lt;projectRoot&gt;/Builds/WebGL.
        /// </summary>
        private static string ResolveOutputDir()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], "-buildOutput", StringComparison.OrdinalIgnoreCase))
                {
                    string custom = args[i + 1];
                    if (!string.IsNullOrWhiteSpace(custom))
                    {
                        return Path.GetFullPath(custom);
                    }
                }
            }

            string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            return Path.Combine(projectRoot, "Builds", "WebGL");
        }

        private static void Log(string message)
        {
            Debug.Log($"[WebGLBuilder] {message}");
            Console.WriteLine($"[WebGLBuilder] {message}");
        }

        private static void LogError(string message)
        {
            Debug.LogError($"[WebGLBuilder] {message}");
            Console.WriteLine($"[WebGLBuilder][ERROR] {message}");
        }
    }
}
