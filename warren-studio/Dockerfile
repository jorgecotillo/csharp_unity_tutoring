# Warren's Game Studio — Node + Express portal + headless GitHub Copilot CLI.
#
# The image contains the portal app AND the Copilot CLI (the "brain"), but NOT
# the Unity repo or the multi-GB game build. The csharp_unity_tutoring checkout
# is created/refreshed at runtime under REPO_ROOT by entrypoint.sh.
#
# On Azure App Service for Containers, /home is the ONLY persistent path
# (requires WEBSITES_ENABLE_APP_SERVICE_STORAGE=true). So we put both the repo
# checkout (REPO_ROOT=/home/repo) and the Copilot auth dir (HOME=/home →
# /home/.copilot) under /home, so Jorge's one-time `copilot` login survives
# restarts and redeploys.

FROM node:24-alpine

ENV NODE_ENV=production

# git + ssh/tls for cloning/pushing the repo; bash so entrypoint.sh is portable.
RUN apk add --no-cache git openssh-client ca-certificates bash

WORKDIR /app

# The headless Copilot CLI — chat.js spawns it from the global install. Pinning
# is optional; we take the latest published CLI.
RUN npm install -g @github/copilot

# Install portal deps first for better layer caching.
COPY package.json package-lock.json* ./
RUN npm ci --omit=dev || npm install --omit=dev

# App source.
COPY . .

# entrypoint.sh may be checked out with CRLF on a Windows host; strip CRs and
# make it executable so it runs under Alpine's /bin/sh.
RUN sed -i 's/\r$//' /app/entrypoint.sh && chmod +x /app/entrypoint.sh

# /home is the persistent volume on App Service for Containers. HOME drives
# where the Copilot CLI stores its OAuth auth dir (/home/.copilot).
ENV HOME=/home \
    REPO_ROOT=/home/repo \
    GAME_BUILD_DIR=docs/game \
    PORT=8080

EXPOSE 8080

# entrypoint.sh clones-or-updates the repo, sets git identity, then exec's node.
ENTRYPOINT ["/app/entrypoint.sh"]
