<#
.SYNOPSIS
  Provision Warren's Game Studio on Azure App Service for Containers (Linux B1).

.DESCRIPTION
  Idempotent deploy script. Creates (or reuses):
    - a resource group
    - a Linux B1 App Service plan
    - a Web App running the public GHCR container image
  ...then applies every app setting the studio needs (persistent /home, port 8080,
  the GitHub App write credential, the git pipeline knobs, the GitHub OAuth gate, etc.).

  This script does NOT bake any secret into the image. The GitHub App private key is
  uploaded separately to /home/secrets/warren-studio.pem (see DEPLOY.md, step E) and
  referenced here only by PATH. SESSION_SECRET is generated for you if you don't pass one.

  Re-running is safe: existing resources are detected and only app settings are re-applied.

.EXAMPLE
  ./azure-deploy.ps1 `
     -GitHubOAuthClientId Iv1.abc123 `
     -GitHubOAuthClientSecret <secret> `
     -GitHubOAuthAllowedUsers "jorgecotillo,warrens-login"

.NOTES
  Requires: az CLI (>= 2.50), logged in as the Gmail/personal account (`az login` — that
  tenant requires MFA in the browser). The script selects the personal subscription for you
  (-SubscriptionId, default 170f5740-...). PowerShell 7+ recommended.
#>

[CmdletBinding()]
param(
    # --- Azure placement ---
    # Personal Pay-As-You-Go subscription (jorge.cotillo@gmail.com). The script selects it
    # for you in preflight, so you don't have to remember `az account set`.
    [string] $SubscriptionId = "170f5740-3e45-4a68-a9f8-435d6fc3c166",
    [string] $ResourceGroup  = "mypersonalrg",
    [string] $Location       = "eastus",
    [string] $PlanName       = "warren-studio-plan",
    [string] $AppName        = "warren-studio",          # must be globally unique -> <AppName>.azurewebsites.net
    [string] $Sku            = "B1",

    # --- Container image (public GHCR, no registry creds needed) ---
    [string] $Image         = "ghcr.io/jorgecotillo/warren-studio:latest",

    # --- GitHub OAuth login (the ONLY front door + per-user push identity). ---
    # Register a classic OAuth App at https://github.com/settings/developers and pass the
    # client id + secret here. ALLOWED users is a comma list of GitHub logins (e.g. you +
    # Warren). Closed-by-default: nobody can sign in unless their login is on this list.
    [string] $GitHubOAuthClientId     = "",
    [string] $GitHubOAuthClientSecret = "",
    [string] $GitHubOAuthAllowedUsers = "",             # e.g. "jorgecotillo,warrens-login"
    [string] $GitHubOAuthCallbackUrl  = "",             # optional; auto-derived from $AppName if blank

    # --- GitHub App (LEGACY optional push fallback when no OAuth pusher is present). ---
    [string] $GitHubAppId             = "",
    [string] $GitHubAppInstallationId = "",
    # The .pem is uploaded to /home/secrets/ via Kudu (DEPLOY.md step E); we only point at it.
    [string] $GitHubAppPrivateKeyPath = "/home/secrets/warren-studio.pem",

    # --- Session secret (auto-generated if omitted) ---
    [string] $SessionSecret = "",

    # --- Repo / git pipeline (defaults match this repo) ---
    [string] $RepoSlug   = "jorgecotillo/csharp_unity_tutoring",
    [string] $CloneUrl   = "https://github.com/jorgecotillo/csharp_unity_tutoring.git",
    [string] $TargetBranch = "main",
    [string] $ChatModel  = "claude-sonnet-4.6",
    [int]    $ChatRatePerHour = 60
)

$ErrorActionPreference = "Stop"

function Say($msg)  { Write-Host "==> $msg" -ForegroundColor Cyan }
function Warn($msg) { Write-Host "!!  $msg" -ForegroundColor Yellow }

# ---------------------------------------------------------------------------
# 0. Preflight
# ---------------------------------------------------------------------------
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "Azure CLI (az) not found on PATH. Install it: https://aka.ms/azcli"
}

if ($SubscriptionId) {
    Say "Selecting subscription $SubscriptionId"
    az account set --subscription $SubscriptionId 2>$null
}

$acct = az account show 2>$null | ConvertFrom-Json
if (-not $acct) {
    throw "Not logged in to Azure. Run 'az login' first (the Gmail tenant needs MFA in the browser), then re-run this script."
}
if ($SubscriptionId -and $acct.id -ne $SubscriptionId) {
    throw "Active subscription is '$($acct.id)' but expected '$SubscriptionId'. Run 'az login' for the right account and retry."
}
Say "Subscription: $($acct.name)  ($($acct.id))  user=$($acct.user.name)"

# Warn if no GitHub OAuth allow-list is configured (closed-by-default = nobody can sign in).
if (-not $GitHubOAuthAllowedUsers) {
    Warn "No GITHUB_OAUTH_ALLOWED_USERS set. The studio is closed-by-default: nobody can sign in until you add GitHub logins (e.g. -GitHubOAuthAllowedUsers 'jorgecotillo,warrens-login')."
}

# Generate a session secret if none supplied
if (-not $SessionSecret) {
    $bytes = New-Object 'System.Byte[]' 48
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    $SessionSecret = [System.BitConverter]::ToString($bytes).Replace("-", "").ToLower()
    Say "Generated a random SESSION_SECRET (48 bytes)."
}

if (-not $GitHubAppId -or -not $GitHubAppInstallationId) {
    Warn "GitHub App not fully configured (need -GitHubAppId and -GitHubAppInstallationId)."
    Warn "Deploy will continue, but git PUSH stays disabled until those are set. See DEPLOY.md step A."
}

# ---------------------------------------------------------------------------
# 1. Resource group
# ---------------------------------------------------------------------------
Say "Resource group '$ResourceGroup' in $Location"
az group create --name $ResourceGroup --location $Location --output none

# ---------------------------------------------------------------------------
# 2. App Service plan (Linux B1 — always-on, ~$13/mo)
# ---------------------------------------------------------------------------
$existingPlan = az appservice plan show --name $PlanName --resource-group $ResourceGroup 2>$null | ConvertFrom-Json
if ($existingPlan) {
    if (-not $existingPlan.reserved) {
        throw "Plan '$PlanName' is a WINDOWS plan (reserved=false). A container web app needs a LINUX plan. Pass -PlanName <a Linux plan> or let the script create a new one."
    }
    $planSku = $existingPlan.sku.name
    Say "Plan '$PlanName' already exists — reusing (Linux, SKU=$planSku, no extra plan cost)."
} else {
    Say "Creating Linux App Service plan '$PlanName' ($Sku)"
    az appservice plan create `
        --name $PlanName `
        --resource-group $ResourceGroup `
        --is-linux `
        --sku $Sku `
        --location $Location `
        --output none
}

# ---------------------------------------------------------------------------
# 3. Web App (public GHCR container)
# ---------------------------------------------------------------------------
$appExists = az webapp show --name $AppName --resource-group $ResourceGroup 2>$null
if ($appExists) {
    Say "Web app '$AppName' already exists — updating container image."
    az webapp config container set `
        --name $AppName `
        --resource-group $ResourceGroup `
        --container-image-name $Image `
        --output none
} else {
    Say "Creating web app '$AppName' from $Image"
    az webapp create `
        --resource-group $ResourceGroup `
        --plan $PlanName `
        --name $AppName `
        --deployment-container-image-name $Image `
        --output none
}

