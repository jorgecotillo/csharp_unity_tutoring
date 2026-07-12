# Warren's Game Studio - KILL SWITCH.
# Takes the studio offline in the RIGHT order: stop the watchdog FIRST (so it
# does not immediately respawn what we are about to kill), then the Node server.
# The public dev tunnel and the build daemon are LEFT RUNNING by default (so a
# quick server bounce does not drop Warren's url or throw away the warm daemon).
# Use the switches to also stop them.
#
#   powershell -ExecutionPolicy Bypass -File .\warren-studio\stop-studio.ps1
#   powershell -ExecutionPolicy Bypass -File .\warren-studio\stop-studio.ps1 -Tunnel
#   powershell -ExecutionPolicy Bypass -File .\warren-studio\stop-studio.ps1 -Tunnel -Daemon
#   powershell -ExecutionPolicy Bypass -File .\warren-studio\stop-studio.ps1 -All

param(
  [string]$Base = "C:\Users\jcotillo\.copilot\repos\copilot-worktrees\csharp_unity_tutoring\jcotillo-microsoft-supreme-journey\warren-studio",
  [int]$Port = 8080,
  [switch]$Tunnel,
  [switch]$Daemon,
  [switch]$All
)

$ErrorActionPreference = "SilentlyContinue"
if ($All) { $Tunnel = $true; $Daemon = $true }

$RepoRoot   = Split-Path -Parent $Base
$stopDaemon = Join-Path $RepoRoot "stop-build-daemon.ps1"

function Stop-ByPidFile($file, $label) {
  if (Test-Path $file) {
    $procId = (Get-Content $file -Raw).Trim()
    if ($procId -match '^\d+$') {
      $proc = Get-Process -Id ([int]$procId) -ErrorAction SilentlyContinue
      if ($proc) {
        Stop-Process -Id ([int]$procId) -Force
        Write-Output "$label : stopped (PID $procId)"
      } else {
        Write-Output "$label : not running"
      }
    }
    Remove-Item $file -ErrorAction SilentlyContinue
  } else {
    Write-Output "$label : no pid file"
  }
}

# 1) Watchdog FIRST - otherwise it relaunches the server/tunnel the instant we
# kill them. (It also stops trying to respawn the daemon.)
Stop-ByPidFile (Join-Path $Base "watchdog.pid") "watchdog"

# 2) Server. Kill by pid file, then sweep any lingering listener on the port as a
# fallback (a hand-started server may not have a matching pid file).
Stop-ByPidFile (Join-Path $Base "server.pid") "server  "
$line = netstat -ano | Select-String "0.0.0.0:$Port.*LISTENING" | Select-Object -First 1
if ($line) {
  $lp = ($line.Line.Trim() -split '\s+')[-1]
  if ($lp -match '^\d+$') {
    Stop-Process -Id ([int]$lp) -Force
    Write-Output "server   : swept leftover listener (PID $lp)"
  }
}

# 3) Tunnel (opt-in). The unified watchdog writes devtunnel.pid; also match the
# host process by command line as a fallback.
if ($Tunnel) {
  Stop-ByPidFile (Join-Path $Base "devtunnel.pid") "tunnel  "
  $tp = Get-CimInstance Win32_Process -Filter "Name='devtunnel.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -match 'host' } | Select-Object -First 1
  if ($tp) {
    Stop-Process -Id $tp.ProcessId -Force
    Write-Output "tunnel   : swept host process (PID $($tp.ProcessId))"
  }
} else {
  Write-Output "tunnel   : left running (use -Tunnel to also stop it)"
}

# 4) Build daemon (opt-in). Delegate to stop-build-daemon.ps1 so the daemon.disabled
# sentinel is written (a restarted watchdog will then respect the stop).
if ($Daemon) {
  if (Test-Path $stopDaemon) {
    & powershell -ExecutionPolicy Bypass -NoProfile -File $stopDaemon | Out-Null
    Write-Output "daemon   : stopped (auto-respawn disabled)"
  } else {
    Write-Output "daemon   : stop-build-daemon.ps1 not found"
  }
} else {
  Write-Output "daemon   : left running (use -Daemon to also stop it)"
}

Write-Output "Done."
