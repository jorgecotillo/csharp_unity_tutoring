# 🚀 Deploying Warren's Game Studio to Azure

This guide ships the studio to **Azure App Service for Containers (Linux B1, ~$13/mo)** in
your **personal Gmail** Azure subscription. The AI brain rides on your **existing GitHub
Copilot subscription** — no Azure OpenAI key, no per-token bill.

Total new cost: **< $15/month** (or **$0 extra plan cost** if you reuse an existing Linux
App Service plan — see step D).

> **One honest caveat (already acknowledged):** Copilot is licensed per seat. Letting Warren
> drive your subscription puts a 2nd person on one seat — against Copilot's ToS. This is a
> private family/tutoring setup, hard-scoped to one repo. You've chosen to proceed; this doc
> just makes it work.

---

## What you'll do (10-minute overview)

| Step | What | Who can do it |
| ---- | ---- | ------------- |
| **A** | Create a repo-scoped **GitHub App** (PAT-free git write credential) | You |
| **B** | Add the `UNITY_LICENSE` secret so the WebGL CI build goes green | You |
| **C** | Build the container image and push it to **GHCR** (public, free) | You / CI |
| **D** | `az login` (Gmail MFA) → run `deploy/azure-deploy.ps1` | **You only** (MFA) |
| **E** | Upload the GitHub App `.pem` to `/home/secrets/` via Kudu | You |
| **F** | Transplant your authenticated `~/.copilot` into `/home/.copilot` via Kudu | You |

Steps **A, B, C** can be done in any order. **D** must come before **E/F**.

---

## A. Create the GitHub App (PAT-free write credential)

The studio pushes Copilot's edits to `main`. Instead of a Personal Access Token (which can
reach **all** your repos), we use a **GitHub App installed on ONLY `csharp_unity_tutoring`**
— it literally cannot touch your other repos.

