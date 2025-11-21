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






