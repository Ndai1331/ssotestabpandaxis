# HCS Document Service

ABP Community 10.6 / .NET 10 bounded context for documents, workflows and signing.

## License boundary

- NuGet dependencies are Community-compatible and restored from `nuget.org`.
- No commercial ABP module, generated commercial template, legacy migration, credential or signing SDK binary is included.
- `Bnn.SignLib` and `Bnn.Sdk` are intentionally absent. A production adapter must not be released until redistribution rights are documented (see `SIGNING-RELEASE-BLOCKER.md`).

## Configuration

All secrets must come from environment variables or .NET User Secrets:

```text
ConnectionStrings__DocumentService=Host=localhost;Database=hcs_document;Username=...;Password=...
AuthServer__Authority=https://localhost:44401
Minio__EndPoint=localhost:9000
Minio__AccessKey=...
Minio__SecretKey=...
RabbitMQ__Connections__Default__HostName=localhost
Signing__RemoteCa__ApiKey=...
```

Buckets are private. Files are returned only through authorized API streams; no public bucket URL is persisted.
