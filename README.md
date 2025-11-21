# Guia Noivas API (skeleton)

Projeto gerado a partir da especificação `api-spec-dotnet9-sqlserver.md`.

Quick start

1. Restore and build

```bash
cd src/GuiaNoivas.Api
dotnet restore
dotnet build
```

2. Configure environment variables (important in production)

- `CONNECTION_STRING` - SQL Server connection string (ex: `Server=MY-SERVER\\SQLEXPRESS;Database=GuiaNoivas;User Id=sa;Password=YourStrong!Passw0rd;`)
- `JWT_SECRET` - secret used to sign JWT tokens (change from default)
- `ASPNETCORE_ENVIRONMENT` - Development/Production

Setting the JWT secret per environment

- **Overview**: The application reads the `Jwt:Secret` configuration value. For environment variables, the `:` is replaced by `__` (double underscore), so set `Jwt__Secret` as an environment variable to override the value in `appsettings.json`.
- **Generate a strong secret** (example):

```bash
openssl rand -base64 48
```

- **Set the secret (bash)**:

```bash
export Jwt__Secret="$(openssl rand -base64 48)"
```

- **Set the secret (PowerShell)**:

```powershell
$env:Jwt__Secret = "$(openssl rand -base64 48)"
```

- **dotnet user-secrets (local development)**:

```bash
cd src/GuiaNoivas.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:Secret" "<your-secret-here>"
```

- **Docker / docker-compose example** (service env):

```yaml
services:
	api:
		image: guia-noivas-api
		environment:
			- Jwt__Secret=${JWT_SECRET}
```

- **Notes / Best Practices**:
	- Do not commit production secrets to source control. Use environment variables, secret stores (Azure Key Vault, AWS Secrets Manager), or CI/CD pipeline secrets.
	- For local development prefer `dotnet user-secrets` so secrets stay out of the repo.
	- Plan for secret rotation and update deployed instances when rotating.

3. Create initial migration (requires dotnet-ef tools)

```bash
dotnet tool install --global dotnet-ef
-- Generate migrations (this repo includes a design-time factory so migrations can be created):
dotnet ef migrations add InitialCreate --project src/GuiaNoivas.Api --startup-project src/GuiaNoivas.Api

-- Apply migrations to the database (or the app will attempt to migrate/seed on startup):
dotnet ef database update --project src/GuiaNoivas.Api --startup-project src/GuiaNoivas.Api
```

4. Run

```bash
dotnet run --project src/GuiaNoivas.Api
```

Notes / Next steps

- Implement real authentication (register/login) and secure refresh tokens.
- Implement media presign with Azure Blob Storage or local fallback to `wwwroot/uploads`.
- Implement Hangfire jobs for background processing.
- Add seeding and migrations to repo.
# Database migrations (create and apply)

- **Overview**: This project uses EF Core migrations. You can create migrations locally and apply them to the database, or run the `database update` step from CI or a container.

- **Create a migration (local dev)**:

```bash
cd src/GuiaNoivas.Api
dotnet tool install --global dotnet-ef
dotnet ef migrations add <MigrationName> --project src/GuiaNoivas.Api --startup-project src/GuiaNoivas.Api
```

- **Apply migrations to the database (local or server)**:

```bash
# Ensure the app can reach the database. You can set the connection string via env var:
export ConnectionStrings__DefaultConnection="Server=...;Database=GuiaNoivas;User Id=...;Password=...;"

dotnet ef database update --project src/GuiaNoivas.Api --startup-project src/GuiaNoivas.Api
```

- **Run migrations from Docker (one-off using SDK image)**:

```bash
# From repo root, with CONNECTION_STRING exported
export CONNECTION_STRING="Server=...;Database=GuiaNoivas;User Id=...;Password=...;"

docker run --rm \
	-e ConnectionStrings__DefaultConnection="$CONNECTION_STRING" \
	-v "$(pwd)":/workspace -w /workspace \
	mcr.microsoft.com/dotnet/sdk:9.0 bash -c "dotnet tool install --global dotnet-ef && dotnet ef database update --project src/GuiaNoivas.Api --startup-project src/GuiaNoivas.Api"
```

- **CI/CD note**: In CI pipelines, install the `dotnet-ef` tool (or use a dotnet SDK image), set the `ConnectionStrings__DefaultConnection` environment variable from a secure pipeline secret, then run `dotnet ef database update` with the project and startup-project flags.

- **If you prefer automatic migrations on startup**: You can add code to `Program.cs` to apply migrations at application start. This is convenient but requires care in production (handle concurrency, backups, and migration failures).
# guia-noivas-api
 
## Storage (Azure Blob) Setup

This project supports generating short-lived SAS upload URLs (presigned URLs) for Azure Blob Storage. The `MediaController` will return an upload URL when a storage connection is configured. If not configured, the controller falls back to a local `wwwroot/uploads` style path for testing.

- Configuration keys:
	- `Storage:ConnectionString` (or env var `STORAGE_CONNECTION_STRING`) — Azure Storage connection string. Recommended to set via environment variable in production.
	- `Storage:Container` — container name to use (default: `media`).

Important: the current SAS generation implementation requires the storage connection string to include an `AccountKey` (AccountName and AccountKey). If you prefer not to store account keys, consider using Azure AD + user delegation SAS (requires additional code and an AAD client).

Example connection string (do NOT commit this to source control):

```
DefaultEndpointsProtocol=https;AccountName=youraccount;AccountKey=<base64key>;EndpointSuffix=core.windows.net
```

