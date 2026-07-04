<#
  publish-game.ps1  —  Ship a new Goblin Siege build to the portal preview.

  WHAT IT DOES (one command):
    1. Takes your fresh Unity WebGL export (mvp_v1\Builds\WebGL by default)
    2. Copies it into docs\game  (the build the portal actually serves)
    3. Stamps docs\game\build-info.json with the current time so the portal
       KNOWS the game changed and auto-reloads the preview
    4. Commits it and pushes to main

  HOW TO USE:
    1. In Unity:  File -> Build Settings -> WebGL -> Build
       (set the output folder to  mvp_v1\Builds\WebGL )
    2. Then run this from the repo root:
         powershell -ExecutionPolicy Bypass -File .\publish-game.ps1
    3. Wait ~30 seconds. The portal preview refreshes itself. Done. 🎮

  OPTIONS:
    -SourceDir <path>   Where your Unity WebGL export is
                        (default: mvp_v1\Builds\WebGL)
    -NoPush             Copy + commit locally but DON'T push to main
                        (use this if you want to review before shipping)
#>

[CmdletBinding()]
param(
    [string]$SourceDir = "mvp_v1\Builds\WebGL",
    [switch]$NoPush
)

$ErrorActionPreference = 'Stop'

function Say([string]$msg, [string]$color = 'Cyan') { Write-Host $msg -ForegroundColor $color }

# --- Resolve repo root from this script's location, so it works from anywhere ---
$RepoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $RepoRoot

$Source = Join-Path $RepoRoot $SourceDir
$Target = Join-Path $RepoRoot 'docs\game'

Say "==============================================" 'DarkCyan'
Say " Warren's Game Studio  -  Publish Game" 'White'
Say "==============================================" 'DarkCyan'

# --- 1. Validate the Unity export exists ---
$srcIndex = Join-Path $Source 'index.html'
if (-not (Test-Path $srcIndex)) {
    Say "X  No Unity build found at: $Source" 'Red'
    Say "   Build it first in Unity:" 'Yellow'
    Say "     File -> Build Settings -> WebGL -> Build" 'Yellow'
    Say "     (output folder: $SourceDir )" 'Yellow'
    exit 1
}
Say "1. Found fresh Unity build at $SourceDir" 'Green'

# --- 2. Copy build into docs\game (replace Build/, TemplateData/, index.html) ---
if (-not (Test-Path $Target)) { New-Item -ItemType Directory -Path $Target | Out-Null }

foreach ($item in @('Build', 'TemplateData')) {
    $dst = Join-Path $Target $item
    if (Test-Path $dst) { Remove-Item $dst -Recurse -Force }
    $src = Join-Path $Source $item
    if (Test-Path $src) { Copy-Item $src $dst -Recurse -Force }
}
Copy-Item $srcIndex (Join-Path $Target 'index.html') -Force
Say "2. Copied build into docs\game" 'Green'

# --- 3. Stamp build-info.json so the portal detects the change & auto-reloads ---
$builtAt = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
$shortSha = (git rev-parse --short HEAD).Trim()
$info = [ordered]@{
    builtAt   = $builtAt
    sourceSha = $shortSha
    publisher = 'publish-game.ps1'
}
$info | ConvertTo-Json | Set-Content -Path (Join-Path $Target 'build-info.json') -Encoding UTF8
Say "3. Stamped build-info.json ($builtAt)" 'Green'

# --- 4. Commit ---
git add -- docs/game | Out-Null
$staged = git diff --cached --name-only
if (-not $staged) {
    Say "Nothing changed. (Did you actually rebuild in Unity?)" 'Yellow'
    exit 0
}

$commitMsg = @"
Publish updated Goblin Siege WebGL build to docs/game

Refreshed the browser preview from the latest Unity WebGL export.

Co-authored-by: Copilot App <223556219+Copilot@users.noreply.github.com>
"@
git commit -m $commitMsg | Out-Null
Say "4. Committed the new build" 'Green'

# --- 5. Push to main (unless -NoPush) ---
if ($NoPush) {
    Say "Skipping push (-NoPush). Run 'git push origin HEAD:main' when ready." 'Yellow'
    exit 0
}
Say "5. Pushing to main..." 'Cyan'
git push origin HEAD:main
Say "==============================================" 'DarkCyan'
Say " Done! The portal preview will refresh in ~30s." 'Green'
Say " Tell Warren to hit refresh and play. 🎮" 'White'
Say "==============================================" 'DarkCyan'