# ---------------------------------------------------------------------------
# 4. App settings (this is the important part)
# ---------------------------------------------------------------------------
Say "Applying app settings"

# Build the settings table. Order doesn't matter to Azure.
$settings = [ordered]@{
    # --- platform / container ---
    "WEBSITES_ENABLE_APP_SERVICE_STORAGE" = "true"   # makes /home persistent (repo + Copilot auth live here)
    "WEBSITES_PORT"                       = "8080"
    "PORT"                                = "8080"
    "NODE_ENV"                            = "production"
    "HOME"                                = "/home"   # so Copilot auth lands in persistent /home/.copilot

    # --- session / gate ---
    "SESSION_SECRET"  = $SessionSecret
    "SESSION_IDLE_MIN" = "120"

    # --- chat ---
    "CHAT_MODEL"         = $ChatModel
    "CHAT_RATE_PER_HOUR" = "$ChatRatePerHour"

    # --- repo + game build location ---
    "REPO_ROOT"      = "/home/repo"
    "GAME_BUILD_DIR" = "docs/game"

    # --- git pipeline ---
    "GIT_PUSH_ENABLED"      = "on"
    "GIT_PULL_ENABLED"      = "on"
    "GAME_PULL_INTERVAL_SEC" = "30"
    "GIT_TARGET_BRANCH"     = $TargetBranch
    "GIT_REMOTE"            = "origin"
    "GIT_REPO_SLUG"         = $RepoSlug
    "GIT_CLONE_URL"         = $CloneUrl
    "GIT_AUTHOR_NAME"       = "Warren Studio Bot"
    "GIT_AUTHOR_EMAIL"      = "warren-studio-bot@users.noreply.github.com"
}

