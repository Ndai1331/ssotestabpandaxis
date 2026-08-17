#!/usr/bin/env node
/**
 * UserPromptSubmit hook: give a fresh worktree's CORE branches a meaningful
 * name derived from the user's intent — automatically, so it never depends on
 * the agent remembering to do it.
 *
 * worktree-launch-config.cjs (SessionStart) sets up services/ui + services/api
 * as git worktrees on a TEMPORARY branch `claude_feat/<worktreename>` (the
 * folder name is app-random, e.g. bold-burnell-17a26d). On the FIRST user
 * prompt this hook:
 *   1. Derives a kebab slug from the prompt text (Vietnamese diacritics
 *      stripped, stopwords dropped, first few content words).
 *   2. Renames the temp branch on ui + api → `claude_feat/<slug>`.
 *   3. Drops a one-time marker so it never runs twice for this worktree.
 *
 * It only renames a branch that is still the untouched temp branch
 * (`claude_feat/<worktreename>`). Handoff-named resume worktrees (feat-<todo>)
 * and `_<n>` parallel branches are left alone. Fail-open: any error leaves the
 * working temp branch in place (still a valid `claude_feat/*` branch).
 *
 * The slug is a best-effort default — if it is ugly, rename by hand before
 * deploy. The branch is functional either way.
 */
'use strict';

const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

const CORE_SUBMODULES = ['ui', 'api'];
const MAX_SLUG_WORDS = 5;
const MAX_SLUG_LEN = 40;
// Minimal VN + EN stopword set — enough to skip filler leading words.
const STOPWORDS = new Set([
  'the', 'a', 'an', 'to', 'of', 'in', 'on', 'for', 'and', 'or', 'with', 'please',
  'add', 'fix', 'make', 'create', 'update', 'change', 'let', 'lets', 'can', 'you',
  'toi', 'minh', 'hay', 'lam', 'cho', 'va', 'cua', 'mot', 'cai', 'nay', 'them',
  'sua', 'tao', 'giup', 'can', 'muon', 'la', 'duoc', 'di', 'voi', 'ban', 'o',
]);

function main() {
  const stdin = fs.readFileSync(0, 'utf-8').trim();
  if (!stdin) return;
  const payload = JSON.parse(stdin);
  const prompt = (payload.prompt || '').trim();
  if (!prompt) return;

  const proj = process.env.CLAUDE_PROJECT_DIR || payload.cwd || process.cwd();
  const marker = `${path.sep}.claude${path.sep}worktrees${path.sep}`;
  const idx = proj.indexOf(marker);
  if (idx === -1) return; // not an app worktree

  const root = proj.slice(0, idx);
  const worktreeName = proj.slice(idx + marker.length).split(path.sep)[0];
  const worktree = path.join(root, '.claude', 'worktrees', worktreeName);

  const stateFile = path.join(worktree, '.claude', '.branch-renamed');
  if (fs.existsSync(stateFile)) return; // already handled once

  const slug = deriveSlug(prompt);
  // Always write the marker so we attempt at most once, even if slug is empty.
  try {
    fs.mkdirSync(path.dirname(stateFile), { recursive: true });
    fs.writeFileSync(stateFile, `${new Date().toISOString()} ${slug || '(no-slug)'}\n`);
  } catch { /* ignore */ }
  if (!slug) return;

  const tempBranch = `claude_feat/${worktreeName}`;
  const newBranch = `claude_feat/${slug}`;
  if (newBranch === tempBranch) return; // nothing to change

  // Only rename submodules still sitting on the untouched temp branch. Both
  // CORE services MUST end up on the SAME branch name (deploy invariant), so
  // pick ONE shared target that is free across all eligible submodules.
  const eligible = [];
  for (const sm of CORE_SUBMODULES) {
    const smPath = path.join(worktree, 'services', sm);
    if (!fs.existsSync(path.join(smPath, '.git'))) continue;
    try {
      const current = execSync('git rev-parse --abbrev-ref HEAD', { cwd: smPath, stdio: ['ignore', 'pipe', 'ignore'] })
        .toString().trim();
      if (current === tempBranch) eligible.push({ sm, smPath });
    } catch { /* skip */ }
  }
  if (!eligible.length) return;

  // Shared target: first name free in EVERY eligible submodule.
  let target = newBranch;
  for (let n = 2; eligible.some(e => branchExists(e.smPath, target)); n++) {
    if (n > 20) { target = null; break; }
    target = `${newBranch}_${n}`;
  }
  if (!target) return; // could not find a name free in all — keep temp branches

  const renamed = [];
  for (const { sm, smPath } of eligible) {
    try {
      execSync(`git branch -m ${shq(target)}`, { cwd: smPath, stdio: 'ignore' });
      renamed.push(`services/${sm}`);
    } catch { /* leave temp branch in place */ }
  }

  if (renamed.length) {
    console.log(`[worktree-branch-rename] feature branch set: ${target} (${renamed.join(', ')})`);
  }
}

/** Derive a kebab-case slug from free-text prompt (VN + EN). */
function deriveSlug(text) {
  const ascii = String(text)
    .replace(/^\s*\/\S+\s*/, '') // drop a leading /slash-command token
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '') // strip combining diacritics
    .replace(/đ/g, 'd').replace(/Đ/g, 'D')
    .toLowerCase();
  const words = ascii
    .replace(/[^a-z0-9]+/g, ' ')
    .split(/\s+/)
    .filter(w => w.length >= 3 && !STOPWORDS.has(w));
  const picked = words.slice(0, MAX_SLUG_WORDS).join('-').slice(0, MAX_SLUG_LEN).replace(/-+$/, '');
  return picked || null;
}

function branchExists(cwd, branch) {
  try {
    execSync(`git show-ref --verify --quiet ${shq('refs/heads/' + branch)}`, { cwd, stdio: 'ignore' });
    return true;
  } catch {
    return false;
  }
}

function shq(s) {
  return `'${String(s).replace(/'/g, `'\\''`)}'`;
}

try {
  main();
} catch (err) {
  console.error(`[worktree-branch-rename] ${err.message}`);
}
process.exit(0);
