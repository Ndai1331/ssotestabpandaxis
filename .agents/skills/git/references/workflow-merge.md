# Merge Workflow

Execute via `git-manager` subagent.

## Variables
- TO_BRANCH: target (defaults to `main`)
- FROM_BRANCH: source (defaults to current branch)

## Step 1: Sync with Remote

**IMPORTANT: Always merge latest `origin/main` to the current feature/fix branch before any deploy or merge to `test/main`.**

For Task9 repos, hard stop unless current branch is `claude_feat/*`, `claude_fix/*`, `codex_feat/*`, or `codex_fix/*`.

```bash
git fetch origin
CURRENT_BRANCH=$(git branch --show-current)
git checkout "$CURRENT_BRANCH"
git merge origin/main
git checkout {TO_BRANCH}
git pull origin {TO_BRANCH}
```

## Step 2: Merge from REMOTE
```bash
git merge origin/{FROM_BRANCH} --no-ff -m "merge: {FROM_BRANCH} into {TO_BRANCH}"
```

**Why `origin/{FROM_BRANCH}`:** Ensures merging only committed+pushed changes, not local WIP.

## Step 3: Resolve Conflicts
If conflicts:
1. Resolve manually
2. `git add . && git commit`
3. If clarifications needed, report to main agent

## Step 4: Push
```bash
git push origin {TO_BRANCH}
```

## Pre-Merge Checklist
- Fetch latest: `git fetch origin`
- Ensure FROM_BRANCH pushed to remote
- Check for conflicts: `git merge --no-commit --no-ff origin/{FROM_BRANCH}` then abort

## Error Handling

| Error | Action |
|-------|--------|
| Merge conflicts | Resolve manually, then commit |
| Branch not found | Verify branch name, ensure pushed |
| Push rejected | `git pull --rebase`, retry |
