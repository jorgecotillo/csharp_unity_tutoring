<#
  start-build-daemon.ps1 - launch the persistent Goblin Siege WebGL build daemon.

  Keeps ONE headless Unity editor resident (GS_BUILD_DAEMON=1, batchmode, NO -quit)
  so the studio's auto-build can trigger repeated WebGL builds WITHOUT paying the
  ~1-2 min editor launch + project import each time. The daemon watches
  mvp_v1\Builds\daemon\request.txt and writes result.txt + a ~1/sec heartbeat.txt.

  The studio (warren-studio/lib/build.js) automatically uses the daemon whenever a
  FRESH heartbeat exists, and falls back to a normal one-shot build otherwise - so
  this is purely opt-in speed. Stop it with stop-build-daemon.ps1 (or just let it
  be; it idles cheaply).

  TRADE-OFF: the daemon holds the Unity project lock for THIS worktree, so you
  can't also open this project in the Unity Editor interactively while it runs.
  (Your separate main-checkout editor is a different project and is unaffected.)

  USAGE (from the repo root):
      powershell -ExecutionPolicy Bypass -File .\start-build-daemon.ps1
#>

[CmdletBinding()]
param(
    [string]$UnityExe = "C:\Program Files\Unity\Hub\Editor\6000.3.15f1\Editor\Unity.exe"
)

$ErrorActionPreference = 'Stop'
function Say([string]$m, [string]$c = 'Cyan') { Write-Host $m -ForegroundColor $c }

$RepoRoot   = Split-Path -Parent $MyInvocation.MyCommand.Path
$Project    = Join-Path $RepoRoot 'mvp_v1'
$DaemonDir  = Join-Path $Project 'Builds\daemon'
$Heartbeat  = Join-Path $DaemonDir 'heartbeat.txt'
$Log        = Join-Path $DaemonDir 'daemon.log'
$PidFile    = Join-Path $DaemonDir 'daemon.pid'

if (-not (Test-Path $UnityExe)) {
    Say "X  Unity not found at: $UnityExe" 'Red'
    exit 1
}

New-Item -ItemType Directory -Path $DaemonDir -Force | Out-Null

# Refuse to start a second daemon / collide with a one-shot build on the lock.
$existing = Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -match '-batchmode' -and $_.CommandLine -match 'mvp_v1' }
if ($existing) {
    Say "!  A batchmode Unity is already running (pid $($existing.ProcessId)) on this project." 'Yellow'
    Say "   If that's the daemon, it's already up. If it's a one-shot build, wait for it to finish." 'Yellow'
    exit 0
}

# Clear a stale heartbeat so the studio doesn't think the daemon is alive before it is.
if (Test-Path $Heartbeat) { Remove-Item $Heartbeat -Force }

Say "Launching persistent WebGL build daemon (headless Unity)..." 'Green'
$env:GS_BUILD_DAEMON = '1'
$proc = Start-Process -FilePath $UnityExe `
    -ArgumentList '-batchmode','-nographics','-projectPath',$Project,'-logFile',$Log `
    -PassThru
Set-Content -Path $PidFile -Value $proc.Id -Encoding ascii
Say "   Daemon Unity PID $($proc.Id). Waiting for it to come alive (first heartbeat)..." 'Cyan'

# Wait up to ~2 min for the first heartbeat (Unity launch + import + compile).
$alive = $false
for ($i = 0; $i -lt 24; $i++) {
    Start-Sleep -Seconds 5
    if (Test-Path $Heartbeat) {
        $age = (Get-Date).ToUniversalTime() - (Get-Item $Heartbeat).LastWriteTimeUtc
        if ($age.TotalSeconds -lt 15) { $alive = $true; break }
    }
    if (-not (Get-Process -Id $proc.Id -ErrorAction SilentlyContinue)) {
        Say "X  Daemon Unity exited during startup. See log: $Log" 'Red'
        exit 1
    }
}

if ($alive) {
    Say "==============================================" 'DarkCyan'
    Say " Build daemon is ALIVE (PID $($proc.Id))." 'Green'
    Say " The studio will now use it automatically for faster builds." 'White'
    Say " Stop it with: powershell -ExecutionPolicy Bypass -File .\stop-build-daemon.ps1" 'Gray'
    Say "==============================================" 'DarkCyan'
} else {
    Say "!  No heartbeat yet after ~2 min. It may still be importing - check:" 'Yellow'
    Say "     $Heartbeat" 'Yellow'
    Say "     $Log" 'Yellow'
}
