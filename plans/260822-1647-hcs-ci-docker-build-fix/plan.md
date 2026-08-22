# HCS CI Docker Build Fix

## Status

- Diagnosis: completed
- Implementation: completed
- Scope: HCS Community source tracking and Docker Hub workflow

## Root Cause

The repository-wide `**/data/` ignore rule matches source directories named `Data/` on this checkout. Those directories exist locally but are absent from Git, so GitHub Actions checks out incomplete C# projects and reports missing namespaces/types. The auth workflow also installs the nonexistent npm package `@abp/cli`.

## Changes

1. Add narrow `.gitignore` exceptions for HCS source `Data/` directories, including EF migrations and DbContext factories.
2. Replace the invalid npm ABP CLI install with the pinned .NET global tool already used locally.
3. Add a workflow source-tree guard so missing ignored source fails with a direct error before Docker publish.
4. Validate clean Release publishes for the affected project matrix and inspect GitHub trigger/push prerequisites.

## Validation

- `git check-ignore` no longer matches HCS source `Data/` files.
- All HCS source `Data/` files are visible to Git.
- `dotnet publish` succeeds for DbMigrator, AuthServer, WebGateway, Platform, Organization, Document, WorkManagement, and Collaboration with the CI project paths.
- Blazor publish is blocked only on this macOS arm64 host by `ComputeWasmBuildAssets` task-host errors (`MSB4216/MSB4027`); the existing Ubuntu CI runner is the authoritative Blazor check.
- GitHub CLI is installed but unauthenticated, so remote run logs and Docker Hub secrets remain unverified.

## Handoff

- The staged commit is ready for review and push.
- After push, monitor the `HCS Docker Publish` matrix and confirm all nine Docker Hub tags.

## Risks

- Existing unrelated dirty files must not be staged or committed.
- Local Docker build may require network access for NuGet/npm and should be reported separately from source errors.
