# Warren's Game Studio - ONE-CLICK BRING-UP.
# Ensures the whole studio is running: the unified watchdog (which in turn keeps
# the Node server, the dev tunnel, and the build daemon alive). Safe to run
# anytime - it is IDEMPOTENT: if something is already up it leaves it alone and
# just reports status. Use it after a machine reboot, or as a pre-class "is
# everything green?" check.
#
#   powershell -ExecutionPolicy Bypass -File .\warren-studio\start-studio.ps1
#   powershell -ExecutionPolicy Bypass -File .\warren-studio\start-studio.ps1 -WaitDaemon
#
# -WaitDaemon waits up to ~3 min for the build daemon's first heartbeat (Unity
# launch + project import) so you can confirm fast builds are ready before class.

param(
  [string]$Base = "C:\Users\jcotillo\.copilot\repos\copilot-worktrees\csharp_unity_tutoring\jcotillo-microsoft-supreme-journey\warren-studio",
  [int]$Port = 8080,
  [string]$PublicHealthUrl = "https://zvplxt8c-8080.usw2.devtunnels.ms/healthz",
  [switch]$WaitDaemon
)

$ErrorActionPreference = "SilentlyContinue"
Set-Location $Base

$RepoRoot       = Split-Path -Parent $Base
$watchdogScript = Join-Path $Base "watchdog.ps1"
$watchdogPid    = Join-Path $Base "watchdog.pid"
$watchdogBeat   = Join-Path $Base "watchdog.heartbeat"
$daemonDir      = Join-Path $RepoRoot "mvp_v1\Builds\daemon"
$daemonDisabled = Join-Path $daemonDir "daemon.disabled"
$daemonPidFile  = Join-Path $daemonDir "daemon.pid"
$daemonBeat     = Join-Path $daemonDir "heartbeat.txt"

function Say([string]$m, [string]$c = "Gray") { Write-Host $m -ForegroundColor $c }

function Test-LocalUp {
  try { return ((Invoke-WebRequest -Uri "http://127.0.0.1:$Port/healthz" -UseBasicParsing -TimeoutSec 5).StatusCode -eq 200) }
  catch { return $false }
}
function Test-PublicUp {
  try { return ((Invoke-WebRequest -Uri $PublicHealthUrl -UseBasicParsing -TimeoutSec 10).StatusCode -eq 200) }
  catch { return $false }
}
function Test-WatchdogAlive {
  # Alive when the pid is a running process AND the heartbeat is fresh (<30s).
  if (-not (Test-Path $watchdogPid)) { return $false }
  $wp = (Get-Content $watchdogPid -Raw).Trim()
  if ($wp -notmatch '^\d+$') { return $false }
  if (-not (Get-Process -Id ([int]$wp) -ErrorAction SilentlyContinue)) { return $false }
  if (-not (Test-Path $watchdogBeat)) { return $false }
  $age = ((Get-Date) - (Get-Item $watchdogBeat).LastWriteTime).TotalSeconds
  return ($age -lt 30)
}
function Test-DaemonAlive {
  if (-not (Test-Path $daemonPidFile)) { return $false }
  $dp = (Get-Content $daemonPidFile -Raw).Trim()
  if ($dp -notmatch '^\d+$') { return $false }
  $pr = Get-Process -Id ([int]$dp) -ErrorAction SilentlyContinue
  return ($pr -and $pr.ProcessName -eq 'Unity')
}

Say "=== Warren's Game Studio - bring-up ===" "Cyan"

# 1) WATCHDOG. It is the supervisor that (re)starts server + tunnel + daemon, so
# getting it running is 90% of the job. Idempotent: never launch a second one.
if (Test-WatchdogAlive) {
  $wp = (Get-Content $watchdogPid -Raw).Trim()
  Say "watchdog : already running (PID $wp)" "Green"
} else {
  Say "watchdog : starting..." "Yellow"
  $p = Start-Process -FilePath "powershell" `
        -ArgumentList "-ExecutionPolicy", "Bypass", "-NoProfile", "-File", $watchdogScript `
        -WorkingDirectory $Base -WindowStyle Hidden -PassThru
  if ($p) {
    Set-Content -Path $watchdogPid -Value $p.Id
    Say "watchdog : started (PID $($p.Id))" "Green"
  } else {
    Say "watchdog : FAILED to start" "Red"
  }
}

# 2) Enable the build daemon (full studio = fast builds). Running this script is an
# explicit "bring everything up", so clear any deliberate-stop sentinel; the
# watchdog will then (re)start the daemon on its next cycle unless a Unity Editor
# is already holding the project.
if (Test-Path $daemonDisabled) {
  Remove-Item $daemonDisabled -Force -ErrorAction SilentlyContinue
  Say "daemon   : re-enabled (auto-respawn on)" "Green"
}

# 3) Wait for the SERVER (local health). The watchdog brings it up within ~15s.
Say "server   : waiting for local health..." "Gray"
$srvUp = $false
for ($i = 0; $i -lt 20; $i++) {
  if (Test-LocalUp) { $srvUp = $true; break }
  Start-Sleep -Seconds 3
}
if ($srvUp) { Say "server   : UP (http://127.0.0.1:$Port)" "Green" }
else        { Say "server   : NOT up yet after ~60s - check server.log.err" "Red" }

# 4) Wait for the TUNNEL (public url Warren connects to).
Say "tunnel   : waiting for public url..." "Gray"
$pubUp = $false
for ($i = 0; $i -lt 20; $i++) {
  if (Test-PublicUp) { $pubUp = $true; break }
  Start-Sleep -Seconds 3
}
if ($pubUp) { Say "tunnel   : UP ($PublicHealthUrl)" "Green" }
else        { Say "tunnel   : public url NOT responding yet - watchdog will keep retrying" "Yellow" }

# 5) DAEMON status (and optional wait).
if (Test-DaemonAlive) {
  Say "daemon   : ALIVE (fast ~6 min builds ready)" "Green"
} elseif ($WaitDaemon) {
  Say "daemon   : waiting for first heartbeat (Unity launch + import, up to ~3 min)..." "Gray"
  $dUp = $false
  for ($i = 0; $i -lt 36; $i++) {
    Start-Sleep -Seconds 5
    if ((Test-Path $daemonBeat) -and (((Get-Date).ToUniversalTime() - (Get-Item $daemonBeat).LastWriteTimeUtc).TotalSeconds -lt 15)) { $dUp = $true; break }
  }
  if ($dUp) { Say "daemon   : ALIVE (fast ~6 min builds ready)" "Green" }
  else      { Say "daemon   : no heartbeat yet - it may still be importing (check daemon.log)" "Yellow" }
} else {
  Say "daemon   : not up yet - the watchdog is starting it (first build may be cold ~8 min)" "Yellow"
  Say "           run with -WaitDaemon to wait for it, or start-build-daemon.ps1 directly" "Gray"
}

Say "======================================" "Cyan"
$overall = if ($srvUp -and $pubUp) { "Studio is UP. Warren can connect." } else { "Studio came up PARTIALLY - see notes above." }
Say $overall $(if ($srvUp -and $pubUp) { "Green" } else { "Yellow" })
Say "Warren's URL:  $($PublicHealthUrl -replace '/healthz$','')" "White"
