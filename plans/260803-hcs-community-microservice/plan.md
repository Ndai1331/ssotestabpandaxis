---
title: "HCS Community Microservice Migration"
description: "Migrate the licensed HCS layered application to a license-clean ABP Community 10.6 microservice solution."
status: in-progress
priority: P1
effort: 20-28w
branch: local
tags: [feature, abp, microservice, auth, migration, community]
blockedBy: [blazorise-organization-license-before-production]
blocks: []
created: 2026-08-03
---

# HCS Community Microservice Migration

## Overview

Build `services/HCS_web_free_license` as a custom ABP Community 10.6/.NET 10 microservice system. Treat `HCS_web_with_license` as read-only input and `abp-blazor` as a behavioral reference only.

## Locked decisions

- Namespace: `HCS.*`; single tenant; Keycloak is the identity source of truth.
- Apps: AuthServer, WebGateway, Blazor host/client.
- Services: Platform, Organization, Document, WorkManagement, Collaboration.
- Keep all custom business features, including Chat/Chat1 and signing.
- Rebuild dynamic Language Management and Audit Viewer on OSS infrastructure.
- Keep OpenIddict core/configuration; omit its runtime administration UI.
- Remove SaaS, GDPR, Text Templates, File Management, Forms, Pro modules and commercial feeds.
- Create fresh databases/migrations and import business data idempotently.

## Phases

| Phase | Name | Status |
|---|---|---|
| 1 | Community baseline and license boundary | Completed |
| 2 | Platform, security and Keycloak SSO | Completed |
| 3 | Business services, MinIO, signing and Chat | In progress |
| 4 | Blazor parity and data importer | In progress |
| 5 | Integration verification and local handoff | Pending |

## Kubernetes single-command handoff

- Deliver a Helm chart inside `services/HCS_web_free_license/deploy/helm/hcs-community` for all HCS application hosts and local infrastructure.
- Provide `scripts/k8s-up.sh` that builds the eight local images, loads them into a Kind cluster, installs/upgrades the chart and runs the database migrator job.
- Keep all credential material out of the chart defaults. The script reads an untracked `.env` and applies it as a Kubernetes Secret.
- Document first-run prerequisites, one command, status/logs, and subsequent-run commands in the service README.
- Verify Helm rendering/lint and shell syntax. Live cluster verification requires Docker, Kind, Helm and runtime credentials.

## Acceptance gates

- Restore/build/test uses nuget.org only and contains no commercial package references or secrets.
- Keycloak app gate, group-role mapping and idempotent first-login provisioning pass E2E tests.
- Custom menu/routes and business APIs retain parity except explicitly removed modules.
- Every service owns its schema; cross-service work uses contracts/events, never cross-database queries.
- Data import produces reconciliation reports and never mutates the source database.

## Current release gates

- LeptonX Lite resolves `Blazorise.Licensing`; production requires documented organization/commercial eligibility or replacement with an approved OSS UI stack. See `services/HCS_web_free_license/docs/dependency-license-decisions.md`.
- `Bnn.SignLib`/`Bnn.Sdk` cannot ship until redistribution rights are verified; signing integrations remain behind adapter boundaries meanwhile.
- Document uploads currently use a request-level DB transaction while staging a blob. Before production, scope this transaction to the database mutation/audit phase so a slow 50 MiB MinIO upload cannot pin a database connection.
- Live Keycloak/RabbitMQ/Redis/MinIO E2E and source-data import dry run remain pending local runtime credentials; the licensed source/database remains untouched.

## Related plans

- Supersedes the HCS domain direction in `260724-1555-hcs-layered-to-microservice`; reusable implementations there remain reference material only.
- Builds on the completed BD Keycloak SSO plans.
