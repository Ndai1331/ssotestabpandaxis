---
status: completed
---

# Phase 02 — Survey public API parity

## Requirements

- Add public location/criteria reads and session/result submission contracts.
- Store the public respondent fields needed by SurveyResult detail views.
- Keep management endpoints and result reads protected.
- Allow only the explicit public survey route through the BFF without login.
- Reuse the existing MinIO survey asset policy for optional public image uploads.

## Success criteria

- Public API is reachable without an authenticated BFF session.
- Public submissions validate active location/session/criteria and score bounds.
- Existing authenticated survey CRUD remains compatible.
