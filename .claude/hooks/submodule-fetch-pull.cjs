#!/usr/bin/env node
// SessionStart hook: fetch & pull latest origin/main for core submodules (ui, api)
// Skips if inside a worktree (worktree hooks handle branching separately)

const { execSync } = require('child_process');
const path = require('path');
const fs = require('fs');

const projectDir = process.env.CLAUDE_PROJECT_DIR || path.resolve(__dirname, '../..');
const servicesDir = path.join(projectDir, 'services');

// Skip in worktrees — worktree-launch-config.cjs handles those
const isWorktree = projectDir.includes('.claude/worktrees');
if (isWorktree) process.exit(0);

const coreServices = ['ui', 'api'];
const results = [];

for (const svc of coreServices) {
  const svcDir = path.join(servicesDir, svc);
  if (!fs.existsSync(path.join(svcDir, '.git')) && !fs.existsSync(svcDir)) continue;

  try {
    execSync('git fetch origin main --prune', { cwd: svcDir, stdio: 'pipe', timeout: 15000 });
    const currentBranch = execSync('git branch --show-current', { cwd: svcDir, stdio: 'pipe' }).toString().trim();

    // Only auto-pull if on main (don't disturb feature branches)
    if (currentBranch === 'main' || !currentBranch) {
      if (!currentBranch) {
        execSync('git checkout main', { cwd: svcDir, stdio: 'pipe', timeout: 10000 });
      }
      execSync('git pull origin main --ff-only', { cwd: svcDir, stdio: 'pipe', timeout: 15000 });
      results.push(`${svc}: pulled latest main`);
    } else {
      results.push(`${svc}: on branch "${currentBranch}" — fetched only (not on main)`);
    }
  } catch (e) {
    results.push(`${svc}: fetch/pull failed — ${e.message.split('\n')[0]}`);
  }
}

if (results.length > 0) {
  console.log(`## Submodule sync\n${results.map(r => `- ${r}`).join('\n')}`);
}
