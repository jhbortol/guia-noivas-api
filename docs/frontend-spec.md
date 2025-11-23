Front-end Specification — Guia Noivas

Versão: 2025-11-21 (revisado)

Propósito: este documento descreve os requisitos, contratos de API e orientações de implementação para a aplicação SPA (Angular 19) que consome a API `guia-noivas-api`. O objetivo é garantir coerência entre frontend e backend (endpoints, DTOs, fluxos de upload, autenticação e critérios de QA/E2E).

Visão geral
- Framework: Angular 19 (TypeScript, RxJS)
- Hospedagem: Netlify (build: `ng build --configuration=production`)
- Autenticação: JWT (Bearer) + Refresh Token (endpoints no backend em `/api/v1/auth`)
 - Upload de imagens: o upload é realizado diretamente pelo backend via endpoint dedicado, amarrando a imagem ao fornecedor. O frontend envia o arquivo e os metadados para o backend, que realiza o armazenamento e retorna a URL pública.
- Observação operacional: a infra usa SQL Server em produção; nos testes locais/integrados usamos SQLite — o frontend não precisa tratar isso, mas testes automatizados devem considerar diferenças de API quando aplicável.

Conteúdo deste documento
- Requisitos funcionais
- Rotas e estrutura de navegação
- Componentes e páginas
- Formulários (campos, validações, botões)
- Grids e listagens
- Fluxo de upload de imagens (detalhado) + contratos presign
- Autenticação e armazenamento de tokens
- Serviços, Interceptors e Guards sugeridos
- Contrato de API (endpoints, DTOs e exemplos)
- UX / Acessibilidade / Notificações
- Netlify / Deploy / Dev proxy
- QA / Critérios de aceitação e E2E
- CI/CD (migrations & smoke-tests)
- Backlog e estimativas

--------------------------------------------------------------------------------

1) Requisitos funcionais (resumido)

- Usuários públicos: visualizar lista de fornecedores, ver detalhe por slug, enviar contato.
- Administradores (`Role = Admin`): criar, editar, excluir fornecedores; gerenciar imagens (upload via presign e associar metadados); marcar imagem principal.
- Sistema de autenticação: login, refresh de tokens e logout. Operações admin exigem claim `Role=Admin`.
 - Uploads de imagens: o frontend envia o arquivo e os metadados via endpoint dedicado para o backend, que realiza o upload, associa ao fornecedor e retorna a URL pública. O frontend deve reagir a erros de upload e exibir progresso.

 Regra importante: Quando associando uma imagem a um `Fornecedor`, este deve existir antes (criar fornecedor primeiro, depois enviar imagens com `fornecedorId`). Contudo, `fornecedorId` no contrato de upload é opcional: o frontend pode omitir o campo para enviar imagens genéricas ou associadas a outras entidades (ex.: categorias). O upload é sempre feito pelo backend, nunca direto para storage.

--------------------------------------------------------------------------------

2) Rotas (SPA)

- `/` — Home (destaques)
- `/fornecedores` — Lista pública com paginação e filtros
- `/fornecedores/:slug` — Detalhes do fornecedor (galeria + contato)
- `/auth/login` — Login
- `/auth/register` — Registro (se aplicável)
- `/admin` — Dashboard admin (protegido)
  - `/admin/fornecedores` — Grid admin
  - `/admin/fornecedores/new` — Formulário criar
  - `/admin/fornecedores/:id/edit` — Formulário editar
  - `/admin/fornecedores/:id/media` — Gerenciador de imagens do fornecedor (upload/list/reorder/delete)

--------------------------------------------------------------------------------

3) Estrutura de projeto sugerida

- `src/app/core/` — serviços centrais: `auth.service.ts`, `api.service.ts`, `blob.service.ts`, `interceptors/`, `guards/`
- `src/app/shared/` — UI components (toasts, modal confirm, spinner, pagination, empty-state)
- `src/app/features/fornecedores/` — `list/`, `detail/`, `form/`, `media/`
- `proxy.conf.json` — proxy para desenvolvimento (`/api` → `https://localhost:5001`)
- `netlify.toml` — config Netlify

