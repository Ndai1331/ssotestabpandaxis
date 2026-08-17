#!/usr/bin/env bash
# git-merge-gate.sh — gate merges through the 3-tier flow: feat -> test -> staging -> main
#
# Usage:
#   .claude/scripts/git-merge-gate.sh <ui|api> <test|staging|main> "commit msg" [--yes]
#
# Rules enforced:
#   1. main = single source of truth: feat MUST contain latest origin/main before any merge.
#   2. main only accepts content already on staging (no feat -> main shortcut).
#   3. squash-merge current feat into target with the required [WEB]/[API] prefix.
#   4. after main: push origin main:test --force + reset staging to main (test/staging = scratch).
#
# Run from the service repo root (services/ui or services/api).
set -euo pipefail

SERVICE="${1:-}"; TARGET="${2:-}"; MSG="${3:-}"; YES="${4:-}"

die() { echo "❌ $*" >&2; exit 1; }

[[ "$SERVICE" =~ ^(ui|api)$ ]]            || die "service must be ui|api (got '$SERVICE')"
[[ "$TARGET"  =~ ^(test|staging|main)$ ]] || die "target must be test|staging|main (got '$TARGET')"
[[ -n "$MSG" ]]                           || die "commit message required (arg 3)"

case "$SERVICE" in
  ui)  PREFIX="[WEB]" ;;
  api) PREFIX="[API]" ;;
esac
[[ "$MSG" == "$PREFIX"* ]] || MSG="$PREFIX $MSG"

FEAT="$(git branch --show-current)"
[[ "$FEAT" =~ ^(claude|codex)_(feat|fix)/ ]] \
  || die "must run from a feat branch (claude_*/codex_*), current: '$FEAT'"

echo "▶ gate: $SERVICE  $FEAT → $TARGET"

git fetch origin main test staging --prune

# Rule 1 — feat must contain latest origin/main
if ! git merge-base --is-ancestor origin/main "$FEAT"; then
  die "feat '$FEAT' does not contain latest origin/main. Run: git merge origin/main (resolve in feat), then retry."
fi

# Rule 2 — main only from staging
if [[ "$TARGET" == "main" ]]; then
  git rev-parse --verify origin/staging >/dev/null 2>&1 || die "origin/staging missing — promote to staging first"
  if ! git merge-base --is-ancestor "$FEAT" origin/staging; then
    die "feat '$FEAT' is not contained in origin/staging. Promote to staging and verify before main."
  fi
fi

# Rule 2b — NEVER lose code on target. If target has commits the feat doesn't,
# a squash-merge could drop them. Warn loudly and require confirmation.
AHEAD=$(git rev-list --count "$FEAT..origin/$TARGET" 2>/dev/null || echo 0)
if [[ "$AHEAD" -gt 0 ]]; then
  echo "⚠️  origin/$TARGET has $AHEAD commit(s) NOT in feat '$FEAT':"
  git log --oneline "$FEAT..origin/$TARGET" | head -20
  echo "   Merging may LOSE these. Merge origin/$TARGET into feat first, or abort."
  [[ "$YES" == "--yes" ]] && die "refusing --yes auto-merge while target is ahead (would risk losing code)"
fi

if [[ "$YES" != "--yes" ]]; then
  read -r -p "Proceed merging $FEAT → $TARGET on $SERVICE? [y/N] " ans
  [[ "$ans" =~ ^[Yy]$ ]] || die "aborted by user"
fi

git checkout "$TARGET"
git pull origin "$TARGET" 2>/dev/null || echo "ℹ️ $TARGET has no upstream yet (first push)"
git merge --squash "$FEAT"
git commit -m "$MSG"
git push origin "$TARGET"
echo "✅ pushed $SERVICE $TARGET — CI will build the matching tag"

# NEVER auto-reset test/staging here. They are integration tiers that ACCUMULATE
# feats waiting for release and may legitimately contain commits not on main.
# Force-resetting them would DELETE colleagues'\'' unreleased work (the exact mistake
# staging exists to prevent). Reset only on explicit user request, with a backup tag.
# See "LUẬT TỐI THƯỢNG" in CLAUDE.md.

git checkout "$FEAT"
