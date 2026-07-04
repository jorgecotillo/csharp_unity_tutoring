# ⚔️ Warren's Game Studio

A small, hosted **Copilot-style studio** scoped to **one purpose**: let **Warren** (and
Jorge) sign in with GitHub, read the design spec, browse the game code, and watch the
**Goblin Siege** Unity game run live (WebGL) in the browser — and iterate on it.

This covers **Phase 1 + Phase 2**: the static 3-pane studio shell **plus a live, repo-scoped
Copilot chat assistant**.

- 📜 **Spec** — renders `FINAL_PROJECT_REVISED.md`
- 💻 **Code** — read-only file tree of `mvp_v1/Assets/Scripts` with syntax highlighting
- 🎮 **Game** — embedded live WebGL preview of the pre-built game
- 🤖 **Copilot chat** — **live.** Streams answers from your GitHub Copilot subscription,
  hard-scoped to this repo, **read/propose only** (no file writes, no shell). See below.

The auto-build loop (chat → edit → rebuild → reload) comes in a later phase (see the plan).

---

## What's in here

```
warren-studio/
├── server.js          # Express app: auth gate + scoped repo API + chat SSE + WebGL serving
├── package.json       # deps + scripts (start, dev)
├── lib/
│   ├── oauth.js       # GitHub OAuth login + two-user allow-list
│   ├── tokens.js      # in-memory per-session push tokens
│   ├── chat.js        # headless Copilot CLI driver → streaming chat (read/propose only)
│   └── repo.js        # read-only, path-scoped repo access + WebGL paths
├── public/
│   ├── login.html     # "Sign in with GitHub" card
│   ├── studio.html    # 3-pane studio shell
│   ├── css/studio.css # Goblin-themed GitHub-dark UI
│   └── js/
│       ├── login.js   # kicks off the GitHub OAuth flow
│       └── studio.js  # wires spec / code / game / chat panes
├── Dockerfile         # node:24-alpine production image
├── .env               # local config (gitignored) — loaded via dotenv
├── .env.example       # config reference
├── .dockerignore
└── .gitignore
```

The app reads from the repo (its parent of `warren-studio/`) but never writes to it.

---

## Run it locally

Requires **Node 20+** (developed on Node 24). Login is **GitHub OAuth only**, so you need a
GitHub OAuth App (see `.env.example` for the three `GITHUB_OAUTH_*` values).

```powershell
cd warren-studio
npm install

# Configure GitHub OAuth + the two-user allow-list in .env, then:
node server.js
```

Then open **http://localhost:8080**, click **Sign in with GitHub**, and authorize. Only the
GitHub usernames in `GITHUB_OAUTH_ALLOWED_USERS` (jorge + warren) may enter — everyone else
is denied.

> The game preview needs a WebGL build to exist at `mvp_v1/Builds/WebGL/index.html`.
> If it's missing, the Game pane shows a friendly "no build yet" state — everything else
> still works. (Phase 0 already produced a build there.)

### Environment variables

| Variable | Purpose | Default |
|---|---|---|
| `PORT` | HTTP port | `8080` |
| `SESSION_SECRET` | Signs the session cookie. **Required in production.** | dev-insecure fallback (non-prod only) |
| `SESSION_IDLE_MIN` | Idle timeout in minutes | `120` |
| `GITHUB_OAUTH_CLIENT_ID` | GitHub OAuth App client ID. **Required — only way to log in.** | — |
| `GITHUB_OAUTH_CLIENT_SECRET` | GitHub OAuth App client secret (server-side only) | — |
| `GITHUB_OAUTH_ALLOWED_USERS` | Comma-separated GitHub logins allowed in (closed by default) | — |
| `REPO_ROOT` | Path to the repo root to read from | `..` (parent of `warren-studio/`) |
| `CHAT_MODEL` | Copilot model the chat assistant uses | `claude-sonnet-4.6` |
| `CHAT_RATE_PER_HOUR` | Max chat messages **per user, per hour** (protects your subscription) | `60` |

Local config is read from a gitignored **`.env`** (loaded via `dotenv`). Copy `.env.example`
to `.env` and fill it in; environment variables already set in the shell win over `.env`.

