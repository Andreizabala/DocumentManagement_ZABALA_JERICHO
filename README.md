# Document Management REST API

A small, interview-friendly ASP.NET Core API for uploading, listing, retrieving, downloading, and deleting documents. Metadata is stored in MySQL through Entity Framework Core; file bytes are stored outside the database in a configurable local directory.

## Technology and architecture

- .NET 8 / ASP.NET Core Web API
- Entity Framework Core 8 with MySQL 8.0
- Swagger / OpenAPI
- xUnit tests
- Lightweight Clean Architecture:
  - `Domain` contains the document entity.
  - `Application` contains use cases, DTOs, validation, and storage/repository abstractions.
  - `Infrastructure` implements EF Core persistence and local file storage.
  - `Api` handles HTTP concerns and maps errors to RFC 7807 problem responses.

Dependencies point inward: API composes the application, Infrastructure implements Application interfaces, and Domain has no external dependencies. The design intentionally avoids CQRS, MediatR, and generic repositories because they add little value at this assessment's scale.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- MySQL Server 8.0
- Optional: `dotnet-ef` 8.x (`dotnet tool install --global dotnet-ef --version 8.*`)

## Configuration

The application uses a local MySQL 8.0 server on port 3306. Keep real credentials outside committed configuration:

```json
"ConnectionStrings": {
  "DocumentsDatabase": "Server=127.0.0.1;Port=3306;Database=DocumentManagement;User=root;Password=<your-password>;"
}
```

Provide the password through an environment variable or .NET user secrets. Do not commit credentials:

```powershell
$env:ConnectionStrings__DocumentsDatabase = "Server=127.0.0.1;Port=3306;Database=DocumentManagement;User=root;Password=<your-password>;"
```

The file root defaults to `storage` relative to the API process. Override it with `FileStorage__RootPath`. The directory is created automatically and is git-ignored.

## Database setup

From the repository root, restore packages and apply the included initial migration:

```powershell
dotnet restore
dotnet ef database update --project src/DocumentManagement.Infrastructure --startup-project src/DocumentManagement.Api
```

## Run the API

```powershell
dotnet run --project src/DocumentManagement.Api
```

In Development, Swagger UI is available at `https://localhost:7080/swagger` (or the URL printed by `dotnet run`).

## Endpoints

| Method | Route | Result |
|---|---|---|
| `POST` | `/api/documents` | Upload a `multipart/form-data` field named `file`; returns `201 Created` |
| `GET` | `/api/documents` | List document metadata without file bytes |
| `GET` | `/api/documents/{id}` | Retrieve one document's metadata |
| `GET` | `/api/documents/{id}/download` | Download the stored file |
| `DELETE` | `/api/documents/{id}` | Delete metadata and its file; returns `204 No Content` |

Example upload:

```powershell
curl.exe -k -F "file=@C:\path\report.pdf" https://localhost:7080/api/documents
```

## Validation, security, and failure behavior

- Empty uploads and files larger than 10 MiB (10 × 1024 × 1024 bytes) are rejected.
- User-supplied paths are reduced to a sanitized leaf filename for display/download only.
- Stored files use server-generated 128-bit random identifiers with no user-controlled path component.
- Storage keys are validated and resolved under the configured root to prevent traversal.
- Upload uses a temporary file and atomic rename, then removes the file if database persistence fails.
- Delete first atomically moves the file to a private quarantine directory. If the database delete fails, the file is restored; after the database commit, the quarantined file is removed. A missing physical file does not prevent removal of a stale metadata record.
- API errors use consistent `application/problem+json` responses and include a trace ID.
- Authentication and authorization are intentionally absent because the assessment excludes them.

## Supported file types and assumptions

All file types are accepted. The API stores the declared MIME type but does not trust it for path construction or execution. It does not perform antivirus scanning, file-signature validation, quotas, encryption at rest, or distributed storage. Those controls would be added based on a real deployment's threat model and supported-format policy.

Local storage is suitable for a single API instance. A scaled or cloud deployment should implement the same storage interface using durable object storage and add an outbox/reconciliation process for cross-system consistency.

## Tests

Run the complete test suite:

```powershell
dotnet test
```

The tests cover successful upload, existing and missing document retrieval, rejection above 10 MiB, deletion, and restoration of a staged file when database deletion fails. The application tests use deterministic in-memory fakes and do not require MySQL.

## AI-assisted development disclosure

OpenAI Codex was used to help read the assessment, draft the initial project structure and implementation, identify security edge cases, and generate test and documentation starting points. I reviewed the complete implementation, adjusted the architecture to remain appropriately lightweight, verified filename/path handling and deletion consistency, and ran the available build and automated tests.

The area I would validate most deeply before production use is failure recovery across MySQL and local/object storage. The quarantine/rollback strategy is appropriate for this single-instance assessment, but cross-service atomicity needs durable reconciliation, observability, and operational testing. I am also least confident in client-declared MIME types because the API intentionally does not inspect file signatures or scan content.

## Known limitations

- No authentication/authorization, per the assessment.
- No pagination; appropriate for the requested simple API, but needed for large collections.
- Local filesystem storage assumes one writable application instance.
- MIME type is client-declared and file content is not scanned.
- Permanent quarantine cleanup failures after a successful database delete can leave an unreachable file; production should retry and monitor cleanup.
