'use strict';

const fs = require('fs');
const path = require('path');

// Repo root. Defaults to two levels up (warren-studio/lib -> warren-studio -> repo root).
// In a container, set REPO_ROOT to the mounted/cloned repo path (e.g. /repo).
const REPO_ROOT = process.env.REPO_ROOT
  ? path.resolve(process.env.REPO_ROOT)
  : path.resolve(__dirname, '..', '..');

// Where the code-browser tree is rooted (Warren only ever sees the game scripts).
const CODE_ROOT_REL = 'mvp_v1/Assets/Scripts';

// The spec file Warren reads (and, later, edits).
const SPEC_FILE_REL = 'competition/warren_game_design_v2.md';

// The already-built WebGL bundle the preview iframe loads.
const WEBGL_DIR = path.join(REPO_ROOT, 'mvp_v1', 'Builds', 'WebGL');

// Read-only allow-list. A requested path must resolve inside one of these roots
// (or be one of the explicitly allowed files) or the read is refused.
const READ_ROOTS = ['mvp_v1/Assets'];
const ALLOWED_FILES = [SPEC_FILE_REL];

// Extensions we are willing to show in the code panel.
const VIEWABLE_EXT = new Set([
  '.cs', '.js', '.ts', '.json', '.md', '.txt', '.shader',
  '.uxml', '.uss', '.xml', '.yml', '.yaml', '.cginc', '.hlsl',
]);

const MAX_FILE_BYTES = 512 * 1024; // 512 KB safety cap

function toPosix(p) {
  return p.split(path.sep).join('/');
}

/**
 * Resolve a repo-relative path safely. Returns an absolute path only if it stays
 * inside the repo AND inside an allowed root/file. Otherwise returns null.
 */
function safeResolve(relPath) {
  const clean = String(relPath || '').replace(/\\/g, '/').replace(/^\/+/, '');
  if (clean.includes('\0')) return null;

  const abs = path.resolve(REPO_ROOT, clean);
  const rel = path.relative(REPO_ROOT, abs);
  if (rel.startsWith('..') || path.isAbsolute(rel)) return null; // escaped repo root

  const relPosix = toPosix(rel);
  const okRoot = READ_ROOTS.some((r) => relPosix === r || relPosix.startsWith(r + '/'));
  const okFile = ALLOWED_FILES.includes(relPosix);
  if (!okRoot && !okFile) return null;

  return abs;
}

/** Recursively build a folder/file tree under the code root. */
function buildTree(absDir, relBase) {
  const entries = fs.readdirSync(absDir, { withFileTypes: true });
  const nodes = [];

  for (const entry of entries) {
    if (entry.name.startsWith('.')) continue;
    const childAbs = path.join(absDir, entry.name);
    const childRel = toPosix(path.join(relBase, entry.name));

    if (entry.isDirectory()) {
      const children = buildTree(childAbs, childRel);
      if (children.length > 0) {
        nodes.push({ type: 'dir', name: entry.name, path: childRel, children });
      }
    } else if (entry.isFile()) {
      const ext = path.extname(entry.name).toLowerCase();
      if (VIEWABLE_EXT.has(ext)) {
        nodes.push({ type: 'file', name: entry.name, path: childRel, ext: ext.slice(1) });
      }
    }
  }

  // Folders first, then files, each alphabetical.
  nodes.sort((a, b) => {
    if (a.type !== b.type) return a.type === 'dir' ? -1 : 1;
    return a.name.localeCompare(b.name);
  });
  return nodes;
}

function getCodeTree() {
  const root = path.join(REPO_ROOT, ...CODE_ROOT_REL.split('/'));
  if (!fs.existsSync(root)) return { root: CODE_ROOT_REL, children: [] };
  return { root: CODE_ROOT_REL, children: buildTree(root, CODE_ROOT_REL) };
}

/** Read an allow-listed file. Returns { path, ext, content } or throws. */
function readFile(relPath) {
  const abs = safeResolve(relPath);
  if (!abs) {
    const err = new Error('Path is not allowed.');
    err.code = 'FORBIDDEN';
    throw err;
  }
  const stat = fs.statSync(abs);
  if (!stat.isFile()) {
    const err = new Error('Not a file.');
    err.code = 'NOT_FILE';
    throw err;
  }
  if (stat.size > MAX_FILE_BYTES) {
    const err = new Error('File is too large to display.');
    err.code = 'TOO_LARGE';
    throw err;
  }
  const ext = path.extname(abs).toLowerCase();
  if (!VIEWABLE_EXT.has(ext)) {
    const err = new Error('File type is not viewable.');
    err.code = 'NOT_VIEWABLE';
    throw err;
  }
  return {
    path: toPosix(path.relative(REPO_ROOT, abs)),
    ext: ext.slice(1),
    content: fs.readFileSync(abs, 'utf8'),
  };
}

/** Read the raw spec markdown. */
function readSpec() {
  const abs = path.join(REPO_ROOT, ...SPEC_FILE_REL.split('/'));
  if (!fs.existsSync(abs)) return null;
  return fs.readFileSync(abs, 'utf8');
}

function webglExists() {
  return fs.existsSync(path.join(WEBGL_DIR, 'index.html'));
}

function webglBuiltAt() {
  try {
    return fs.statSync(path.join(WEBGL_DIR, 'index.html')).mtime.toISOString();
  } catch {
    return null;
  }
}

module.exports = {
  REPO_ROOT,
  WEBGL_DIR,
  SPEC_FILE_REL,
  CODE_ROOT_REL,
  getCodeTree,
  readFile,
  readSpec,
  webglExists,
  webglBuiltAt,
};