Login is **GitHub-only**: there is no password fallback. `GITHUB_OAUTH_ALLOWED_USERS` is the
allow-list — leave it empty and nobody can sign in.

## Copilot chat (Phase 2)

The 🤖 pane is a live AI assistant powered by **your own GitHub Copilot subscription**,
driven through the headless **Copilot CLI** running as you. No second seat, no Azure
OpenAI key.

**How it works**

- The browser POSTs to **`POST /api/chat`** (auth-gated) and reads the reply as a
  **Server-Sent Events** stream: `delta` events (token-by-token), then a final `done`
  event (`{ html, text, model, premiumRequests }`), or an `error` event.
- The backend (`lib/chat.js`) spawns the Copilot CLI **as you** with the working directory
  pinned to this repo (`-C <repo root>`), so it can read the spec + game code for grounding.

**Hard scoping / guardrails**

- **Read/propose only** — launched with `--mode plan --deny-tool write --deny-tool shell`,
  so the assistant can explain code and *propose* diffs but **cannot edit files or run
  shell**. (Writes + the auto-build loop arrive in a later phase, behind a GitHub App.)
- **This repo only** — the CLI's working dir is this repo; nothing else is reachable.
- **Rate limited** — `CHAT_RATE_PER_HOUR` (default **60**) messages per user per hour.
- MCP servers are disabled for the chat process to keep responses fast (~10s) and focused.

**Cost note** 💸

Chat rides on your **existing Copilot subscription** — there is **no per-token Azure
bill**. Each reply reports a `premiumRequests` count; higher-tier models (like the default
`claude-sonnet-4.6`) consume **premium requests** from your monthly Copilot allowance. The
per-user hourly cap is the main throttle protecting that allowance — tune `CHAT_RATE_PER_HOUR`
and `CHAT_MODEL` to taste.

> In production the deployed container won't have your local `~/.copilot` MCP config; it
> relies on the persisted Copilot **auth directory** (your one-time device login) to stay
> authenticated. See the project plan §4.2.

---


The included `Dockerfile` builds a small `node:24-alpine` image listening on `8080`.

High-level steps:

1. **Build & push the image** to Azure Container Registry (ACR).
2. **Provide the repo at `/repo`** in the container and set `REPO_ROOT=/repo`. The image
   defaults `REPO_ROOT=/repo`, so mount or bake the repo (including a WebGL build at
   `mvp_v1/Builds/WebGL`) there. In later phases the build loop publishes the WebGL output.
3. **Set secrets via Key Vault / Container Apps secrets:**
   - `SESSION_SECRET` — a long random string (required in production).
   - `GITHUB_OAUTH_CLIENT_ID` / `GITHUB_OAUTH_CLIENT_SECRET` — the OAuth App credentials.
   - `GITHUB_OAUTH_ALLOWED_USERS` — `jorge,warren` (the two allowed logins).
4. **Enable HTTPS + a custom domain** on the Container App. The app already sets
   `trust proxy`, secure cookies, `helmet`, and `compression`.

```powershell
# Example: build & push to ACR
az acr build --registry <yourRegistry> --image warren-studio:latest .
```

> **Note on WebGL serving:** the app serves the Unity build with the correct MIME types
> (`.wasm` → `application/wasm`, etc.) and disables COEP so the game runs in the iframe.
> The build is configured with compression **disabled + decompression fallback**, so a
> plain static host works. It must be served over **HTTP(S)** — it will not run from
> `file://`.

---

## Security notes

- GitHub OAuth client secret and `SESSION_SECRET` live **server-side only**.
- Repo access is **read-only** and **path-scoped** (`lib/repo.js` allow-lists
  `mvp_v1/Assets` + the spec file, rejects `..` escapes and null bytes, caps file size).
- The Copilot chat is **read/propose only** (`--mode plan`, `--deny-tool write`,
  `--deny-tool shell`) and pinned to this repo — no file writes, no shell, no other repos.
- Per-user AI **rate cap** (`CHAT_RATE_PER_HOUR`, default 60/hr) protects your subscription.
- `helmet` CSP/COEP are relaxed because Unity WebGL needs `wasm-eval` + blob + iframe; this
  gets hardened in a later phase.
