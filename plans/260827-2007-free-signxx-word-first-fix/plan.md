---
title: "Free SignXX Word-first signing fix"
status: completed
priority: P1
branch: local
tags: [hcs, free-license, signing, docx, signxx, visnam, tag]
created: 2026-08-27
---

# Free SignXX Word-first signing fix

## Objective

Preserve the source Word file and make each SignXX step render from a new working
DOCX before converting to PDF. VISNAM and TAG continue to receive the converted
PDF because their provider APIs are PDF-based.

## Root cause

The free runtime selects a PDF for SignXX, replaces approval fields in that PDF,
and mutates the selected PDF row. The Word approval replacer exists but is not
used by the runtime. The licensed electronic flow creates a new DOCX/PDF pair.

## Implementation

1. Add explicit source/working pair metadata to the free document file model.
2. Add a DOCX working-copy service that clones the source bytes and creates a
   new DOCX/PDF pair without mutating the source rows.
3. In `SignAsync`, resolve the paired DOCX, replace SignXX/name/note in DOCX,
   convert it, then pass the resulting PDF to the selected provider.
4. Keep a PDF-only fallback for legacy documents that have no DOCX source.
5. Add regression coverage for pair preservation, Word replacement and the
   legacy fallback/provider boundary.

## Acceptance criteria

- Original DOCX hash/blob and row remain unchanged after SignXX.
- New working DOCX contains no targeted SignXX/name/note placeholders.
- New PDF is generated from that DOCX before VISNAM/TAG invocation.
- Existing PDF-only workflows still sign through the current provider adapters.
- DocumentService tests and build pass with no new warnings/errors.

## Completion

- Added immutable DOCX/PDF working pairs for submission preparation and SignXX.
- Routed paired Word files through DOCX replacement and conversion; retained the
  PDF-only compatibility path for legacy documents.
- Added regression coverage for electronic image replacement, digital provider
  placeholders, and the no-second-overlay adapter boundary.
- Verified 75/75 DocumentService tests and clean service/client builds.