1. Go to **https://github.com/settings/apps** → **New GitHub App**.
2. Fill in:
   - **GitHub App name**: `warren-studio-bot` (or anything unique).
   - **Homepage URL**: `https://github.com/jorgecotillo/csharp_unity_tutoring` (any URL is fine).
   - **Webhook**: **uncheck "Active"** (we don't use webhooks).
3. **Repository permissions** → set **only**:
   - **Contents: Read and write**  ← this is the one that lets it commit/push.
   - Leave everything else at "No access".
4. **Where can this GitHub App be installed?** → **Only on this account**.
5. Click **Create GitHub App**.
6. On the app's page, note the **App ID** (top of the page).
7. Scroll to **Private keys** → **Generate a private key**. A `.pem` file downloads — **keep
   it safe**; you'll upload it in step E.
8. Left sidebar → **Install App** → **Install** on **your account** → choose **Only select
   repositories** → pick **`csharp_unity_tutoring`** → **Install**.
9. After installing, the browser URL looks like
   `https://github.com/settings/installations/<INSTALLATION_ID>`. Note that
   **Installation ID** number.

You now have three things for the deploy script:
- **App ID** → `-GitHubAppId`
- **Installation ID** → `-GitHubAppInstallationId`
- The **`.pem` file** → uploaded in step E (the script only references its path).

---

## B. Add the `UNITY_LICENSE` secret (makes the game build go green)

The WebGL build runs in GitHub Actions (`.github/workflows/webgl.yml`). It needs your free
Unity Personal license. **Until this is set, the Action fails fast — that red ❌ is expected.**

1. On your machine, activate Unity Personal once (Unity Hub → sign in). This creates a
   `.ulf` license file:
   - **Mac**: `~/Library/Application Support/Unity/Unity_lic.ulf`
   - **Windows**: `C:\ProgramData\Unity\Unity_lic.ulf`
2. Open that `.ulf` file in a text editor and copy its **entire** contents (it's XML).
3. Go to **repo → Settings → Secrets and variables → Actions → New repository secret**:
   - **Name**: `UNITY_LICENSE`
   - **Value**: paste the full `.ulf` contents.
4. Push any change to `mvp_v1/**` (or re-run the workflow) → the build should now go **green**
   and publish the playable game to `docs/game/**`.

> The CI publishes the build with the built-in `GITHUB_TOKEN` (the workflow has
> `permissions: contents: write`) — **no PAT needed** for the build-and-publish half.

---

## C. Build & push the container image to GHCR

The deploy script pulls a **public GHCR image** (no registry credentials on Azure). Build and
push it from your machine (Docker is required):

```powershell
cd warren-studio

# 1) Log in to GHCR with a GitHub token that has write:packages
#    (a classic PAT with write:packages, or `gh auth token` if your gh login has it)
$env:CR_PAT = (gh auth token)
$env:CR_PAT | docker login ghcr.io -u jorgecotillo --password-stdin

# 2) Build for Linux/amd64 (App Service runs amd64)
docker build --platform linux/amd64 -t ghcr.io/jorgecotillo/warren-studio:latest .

# 3) Push
docker push ghcr.io/jorgecotillo/warren-studio:latest
```

Then make the package **public** so Azure can pull it without creds:
**https://github.com/users/jorgecotillo/packages/container/warren-studio/settings** →
**Change visibility** → **Public**.

> Re-deploying later = repeat C (rebuild/push `:latest`), then re-run the script in step D —
> it detects the existing web app and just updates the image + restarts.

---

## D. Log in to Azure (Gmail MFA) and run the deploy script

Your personal subscription lives in the **Gmail tenant**, which **requires interactive MFA in
a browser**. This is the **one step nobody can do for you** — it needs your phone/auth app.

```powershell
# Interactive browser login to the Gmail tenant (does the MFA prompt).
az login --tenant 4620f07d-550f-4c60-9095-b95b4a44ba9c `
         --scope "https://management.core.windows.net//.default"
```

After login, **confirm you can see resources** (this is how you know the MFA token is fresh):

```powershell
az account show --query "{name:name, user:user.name, id:id}" -o table
az group list -o table        # should now list mypersonalrg, etc. (no AADSTS50076 error)
```

**(Optional but recommended) Reuse your existing `pnw-movement` plan for $0 extra.**
A container web app needs a **Linux** plan. Check what OS `pnw-movement` runs on:

```powershell
# Find the plan behind the existing pnw-movement site, then inspect its OS/SKU:
$planId = az webapp show --name pnw-movement --resource-group mypersonalrg `
            --query appServicePlanId -o tsv
az appservice plan show --ids $planId `
   --query "{name:name, os_isLinux:reserved, sku:sku.name}" -o table
```

- If **`os_isLinux` = True** → you can host the studio on that **same plan for no extra plan
  cost**. Pass it to the script: `-PlanName <that plan's name>`.
- If **`os_isLinux` = False** (Windows) → leave `-PlanName` at the default; the script creates
  a fresh Linux B1 plan (~$13/mo). (The script will throw a friendly error if you try to
  reuse a Windows plan, so you can't get this wrong.)

**Run the deploy** (fill in the App ID / Installation ID from step A):

```powershell
cd warren-studio\deploy

# Quick start with a single shared dev password (simplest):
.\azure-deploy.ps1 `
   -GitHubAppId <APP_ID> `
   -GitHubAppInstallationId <INSTALLATION_ID> `
   -DevPassword goblins
   # add  -PlanName <linux-plan-name>  to reuse pnw-movement's plan for $0 extra

# OR with per-user hashed passwords (jorge + warren):
.\azure-deploy.ps1 `
   -GitHubAppId <APP_ID> `
   -GitHubAppInstallationId <INSTALLATION_ID> `
   -StudioUsersJsonPath ..\studio-users.json
```

The script is **idempotent** — re-running only re-applies app settings and updates the image.
It auto-selects your personal subscription (`-SubscriptionId` default `170f5740-...`) and
**generates `SESSION_SECRET` for you** if you don't pass one.

When it finishes it prints:
- **Studio URL** — `https://warren-studio.azurewebsites.net`
- **Health** — `…/healthz`
- **Kudu (SCM)** — `https://warren-studio.scm.azurewebsites.net` (used in steps E & F)

---

## E. Upload the GitHub App private key (Kudu)

The `.pem` from step A must live at `/home/secrets/warren-studio.pem` — a **persistent** path
that is **not** baked into the image.

1. Open **Kudu**: `https://warren-studio.scm.azurewebsites.net` → **Debug console → Bash**
   (or the **CMD** console).
2. Create the folder and confirm it persists:
   ```bash
   mkdir -p /home/secrets
   ```
3. Navigate the Kudu file explorer into `/home/secrets`, then **drag-and-drop** your `.pem`
   file into the file list. Rename it to exactly **`warren-studio.pem`** if needed.
4. Verify:
   ```bash
   ls -l /home/secrets/warren-studio.pem
   ```

> The path is configurable via `-GitHubAppPrivateKeyPath` (default `/home/secrets/warren-studio.pem`).
> Until this file exists, git **push** stays off and Copilot's edits won't reach `main` — but
> the rest of the studio (chat, spec, code, preview) still works.

---

## F. Transplant your Copilot login (one-time, makes the AI run as you)

The Copilot CLI stores its OAuth login under `~/.copilot`. Inside the container `HOME=/home`,
so it looks in **`/home/.copilot`** (persistent). Copy your **already-authenticated** local
auth dir up once; after that the CLI refreshes its own token forever.

1. On your machine, make sure you're logged in locally:
   ```powershell
   copilot   # if it prompts to log in, complete the device login as Jorge, then exit
   ```
   Your auth dir is at `~/.copilot` (Windows: `C:\Users\jcotillo\.copilot`).
2. Zip the auth-relevant files (the CLI keeps its token + config here). At minimum you need
   the login/token files; zipping the whole folder is fine:
   ```powershell
   Compress-Archive -Path "$env:USERPROFILE\.copilot\*" `
                    -DestinationPath "$env:TEMP\copilot-auth.zip" -Force
   ```
3. In **Kudu** (`…scm.azurewebsites.net` → **Debug console → Bash**), navigate to `/home`,
   then **drag-and-drop `copilot-auth.zip`** into the `/home` listing.
4. In the Kudu Bash console, unzip into `/home/.copilot`:
   ```bash
   mkdir -p /home/.copilot
   cd /home/.copilot
   unzip -o /home/copilot-auth.zip
   rm /home/copilot-auth.zip
   ls -la /home/.copilot
   ```
5. Restart so the running process picks up the new auth dir:
   ```powershell
   az webapp restart --name warren-studio --resource-group mypersonalrg
   ```
6. Test: open the Studio URL, log in (password gate), and send a chat message. The AI should
   respond using **your** Copilot subscription.

> If chat says it's not authenticated, double-check that the files unzipped **directly** into
> `/home/.copilot/` (not `/home/.copilot/.copilot/`).

---

## ✅ Verify the full loop

1. **Studio loads** → `https://warren-studio.azurewebsites.net` → password gate → 3-pane UI.
2. **Spec + Code panels** render from the repo.
3. **Chat** responds (proves step F worked).
4. **Ask Copilot to make a small game change** → it commits to `main` (proves steps A + E).
5. **GitHub Actions** runs `webgl.yml` → green build publishes `docs/game/**` (proves step B).
6. **Preview reloads** within ~30s of the build landing (the container's puller pulls `main`).

End-to-end latency from chat to a playable new build is **~8–15 min** (Unity WebGL build
dominates). The UI shows a "forging your game 🔨" state while it builds.

---

## 🔧 Operations cheat-sheet

```powershell
# Tail live logs
az webapp log tail --name warren-studio --resource-group mypersonalrg

# Restart (after uploading secrets / auth, or to clear state)
az webapp restart --name warren-studio --resource-group mypersonalrg

# Redeploy a new image build
#   1) rebuild & push :latest  (step C)
#   2) re-run deploy\azure-deploy.ps1 (step D)  -> it just updates the image + restarts

# Turn the git pipeline on/off without redeploying
az webapp config appsettings set --name warren-studio --resource-group mypersonalrg `
   --settings GIT_PUSH_ENABLED=off
```

## 🧯 Troubleshooting

| Symptom | Likely cause | Fix |
| ------- | ------------ | --- |
| `az` commands return empty / `AADSTS50076` | Gmail tenant token needs MFA | Re-run the `az login --tenant 4620f07d-...` in step D |
| WebGL Action is red ❌ | `UNITY_LICENSE` secret missing/expired | Step B — paste the full `.ulf` contents |
| "No build yet" in preview | First green CI build hasn't published `docs/game/` yet | Wait for step B's build to finish, or it's still building |
| Chat says "not authenticated" | `/home/.copilot` missing/wrong layout | Re-do step F; ensure files are **directly** under `/home/.copilot/` |
| Copilot edits never reach `main` | `.pem` missing or App not installed on the repo | Steps A + E; confirm Contents:write + repo install |
| Container web app create fails on plan | Reusing a **Windows** plan | Omit `-PlanName` (new Linux plan) or pass a Linux one |
| Image pull fails on Azure | GHCR package still private | Make the package **Public** (step C) |
