# Warren's Game Studio — Node + Express portal.
# The image contains only the portal app. The Unity repo (including the prebuilt
# WebGL bundle) is provided at runtime via REPO_ROOT (a mounted volume or a clone),
# so we don't bake the multi-GB game build into the image.

FROM node:24-alpine

ENV NODE_ENV=production
WORKDIR /app

# Install deps first for better layer caching.
COPY package.json package-lock.json* ./
RUN npm ci --omit=dev || npm install --omit=dev

# App source.
COPY . .

# Default repo mount point inside the container. Override with REPO_ROOT.
ENV REPO_ROOT=/repo
ENV PORT=8080
EXPOSE 8080

CMD ["node", "server.js"]