# GitHub OAuth login (PRIMARY front door). Only set when client id+secret provided.
if ($GitHubOAuthClientId -and $GitHubOAuthClientSecret) {
    $settings["GITHUB_OAUTH_CLIENT_ID"]     = $GitHubOAuthClientId
    $settings["GITHUB_OAUTH_CLIENT_SECRET"] = $GitHubOAuthClientSecret
    if ($GitHubOAuthAllowedUsers) { $settings["GITHUB_OAUTH_ALLOWED_USERS"] = $GitHubOAuthAllowedUsers }
    # Pin the callback so it exactly matches what you register in the OAuth App.
    $cb = if ($GitHubOAuthCallbackUrl) { $GitHubOAuthCallbackUrl } else { "https://$AppName.azurewebsites.net/auth/github/callback" }
    $settings["GITHUB_OAUTH_CALLBACK_URL"] = $cb
}

# GitHub App (only set when provided)
if ($GitHubAppId)             { $settings["GITHUB_APP_ID"] = $GitHubAppId }
if ($GitHubAppInstallationId) { $settings["GITHUB_APP_INSTALLATION_ID"] = $GitHubAppInstallationId }
$settings["GITHUB_APP_PRIVATE_KEY_PATH"] = $GitHubAppPrivateKeyPath

# Write to a temp JSON file in az's expected shape to avoid all shell-quoting issues.
$azSettings = foreach ($k in $settings.Keys) {
    [pscustomobject]@{ name = $k; value = [string]$settings[$k]; slotSetting = $false }
}
$tmp = Join-Path ([System.IO.Path]::GetTempPath()) ("warren-studio-settings-{0}.json" -f ([guid]::NewGuid()))
$azSettings | ConvertTo-Json -Depth 5 | Set-Content -Path $tmp -Encoding utf8

try {
    az webapp config appsettings set `
        --name $AppName `
        --resource-group $ResourceGroup `
        --settings "@$tmp" `
        --output none
} finally {
    Remove-Item $tmp -ErrorAction SilentlyContinue
}

# Container restarts on settings change; nudge it to be sure the new image/env are live.
Say "Restarting app to pick up settings"
az webapp restart --name $AppName --resource-group $ResourceGroup --output none

# ---------------------------------------------------------------------------
# 5. Done — print the URLs and the remaining manual steps
# ---------------------------------------------------------------------------
$appHost = "$AppName.azurewebsites.net"
Write-Host ""
Say "Deployed."
Write-Host "    Studio URL : https://$appHost"             -ForegroundColor Green
Write-Host "    Health     : https://$appHost/healthz"     -ForegroundColor Green
Write-Host "    Kudu (SCM) : https://$AppName.scm.azurewebsites.net" -ForegroundColor Green
Write-Host ""
Warn "One-time manual steps remain (see DEPLOY.md):"
Write-Host "    A) Register a GitHub OAuth App (https://github.com/settings/developers):"
Write-Host "         Homepage URL            = https://$appHost"
Write-Host "         Authorization callback  = https://$appHost/auth/github/callback"
Write-Host "       then re-run this script with -GitHubOAuthClientId/-GitHubOAuthClientSecret"
Write-Host "       and -GitHubOAuthAllowedUsers ""<your-login>,<warrens-login>""."
Write-Host "    B) Add Warren as a Write collaborator on $RepoSlug (so his pushes land)."
Write-Host "    E) Upload the GitHub App private key to /home/secrets/warren-studio.pem (Kudu) — LEGACY/optional."
Write-Host "    F) Transplant your authenticated ~/.copilot into /home/.copilot (Kudu), so the"
Write-Host "       AI brain runs as you with no interactive login on the server."
Write-Host ""
Write-Host "Tail logs with:  az webapp log tail --name $AppName --resource-group $ResourceGroup"
