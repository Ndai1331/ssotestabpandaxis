#!/usr/bin/env bash
# git-release.sh — pin a fixed release marker after content is on main (prod).
#
# Usage:
#   .claude/scripts/git-release.sh [--yes]
#
# Tags vYYYYMMDD-n on main of services/ui + services/api, pins submodule refs in the
# meta repo. Release = fixed milestone tag, NOT a branch. Does NOT touch test/staging
# (they accumulate unreleased feats; force-resetting them would delete in-flight work).
#
# Run from the workspace root.
set -euo pipefail
YES="${1:-}"
die() { echo "❌ $*" >&2; exit 1; }

[[ -d services/ui && -d services/api ]] || die "run from workspace root (services/ui, services/api missing)"

DATE="$(date +%Y%m%d)"

# next -n for today across both services
next_n() {
  local repo="$1" n=1
  ( cd "$repo" && git fetch origin --tags --quiet 2>/dev/null || true )
  while ( cd "$repo" && git rev-parse "v${DATE}-${n}" >/dev/null 2>&1 ); do n=$((n+1)); done
  echo "$n"
}
N="$(next_n services/ui)"
M="$(next_n services/api)"
N=$(( N > M ? N : M ))          # keep ui/api tag numbers aligned
TAG="v${DATE}-${N}"

echo "▶ release tag: $TAG (ui + api)"
if [[ "$YES" != "--yes" ]]; then
  read -r -p "Tag $TAG on main of ui+api and pin submodules? [y/N] " ans
  [[ "$ans" =~ ^[Yy]$ ]] || die "aborted"
fi

for repo in services/ui services/api; do
  ( cd "$repo"
    git fetch origin main --prune --quiet
    git checkout main && git pull origin main
    git tag -a "$TAG" -m "release $TAG"
    git push origin "$TAG"
    # Do NOT reset test/staging here — they accumulate unreleased feats and
    # force-resetting would delete colleagues' in-flight work. Release = tag only.
  )
  echo "✅ $repo tagged $TAG (test/staging left intact)"
done

# pin submodule refs in meta repo
git add services/ui services/api
git commit -m "chore: pin release $TAG (ui+api)"
git push
echo "✅ meta repo pinned $TAG"
