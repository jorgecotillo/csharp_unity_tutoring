param(
  [string]$Base = "C:\Users\jcotillo\.copilot\repos\copilot-worktrees\csharp_unity_tutoring\jcotillo-microsoft-supreme-journey\warren-studio",
  [int]$Port = 8080,
  [int]$IntervalSec = 10,
  [string]$TunnelId = "warren-studio.usw2",
  [string]$PublicHealthUrl = "https://zvplxt8c-8080.usw2.devtunnels.ms/healthz"
)

# Warren's Game Studio - UNIFIED watchdog.
# Supervises THREE things so the studio survives crashes during a live class:
#   1. Node server  - relaunched if http://127.0.0.1:PORT/healthz stops answering.
#   2. Dev tunnel   - the PUBLIC url Warren connects to. Relaunched if the host
#                     process dies; restarted if the tunnel is wedged (local server
#                     healthy but the public url fails 3 checks in a row).
#   3. Build daemon - the resident headless Unity. Respawned if it dies, UNLESS a
#                     daemon.disabled sentinel exists (a deliberate stop via
#                     stop-build-daemon.ps1) OR any Unity.exe is already running
#                     (so we never collide with a cold fallback build on the lock).
# Runs forever until stop-studio.ps1 kills it. ASCII-only (PS 5.1 parser is picky
# about UTF-8 non-ASCII in .ps1 files).

$ErrorActionPreference = "SilentlyContinue"
Set-Location $Base

$RepoRoot      = Split-Path -Parent $Base
$serverPidFile = Join-Path $Base "server.pid"
$logFile       = Join-Path $Base "server.log"
$errFile       = "$logFile.err"
$heartbeat     = Join-Path $Base "watchdog.heartbeat"

$tunnelOut     = Join-Path $Base "devtunnel.log.out"
$tunnelErr     = Join-Path $Base "devtunnel.log.err"
$tunnelPidFile = Join-Path $Base "devtunnel.pid"

$daemonDir      = Join-Path $RepoRoot "mvp_v1\Builds\daemon"
$daemonPidFile  = Join-Path $daemonDir "daemon.pid"
$daemonDisabled = Join-Path $daemonDir "daemon.disabled"
$startDaemon    = Join-Path $RepoRoot "start-build-daemon.ps1"

# Resolve the devtunnel exe once (PATH first, WinGet fallback) so relaunch is robust.
$devtunnelExe = (Get-Command devtunnel -ErrorAction SilentlyContinue).Source
if (-not $devtunnelExe) {
  $wg = "C:\Users\jcotillo\AppData\Local\Microsoft\WinGet\Packages\Microsoft.devtunnel_Microsoft.Winget.Source_8wekyb3d8bbwe\devtunnel.exe"
  if (Test-Path $wg) { $devtunnelExe = $wg }
}

$tunnelFail = 0

function Test-LocalUp {
  try { return ((Invoke-WebRequest -Uri "http://127.0.0.1:$Port/healthz" -UseBasicParsing -TimeoutSec 5).StatusCode -eq 200) }
  catch { return $false }
}
function Test-PublicUp {
  try { return ((Invoke-WebRequest -Uri $PublicHealthUrl -UseBasicParsing -TimeoutSec 10).StatusCode -eq 200) }
  catch { return $false }
}
function Get-TunnelProc {
  Get-CimInstance Win32_Process -Filter "Name='devtunnel.exe'" -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -match 'host' -and $_.CommandLine -match [regex]::Escape($TunnelId) } |
    Select-Object -First 1
}
function Start-Tunnel {
  if (-not $devtunnelExe) { return }
  $p = Start-Process -FilePath $devtunnelExe -ArgumentList "host", $TunnelId `
        -RedirectStandardOutput $tunnelOut -RedirectStandardError $tunnelErr `
        -WindowStyle Hidden -PassThru
  if ($p) { Set-Content -Path $tunnelPidFile -Value $p.Id }
}
function Test-DaemonAlive {
  if (-not (Test-Path $daemonPidFile)) { return $false }
  $dp = (Get-Content $daemonPidFile -Raw).Trim()
  if ($dp -notmatch '^\d+$') { return $false }
  $pr = Get-Process -Id ([int]$dp) -ErrorAction SilentlyContinue
  return ($pr -and $pr.ProcessName -eq 'Unity')
}
function Test-AnyUnity {
  $u = Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" -ErrorAction SilentlyContinue | Select-Object -First 1
  return [bool]$u
}

while ($true) {
 try {
  Set-Content -Path $heartbeat -Value (Get-Date -Format o)

  # 1) SERVER - relaunch detached if the local health endpoint is down.
  $localUp = Test-LocalUp
  if (-not $localUp) {
    $p = Start-Process -FilePath "node" `
                       -ArgumentList "server.js" `
                       -WorkingDirectory $Base `
                       -RedirectStandardOutput $logFile `
                       -RedirectStandardError $errFile `
                       -WindowStyle Hidden `
                       -PassThru
    if ($p) { Set-Content -Path $serverPidFile -Value $p.Id }
    Start-Sleep -Seconds 8
    $localUp = Test-LocalUp
  }

  # 2) TUNNEL - the public url. Relaunch if the process died; restart if wedged.
  $tp = Get-TunnelProc
  if (-not $tp) {
    Start-Tunnel
    $tunnelFail = 0
    Start-Sleep -Seconds 5
  } elseif ($localUp) {
    # Only blame the tunnel when the LOCAL server is healthy (else it's the
    # server's fault and section 1 handles it). Require 3 consecutive public
    # failures (~30s) so a transient internet blip never causes needless churn.
    if (Test-PublicUp) {
      $tunnelFail = 0
    } else {
      $tunnelFail++
      if ($tunnelFail -ge 3) {
        Stop-Process -Id $tp.ProcessId -Force
        Start-Sleep -Seconds 2
        Start-Tunnel
        $tunnelFail = 0
        Start-Sleep -Seconds 5
      }
    }
  }

  # 3) BUILD DAEMON - keep the fast-build daemon alive for the class. Respawn only
  # when it is truly dead AND not deliberately disabled AND no Unity.exe is running
  # (a running Unity is either the daemon booting or a cold fallback build holding
  # the project lock; launching a second Unity would collide and fail).
  if (-not (Test-Path $daemonDisabled)) {
    if (-not (Test-DaemonAlive)) {
      if (-not (Test-AnyUnity)) {
        Start-Process -FilePath "powershell" `
                      -ArgumentList "-ExecutionPolicy", "Bypass", "-NoProfile", "-File", $startDaemon `
                      -WindowStyle Hidden
      }
    }
  }
 } catch {
  # A single cycle must NEVER kill the supervisor. Swallow any transient error
  # (a flaky CIM query, a locked file, a launch hiccup) and try again next tick.
 }

  Start-Sleep -Seconds $IntervalSec
}
