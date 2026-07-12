<#
  stop-build-daemon.ps1 - stop the persistent Goblin Siege WebGL build daemon.

  Kills the resident headless Unity started by start-build-daemon.ps1 and clears
  its heartbeat so the studio immediately falls back to normal one-shot builds.

  USAGE (from the repo root):
      powershell -ExecutionPolicy Bypass -File .\stop-build-daemon.ps1
#>

[CmdletBinding()]
param()

function Say([string]$m, [string]$c = 'Cyan') { Write-Host $m -ForegroundColor $c }

$RepoRoot  = Split-Path -Parent $MyInvocation.MyCommand.Path
$DaemonDir = Join-Path $RepoRoot 'mvp_v1\Builds\daemon'
$Heartbeat = Join-Path $DaemonDir 'heartbeat.txt'
$PidFile   = Join-Path $DaemonDir 'daemon.pid'

$killed = $false

if (Test-Path $PidFile) {
    $daemonPid = (Get-Content $PidFile -Raw).Trim()
    if ($daemonPid -match '^\d+$') {
        $p = Get-Process -Id ([int]$daemonPid) -ErrorAction SilentlyContinue
        # Only kill it if it's actually a Unity process (avoid PID reuse mistakes).
        if ($p -and $p.ProcessName -eq 'Unity') {
            Stop-Process -Id ([int]$daemonPid) -Force -ErrorAction SilentlyContinue
            Say "Stopped build daemon (PID $daemonPid)." 'Green'
            $killed = $true
        }
    }
    Remove-Item $PidFile -Force -ErrorAction SilentlyContinue
}

if (-not $killed) {
    Say "No daemon PID found (or process already gone)." 'Yellow'
}

# Clear the heartbeat so the studio stops treating the daemon as alive.
if (Test-Path $Heartbeat) { Remove-Item $Heartbeat -Force -ErrorAction SilentlyContinue }
Say "Heartbeat cleared. The studio will fall back to one-shot builds." 'Gray'
