# TAG signing parity and signing KPI report

## Goal

Make the free-license HCS service expose the same usable TAG Remote CA signing flow and signing KPI report surface as `services/HCS_web_with_license`, while preserving the free service boundaries and keeping all credentials/endpoints runtime-configured.

## Findings

- The licensed source calls the TAG-style provider `REMOTE_CA` and implements `SignTextV2` against `/api/v2/pdf/sign/originaldata` with an HMAC-SHA256 authorization header.
- The free-license baseline already contains the migrated TAG implementation: `LicensedRemoteCaSigningAdapter`, `SignTextV2`, provider normalization/defaults, endpoint allowlisting, UI provider selection, and documentation. No duplicate provider implementation should be introduced.
- The free `SigningKpiReport.razor` is currently a minimal placeholder that loops over documents and per-document signing reports. It does not expose the licensed report's metrics, group breakdown, date filtering, chart, or export flow.
- The free service stores workflow/signing data in `HCS.DocumentService`; the report API should query that bounded context. Legacy SQL Server data remains optional and must be configured through environment variables/User Secrets if parity with the legacy source is required.

## Implementation plan

1. TAG parity verification
   - Add focused tests for TAG provider normalization, default selection, internal host/port endpoint normalization, and Base64-secret validation behavior.
   - Update the free signing runbook/configuration text only where it is missing a concrete runtime environment example; never add a real internal IP or secret.

2. KPI backend
   - Add shared signing KPI contracts for filters, metrics, group rows, pie slices, and report availability/errors.
   - Add a Document Service KPI query/app-service path that calculates total, completed, rejected, cancelled, in-progress, average processing time, and on-time metrics from workflow instances/tasks, grouped by workflow definition.
   - Add API endpoints for the report and a bounded detail export. Keep authorization on the existing signing-report permission and do not expose credentials in responses or logs.
   - Register the service and add tests for metric aggregation, date filtering, status mapping, and authorization/error behavior.

3. Free UI/client
   - Extend the existing document client/models with typed KPI calls.
   - Replace the placeholder `/signing-kpi-report` page with the licensed page's useful parity: date range filter, KPI cards, pie chart, group table, loading/error/empty states, and export action adapted to the BFF route.
   - Add the report entry to the existing Documents/Reports navigation only if it is not already present; preserve the current permission-aware menu behavior.
   - Add/update localization keys and the free-service runbook.

4. Verification
   - Run the TAG/provider and Document Service tests.
   - Build the affected free-license solution/projects with `--no-restore` where assets are already restored, then run the relevant client/build checks.
   - Review the final diff to ensure existing unrelated worktree changes are untouched and no secrets or hard-coded internal endpoints are added.

## Files expected to change

- `services/HCS_web_free_license/services/document/HCS.DocumentService.Contracts/Signing/`
- `services/HCS_web_free_license/services/document/HCS.DocumentService/Signing/`
- `services/HCS_web_free_license/services/document/HCS.DocumentService/Controllers/SigningController.cs`
- `services/HCS_web_free_license/services/document/HCS.DocumentService/HcsDocumentServiceModule.cs`
- `services/HCS_web_free_license/services/document/HCS.DocumentService.Tests/`
- `services/HCS_web_free_license/src/HCS.Blazor.Client/Documents/`
- `services/HCS_web_free_license/src/HCS.Blazor.Client/Pages/SigningKpiReport.razor`
- `services/HCS_web_free_license/src/HCS.Blazor.Client/Navigation/HCSMenuContributor.cs`
- `services/HCS_web_free_license/src/HCS.Domain.Shared/Localization/HCS/{vi,en}.json`
- `services/HCS_web_free_license/services/document/docs/signing-provider-configuration.md`

## Open questions / assumptions

- The internal TAG IP was not supplied, so the implementation will not invent or commit one. Configure it with `Signing__Providers__TAG__DefaultEndpoint` and add its host to `Signing__AllowedEndpointHosts` at runtime.
- KPI parity in the free microservice means current HCS workflow data is authoritative. Legacy SQL Server reporting will be included only as an optional runtime-configured source if the existing free service dependencies make that safe without coupling services incorrectly.
