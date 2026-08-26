# Phase 03 — signing note placeholders

## Goal

Make each free approval signing operation preserve `NoteContent` semantics from the licensed flow while retaining current provider adapters.

## Tasks

- Derive the numbered signing placeholder suffix from the requested/default `<<SignNN>>` marker.
- Merge `<<FullNameNN>>` and `<<NoteContentNN>>` (including the plain `<<NoteContent>>` alias if present) into the PDF/working document before provider execution.
- Resolve signer display name from trusted current-user claims when the request does not supply one.
- Keep `SigningProviderRequest.Note` bounded and passed unchanged semantically to Electronic/Remote CA/HSM/USB adapters.
- Ensure idempotency and failed-attempt behavior remain unchanged.

## Gate

Provider tests and signing security tests prove that a failed sign cannot be followed by approval, and note/name placeholders are not left behind for supported PDF text.

