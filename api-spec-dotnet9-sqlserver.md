# Especificação da API — Guia de Noivas Piracicaba

Versão: 1.0

Última atualização: Novembro de 2025

Escopo
------
Este documento descreve, em detalhe, os requisitos e contratos da API RESTful a ser implementada em C# (.NET 9) para suportar o frontend do projeto Guia de Noivas Piracicaba.

Requisitos principais (ajustados)
- Plataforma alvo: Windows Server 2022 + IIS 10.0
- Runtime: .NET 9 (ASP.NET Core)
- Banco de dados: Microsoft SQL Server 2022 Express
- Storage de mídia: Azure Blob Storage (opcional), ou armazenamento local em `wwwroot/uploads` (fallback)
- Não utilizar: Docker, Redis, Azure Key Vault ou serviços de secrets gerenciados, Application Insights ou qualquer serviço pago de observabilidade (exceto Blob Storage)

Princípios de projeto
- APIs RESTful versionadas sob `/api/v1`.
- Respostas padronizadas; erros via RFC 7807 (Problem Details, `application/problem+json`).
- Autenticação via JWT Bearer; refresh tokens persistidos em DB.
- Documentação gerada por Swagger/Swashbuckle.

Tecnologias recomendadas
- .NET 9, ASP.NET Core Web API
- EF Core (`Microsoft.EntityFrameworkCore.SqlServer`)
- Hangfire para jobs em background (usando SQL Server storage)
- Serilog (sinks: files, EventLog)
- Swashbuckle (Swagger)
- Azure.Storage.Blobs (se usar Blob Storage)
- xUnit + Moq para testes

Modelos de domínio (resumo)
- Fornecedor
  - Id: uniqueidentifier (GUID)
  - Nome: nvarchar(200) NOT NULL
  - Slug: nvarchar(200) UNIQUE NOT NULL
  - Descricao: nvarchar(max)
  - Cidade: nvarchar(100)
  - Telefone, Email, Website
  - Destaque: bit
  - SeloFornecedor: bit
  - Rating: decimal(3,2) NULL
  - Visitas: int
  - CreatedAt, UpdatedAt: datetimeoffset

- Categoria
  - Id, Nome, Slug, Descricao, Order, CreatedAt, UpdatedAt

- Media
  - Id, FornecedorId (nullable), Url, Filename, ContentType, Width, Height, IsPrimary, CreatedAt

- Usuario (Admin/Supplier)
  - Id, Email, PasswordHash, Roles, DisplayName, CreatedAt

- ContatoSubmission
  - Id, FornecedorId (nullable), Nome, Email, Telefone, Mensagem, CreatedAt

- InstitucionalContent
  - Key (sobre, termos), Title, ContentHtml, Version, UpdatedAt

Banco de dados: SQL Server 2022 Express
- Provider EF Core: `Microsoft.EntityFrameworkCore.SqlServer`.
- Exemplos de connection string:
  - Trusted (Windows Auth):
    `Server=.\SQLEXPRESS;Database=GuiaNoivas;Trusted_Connection=True;MultipleActiveResultSets=true;`
  - SQL Auth:
    `Server=MY-SERVER\SQLEXPRESS;Database=GuiaNoivas;User Id=sa;Password=YourStrong!Passw0rd;MultipleActiveResultSets=true;`
- Migrations: usar `dotnet ef migrations add InitialCreate` e `dotnet ef database update`.
- Índices recomendados:
  - UNIQUE on `Fornecedores(Slug)`
  - INDEX on `Fornecedores(Destaque, Rating DESC)`
  - INDEX on `Fornecedores(Cidade)`
  - INDEX on `FornecedoresCategorias(FornecedorId, CategoriaId)`

Tabela de exemplo (DDL simplificado)
```sql
CREATE TABLE Fornecedores (
  Id UNIQUEIDENTIFIER PRIMARY KEY,
  Nome NVARCHAR(200) NOT NULL,
  Slug NVARCHAR(200) NOT NULL UNIQUE,
  Descricao NVARCHAR(MAX),
  Cidade NVARCHAR(100),
  Telefone NVARCHAR(50),
  Email NVARCHAR(200),
  Website NVARCHAR(250),
  Destaque BIT NOT NULL DEFAULT 0,
  SeloFornecedor BIT NOT NULL DEFAULT 0,
  Rating DECIMAL(3,2) NULL,
  Visitas INT NOT NULL DEFAULT 0,
  CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt DATETIMEOFFSET NULL
);

CREATE INDEX IX_Fornecedores_Destaque_Rating ON Fornecedores (Destaque DESC, Rating DESC);
CREATE INDEX IX_Fornecedores_Cidade ON Fornecedores (Cidade);
```

