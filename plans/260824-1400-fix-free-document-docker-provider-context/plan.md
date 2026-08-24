# Fix free DocumentService Docker provider build context

## Diagnosis

`HCS.DocumentService.csproj` compiled provider helpers from
`../../../../HCS_web_with_license/...`. The free service Docker build uses
`services/HCS_web_free_license` as its context, so that sibling directory is
not sent to Docker and CSC raises CS2001.

The free service README also defines the licensed tree as read-only migration
input, so widening the Docker context would violate the repository boundary.

## Plan

1. Copy the provider helper source and embedded signing assets required by the
   free runtime into the free DocumentService.
2. Replace external `Compile`/`EmbeddedResource` items with project-local
   paths.
3. Add a boundary regression test that rejects `HCS_web_with_license` source
   references in the free DocumentService project file.
4. Run the exact publish path used by Docker, targeted tests, and the document
   container build if Docker is available.

## Acceptance

- No `HCS_web_with_license` path remains in the free DocumentService project.
- `dotnet publish` succeeds from the free-service build context.
- The document Docker image builds successfully.
- Existing signing provider behavior remains source-equivalent.
