# Phase 04 — tests and review

## Goal

Verify behavior, integration boundaries and the pre-existing worktree safety constraints.

## Tasks

- Add pure tests for DOCX placeholder replacement and PDF placeholder matching/overlay where practical.
- Run targeted DocumentService tests, organization tests, gateway tests and client/document builds.
- Run `git diff --check` and `./scripts/audit-license-clean.sh`.
- Use tester/debugger and code-reviewer subagents for independent verification; use project/doc/git review agents for handoff evidence without committing.
- Record unresolved deployment/runtime configuration assumptions in the final report.

## Gate

All relevant gates pass or the final report identifies the exact external/runtime blocker and the safe partial result.

