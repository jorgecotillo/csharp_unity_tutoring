#!/bin/sh
# Warren's Game Studio container entrypoint.
#
# Runs once per container start. Ensures a working clone of the Unity repo
# exists at $REPO_ROOT (on the persistent /home volume), sets a git identity
# for AI commits, then hands off to the Node server.
#
# Idempotent: on a warm restart the repo already exists, so we just fast-forward.

set -e

# --- Config (all overridable via App Service app settings) -------------------
: "${HOME:=/home}"
: "${REPO_ROOT:=/home/repo}"
: "${GIT_REMOTE:=origin}"
: "${GIT_TARGET_BRANCH:=main}"
: "${GIT_CLONE_URL:=https://github.com/jorgecotillo/csharp_unity_tutoring.git}"
: "${GIT_AUTHOR_NAME:=Warren Studio Bot}"
: "${GIT_AUTHOR_EMAIL:=warren-studio-bot@users.noreply.github.com}"

export HOME REPO_ROOT

echo "[entrypoint] HOME=$HOME REPO_ROOT=$REPO_ROOT branch=$GIT_TARGET_BRANCH"

# git refuses to operate on a repo owned by another uid (volume-mounted dirs).
git config --global --add safe.directory "$REPO_ROOT" 2>/dev/null || true

# --- Clone or update ---------------------------------------------------------
if [ ! -d "$REPO_ROOT/.git" ]; then
  echo "[entrypoint] No checkout found — cloning $GIT_CLONE_URL"
  mkdir -p "$REPO_ROOT"
  # Clone into a temp dir then move contents, so REPO_ROOT can be a pre-existing
  # (possibly non-empty) mount point.
  TMP_CLONE="$(mktemp -d)"
  git clone --branch "$GIT_TARGET_BRANCH" --single-branch "$GIT_CLONE_URL" "$TMP_CLONE/repo"
  # Move including dotfiles.
  cp -a "$TMP_CLONE/repo/." "$REPO_ROOT/"
  rm -rf "$TMP_CLONE"
  echo "[entrypoint] Clone complete."
else
  echo "[entrypoint] Existing checkout — fetching latest."
  git -C "$REPO_ROOT" remote set-url "$GIT_REMOTE" "$GIT_CLONE_URL" 2>/dev/null || true
  if git -C "$REPO_ROOT" fetch --quiet "$GIT_REMOTE" "$GIT_TARGET_BRANCH"; then
    git -C "$REPO_ROOT" checkout --quiet "$GIT_TARGET_BRANCH" 2>/dev/null || true
    # Fast-forward only; never clobber local AI commits that haven't pushed.
    git -C "$REPO_ROOT" merge --ff-only "$GIT_REMOTE/$GIT_TARGET_BRANCH" 2>/dev/null \
      && echo "[entrypoint] Fast-forwarded to $GIT_REMOTE/$GIT_TARGET_BRANCH." \
      || echo "[entrypoint] Could not ff-merge (diverged or up to date) — continuing."
  else
    echo "[entrypoint] Fetch failed — continuing with existing checkout."
  fi
fi

# --- Git identity for AI commits --------------------------------------------
git -C "$REPO_ROOT" config user.name  "$GIT_AUTHOR_NAME"
git -C "$REPO_ROOT" config user.email "$GIT_AUTHOR_EMAIL"

# --- Hand off to the server --------------------------------------------------
echo "[entrypoint] Starting server on port ${PORT:-8080}..."
exec node /app/server.js
