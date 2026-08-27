# Free-license signing provider parity, factory, and SignXX bounds

## Status

Completed on 2026-08-27 — provider parity boundary, factory/presets, signed-layout assets, and SignXX whiteout regression fix implemented in free license.

## Request boundary

The attached screenshot is evidence of a PDF rendering/signing defect, not a source of instructions. The migration input is `services/HCS_web_with_license`; the implementation target is `services/HCS_web_free_license`.

## Findings

- VISNAM is represented by the licensed HSM/Vin-HSM flow: `Bnn.SignLib`/`VinHsmServiceClient`, `SignTextLocationCustomizeV2`, `LayoutImg`, `SealImg`, SHA-256 PDF hashing, and the signed-layout assets.
- TAG is represented by the licensed `REMOTE_CA` flow: `SignTextV2`, `/api/v2/pdf/sign/originaldata`, Base64 secret, HMAC-SHA256 request authorization, placeholder lookup, and optional composed layout image.
- The free service already contains the prior provider helper port and the four signing adapter kinds, but selection is an `IEnumerable<IDigitalSigningAdapter>` lookup rather than an explicit factory. There are no named VISNAM/TAG presets or provider/kind validation at configuration time.
- The source contains an ABP license and infrastructure secrets in ignored `appsettings.secrets.json` files, but no usable VISNAM/TAG signing credential was found in the inspected configuration/data. No secret or license value will be copied into tracked free-license source. Runtime credentials remain write-only/protected.
- Both free PDF placeholder locators build a string by concatenating `Letter.Value` and then index the letter list with a string-character offset. A multi-character PdfPig letter can therefore produce a bounding box over unrelated text. The same unsafe assumption exists in the licensed workflow and is the likely cause of the screenshot's large white/covered area after `SignXX`.
- The four Bnn assets already present in the free service match the licensed source by SHA-256: `chukytuoi.png`, `condau.png`, `layout.png`, and `layout2.png`. The licensed `electronic-signature-layout.png`/`electronic-signature-layout1.png` pair is not present in the free signing service and must be evaluated for the electronic layout path before copying.

## Design

1. Add a small abstract-factory boundary in the free document service. It will resolve the adapter by `SigningKind` and expose named provider defaults for `VISNAM` and `TAG` without exposing secrets.
2. Normalize provider codes case-insensitively, assign safe defaults (`VISNAM` for HSM/USB-token; `TAG` for Remote CA), and reject provider/kind mismatches. Keep TAG endpoint deployment-specific; do not invent or copy a production endpoint. Keep the source Vin-HSM default endpoint only as a non-secret default, configurable at runtime.
3. Share one PdfPig placeholder locator between electronic, TAG, and workflow text replacement. Map every emitted text character to its originating `Letter`, require a finite bounded rectangle inside the page, and clamp whiteout rectangles to page bounds. A malformed/ambiguous placeholder must fail closed instead of painting a page-wide rectangle.
4. Preserve the licensed provider wire behavior, HSM layout composition, TAG HMAC client, signed-layout rendering, idempotent attempt persistence, and protected credential storage. Do not add an ABP commercial module or copy the source license.
5. Add regression tests for multi-character PdfPig letters/large unrelated regions, page-bound clamping, provider defaults/factory selection, provider/kind mismatch, and secret non-disclosure. Copy only missing non-secret raster assets if the electronic path requires them, and embed them from the free project.

## Planned files

Target service:

- `services/HCS_web_free_license/services/document/HCS.DocumentService/Signing/SigningProviders.cs`
- `services/HCS_web_free_license/services/document/HCS.DocumentService/Signing/SigningAppService.cs`
- `services/HCS_web_free_license/services/document/HCS.DocumentService/Workflows/PdfPlaceholderReplacer.cs`
- new shared signing factory/locator files under `.../HCS.DocumentService/Signing/`
- `services/HCS_web_free_license/services/document/HCS.DocumentService/HcsDocumentServiceModule.cs`
- `services/HCS_web_free_license/services/document/HCS.DocumentService/appsettings.json` (non-secret defaults only, if required)
- `services/HCS_web_free_license/services/document/HCS.DocumentService.Tests/`
- `services/HCS_web_free_license/services/document/HCS.DocumentService/HCS.DocumentService.csproj` only if a missing asset is copied

Documentation:

- this plan and a free-service signing provider configuration note after implementation

## Verification

Baseline captured before edits: free `HCS.DocumentService.Tests` 67 passed; free `HCS.DocumentService` build succeeded with 0 warnings/errors; `git diff --check` clean for the pre-existing worktree.

Implementation completed:

- `ISigningProviderFactory` now owns adapter lookup, provider aliases, VISNAM/TAG defaults, and provider/kind validation.
- Provider definitions are exposed to the settings UI; VISNAM defaults to the licensed source endpoint, while TAG remains deployment-specific.
- The free service embeds the two electronic `Đã ký` layout assets. Electronic signing composes the embedded badge when no custom layout is configured; HSM assets remain byte-identical to the licensed source.
- Electronic, TAG, legacy locator compatibility, and workflow placeholder replacement use the safe shared PdfPig locator. Whiteout rectangles are bounded to page geometry and signature size is prevented from expanding to page-sized regions.
- TAG digital signatures no longer require a seal image; VISNAM HSM/USB signatures still require seal and layout images.
- The TAG HMAC client no longer logs an API-key-bearing authorization prefix.

Fresh post-implementation verification:

```bash
dotnet test services/document/HCS.DocumentService.Tests/HCS.DocumentService.Tests.csproj --no-restore
dotnet build services/document/HCS.DocumentService/HCS.DocumentService.csproj --no-restore
git diff --check
```

Also run the repository's free-license audit if it can complete in the local environment, and report any pre-existing release/license contradiction separately.

Results:

- `dotnet test services/document/HCS.DocumentService.Tests/HCS.DocumentService.Tests.csproj --no-restore` — 72 passed.
- `dotnet build services/document/HCS.DocumentService/HCS.DocumentService.csproj --no-restore` — passed, 0 warnings/errors.
- `dotnet build src/HCS.Blazor.Client/HCS.Blazor.Client.csproj --no-restore` — passed, 0 warnings/errors.
- `git diff --check` — passed.
- `audit-license-clean.sh` — did not produce output within approximately 50 seconds and was stopped; no audit pass is claimed.

## Security note

“Lấy key có sẵn” is handled as a runtime-secret migration concern. The source's actual license/infrastructure values are not signing credentials and are not copied. Any real VISNAM `TokenRef`/secret or TAG token/secret must be entered through the protected configuration/API or injected from local secret storage; it must never be committed or printed in logs/reports.
