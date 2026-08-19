---
title: "Community-only runtime cutover"
description: "Stop using the commercial hanhchinhso template at runtime and validate the existing license-clean HCS Community solution as the Kubernetes target."
status: completed
priority: P1
effort: 2-4d
branch: local
tags: [abp, community, kubernetes, sso, migration]
blockedBy: []
blocks: [260808-abp-blazor-wasm-workspace-login]
created: 2026-08-08
---

# Community-only runtime cutover

## Decision

`services/abp-blazor` is an ABP Commercial template, not a partly-free runtime:
it still references Account Pro, Identity Pro, OpenIddict Pro and commercial
Language Management. Removing package references in place would break its module
graph and does not produce an equivalent Community application.

The implementation target is `services/HCS_web_free_license`, the existing
Community 10.6 microservice replacement. It uses only `nuget.org`, has a
commercial-dependency audit, and provides the supported Kubernetes entry point
`scripts/k8s-up.sh --kind`.

## Acceptance criteria

- No runtime deployment uses images built from `services/abp-blazor`.
- `services/HCS_web_free_license/scripts/audit-license-clean.sh` passes.
- Restore/build/test of `HCS.slnx` passes without commercial feeds or packages.
- The Community Kubernetes chart renders, deploys using only supplied runtime
  secrets/certificates, and application logs contain no `ABP-LIC-` failure.
- The former commercial Helm release is explicitly documented as retired for
  this local lab; it is not patched with a license code.

## Phases

| # | Phase | Status |
|---|---|---|
| 1 | Inventory and select the Community runtime | Completed |
| 2 | Validate Community dependency/build boundary | Completed |
| 3 | Prepare Community Kubernetes runtime configuration | Completed |
| 4 | Deploy and smoke-test SSO/workspace | Completed (UI route; BFF ingress requires HTTPS host) |
| 5 | Retire commercial local release and update handoff | Completed |

## Evidence

- The commercial runtime logs name `Volo.Abp.Account.Pro.Public.Web.Impersonation`
  and `Volo.Abp.LanguageManagement.Domain` before exiting with `ABP-LIC-0020`.
- The commercial solution contains 35 direct `*.Pro` package references and 31
  `Abp*Pro` module dependencies.
- The Community license audit passed on 2026-08-08.

## Risks

- The Community runtime needs its own `.env` runtime secrets, trusted TLS
  certificate, and Keycloak client; no credential from the commercial template
  will be copied into source.
- The Community solution is still a migration in progress, so feature parity
  is governed by `260803-hcs-community-microservice`, not assumed from the
  commercial template.
