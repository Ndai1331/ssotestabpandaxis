---
title: "HCS CI/CD selective Docker publish by commit prefix"
description: "Select HCS Docker images from commit prefixes while preserving explicit manual dispatch and safe dynamic matrix behavior."
status: pending
priority: P1
effort: 4-6h
branch: main
tags: [infra, ci, docker, github-actions, hcs]
blockedBy: []
blocks: []
created: 2026-08-23
---

# HCS CI/CD selective Docker publish by commit prefix

## Overview

Update only `.github/workflows/hcs-docker-publish.yml` so a push to `main`/`master` publishes only the HCS image selected by commit-message prefix:

| Commit prefix | Matrix selection |
|---|---|
| `[blazor]` | `blazor` |
| `[auth-server]`, `[web-gateway]`, etc. | Matching service only |
| `[all]` | All nine current services |
| No recognized prefix | No Docker build/push |

Service names remain the existing image tags: `db-migrator`, `auth-server`, `web-gateway`, `blazor`, `platform`, `organization`, `document`, `work-management`, `collaboration`.

The implementation remains workflow-local (no new action/script/config file) and preserves the existing Docker build, ABP asset, source-tree guard, cache, and registry behavior for selected services.

## Existing context and scope guard

- Read: `README.md`, `CLAUDE.md`, `docs/workspace-architecture.md`, `.claude/rules/development-rules.md`, and the target workflow.
- `docs/development-rules.md` does not exist in this checkout; `.claude/rules/development-rules.md` is the applicable development-rules file.
- Workspace documents describe local-first operation and no established CI/CD contract, while the target workflow already publishes HCS Community images on `main`/`master`.
- Existing worktree changes are extensive, including HCS source/assets/docs and a modified historical plan. They are user-owned and must not be reset, staged, reformatted, or included in this work.
- This plan owns the target workflow only. No implementation is included in this plan turn.
- YAGNI/KISS/DRY: one preparation decision point, one canonical service metadata list, one existing build job body, no per-service workflows.

## Proposed behavior contract

### Push event

1. Read every commit in the push payload/range, not only `github.sha`'s subject.
2. Recognize a prefix only when it starts at character 1 of the commit message and exactly matches `[` + canonical service name or `all` + `]`.
3. Use the set union of recognized service prefixes across the push. Duplicate prefixes produce one matrix entry.
4. `[all]` dominates all service-specific prefixes.
5. Ignore malformed/unknown prefixes (`[Blazor]`, `[blazor-ui]`, `[foo]`, text before `[blazor]`); do not silently treat them as `all`.
6. If the resulting set is empty, emit a successful no-op selection and do not execute checkout, Docker login, asset installation, source verification, or `docker/build-push-action`.

The multi-commit union is deliberate: a single push can contain several commits and using only the tip would miss an earlier requested service. It can build an image even when that service was not the only changed project in the range; this is the direct interpretation of the requested commit contract.

### Manual `workflow_dispatch`

Add a typed `service` choice input containing `all` and each canonical service. The default is `all` to preserve today's manual behavior, which always publishes the full matrix. Manual runs therefore use explicit operator intent rather than a commit message. The preparation step must still validate the raw input defensively, because REST/API callers can bypass assumptions made by the UI.

- Valid input: select the requested service(s) according to the chosen input contract; `all` selects all.
- Invalid input: fail in preparation with a clear error before Docker login/push; never fall back to all.
- The plan assumes one service or `all` per dispatch. If operators need multiple selected services, add a comma-separated input only after confirming the need; do not introduce it speculatively.

## Architecture and data flow

```text
push webhook / workflow_dispatch input
              |
              v
prepare (ubuntu, no secrets)
  - canonical service metadata
  - normalize event mode
  - parse prefixes / validate input
  - select service records
  - emit matrix JSON + selection summary
              |
              v
build-push (dynamic matrix, fail-fast: false)
  - checkout selected source
  - verify source tree
  - Buildx + Docker Hub login
  - install/verify ABP libs only where required
  - build and push selected tag
```

Data entering `prepare`: `github.event_name`, push commit messages (and, if needed, before/after SHAs), or `inputs.service` for manual dispatch. Data transformed: normalized prefix set and selected records from the canonical nine-record service metadata. Data exiting `prepare`: JSON matrix, `has_work`, and a human-readable selection summary through `$GITHUB_OUTPUT`. The build job consumes only that JSON and existing matrix fields; secrets never enter the selection output.

Keep all current fields (`project`, `app_dll`, `publish_properties`, `abp_libs`, `abp_libs_path`, `install_libreoffice`) in the single canonical metadata structure. Do not duplicate the nine records in both a static matrix and selection logic.

### Dynamic matrix and empty selection

Use the supported job-output → `fromJSON(needs.prepare.outputs.matrix)` pattern. Do not emit an empty matrix. GitHub Actions can fail while expanding an empty dynamic matrix; emit a one-record sentinel such as `service: __none__`, `selected: false` when there is no recognized push prefix. Guard every build-side step with the selection flag (or route the sentinel to an explicit no-op step). The no-op must be observable in logs and must never invoke a Docker action or secret-consuming login.