API: convenções e padrões
- Prefixo: `/api/v1`.
- Paginação: `page` (1-based), `pageSize` (default 12, max 100).
- Filtros via query params: `category`, `q`, `cidade`, `destaque`, `exclude` (CSV), `sort`.
- Erros: ProblemDetails com `status`, `title`, `detail` e `errors` quando aplicável.

Endpoints (detalhado)

1) Autenticação
- POST `/api/v1/auth/register`
  - Body: `{ email, password, displayName, role? }`
  - 201 Created: `{ id, email, displayName }`
- POST `/api/v1/auth/login`
  - Body: `{ email, password }`
  - 200 OK: `{ accessToken, refreshToken, expiresIn, user:{id,email,roles}}`
- POST `/api/v1/auth/refresh`
  - Body: `{ refreshToken }` → 200 new tokens
- POST `/api/v1/auth/logout`
  - Revoga refresh token (usuário autenticado)

2) Fornecedores (público)
- GET `/api/v1/fornecedores`
  - Query: `page,pageSize,q,category,cidade,destaque,exclude,sort`
  - Response: `{ data: FornecedorListDto[], meta: { total, page, pageSize } }`
- GET `/api/v1/fornecedores/{id}`
  - Response: `FornecedorDetailDto` (inclui imagens, categorias)
- GET `/api/v1/fornecedores/slug/{slug}`
- POST `/api/v1/fornecedores/{id}/visit` (incrementa visitas)
- POST `/api/v1/fornecedores/{id}/contact`
  - Body: `{ nome, email, telefone, mensagem }` → cria ContatoSubmission, envia e-mail ao fornecedor (background job)

3) Admin Fornecedores (autenticado, role=Admin)
- POST `/api/v1/admin/fornecedores` — cria
- PUT `/api/v1/admin/fornecedores/{id}` — atualiza
- PATCH `/api/v1/admin/fornecedores/{id}/destaque` — body `{ destaque: true|false }`
- DELETE `/api/v1/admin/fornecedores/{id}`

4) Categorias
- GET `/api/v1/categorias` — lista
- GET `/api/v1/categorias/{id}`
- Admin: POST/PUT/DELETE sob `/api/v1/admin/categorias`

5) Home / Destaques
- GET `/api/v1/home/destaques?limit=4&category=&exclude=`
  - Retorna apenas fornecedores com `Destaque = true` e aplica filtros

6) Institucional
- GET `/api/v1/institucional/{key}` (e.g. sobre, termos)
- Admin: PUT `/api/v1/admin/institucional/{key}` (atualiza conteúdo HTML/Markdown)

7) Contato / Anuncie
- POST `/api/v1/contato` → cria ContatoSubmission e notifica
- POST `/api/v1/anuncie` → cria AnuncieSubmission

8) Media
- POST `/api/v1/media/presign` (Autenticado para uploads administrativos ou fornecedor autenticado)
  - Body: `{ filename, contentType, fornecedorId? }`
  - Response: `{ uploadUrl (SAS), publicUrl, blobName }`
- POST `/api/v1/media` (opcional: multipart server-side upload)
- DELETE `/api/v1/media/{id}` (Admin)

9) Health
- GET `/api/v1/health/ready`
- GET `/api/v1/health/live`

Exemplos de response
- GET `/api/v1/fornecedores?page=1&pageSize=12&destaque=true`
```json
{
  "data": [
    {
      "id": "b8f1c6d3-...",
      "nome": "Doce Sonho Bolos",
      "slug": "doce-sonho-bolos",
      "descricao": "Bolos artísticos...",
      "cidade": "Piracicaba",
      "telefone": "19 99999-9999",
      "website": "https://docesonho.com",
      "destaque": true,
      "seloFornecedor": true,
      "rating": 4.8,
      "imagens": [{ "id":"...","url":"https://...","isPrimary":true }],
      "categorias": [{ "id":"...","nome":"Confeitaria","slug":"confeitaria" }]
    }
  ],
  "meta": { "total": 42, "page": 1, "pageSize": 12 }
}
```

