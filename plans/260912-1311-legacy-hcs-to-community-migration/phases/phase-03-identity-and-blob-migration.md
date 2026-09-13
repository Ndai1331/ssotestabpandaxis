---
title: Identity and blob migration
phase: 3
status: planned
updated: 2026-09-12
---

# Phase 3 — Identity and blob migration

## Context links

- [Migration plan](../plan.md)
- [Importer README](../../../services/HCS_web_free_license/tools/HCS.MigrationImporter/README.md)
- [Keycloak local setup](../../../docs/workspace-architecture.md)
- [Community environment example](../../../services/HCS_web_free_license/.env.example)

## Overview

Separate identity and object storage from business-row migration. The legacy `AbpUsers` table is not a password source and the old OpenIddict records are not a replacement for Keycloak.

## Identity workflow

```text
verified Keycloak export
       |
       v
normalized email/username match against AxisHCS.AbpUsers
       |
       +--> exact match: source user ID -> Keycloak/target user ID
       +--> missing/ambiguous: quarantine and report
       +--> duplicate: business decision required
```

1. Export the current Keycloak users in a controlled, reviewable JSON file.
2. Normalize email and username using the importer’s documented matching rules.
3. Produce an immutable mapping report with exact, ambiguous, missing, and unmatched counts.
4. Remap every user-bearing column (`UserId`, `CreatorId`, `AssigneeId`, `OwnerId`, `IdentityUserId`, and equivalents).
5. Decide separately whether legacy business roles/permissions can be mapped to current Free permissions. Do not import old OpenIddict applications, authorizations, tokens, or client secrets.
6. Preserve only non-sensitive profile/business metadata approved for the target platform.

## Security rules

- Never migrate passwords, password hashes, refresh/access tokens, signing private keys, PINs, provider secrets, or client secrets.
- Imported signing settings contain configuration/metadata only. Credentials must be re-provisioned using the target encrypted write-only flow.
- Keycloak remains the identity authority; target databases store only the identifiers required by the current Community contracts.
- Store mapping/export files outside the repository and delete temporary copies after review according to the team’s retention policy.

## Blob workflow

The dump contains rows such as `AppDocumentFiles`, `AppUserSignatures`, and `AppSurveyFiles`, but not the binary object-store contents. The plan therefore requires:

1. Identify the source MinIO/S3 endpoint, bucket, path convention, and credentials without committing them.
2. Extract and normalize referenced paths from the source rows.
3. Compare row references with source object existence.
4. Copy objects to the target bucket using a resumable, checksum-verifying process.
5. Map legacy paths to target `BlobName`/file-reference fields.
6. Verify SHA-256, content type, size, and count after copy.
7. Quarantine missing or unreadable objects; do not fabricate files.

## Success criteria

- Every migrated user reference resolves to one approved target identity.
- No secret-bearing source field is written to a target table or report.
- Every imported blob has a source reference, target reference, size/hash verification, and status.
- Missing/ambiguous identity and blob records are actionable before execute mode.