Set the connection string via environment variable (PowerShell):

```powershell
#$env:STORAGE_CONNECTION_STRING = "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net"
#$env:Storage__ConnectionString = "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net"

# For the appsettings configuration path (double underscore maps to `:`)
#$env:Storage__ConnectionString = "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net"
```

How it works (quick):

- Client requests a presign: `POST /api/v1/media/presign` with JSON body `{ "filename": "photo.jpg", "contentType": "image/jpeg" }` and `Authorization: Bearer <token>` header.
- Server returns `{ "uploadUrl": "https://...sas...", "blobName": "..." }`.
- Client performs a `PUT` to the returned `uploadUrl` with the file bytes and the `x-ms-blob-type: BlockBlob` header.

Curl example (get SAS URL):

```bash
TOKEN="<your-jwt>"
curl -X POST "http://localhost:5000/api/v1/media/presign" \
	-H "Authorization: Bearer $TOKEN" \
	-H "Content-Type: application/json" \
	-d '{"filename":"photo.jpg","contentType":"image/jpeg"}'
```

Sample response:

```json
{ "uploadUrl": "https://youraccount.blob.core.windows.net/media/abc123?sv=...", "blobName": "abc123_photo.jpg" }
```

Upload the file to the SAS URL (curl):

```bash
curl -X PUT "<uploadUrl>" \
	-H "x-ms-blob-type: BlockBlob" \
	-H "Content-Type: image/jpeg" \
	--data-binary "@/path/to/photo.jpg"
```

Browser / JavaScript example (fetch):

```javascript
// 1) request a SAS upload URL from the API
const presignRes = await fetch('/api/v1/media/presign', {
	method: 'POST',
	headers: {
		'Authorization': `Bearer ${token}`,
		'Content-Type': 'application/json'
	},
	body: JSON.stringify({ filename: file.name, contentType: file.type })
});
const { uploadUrl } = await presignRes.json();

// 2) upload file bytes to the SAS URL
await fetch(uploadUrl, {
	method: 'PUT',
	headers: {
		'x-ms-blob-type': 'BlockBlob',
		'Content-Type': file.type
	},
	body: file
});
```

Notes and recommendations:

- CORS: If uploading directly from a browser to Azure Blob Storage, configure CORS rules on the storage account to allow the origin and headers (e.g., `x-ms-blob-type`, `Content-Type`).
- Security: Do not commit `AccountKey` to source control. Use environment variables, CI/CD secrets, or Azure Key Vault in production.
- Alternative: Implement user-delegation SAS using Azure AD for improved security (requires server code to request a user delegation key with proper RBAC).

If you want, I can:
- Add README instructions for configuring CORS on the storage account.
- Implement user-delegation SAS (Azure AD) instead of account key SAS.
- Add a small client example in this repo that demonstrates requesting a SAS and uploading a test file.

### Cost considerations when using SAS + direct uploads

A few important cost notes when you allow clients to upload directly to Azure Blob Storage using SAS URLs:

- **Generating a SAS itself has no storage charge.** The server-side operation to create a SAS is compute work on your API (negligible) and does not incur storage transaction costs.
- **Uploads (PUT / block uploads) are charged as storage transactions.** Each PUT/Block/PutBlockList/Commit is a billable operation (charged per 10k operations). If clients upload large files in many small blocks, transaction counts increase accordingly.
- **Ingress (uploads) is typically free, but egress (downloads) may be charged.** Downloading blobs to users (especially over the public Internet or cross-region) can generate outbound bandwidth costs. Plan where clients and storage are located to minimize cross-region egress.
- **Storage capacity and tiers cost per GB/month.** Choose Hot/Cool/Archive depending on access pattern — Cold tiers lower storage cost but increase access/read costs and latency.
- **Replication and redundancy affect cost.** Options like LRS, ZRS, GRS have different pricing and durability/availability trade-offs.
- **Extra operations for multipart uploads increase cost.** Using block uploads (Put Block for each chunk + Put Block List) means multiple transactions; prefer single PUT for small files when possible or batch/block sizes to reduce the number of calls.
- **Using a CDN can reduce outbound (egress) costs and improve performance.** Fronting public downloads with a CDN reduces load and may be cheaper for heavy download traffic.

Recommendations:

- Monitor storage metrics and set billing alerts in Azure to detect unexpected transaction/egress spikes.
- Use reasonable SAS TTLs (short-lived) and scope (only necessary permissions) to limit abuse.
- Prefer server-side validation and file-size/content checks before issuing SAS tokens (e.g., require an authenticated request that includes declared content length/type and enforce limits server-side).
- Consider user-delegation SAS (Azure AD) or a server-side upload proxy if you want to avoid storing account keys; user-delegation SAS still requires proper RBAC/AAD setup but avoids AccountKey in config.
- If your clients upload from browsers, configure CORS correctly and prefer larger upload chunk sizes to reduce transaction counts.
- For heavy ingress from client devices, verify whether placing the storage account in the same region as your clients or using an edge solution (CDN/Edge) reduces cost and latency.

If you want, I can add a short `samples/` client demonstrating a chunked upload with sensible block sizes and show a cost comparison example (rough math) for transactions and egress. Mark whether you prefer a server-side proxy or user-delegation SAS and I will implement a sample.








 & $env:TEMP\dotnet-install.ps1 -Channel 9.0 -InstallDir $env:USERPROFILE\.dotnet