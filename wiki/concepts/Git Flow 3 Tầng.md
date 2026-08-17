---
type: concept
title: "Git Flow 3 Tầng"
created: 2026-06-27
updated: 2026-06-27
tags:
  - git
  - devops
  - workflow
status: mature
complexity: intermediate
related:
  - "[[CI/CD Pipeline]]"
  - "[[Submodule Worktree Split]]"
sources:
  - "[[workspace-architecture.md]]"
---

# Git Flow 3 Tầng

## Vòng đời 1 Feature

```
origin/main → claude_feat/<slug> → test (QA) → staging (chờ release) → main (release)
```

## 5 Luật Vàng

1. **main = chân lý.** Feat LUÔN cắt từ `origin/main` mới nhất. Không từ local main cũ, không từ feat khác.
2. **Sync main TRƯỚC merge.** Trước khi đưa feat lên test/staging/main: `git merge origin/main` vào feat → resolve conflict TRONG feat. Dùng script: `.claude/scripts/git-merge-gate.sh <ui|api> <test|staging|main> "msg"`
3. **test/staging = khu tích lũy, KHÔNG reset tự động.** Được phép chứa thứ main chưa có. Reset CHỈ khi user yêu cầu rõ + đã backup tag.
4. **KHÔNG `--force` lên main BAO GIỜ.** main chỉ nhận thêm (fast-forward/merge).
5. **Backup tag trước mọi reset/force-push.** `gh api repos/<org>/<repo>/git/refs -f ref="refs/tags/backup-<branch>-<date>" -f sha="<sha>"`

## Branch Naming

| Agent | Feature | Bug fix |
|-------|---------|---------|
| Claude | `claude_feat/<slug>` | `claude_fix/<slug>` |
| Codex | `codex_feat/<slug>` | `codex_fix/<slug>` |

## Đồng bộ Branch Đa Service

Sửa nhiều service → **workspace root + tất cả services PHẢI cùng tên branch**:

```bash
BRANCH=claude_feat/my-feature
cd services/ui  && git checkout -B "$BRANCH" origin/main
cd ../api       && git checkout -B "$BRANCH" origin/main
cd ../..        && git checkout -B "$BRANCH" origin/main
```

## Quy trình Merge vào Test/Main

```bash
# UI
cd services/ui
git fetch origin main test --prune && git merge origin/main  # sync main vào feat trước
git checkout test && git pull origin test
git merge --squash "$CURRENT_BRANCH"
git commit -m "[WEB] feat(<scope>): <mô tả>"  # PREFIX BẮT BUỘC
git push origin test

# Sau khi user confirm staging OK → merge main tương tự
```

## Release

```bash
.claude/scripts/git-release.sh [--yes]
# → tag vYYYYMMDD-n trên ui+api, ghim submodule ref ở meta
```

## Hard Stops

- Đang ở `main`, detached HEAD, branch không có prefix `claude_`/`codex_` → DỪNG, tạo branch đúng
- Target có commit source chưa có → DỪNG báo user (`git log origin/main..<branch>`)
- Lỡ code trên branch sai → cherry-pick sang branch đúng