Autenticação e segredos (sem KeyVault)
- JWT + Refresh tokens armazenados (DB).
- Proteção de secrets em produção:
  - Preferir Environment Variables no Windows Server (definir via IIS App Pool Environment Variables ou system environment variables com acesso restrito).
  - `appsettings.Production.json` com valores mínimos; secrets sensíveis como connection strings devem vir de environment variables.
  - Proteja Data Protection key ring: configure `DataProtection` para persistir keys em disco num diretório com ACL restrita (ex.: `C:\AppData\GuiaNoivas\keys`) e conceda acesso apenas ao identity do App Pool.

Proteção de dados
- Habilitar HTTPS em IIS; forçar redirect HTTP → HTTPS.
- Configurar HSTS.
- Sanitizar HTML do conteúdo institucional (biblioteca: Ganss.XSS ou similar).
- Validar uploads (tamanho máximo, tipos permitidos) e bloquear execução de arquivos em diretórios de upload.

Upload de mídia (fluxo recomendado)
1. Cliente solicita SAS URL: `POST /api/v1/media/presign` → API cria blob name e gera SAS com permissões PUT, tempo curto (ex.: 5–15 minutos).
2. Cliente faz PUT direto ao Blob Storage.
3. Cliente notifica API (`POST /api/v1/media/complete`) para associar `Media` ao `Fornecedor`.

Background jobs
- Usar Hangfire com storage em SQL Server Express.
- Jobs recomendados: image resizing, sending emails, cleanup tasks.

Logging e observabilidade (sem serviços pagos)
- Serilog configurado com sinks:
  - `Serilog.Sinks.File` (rolling files)
  - `Serilog.Sinks.EventLog` (Windows Event Log)
- Rotação de logs: por dia e por tamanho; manter política de retenção definida.
- Métricas básicas: endpoints de health; opcionalmente `prometheus-net` para métricas expostas localmente.

Deploy para Windows Server 2022 / IIS
1. Instalar .NET 9 Hosting Bundle (ANCM) no servidor.
2. Criar App Pool (identidade dedicada — ex.: `guia_noivas_pool`) e Site apontando para pasta `publish`.
3. Publicar com `dotnet publish -c Release -o publish` e copiar para servidor (SMB, Web Deploy, ou manual).
4. Definir Environment Variables no IIS (connection string, JWT secret, blob storage connection).
5. Configurar pasta de keys do Data Protection com ACL restrita para identity do App Pool.
6. Habilitar HTTPS (certificado instalado) e redirecionamento.
7. Rodar migrations manualmente (`dotnet ef database update`) ou executar rotina de startup para aplicar migrações (opção arriscada sem supervisão).

Backups e manutenção
- Agendar backups do banco SQL Server (scripts T-SQL agendados via Task Scheduler ou SQL Agent).
- Backup de blobs: se Azure Blob, usar lifecycle e snapshots; se local, rotina de backup do filesystem.

Testes
- Unit tests: xUnit + Moq para services e validators.
- Integration tests: WebApplicationFactory<TEntryPoint> com SQL Server LocalDB / SQL Express de teste.
- E2E: Cypress/Playwright no frontend apontando para ambiente de staging.

OpenAPI / Swagger
- Gerar com Swashbuckle; habilitar nos ambientes de dev/staging; proteger no prod se necessário.

Migrações e seeds
- Manter migrations no repositório (Migrations folder).
- Criar seed idempotente para categorias padrão e alguns fornecedores de exemplo.

Critérios de aceitação
- Endpoints funcionando conforme contratos acima.
- Autenticação JWT + refresh funcionando com roles.
- Upload de mídia via SAS URL funcionando e arquivos acessíveis por CDN/Blob public URL.
- Admin consegue marcar `destaque` e editar conteúdo institucional.
- Swagger disponível e documentação legível.

Estimativa de esforço (alto nível)
- MVP (CRUD Fornecedores, Categorias, Institucional, Contato): 2–3 semanas (1 dev experiente em .NET)
- Autenticação + Admin + Media upload (SAS): 1–2 semanas
- Background jobs + deploy em IIS + testes: 1 semana
- Testes e documentação final: 1 semana
Total estimado: 5–7 semanas para produto mínimo viável.

Próximos passos sugeridos
1. Deseja que eu gere um esqueleto de projeto .NET 9 (Controllers, DTOs, EF Core configurado para SQL Server Express, exemplo de migrations, README com passos de deploy para IIS)?
2. Ou prefere que eu gere primeiro um documento OpenAPI (YAML) com todos os endpoints e DTOs para enviar ao desenvolvedor?

Arquivo gerado: `docs/api-spec-dotnet9-sqlserver.md`

---