Observação: mantenha os serviços pequenos e testáveis; prefira `HttpClient` com métodos centrados em contratos (DTOs) e mapeamento de erros central.

--------------------------------------------------------------------------------

4) Componentes / Páginas (detalhado)

- `FornecedoresListComponent`: grid com paginação server-side, filtros por nome/cidade, ações (view/edit/media/delete). Implementar debounce nos filtros.
- `FornecedorDetailComponent`: dados do fornecedor, galeria de imagens (lazy load), contato público e botão para registrar visita (POST `/fornecedores/{id}/visit`).
- `FornecedorFormComponent`: formulário reusável para create/edit (exibir validações do servidor).
 - `MediaManagerComponent`: upload em lote (via backend), preview, progresso, deletar, marcar primária, reordenar (ou endpoint de prioridade)
- `LoginComponent`, `RegisterComponent`, `AdminLayoutComponent`

--------------------------------------------------------------------------------

5) Formulários — campos, validações, botões (contrato com backend)

Fornecedor (Create/Edit) — DTO esperado no backend (`FornecedorCreateDto` / `FornecedorUpdateDto`)
- `nome` (string, required, max 200)
- `slug` (string, optional, max 200) — frontend pode auto-gerar `slug` baseado no `nome` e permitir edição
- `descricao` (string, optional, max 4000)
- `cidade` (string, optional, max 100)
- `telefone` (string, optional, max 50)
- `email` (string, optional, max 200, email format)
- `website` (string, optional, max 250, URL format)
- `destaque` (bool)
- `seloFornecedor` (bool)
- `rating` (decimal?, optional 0..5)
- `categoriaId` (GUID?, optional) — validate existence client-side if selected

Botões: `Salvar` (primary), `Cancelar`, `Excluir` (danger — somente em edição e exibido conforme role)

Contact form (public)
- `nome` (required), `email` (required), `telefone` (optional), `mensagem` (required, max 2000)

Media metadata (CreateMediaDto)
- `fornecedorId` (GUID, required)
- `url` (string, required) — publicUrl or storage URL
- `filename` (string)
- `contentType` (string)
- `width` (int?), `height` (int?)
- `isPrimary` (bool)

Validação: implementar validação client-side refletindo as regras acima e mapear `ValidationProblem` do servidor para cada campo usando `setErrors`.

--------------------------------------------------------------------------------

6) Grids / Listagens

- Fornecedores grid (desktop): colunas principais — Nome, Cidade, Rating, Destaque, Visitas, Actions (View/Edit/Media/Delete)
- Paginação server-side: query params `page` (int), `pageSize` (int), `categoriaId` (GUID optional)
- pageSize options: 12/24/48
- Filtros: nome (text, contains), cidade (text)
- Empty state com CTA criar (admin)

API contract note: the API returns `{ data: [...], meta: { total, page, pageSize } }` for paged endpoints.

--------------------------------------------------------------------------------

7) Fluxo de upload de imagens (presign → PUT → metadata) — contrato e exemplos

Fluxo de upload de imagens (upload via backend) — contrato e exemplos

Resumo rápido (novo fluxo):
1) Frontend envia `POST /api/v1/media/upload` (multipart/form-data) com o arquivo da imagem e os metadados (`fornecedorId`, `filename`, `contentType`, `isPrimary`, etc).
2) Backend realiza o upload, associa a imagem ao fornecedor, armazena e retorna a URL pública e os dados persistidos.

Exemplo de request (multipart/form-data):
```
POST /api/v1/media/upload
Content-Type: multipart/form-data

 Campos:
 - file: arquivo da imagem
 - fornecedorId: GUID (opcional) — quando informado, será associado ao fornecedor; omita para media genérica ou de outras entidades
 - filename: string
 - contentType: string
 - isPrimary: bool
 - width: int (opcional)
 - height: int (opcional)
```

