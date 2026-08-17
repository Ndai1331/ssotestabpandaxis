#!/usr/bin/env node
/**
 * Preflight Check - UserPromptSubmit Hook
 *
 * Scans the user prompt for dangerous git/db/deploy phrases. On match, injects a
 * WARNING block + a short safety checklist + relevant memory index lines so Claude
 * re-reads its own constraints BEFORE acting. Never blocks — always exits 0.
 *
 * Part of the Task9 Self-Learning System (Tier 2 — Reinforce).
 */

try {
  const fs = require('fs');
  const path = require('path');

  // Danger phrases (case-insensitive). Keep exact-ish to avoid false positives.
  const DANGERS = [
    'merge main', 'merge test', 'merge staging',
    'push origin main', 'push origin test', 'push origin staging',
    'force push', 'push --force', 'push -f',
    'alter table', 'drop table', 'drop database', 'truncate',
    'docker push', 'buildx build',
    'git reset --hard', 'git clean -f',
    'reset test', 'reset staging',
  ];

  const stdin = fs.readFileSync(0, 'utf-8').trim();
  if (!stdin) process.exit(0);

  let prompt = '';
  try {
    prompt = String(JSON.parse(stdin).prompt || '');
  } catch (_) {
    prompt = stdin; // fail-open: treat raw stdin as text
  }
  const lower = prompt.toLowerCase();

  const matched = DANGERS.filter((kw) => lower.includes(kw));
  if (matched.length === 0) process.exit(0);

  const out = [];
  out.push('⚠️  PREFLIGHT WARNING — detected: ' + matched.join(', '));
  out.push('');
  out.push('Safety checklist (per CLAUDE.md) before proceeding:');
  out.push('  □ Branch đúng prefix claude_/codex_, cắt từ origin/main mới nhất?');
  out.push('  □ Đã merge origin/main vào feat, resolve conflict TRONG feat?');
  out.push('  □ Commit deploy có prefix [WEB]/[API] và ĐÚNG target branch?');
  out.push('  □ DB target đã confirm (qcadmin vs seo_data)? Không DDL lên prod?');
  out.push('  □ KHÔNG --force main; đã backup tag trước reset test/staging?');
  out.push('  □ User đã nói "deploy"? (không có = KHÔNG kích build/merge)');

  // Surface memory index (one-liner hooks) if available for this project.
  const projectDir = process.env.CLAUDE_PROJECT_DIR || process.cwd();
  const slug = projectDir.replace(/\//g, '-');
  const memIndex = path.join(
    process.env.HOME || '',
    '.claude', 'projects', slug, 'memory', 'MEMORY.md'
  );
  try {
    const lines = fs.readFileSync(memIndex, 'utf-8')
      .split('\n')
      .filter((l) => l.trim().startsWith('- ['));
    if (lines.length) {
      out.push('');
      out.push('📋 Relevant memories:');
      lines.slice(0, 8).forEach((l) => out.push('  ' + l.trim()));
    }
  } catch (_) { /* memory index absent — skip silently */ }

  console.log(out.join('\n'));
  process.exit(0);
} catch (e) {
  process.exit(0); // fail-open: never block a prompt
}