The sentinel is a reliability guard, not an image definition: it must not have a project path, Docker tag, or permissive defaults that could accidentally cause a publish.

### Commit parsing safety

Pass event data to the shell through environment variables, not direct unquoted interpolation into shell code. Use a JSON parser available on the Ubuntu runner (prefer `jq`) and write outputs via `$GITHUB_OUTPUT`. Treat commit messages as untrusted data: do not `eval`, construct shell source from messages, or interpolate a message into a command. Log only normalized selected service names, not full commit messages.

For push handling, prefer the webhook's commit list because it is already event-scoped and avoids an unnecessary checkout in `prepare`. If implementation chooses `git log` to cover a range, use `actions/checkout` with an explicit fetch strategy and handle the all-zero `before` SHA/force-push case; document and test that fallback. Do not silently inspect unrelated repository history.

## Dependency graph and execution order

```text
Workflow syntax + event contract
              |
              v
Canonical service metadata + parser contract
              |
              v
prepare outputs (matrix/has_work/summary)
              |
              v
dynamic build matrix and guarded existing steps
              |
              v
static validation -> simulated event matrix -> optional GitHub dry run
```

No existing plan blocks this work. `plans/260822-1647-hcs-ci-docker-build-fix/plan.md` is marked completed in its own content and is historical context only; this plan does not modify it. Runtime HCS services and application code are not dependencies for the selection logic, but their current matrix paths are contract inputs and must remain unchanged.

## Implementation phases and file ownership

### Phase 1 — Workflow selection and dispatch contract (2-3h)

Exclusive ownership: `.github/workflows/hcs-docker-publish.yml`.

1. Add the `workflow_dispatch.inputs.service` choice/default contract.
2. Move/retain the current nine service records as one canonical metadata source consumed by preparation and dynamic matrix creation.
3. Add `prepare` job to parse push commits or validated dispatch input.
4. Implement exact prefix matching, union across multiple push commits, `[all]` precedence, unknown-prefix ignore, and clear no-op summary.
5. Emit a non-empty matrix JSON with a safe sentinel for no-work cases.
6. Make `build-push` depend on preparation and retain the existing build steps/arguments/cache scopes, guarded so the sentinel performs no build/push.
7. Keep `fail-fast: false`; consider branch-level concurrency with `cancel-in-progress: true` only if the repository wants newer pushes to supersede stale image tags. If added, document that a cancelled run can leave a partial set and the newer run is authoritative.

### Phase 2 — Static and behavioral validation (1-2h)

Exclusive ownership: no production file. Tests are commands/temporary fixtures unless the implementer gets explicit approval to add a checked-in test harness.

1. Parse/lint the workflow with `actionlint`; use a YAML parser only as a secondary syntax check because generic YAML 1.1 parsers may interpret the GitHub `on` key unexpectedly.
2. Validate expressions and output shape, especially `needs.prepare.outputs.matrix` → `fromJSON` and all optional matrix fields.
3. Run a shell-level selection harness against representative push/dispatch JSON without Docker login or publish.
4. Review `git diff -- .github/workflows/hcs-docker-publish.yml` and confirm all unrelated pre-existing changes remain untouched.
5. If repository access is available, manually dispatch a non-publishing validation branch or use a controlled run; never use real Docker Hub credentials in a local test.

## Test matrix

| Case | Event/input | Expected selection | Expected publish |
|---|---|---|---|
| T1 | push, one `[blazor] ...` commit | `{blazor}` | exactly `:blazor` |
| T2 | push, one `[auth-server] ...` commit | `{auth-server}` | exactly `:auth-server` |
| T3 | push, commits `[blazor]`, `[document]` | `{blazor, document}` | exactly two tags |
| T4 | push, `[document]`, then `[all]` | all nine | nine tags |
| T5 | push, `[all]`, then unknown | all nine | nine tags |
| T6 | push, no prefix | sentinel/no-op | zero Docker builds/pushes |
| T7 | push, `[Blazor]` / `[blazor-ui]` / text before prefix | sentinel/no-op | zero |
| T8 | push, duplicate `[blazor]` commits | one `blazor` record | one tag, not duplicate jobs |
| T9 | dispatch, default input | all nine | backward-compatible full publish |
| T10 | dispatch, `blazor` | `{blazor}` | exactly `:blazor` |
| T11 | dispatch, `all` | all nine | nine tags |
| T12 | dispatch/API invalid value | preparation fails | zero login/push |
| T13 | selected `auth-server` | `abp_libs=true`, correct asset path | asset install + publish |
| T14 | selected `blazor` | compression property preserved | correct build arg |
| T15 | selected `document` | `install_libreoffice=true` preserved | LibreOffice path unchanged |
| T16 | no-work sentinel | `selected=false`, no project/tag | no Docker action and no secret use |

Static assertions should also verify:

