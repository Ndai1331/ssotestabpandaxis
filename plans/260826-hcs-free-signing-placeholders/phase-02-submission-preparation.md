# Phase 02 — submission preparation

## Goal

Port the licensed pre-submission electronic-signature check and prepared placeholder merge into the free workflow start path.

## Tasks

- Select the target DOCX/PDF after source duplication/template attachment.
- Validate current user's active electronic signature, image blob and validity range.
- Resolve full name, position and department through trusted server-side context.
- Replace prepared aliases and signing content in DOCX; convert the merged Word file to PDF where conversion is available.
- Replace detectable prepared placeholders in PDF-only files with a safe overlay helper.
- Update blob contents, hashes, pair references and document children atomically with workflow creation; clean up newly-created blobs on failure.
- Keep the existing `SigningContent` review history detail.

## Gate

No workflow instance or review history is committed on a missing/invalid electronic signature; prepared files are usable by the existing signing queue.

