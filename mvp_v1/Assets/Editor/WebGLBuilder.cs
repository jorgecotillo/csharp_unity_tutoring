using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
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

                // FAST vs QUALITY build. Pass "-fastBuild" (the studio auto-build does) to
                // trade runtime speed / download size for a MUCH shorter build — great for
                // iterating. Without it we do a full optimized build for a final/ship export.
                bool fast = HasArg("-fastBuild");
                ApplyBuildProfile(fast);

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
        /// Applies build settings tuned for either FAST iteration or QUALITY output.
        ///
        /// Fast mode is the big win for the studio's auto-rebuild loop: it compiles the
        /// IL2CPP-generated C++ in Debug (far quicker than Release), tells Emscripten to
        /// skip heavy LLVM optimization (BuildTimes), and turns off code stripping — all
        /// of which roughly halves the build. The cost (slightly slower runtime, a bigger
        /// download) is irrelevant for a low-complexity game previewed locally.
        ///
        /// Both modes are set EXPLICITLY so the result never depends on whatever a
        /// previous build persisted into ProjectSettings.
        /// </summary>
        private static void ApplyBuildProfile(bool fast)
        {
            try
            {
                PlayerSettings.SetIl2CppCompilerConfiguration(
                    NamedBuildTarget.WebGL,
                    fast ? Il2CppCompilerConfiguration.Debug : Il2CppCompilerConfiguration.Release);

                PlayerSettings.SetManagedStrippingLevel(
                    NamedBuildTarget.WebGL,
                    fast ? ManagedStrippingLevel.Minimal : ManagedStrippingLevel.Low);

                PlayerSettings.stripEngineCode = !fast;

                Log(fast
                    ? "Profile: FAST (IL2CPP=Debug, stripping=Minimal, stripEngineCode=off)"
                    : "Profile: QUALITY (IL2CPP=Release, stripping=Low, stripEngineCode=on)");
            }
            catch (Exception ex)
            {
                LogError($"Applying managed build profile failed (continuing): {ex.Message}");
            }

            // The Emscripten/LLVM optimization level is a WebGL-extension setting. We reach
            // it by reflection so a missing/renamed API can NEVER break the build — worst
            // case we just don't get this particular speedup.
            SetWebGLCodeOptimization(fast ? "BuildTimes" : "RuntimeSpeed");
        }

        /// <summary>
        /// Sets UnityEditor.WebGL.UserBuildSettings.codeOptimization by name via reflection.
        /// "BuildTimes" = fastest build (skip -O optimization); "RuntimeSpeed" = optimized.
        /// </summary>
        private static void SetWebGLCodeOptimization(string valueName)
        {
            try
            {
                Type t = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => { try { return a.GetType("UnityEditor.WebGL.UserBuildSettings"); } catch { return null; } })
                    .FirstOrDefault(x => x != null);
                if (t == null) { Log("codeOptimization: UserBuildSettings type not found (skipped)"); return; }

                PropertyInfo prop = t.GetProperty("codeOptimization", BindingFlags.Public | BindingFlags.Static);
                if (prop == null) { Log("codeOptimization: property not found (skipped)"); return; }

                object enumVal = Enum.Parse(prop.PropertyType, valueName, ignoreCase: true);
                prop.SetValue(null, enumVal);
                Log($"WebGL codeOptimization = {valueName}");
            }
            catch (Exception ex)
            {
                Log($"codeOptimization set skipped ({ex.Message})");
            }
        }

        /// <summary>True if the given flag is present on the Unity command line.</summary>
        private static bool HasArg(string name)
        {
            return Environment.GetCommandLineArgs()
                .Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
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