Exemplo de response:
```json
{
  "id": "{GUID}",
  "fornecedorId": "{GUID}",
  "url": "https://cdn.example.com/media/...jpg",
  "filename": "foto.jpg",
  "contentType": "image/jpeg",
  "width": 1200,
  "height": 800,
  "isPrimary": false
}
```

Endpoint adicional recomendado: `GET /api/v1/media?fornecedorId={GUID}` — lista as medias associadas ao fornecedor.

UX: exibir progress bar por arquivo, retries automáticos para falhas de rede (com backoff) e um estado de erro que permite tentar enviar novamente ou remover o item. O upload é sempre feito pelo backend, nunca direto para storage.

--------------------------------------------------------------------------------

8) Autenticação — recomendações de implementação no frontend

Armazenamento de tokens (recomendado por agora)
- `accessToken`: armazenar em memória (AuthService) e expor via `BehaviorSubject`.
- `refreshToken`: armazenar em `localStorage` (temporário) OU preferir cookie httpOnly Secure SameSite se infraestrutura permitir.

Fluxo (recap):
- Login: `POST /api/v1/auth/login` → `{ accessToken, refreshToken, expiresIn, user }`
- Interceptor (AuthInterceptor) anexa `Authorization: Bearer <accessToken>` a requests protegidas.
- Em caso de 401: disparar um fluxo único de refresh (POST `/api/v1/auth/refresh`) e enfileirar requests até o refresh completar, conforme pseudo-código abaixo.

AuthInterceptor — fluxo recomendado (implementação resumida)
- Antes de enviar request: se rota inicia por `/api/v1/auth` não anexar token.
- Em erro 401: se não estiver em refresh, iniciar refresh; enfileirar as demais requests. Quando refresh retorna novo token, repetir as requests; se refresh falhar, logout e redirecionar para login.

Pseudocódigo (esqueleto TypeScript já usado pela equipe):

```ts
// comportamento resumido: attach token, handle 401 with single refresh operation and a queue
```

Obs.: documentar o formato do payload de login/refresh para o time de frontend (veja seção Contrato de API).

--------------------------------------------------------------------------------

9) Error handling & ValidationProblem mapping (detalhado)

