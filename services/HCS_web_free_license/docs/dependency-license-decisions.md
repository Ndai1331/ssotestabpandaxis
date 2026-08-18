# Dependency license decisions

## Release blockers

### Blazorise 2.2 / Blazorise.Licensing 1.3

LeptonX Lite 5.6 and the current Blazor UI resolve `Blazorise.Licensing` from nuget.org. The package uses the Blazorise Software License Agreement rather than an OSI-approved open-source license. Blazorise states that organizations, including government entities, require a commercial license.

Development may continue so the requested LeptonX Lite UI can be migrated, but a production release is blocked until the deploying organization records proof of an applicable Blazorise commercial license. If that approval cannot be obtained, replace LeptonX Lite/Blazorise with an approved OSS UI stack before release.

This exception does not permit any ABP Commercial/Pro package. `scripts/audit-license-clean.sh` continues to reject those packages.
