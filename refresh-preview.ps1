<#
  refresh-preview.ps1  —  REBUILD the Goblin Siege game AND ship it to the portal.

  This is the ONE command to run when you want Warren's live preview to reflect the
  latest C# code. It does the whole pipeline:

      1. Runs a REAL Unity WebGL build (headless, ~7 min) from the C# source
      2. Confirms the build actually succeeded (reads the build log)
      3. Calls publish-game.ps1 to copy the fresh export into docs\game,
         stamp build-info.json, commit, and push to main
      4. The portal poller ff-merges main within ~30s and the preview reloads

  WHY THIS EXISTS:
    publish-game.ps1 by itself only COPIES an existing export — it never rebuilds.
    Pushing C# to main also never rebuilds the game. So without this script, a
    "refresh" could publish STALE binaries under a fresh label. This script closes
    that gap: it compiles first, verifies, THEN publishes.

  HOW TO USE (from the repo root):
      powershell -ExecutionPolicy Bypass -File .\refresh-preview.ps1

  OPTIONS:
    -NoPush        Build + copy + commit locally, but DON'T push to main
                   (review before shipping).
    -SkipBuild     Skip the Unity build and just publish whatever is already in
                   mvp_v1\Builds\WebGL (same as the old publish-only behavior).
    -UnityExe <p>  Path to Unity.exe (default: the installed 6000.3.15f1 editor).

  GOTCHAS THIS SCRIPT HANDLES FOR YOU:
    - Cold Library no-op: if Unity's Library cache is cold, a headless
      -executeMethod build can exit 0 WITHOUT running the build (Unity just
      compiles scripts and quits). This script detects that (the build log has no
      "[WebGLBuilder] Starting WebGL build" line) and automatically retries once
      with a now-warm Library.
    - It only publishes if the log contains "[WebGLBuilder] WebGL build SUCCEEDED".
      A failed or no-op build never ships.

  NOTE: Close any Unity Editor that has THIS worktree's mvp_v1 project open first,
  or the batch build will wait on the Unity lock. (Your separate main-checkout
  Editor is a different project and does not conflict.)
#>

[CmdletBinding()]
param(
    [switch]$NoPush,
    [switch]$SkipBuild,
    [string]$UnityExe = "C:\Program Files\Unity\Hub\Editor\6000.3.15f1\Editor\Unity.exe"
)

$ErrorActionPreference = 'Stop'

function Say([string]$msg, [string]$color = 'Cyan') { Write-Host $msg -ForegroundColor $color }

# --- Resolve repo root from this script's location, so it works from anywhere ---
$RepoRoot    = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $RepoRoot

$ProjectPath = Join-Path $RepoRoot 'mvp_v1'
$ExportDir   = Join-Path $ProjectPath 'Builds\WebGL'
$LogFile     = Join-Path $ProjectPath 'Builds\webgl-build.log'   # Builds\ is gitignored
$Method      = 'GoblinSiege.EditorTools.WebGLBuilder.Build'
$Publish     = Join-Path $RepoRoot 'publish-game.ps1'

Say "==================================================" 'DarkCyan'
Say " Warren's Game Studio  -  Rebuild + Publish Preview" 'White'
Say "==================================================" 'DarkCyan'

# ------------------------------------------------------------------
#  Runs one headless Unity WebGL build. Returns a status string:
#    'SUCCEEDED' | 'NOOP' (cold-Library, safe to retry) | 'FAILED'
# ------------------------------------------------------------------
function Invoke-UnityBuild {
    if (Test-Path $LogFile) { Remove-Item $LogFile -Force }

    $unityArgs = @(
        '-batchmode', '-nographics',
        '-projectPath', $ProjectPath,
        '-buildTarget', 'WebGL',
        '-executeMethod', $Method,
        '-logFile', $LogFile
    )
    # NOTE: do NOT pass -quit; WebGLBuilder.Build calls EditorApplication.Exit itself.

    Say "   Launching Unity (headless). This takes ~7 minutes..." 'Cyan'
    $proc = Start-Process -FilePath $UnityExe -ArgumentList $unityArgs -PassThru -Wait
    Say "   Unity exited (code $($proc.ExitCode)). Checking the build log..." 'Cyan'

    $log = ''
    if (Test-Path $LogFile) { $log = Get-Content $LogFile -Raw }

    if ($log -match '\[WebGLBuilder\] WebGL build SUCCEEDED') { return 'SUCCEEDED' }
    if ($log -notmatch '\[WebGLBuilder\] Starting WebGL build') { return 'NOOP' }
    return 'FAILED'
}

# --- 1. Build (unless -SkipBuild) ---
if ($SkipBuild) {
    Say "1. Skipping Unity build (-SkipBuild). Publishing existing export." 'Yellow'
} else {
    if (-not (Test-Path $UnityExe)) {
        Say "X  Unity not found at: $UnityExe" 'Red'
        Say "   Pass -UnityExe <path> to point at your editor." 'Yellow'
        exit 1
    }

    Say "1. Building Goblin Siege for WebGL from the C# source..." 'Green'
    $result = Invoke-UnityBuild

    if ($result -eq 'NOOP') {
        Say "   Cold-Library no-op detected (Unity compiled but didn't build)." 'Yellow'
        Say "   Library is now warm - retrying the build once..." 'Yellow'
        $result = Invoke-UnityBuild
    }

    if ($result -ne 'SUCCEEDED') {
        Say "X  WebGL build did NOT succeed (status: $result)." 'Red'
        Say "   See the build log for details:" 'Yellow'
        Say "     $LogFile" 'Yellow'
        Say "   Nothing was published - your preview is unchanged." 'Yellow'
        exit 1
    }

    $sizeLine = (Select-String -Path $LogFile -Pattern '\[WebGLBuilder\] WebGL build SUCCEEDED.*' | Select-Object -Last 1).Line
    Say "   Build SUCCEEDED. $sizeLine" 'Green'
}

# --- 2. Sanity-check the export exists before publishing ---
if (-not (Test-Path (Join-Path $ExportDir 'index.html'))) {
    Say "X  No WebGL export at $ExportDir even though the build reported success." 'Red'
    exit 1
}

# --- 3. Hand off to the publish step ---
Say "2. Publishing the fresh build to the portal preview..." 'Green'
$publishArgs = @('-ExecutionPolicy', 'Bypass', '-File', $Publish)
if ($NoPush) { $publishArgs += '-NoPush' }
& powershell @publishArgs
if ($LASTEXITCODE -ne 0) {
    Say "X  publish-game.ps1 failed (exit $LASTEXITCODE)." 'Red'
    exit $LASTEXITCODE
}

Say "==================================================" 'DarkCyan'
if ($NoPush) {
    Say " Rebuilt + committed locally (-NoPush). Push when ready." 'Green'
} else {
    Say " Rebuilt AND published! Preview refreshes in ~30s." 'Green'
    Say " Tell Warren to hit refresh and play. GG" 'White'
}
Say "==================================================" 'DarkCyan'