- trigger branch/path filters remain `main`, `master`, HCS source, and this workflow;
- all nine canonical service records and image tags are present exactly once;
- no unconditional `push: true` step can run for the sentinel;
- no `github.event.commits[0]`/tip-only logic is introduced;
- no secret is referenced by `prepare`;
- YAML expressions are not embedded in shell as unquoted commit text.

## Risk assessment

| Risk | Likelihood | Impact | Mitigation |
|---|---:|---:|---|
| Empty dynamic matrix makes workflow invalid/fails before jobs run | High | High | Always emit a safe sentinel record; test no-prefix event before any real run. |
| Only tip commit is inspected, missing earlier requested service | High | High | Iterate the complete push commit list/range; T3/T4/T5 tests. |
| Unknown prefix accidentally means all | Medium | High | Exact allowlist; invalid push prefixes become no-op; invalid dispatch fails. |
| Manual dispatch loses existing full-publish behavior | Medium | Medium | Typed input default `all`; T9 regression test and document contract. |
| Dynamic matrix metadata diverges from current Docker args | Medium | High | One canonical record source; T13-T15 field-preservation checks. |
| Shell injection or malformed commit JSON | Low | High | Environment transport, `jq`, no `eval`, quote output, log only normalized names. |
| Older run overwrites newer service tag | Medium | Medium | Consider branch concurrency; at minimum document last-writer behavior and verify run SHA in logs. |
| Workflow trigger runs for workflow-only changes and no-prefix no-ops | Low | Low | Keep existing path filter; no-op is intentional and visible. |
| Required check semantics become confusing when no-op/sentinel is used | Medium | Medium | Give prepare/build jobs stable names and document no-op success; inspect branch protection if configured. |
| GitHub expression/YAML parser behavior differs from local lint | Medium | High | `actionlint` plus a controlled workflow run; do not rely on generic parser alone. |

## Backwards compatibility and migration

- No data migration, image rename, Dockerfile change, service path change, secret rename, or registry change.
- Existing manual users retain full publish by accepting the default `all` input.
- Existing pushes with a recognized prefix gain selective behavior; pushes without a recognized prefix stop publishing by design.
- Existing downstream deployments continue consuming the same nine `longnguyen1331/hanhchinhso:<service-name>` tags. A selective run can leave other tags at their previous revision; operators must use `[all]` for a coherent release set.
- Existing ABP asset installation, source-tree verification, LibreOffice flag, Blazor compression property, and GHA cache scope stay attached to the same service records.
- No migration flag is required. Roll out first on a branch/controlled manual run if repository policy allows, then merge to the default branch.

## Rollback plan

1. Revert only the workflow commit (or restore the previous workflow version from Git history); do not reset the worktree.
2. Disable the workflow in GitHub Actions if the YAML is rejected or publishes unexpectedly.
3. Existing Docker tags are not deleted. Reverting restores future full-matrix publishing but cannot undo an already-pushed tag; republish the intended service or `[all]` from the corrected commit.
4. If a partial matrix run fails, use `workflow_dispatch` with the affected service or `all` after fixing the cause. `fail-fast: false` keeps independent selected services observable.

## Measurable success criteria

- `actionlint` and the repository-approved YAML/static checks pass with zero errors.
- T1-T16 selection and guard assertions pass; T3 proves union across multi-commit pushes.
- For a controlled run, the job summary/log states the selected service set and the number of matrix entries.
- A `[blazor]` push produces one successful Docker publish tagged `blazor` and no other image publish.
- An `[all]` push produces nine publishes with the existing tags/args.
- A no-prefix or invalid-prefix push completes without Docker login/build/push.
- Manual default dispatch still selects all nine; explicit service dispatch selects only that service.
- `git diff` shows only the intended workflow change plus this new plan file; no unrelated user change is staged or overwritten.

## Unresolved questions

1. Should a manual `workflow_dispatch` with no explicit input remain backward-compatible full publish (`all`, recommended), or must “no prefix means no build” also imply a default manual no-op? This plan assumes explicit manual input is a separate contract and keeps `all` as the default.
2. For a push containing both valid service prefixes and an unknown prefix, should unknown prefixes be ignored (recommended) or fail the workflow to force commit-message correction? This plan ignores them and logs the recognized set.
3. Should concurrency cancel an older in-progress push on the same branch to reduce stale tag overwrites? This plan treats it as a deployment-policy decision, not a prerequisite for prefix selection.

## References

- [GitHub: Triggering a workflow](https://docs.github.com/en/actions/how-tos/write-workflows/choose-when-workflows-run/trigger-a-workflow) — `workflow_dispatch` inputs and event context.
- [GitHub: Running variations of jobs](https://docs.github.com/en/actions/how-tos/write-workflows/choose-what-workflows-do/run-job-variations) — matrix configuration and context-driven matrices.
- [GitHub: Workflow syntax](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax) — matrix limits, `include`, and `fail-fast`.
- [GitHub: Expressions](https://docs.github.com/en/actions/concepts/workflows-and-actions/expressions) — safe expression/output design and untrusted input warning.
