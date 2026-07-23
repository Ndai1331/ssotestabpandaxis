#!/usr/bin/env node
/**
 * SessionStart hook: prepare an app-created worktree to develop the CORE
 * services (UI + API) in isolation, and make `preview_start` work.
 *
 * App worktrees (<root>/.claude/worktrees/<name>) check out only committed
 * meta-repo files, so submodule dirs (services/ui, services/api) are empty
 * placeholders and the machine-local launch.json is missing. On session start
 * inside such a worktree this hook:
 *   1. CORE submodules (ui, api): creates a dedicated git worktree of each,
 *      checked out on its own branch `claude_<type>/<slug>` cut FROM THE
 *      LATEST origin/main. This forces every worktree to start from current
 *      main (no stale-branch drift) and isolates files + build output per
 *      worktree. The branch is the durable feature identity (see git-flow
 *      rules in CLAUDE.md); the random worktree folder name is NOT the
 *      identity — worktree-branch-rename.cjs renames the branch to a real
 *      slug on the first user prompt.
 *   2. Allocates unique UI/API ports (deterministic per worktree name,
 *      probing past ports that are listening or claimed elsewhere).
 *   3. Writes <worktree>/.claude/launch.json with ONLY UI + API configs.
 *      The API entry runs through run-api-testdb.sh, which forces the
 *      qcadmin_test DB (never PROD).
 *
 * WORKER submodules (agent, worker, geoblock, metabase, ahrefs-mcp,
 * worker-lite, worker-lite-cpd, similarweb-worker, ncc-worker) are left as
 * empty placeholders — they run separately via `docker compose up <svc>`.
 *
 * No-op at the workspace root or when a launch.json already exists.
 *
 * Branch identity from the worktree name:
 *   - `feat-<slug>` / `fix-<slug>` / `chore-<slug>` / `lite-<slug>`
 *      → `claude_<type>/<slug>` (used when handoff names a resume worktree).
 *   - anything else (app-random, e.g. bold-burnell-17a26d)
 *      → `claude_feat/<name>` (temporary; renamed on first prompt).
 * If the target branch already exists and is free → it is REUSED (resume a
 * paused feature → continuity). If it is checked out in another live worktree
 * → a `_<n>` suffix is allocated (parallel session).
 */
'use strict';

const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

const UI_BASE = 5300; // UI range 5300-5999
const API_BASE = 7100; // API range 7100-7799
const RANGE = 700;
// Ports used by root launch.json / docker services — never allocate these.
const RESERVED = new Set([3001, 5000, 5001, 5005, 5006, 5010, 5053, 5093, 5173, 7093, 8080]);
// Only these submodules are CORE (isolated worktree + branch). Everything else
// in .gitmodules is a WORKER and is left untouched.
const CORE_SUBMODULES = ['ui', 'api'];
const KNOWN_TYPES = ['feat', 'fix', 'chore', 'lite'];

function main() {
  const proj = process.env.CLAUDE_PROJECT_DIR || process.cwd();
  const marker = `${path.sep}.claude${path.sep}worktrees${path.sep}`;
  const idx = proj.indexOf(marker);
  if (idx === -1) return; // not an app worktree → nothing to do

  const root = proj.slice(0, idx);
  const worktreeName = proj.slice(idx + marker.length).split(path.sep)[0];
  const worktree = path.join(root, '.claude', 'worktrees', worktreeName);

  const { type, slug } = parseWorktreeName(worktreeName);
  for (const sm of CORE_SUBMODULES) {
    try {
      setupCoreWorktree(sm, worktree, root, type, slug);
    } catch (err) {
      console.error(`[worktree-launch-config] services/${sm}: ${err.message}`);
    }
  }

  const launchPath = path.join(worktree, '.claude', 'launch.json');
  if (fs.existsSync(launchPath)) return; // respect existing config (manual or prior run)

  const claimed = collectClaimedPorts(root, worktree);
  const offset = hash(worktreeName) % RANGE;
  const uiPort = probe(UI_BASE, offset, claimed);
  claimed.add(uiPort);
  const apiPort = probe(API_BASE, offset, claimed);

  const dotnet = resolveDotnet();
  // Prefer the worktree's checked-out copy; fall back to the root's.
  let apiWrapper = path.join(worktree, '.claude', 'scripts', 'run-api-testdb.sh');
  if (!fs.existsSync(apiWrapper)) {
    apiWrapper = path.join(root, '.claude', 'scripts', 'run-api-testdb.sh');
  }
  const config = {
    version: '0.0.1',
    generatedBy: 'worktree-launch-config',
    configurations: [
      {
        name: 'UI · Blazor Server',
        runtimeExecutable: dotnet,
        runtimeArgs: ['run', '--urls', `http://localhost:${uiPort}`, '--launch-profile', 'console'],
        cwd: 'services/ui/src/BootstrapBlazor.Server',
        port: uiPort,
      },
      {
        name: 'API · .NET WebApi (qcadmin_test)',
        runtimeExecutable: '/bin/bash',
        runtimeArgs: [apiWrapper, String(apiPort)],
        cwd: 'services/api/WebApi',
        port: apiPort,
      },
    ],
    autoVerify: false,
  };

  fs.mkdirSync(path.dirname(launchPath), { recursive: true });
  fs.writeFileSync(launchPath, JSON.stringify(config, null, 2) + '\n');
  console.log(`[worktree-launch-config] generated .claude/launch.json — UI :${uiPort}, API :${apiPort} (test DB qcadmin_test)`);
}

