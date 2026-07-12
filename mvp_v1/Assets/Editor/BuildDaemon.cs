using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GoblinSiege.EditorTools
{
    /// <summary>
    /// Persistent WebGL build daemon. Keeps a headless Unity editor resident so
    /// repeated builds skip the ~1-2 minutes of editor launch + initial project
    /// import that a fresh one-shot build pays every time.
    ///
    /// SAFETY: Editor-only (never in a player build) and INERT unless launched with
    /// the environment variable GS_BUILD_DAEMON=1. So a normal one-shot headless
    /// build (refresh-preview.ps1) loads this type but the daemon does nothing.
    ///
    /// PROTOCOL (files under mvp_v1/Builds/daemon/):
    ///   heartbeat.txt  — rewritten ~1x/sec: "utcIso|beats=N|phase=...". The studio
    ///                    treats a FRESH heartbeat as "daemon is alive".
    ///   request.txt    — the studio writes a unique build id here to ask for a build.
    ///   result.txt     — the daemon writes "id|SUCCESS|msg" or "id|FAILED|msg".
    ///
    /// DOMAIN-RELOAD SURVIVAL: picking up Warren's code edits requires
    /// AssetDatabase.Refresh(), which recompiles changed scripts and triggers a
    /// DOMAIN RELOAD (all static state is wiped and [InitializeOnLoad] re-runs). We
    /// therefore never build in the same tick as Refresh(); instead we stash the
    /// pending build id in SessionState (which survives the reload) and let a later
    /// tick — after compilation settles — run the build against the fresh code.
    /// </summary>
    [InitializeOnLoad]
    public static class BuildDaemon
    {
        private const string PendingKey = "gs_daemon_pending_id";
        private const string PendingAtKey = "gs_daemon_pending_at";
        // After a request we wait until NOT compiling AND this many seconds have
        // elapsed, so a compile/reload triggered by Refresh() has a chance to start.
        private const double SettleSeconds = 2.0;

        private static readonly string ProjectRoot =
            Directory.GetParent(Application.dataPath)!.FullName;
        private static readonly string DaemonDir =
            Path.Combine(ProjectRoot, "Builds", "daemon");
        private static readonly string HeartbeatFile = Path.Combine(DaemonDir, "heartbeat.txt");
        private static readonly string RequestFile = Path.Combine(DaemonDir, "request.txt");
        private static readonly string ResultFile = Path.Combine(DaemonDir, "result.txt");

        private static double _lastBeat;
        private static int _beats;

        static BuildDaemon()
        {
            if (Environment.GetEnvironmentVariable("GS_BUILD_DAEMON") != "1")
            {
                return; // inert for normal one-shot builds
            }

            try { Directory.CreateDirectory(DaemonDir); } catch { }
            EditorApplication.update += Tick;
            Console.WriteLine("[BuildDaemon] registered via InitializeOnLoad");
            WriteBeat("registered");
        }

        private static void Tick()
        {
            // Heartbeat ~1x/sec so the studio can tell the daemon is alive.
            double now = EditorApplication.timeSinceStartup;
            if (now - _lastBeat >= 1.0)
            {
                _lastBeat = now;
                _beats++;
                WriteBeat("tick");
            }

            // Never touch builds while the editor is busy compiling / importing.
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            // Is a build pending (freshly requested, or carried across a reload)?
            string pending = SessionState.GetString(PendingKey, "");
            if (!string.IsNullOrEmpty(pending))
            {
                double reqAt = ParseDouble(SessionState.GetString(PendingAtKey, "0"));
                if (now - reqAt < SettleSeconds)
                {
                    return; // give a possible recompile/reload time to begin
                }
                SessionState.SetString(PendingKey, ""); // consume before building
                DoBuild(pending);
                return;
            }

            // New request from the studio?
            if (File.Exists(RequestFile))
            {
                string id;
                try { id = File.ReadAllText(RequestFile).Trim(); }
                catch { return; } // mid-write; try again next tick
                try { File.Delete(RequestFile); } catch { }
                if (string.IsNullOrEmpty(id)) return;

                WriteBeat("request:" + id);
                SessionState.SetString(PendingKey, id);
                SessionState.SetString(PendingAtKey, now.ToString(CultureInfo.InvariantCulture));

                // Pull in any external code edits. If scripts changed this recompiles
                // and reloads the domain; the pending id (in SessionState) survives and
                // the post-reload tick runs the build against the fresh code.
                AssetDatabase.Refresh();
            }
        }

        private static void DoBuild(string id)
        {
            WriteBeat("building:" + id);
            bool ok;
            string msg;
            try
            {
                ok = WebGLBuilder.BuildOnce(out msg);
            }
            catch (Exception ex)
            {
                ok = false;
                msg = "daemon build threw: " + ex.Message;
            }

            try
            {
                File.WriteAllText(ResultFile, id + "|" + (ok ? "SUCCESS" : "FAILED") + "|" + msg);
            }
            catch { /* best effort — the studio will time out and fall back */ }

            WriteBeat("done:" + id);
            _lastBeat = 0; // force an immediate heartbeat so "alive" stays fresh
        }

        private static double ParseDouble(string s)
        {
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double v)
                ? v : 0.0;
        }

        private static void WriteBeat(string phase)
        {
            try
            {
                File.WriteAllText(
                    HeartbeatFile,
                    $"{DateTime.UtcNow:o}|beats={_beats}|phase={phase}");
            }
            catch { /* best effort */ }
        }
    }
}
