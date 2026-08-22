# HCS CI Docker Build Fix

## Status

- Diagnosis: completed
- Implementation: completed
- Scope: HCS Community source tracking and Docker Hub workflow

## Root Cause

The repository-wide `**/data/` ignore rule matches source directories named `Data/` on this checkout. Those directories exist locally but are absent from Git, so GitHub Actions checks out incomplete C# projects and reports missing namespaces/types. The auth workflow also installed the nonexistent npm package `@abp/cli`, and its first replacement still ran Yarn without ignoring a dependency engine constraint (`select2@4.1.0` requires Node `>=24`).

## Changes

1. Add narrow `.gitignore` exceptions for HCS source `Data/` directories, including EF migrations and DbContext factories.
2. Replace the invalid npm ABP CLI install with the pinned .NET global tool already used locally.
3. Apply `YARN_IGNORE_ENGINES=1` to both Yarn and ABP asset installation commands.
4. Add a workflow source-tree guard so missing ignored source fails with a direct error before Docker publish.
5. Validate clean Release publishes for the affected project matrix and inspect GitHub trigger/push prerequisites.

## Validation

- `git check-ignore` no longer matches HCS source `Data/` files.
- All HCS source `Data/` files are visible to Git.
- `dotnet publish` succeeds for DbMigrator, AuthServer, WebGateway, Platform, Organization, Document, WorkManagement, and Collaboration with the CI project paths.
- Blazor publish is blocked only on this macOS arm64 host by `ComputeWasmBuildAssets` task-host errors (`MSB4216/MSB4027`); the existing Ubuntu CI runner is the authoritative Blazor check.
- Local reproduction confirms `yarn install --frozen-lockfile` fails on the dependency engine constraint, while `YARN_IGNORE_ENGINES=1 yarn install --frozen-lockfile` and `YARN_IGNORE_ENGINES=1 abp install-libs` pass.
- Run #2 confirmed all non-AuthServer matrix jobs pass; AuthServer failed only in the static-assets step before Docker publish.
- Run #3 (`32567324363`) completed successfully with all 9 matrix jobs passing.
- Docker Hub verification found all 9 expected tags under `longnguyen1331/hanhchinhso`.
- GitHub CLI remains unauthenticated locally, but public run metadata and Docker Hub tags verified the remote result.

## Handoff

- Commits `f0b62727` and `e39444d4` are pushed to `main`.
- Run #3 and all nine Docker Hub tags are verified; no further action is required for this fix.

## Risks

- Existing unrelated dirty files must not be staged or committed.
- Local Docker build may require network access for NuGet/npm and should be reported separately from source errors.