- Quando o backend retornar `400 ValidationProblem` (padrão ASP.NET), o `ErrorInterceptor` deve extrair `errors` e repassar para o componente para aplicar `setErrors` nos controles.
- Exemplo de payload (será retornado pela API):

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Nome": ["O campo Nome é obrigatório."],
    "Email": ["Email inválido."]
  }
}
```

Implementação prática:
- `ErrorInterceptor` detecta `status === 400` e `body.errors` → transforma em `{ fieldErrors: Record<string,string[]> }` e repassa no `HttpErrorResponse` (ou lança um custom error) que o componente tenta mapear para `FormGroup`.

--------------------------------------------------------------------------------

10) Contrato de API — endpoints, DTOs e exemplos (completos)

Base: `${API_BASE_URL}/api/v1`

Auth
- `POST /auth/login`  
  Request:
  ```json
  { "email": "admin@example.com", "password": "Secret123!" }
  ```
  Response (200):
  ```json
  { "accessToken": "...", "refreshToken": "...", "expiresIn": 3600, "user": { "id": "...", "email": "...", "roles": ["Admin"] } }
  ```

- `POST /auth/refresh`  
  Request:
  ```json
  { "refreshToken": "..." }
  ```
  Response (200): novo par `accessToken`/`refreshToken`.

Fornecedores (público)
- `GET /fornecedores?page=&pageSize=&categoriaId=`  
  Response shape:
  ```json
  { "data": [ { /* FornecedorListDto */ } ], "meta": { "total": 123, "page": 1, "pageSize": 12 } }
  ```

- `GET /fornecedores/slug/{slug}` — retorna `FornecedorDetailDto` (ver exemplo abaixo)

Fornecedor DTOs (exemplos)
- FornecedorListDto (server → client)
  ```json
  {
    "id": "{GUID}",
    "nome": "Ateliê Flores & Cia",
    "slug": "atelie-flores-cia",
    "descricao": "...",
    "cidade": "São Paulo",
    "rating": 4.7,
    "destaque": true,
    "seloFornecedor": false,
    "ativo": true,
    "categoria": { "id": "{GUID}", "nome": "Decoração", "slug": "decoracao" },
    "thumbnail": { "id": "{GUID}", "url": "https://.../thumb.jpg", "isPrimary": true }
  }
  ```

- FornecedorDetailDto (server → client)
  ```json
  {
    "id": "{GUID}",
    "nome": "...",
    "slug": "...",
    "descricao": "...",
    "cidade": "...",
    "telefone": "...",
    "email": "...",
    "website": "...",
    "destaque": true,
    "seloFornecedor": false,
    "ativo": true,
    "rating": 4.7,
    "visitas": 123,
    "createdAt": "2025-11-21T...",
    "updatedAt": null,
    "medias": [ { "id": "{GUID}", "url": "...", "filename": "...", "contentType": "image/jpeg", "isPrimary": true } ],
    "categoria": { "id": "{GUID}", "nome": "...", "slug": "..." }
  }
  ```

Admin Fornecedores (require Admin role)
- `POST /admin/fornecedores` — request `FornecedorCreateDto` (veja seção 5)
- `PUT /admin/fornecedores/{id}` — update
- `PATCH /admin/fornecedores/{id}/destaque` — toggles destaque
- `DELETE /admin/fornecedores/{id}` — remove fornecedor (should cascade delete medias or set null per DB rules)

Nota importante: os endpoints administrativos de criação/edição (`POST /admin/fornecedores` e `PUT /admin/fornecedores/{id}`) aceitam o campo opcional `categoriaId` (GUID). O backend valida a existência da categoria e persiste `CategoriaId` na entidade. O frontend deve enviar `categoriaId` quando o usuário selecionar uma categoria no formulário de criar/editar.

Media
- `POST /media/upload` — upload de imagem e metadados (Admin). Body: multipart/form-data conforme seção 7.
- `GET /media?fornecedorId={GUID}` — lista medias para um fornecedor (público ou admin conforme design).
- `DELETE /media/{id}` — remove media (Admin).

Error shapes
- Validation (400): `ValidationProblem` as shown in section 9.
- Unauthorized (401): `{ message: 'Unauthorized' }` or 401 status (interceptor handles).

--------------------------------------------------------------------------------

11) UX / Acessibilidade / Notificações

- Use markup semântico, `aria-*` attributes, foco visível, contraste adequado e keyboard navigation.
- Toasts para sucessos/erros; modal confirm para ações destrutivas (delete).
- Em upload: mostrar progress bar por arquivo, contagem de uploads pendentes, e feedback de sucesso/erro por item.

--------------------------------------------------------------------------------

12) Netlify / Build / Dev proxy

netlify.toml (exemplo)
```toml
[build]
  command = "npm run build"
  publish = "dist/<app-name>"

[[redirects]]
  from = "/*"
  to = "/index.html"
  status = 200
```

`proxy.conf.json` (dev)
```json
{
  "/api": { "target": "https://localhost:5001", "secure": false, "changeOrigin": true }
}
```

Env vars Netlify recomendadas:
- `API_BASE_URL` = `https://guia-noivas.somee.com/api/v1`
- `NG_BUILD_CONFIGURATION` = `production`

--------------------------------------------------------------------------------

13) QA / Critérios de aceitação e E2E (detalhado)

Critérios automáticos (unit/integration/E2E):
- Login/refresh/logout automático com interceptor e queue de refresh.
- Guards bloqueiam rotas admin para usuários sem role.
- CRUD admin com validação server-side exibida no frontend.
 - Upload funcionando: frontend envia arquivo e metadados para o backend via `/media/upload`, backend realiza o upload, associa ao fornecedor e retorna a thumbnail.