/**
 * Derive {type, slug} from a worktree folder name.
 *   feat-seo-dashboard → { type: 'feat', slug: 'seo-dashboard' }
 *   bold-burnell-17a26d → { type: 'feat', slug: 'bold-burnell-17a26d' }
 */
function parseWorktreeName(name) {
  const m = /^(feat|fix|chore|lite)[-_](.+)$/.exec(name);
  if (m && KNOWN_TYPES.includes(m[1])) return { type: m[1], slug: m[2] };
  return { type: 'feat', slug: name };
}

/**
 * Create a dedicated git worktree of a core submodule inside the meta worktree,
 * on branch `claude_<type>/<slug>` cut from the latest origin/main.
 * Reuses the branch if it already exists and is free (resume); allocates a
 * `_<n>` suffix if the branch is checked out in another live worktree.
 */
function setupCoreWorktree(sm, worktree, root, type, slug) {
  const target = path.join(worktree, 'services', sm);
  // Already a populated worktree (resume / prior run) → leave it.
  if (fs.existsSync(path.join(target, '.git'))) return;

  const rootSm = path.join(root, 'services', sm);
  if (!fs.existsSync(path.join(rootSm, '.git'))) return; // root submodule not checked out

  // Always start from the freshest main.
  try {
    execSync('git fetch origin main --prune', { cwd: rootSm, stdio: 'ignore' });
  } catch {
    /* offline — fall back to whatever origin/main we already have */
  }

  // Remove the empty placeholder dir so `git worktree add` can populate it.
  try {
    if (fs.lstatSync(target).isDirectory() && fs.readdirSync(target).length === 0) {
      fs.rmdirSync(target);
    }
  } catch { /* missing — fine, git worktree add will create it */ }

  const desired = `claude_${type}/${slug}`;
  const branch = addSubmoduleWorktree(rootSm, target, desired);
  console.log(`[worktree-launch-config] services/${sm} → worktree on ${branch} (from origin/main)`);
}

/**
 * Try to add a worktree for `desiredBranch`; on collision (branch checked out
 * elsewhere) retry with `_2`, `_3`, … Existing free branch → reuse (resume);
 * missing branch → create from origin/main.
 * @returns {string} the branch name actually used.
 */
function addSubmoduleWorktree(rootSm, target, desiredBranch) {
  for (let n = 0; n < 20; n++) {
    const branch = n === 0 ? desiredBranch : `${desiredBranch}_${n + 1}`;
    const exists = branchExists(rootSm, branch);
    const cmd = exists
      ? `git worktree add ${shq(target)} ${shq(branch)}`
      : `git worktree add -b ${shq(branch)} ${shq(target)} origin/main`;
    try {
      execSync(cmd, { cwd: rootSm, stdio: 'pipe' });
      return branch;
    } catch {
      // Most likely the branch is already checked out in a sibling worktree.
      // Try the next suffix.
    }
  }
  throw new Error(`could not allocate a free worktree branch for ${desiredBranch}`);
}

function branchExists(rootSm, branch) {
  try {
    execSync(`git show-ref --verify --quiet ${shq('refs/heads/' + branch)}`, { cwd: rootSm, stdio: 'ignore' });
    return true;
  } catch {
    return false;
  }
}

/** Single-quote a string for safe use in a shell command. */
function shq(s) {
  return `'${String(s).replace(/'/g, `'\\''`)}'`;
}

/** Ports already promised to root configs or sibling worktrees. */
function collectClaimedPorts(root, selfWorktree) {
  const claimed = new Set(RESERVED);
  const files = [path.join(root, 'launch.json'), path.join(root, '.claude', 'launch.json')];
  const wtDir = path.join(root, '.claude', 'worktrees');
  try {
    for (const name of fs.readdirSync(wtDir)) {
      const f = path.join(wtDir, name, '.claude', 'launch.json');
      if (path.join(wtDir, name) !== selfWorktree) files.push(f);
    }
  } catch { /* no worktrees dir */ }
  for (const f of files) {
    try {
      const cfg = JSON.parse(fs.readFileSync(f, 'utf8'));
      for (const c of cfg.configurations || []) {
        if (Number.isInteger(c.port)) claimed.add(c.port);
      }
    } catch { /* missing or invalid file — ignore */ }
  }
  return claimed;
}

/** djb2 string hash — deterministic port offset per worktree name. */
function hash(str) {
  let h = 5381;
  for (let i = 0; i < str.length; i++) h = ((h * 33) ^ str.charCodeAt(i)) >>> 0;
  return h;
}

/** First port in [base, base+RANGE) starting at offset that is unclaimed and not listening. */
function probe(base, offset, claimed) {
  for (let i = 0; i < RANGE; i++) {
    const port = base + ((offset + i) % RANGE);
    if (claimed.has(port) || isListening(port)) continue;
    return port;
  }
  throw new Error(`no free port in range ${base}-${base + RANGE - 1}`);
}

function isListening(port) {
  try {
    execSync(`lsof -nP -ti tcp:${port} -s tcp:LISTEN`, { stdio: ['ignore', 'pipe', 'ignore'] });
    return true; // lsof exits 0 only when it found a listener
  } catch {
    return false;
  }
}

function resolveDotnet() {
  const candidates = ['/opt/homebrew/opt/dotnet@9/bin/dotnet', '/opt/homebrew/bin/dotnet', '/usr/local/bin/dotnet'];
  for (const c of candidates) {
    if (fs.existsSync(c)) return c;
  }
  return 'dotnet'; // fall back to PATH resolution
}

try {
  main();
} catch (err) {
  // Never block session start — report and exit clean.
  console.error(`[worktree-launch-config] ${err.message}`);
}
