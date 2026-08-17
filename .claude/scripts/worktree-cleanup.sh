#!/usr/bin/env bash
# Remove app-created worktrees cleanly, including the CORE submodule worktrees
# (services/ui, services/api) that worktree-launch-config.cjs created inside
# them. `git worktree remove` on the meta repo does NOT touch submodule
# worktrees, so without this they leak as dangling registrations under
# .git/modules/services/<sm>/worktrees.
#
# Usage:
#   worktree-cleanup.sh <name>        # remove one worktree (.claude/worktrees/<name>)
#   worktree-cleanup.sh all           # remove every worktree under .claude/worktrees
#   worktree-cleanup.sh --list        # show worktrees + submodule worktrees, do nothing
#
# Always safe to re-run; prunes stale registrations at the end.
set -euo pipefail

# Workspace root = two levels up from this script (.claude/scripts/ -> root).
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
WT_DIR="$ROOT/.claude/worktrees"
CORE_SUBMODULES=(ui api)

usage() { sed -n '2,12p' "${BASH_SOURCE[0]}"; exit 1; }

prune_all() {
  git -C "$ROOT" worktree prune || true
  for sm in "${CORE_SUBMODULES[@]}"; do
    [ -d "$ROOT/services/$sm/.git" ] || [ -e "$ROOT/services/$sm/.git" ] && \
      git -C "$ROOT/services/$sm" worktree prune || true
  done
}

list_all() {
  echo "=== meta worktrees ==="
  git -C "$ROOT" worktree list
  for sm in "${CORE_SUBMODULES[@]}"; do
    echo "=== services/$sm worktrees ==="
    git -C "$ROOT/services/$sm" worktree list 2>/dev/null || echo "(none)"
  done
}

remove_one() {
  local name="$1"
  # Guard against path traversal — only a plain worktree name is allowed.
  case "$name" in
    */*|..*) echo "❌ invalid worktree name '$name'"; exit 1 ;;
  esac
  local wt="$WT_DIR/$name"
  echo "→ Removing worktree '$name'"

  # 1) Remove the submodule worktrees living inside this meta worktree first.
  for sm in "${CORE_SUBMODULES[@]}"; do
    local smwt="$wt/services/$sm"
    if [ -e "$smwt/.git" ]; then
      echo "  - services/$sm worktree"
      git -C "$ROOT/services/$sm" worktree remove --force "$smwt" 2>/dev/null \
        || rm -rf "$smwt"
    fi
  done

  # 2) Remove the meta worktree itself.
  if git -C "$ROOT" worktree list --porcelain | grep -qF "worktree $wt"; then
    git -C "$ROOT" worktree remove --force "$wt" 2>/dev/null || rm -rf "$wt"
  elif [ -d "$wt" ]; then
    rm -rf "$wt"
  fi
}

main() {
  [ $# -eq 1 ] || usage
  case "$1" in
    --list|-l) list_all; exit 0 ;;
    --help|-h) usage ;;
    all)
      if [ -d "$WT_DIR" ]; then
        for d in "$WT_DIR"/*/; do
          [ -d "$d" ] || continue
          remove_one "$(basename "$d")"
        done
      fi
      ;;
    *) remove_one "$1" ;;
  esac
  prune_all
  echo "✓ cleanup done"
}

main "$@"