- Contact form responde 202 e exibe confirmação.

E2E (Cypress) suggested flows (concrete):
1. Admin create + upload flow
  - Login as admin (seed user or test account)
  - POST create fornecedor via UI
  - Open media manager for created fornecedor
  - Upload one image: enviar arquivo e metadados via `/media/upload` (multipart/form-data)
  - Assert thumbnail appears and `isPrimary` can be toggled

2. Public contact
   - Visit fornecedor detail, fill contact form, submit, assert 202 and visible confirmation

3. Authorization guard
   - Attempt to access `/admin/fornecedores` as anonymous or non-admin and assert redirect/403 UI

Smoke tests (CI) — minimal curl script
```bash
#!/usr/bin/env bash
BASE="${API_BASE_URL}"
# 1) public list
curl -s "${BASE}/fornecedores?page=1&pageSize=1" | jq . >/dev/null || exit 1
# 2) admin login + create fornecedor (requires test admin credentials set in CI secrets)
 # 3) upload via backend
echo "smoke ok"
```

CI/CD recommendation (GitHub Actions)
- Job `migrations`: run the committed idempotent SQL script `src/GuiaNoivas.Api/Data/Migrations/sql/ef_set_delete_behavior_idempotent.sql` against the target DB (use secure connection string secret). Prefer ops review. If runner has .NET SDK and permission, use `dotnet ef database update`.
- Job `smoke-test`: after deploy, run smoke curl script and optional headless E2E.

--------------------------------------------------------------------------------

14) Backlog técnico (tarefas e estimativas) — atualizado

- Inicialização + Netlify: 4h
- AuthService + Interceptor + Guards: 10h
- Lista Fornecedores: 8h
- Detalhe + Contato: 6h
- Admin CRUD: 12h
  - MediaManager (upload/preview/reorder): 16h
- Shared components: 6h
- E2E tests (Cypress): 10h
- Docs, Swagger examples & deploy scripts: 6h

Estimativa total ≈ 78h (ajustado)

--------------------------------------------------------------------------------

15) Próximos passos recomendados (prioridade)

Alta:
- Implementar `GET /api/v1/media?fornecedorId={id}` no frontend (consumir endpoint já disponível) e adicionar UI de listagem no `MediaManager`.
- Implementar upload flow completo no frontend (POST `/media/upload` com arquivo e metadados), e testes integration.
- Escrever E2E básico Cypress cobrindo create fornecedor + upload + mark-primary.

Média:
- Adicionar exemplos de request/response no Swagger (backend) e exportar exemplos JSON para o frontend.
- Implementar `DatabaseSeeder` para povoar categorias/fornecedores em dev.

Baixa:
- Pipeline CI para aplicar scripts idempotentes e rodar smoke-tests; checklist operacional de storage CORS/SAS.

--------------------------------------------------------------------------------

16) Anexos técnicos rápidos (copiar/colar)


  - Upload example (Angular HttpClient)
```ts
const formData = new FormData();
formData.append('file', file);
formData.append('fornecedorId', fornecedorId);
formData.append('filename', file.name);
formData.append('contentType', file.type);
formData.append('isPrimary', isPrimary);
this.http.post(`${API_BASE}/media/upload`, formData);
```

--------------------------------------------------------------------------------

Documento revisado para a squad frontend. Se quiser, eu:
- implemento o `GET /api/v1/media?fornecedorId=` no frontend/service scaffold;
- crio esqueleto Cypress com um teste E2E básico (admin create + upload);
- adiciono exemplos Swagger/JSON no backend para o fluxo de upload.

Para dúvidas ou alteração de contrato (ex.: persistir metadata no presign vs. persistir após PUT), abrir issue com tag `frontend-spec` e marcar `backend` e `frontend`.

Fim do documento
