Front-end Specification — Guia Noivas

Versão: 2025-11-21

Propósito: este documento descreve todos os requisitos e especificações para a squad frontend implementar a aplicação SPA em Angular 19 que consome a API `guia-noivas-api`.

Visão geral
- Framework: Angular 19 (TypeScript, RxJS)
- Hospedagem: Netlify (build: `ng build --configuration=production`)
- Autenticação: JWT (Bearer) + Refresh Token (endpoints no backend em `/api/v1/auth`)
- Upload de imagens: fluxo presign -> PUT direto para `uploadUrl` (SAS) -> POST metadata (`/api/v1/media`)
- Observação: Não usar KeyVault, Application Insights ou Redis.

Conteúdo deste documento
- Requisitos funcionais
- Rotas e estrutura de navegação
- Componentes e páginas
- Formulários (campos, validações, botões)
- Grids e listagens
- Fluxo de upload de imagens (detalhado)
- Autenticação e armazenamento de tokens
- Serviços, Interceptors e Guards sugeridos
- Contrato de API (endpoints e exemplos de payload)
- UX / Acessibilidade / Notificações
- Netlify / Deploy / Dev proxy
- QA / Critérios de aceitação
- Backlog de tarefas e estimativas

--------------------------------------------------------------------------------

1) Requisitos funcionais (resumido)

- Usuários públicos: visualizar lista de fornecedores, ver detalhe por slug, enviar contato.
- Administradores (`Role = Admin`): criar, editar, excluir fornecedores; gerenciar imagens (upload via presign e associar metadados).
- Sistema de autenticação: login, registro, refresh de tokens e logout.
- Uploads de imagens: presign (API) + upload direto para storage + criação de metadados na API.

Regra importante: Um Fornecedor deve existir antes de associar imagens a ele (criar fornecedor primeiro, depois enviar imagens com `FornecedorId`).

--------------------------------------------------------------------------------

2) Rotas (SPA)

- `/` — Home (destaques)
- `/fornecedores` — Lista pública com paginação
- `/fornecedores/:slug` — Detalhes do fornecedor (galeria + contato)
- `/auth/login` — Login
- `/auth/register` — Registro
- `/admin` — Dashboard admin (protegido)
  - `/admin/fornecedores` — Grid admin
  - `/admin/fornecedores/new` — Formulário criar
  - `/admin/fornecedores/:id/edit` — Formulário editar
  - `/admin/fornecedores/:id/media` — Gerenciador de imagens do fornecedor

--------------------------------------------------------------------------------

3) Estrutura de projeto sugerida

- `src/app/core/` — serviços centrais: `auth.service.ts`, `api.service.ts`, `blob.service.ts`, `interceptors/`, `guards/`
- `src/app/shared/` — UI components (toasts, modal confirm, spinner, pagination)
- `src/app/features/fornecedores/` — `list/`, `detail/`, `form/`, `media/`
- `proxy.conf.json` — proxy para desenvolvimento (`/api` → `https://localhost:5001`)
- `netlify.toml` — config Netlify

--------------------------------------------------------------------------------

4) Componentes / Páginas (detalhado)

- `FornecedoresListComponent`: grid com paginação server-side, filtros por nome/cidade, ações (view/edit/media/delete)
- `FornecedorDetailComponent`: dados do fornecedor, galeria de imagens, contato e botão de visita
- `FornecedorFormComponent`: formulário reusável para create/edit
- `MediaManagerComponent`: upload em lote, preview, progresso, deletar
- `LoginComponent`, `RegisterComponent`

--------------------------------------------------------------------------------

5) Formulários — campos, validações, botões

Fornecedor (Create/Edit)
- Nome: required, maxlength 200
- Slug: optional, maxlength 200 (auto-gerar se vazio)
- Descrição: optional, maxlength 4000
- Cidade: optional, maxlength 100
- Telefone: optional, maxlength 50
- Email: optional, maxlength 200, email validator
- Website: optional, maxlength 250, URL validator
- Destaque: boolean
- SeloFornecedor: boolean
- Rating: decimal optional 0..5 (step 0.1)

Botões: `Salvar` (primary), `Cancelar`, `Excluir` (danger — somente em edição)

Contact form (public)
- Nome (required), Email (required), Telefone (opcional), Mensagem (required, maxlength 2000)

Media metadata form (admin) — campos: FornecedorId (required), Url, Filename, ContentType, Width, Height, IsPrimary

Validação: implementar validação client-side refletindo atributos do backend e mapear `ValidationProblem` do servidor para os campos.

--------------------------------------------------------------------------------

6) Grids / Listagens

- Fornecedores grid (desktop): Nome, Cidade, Rating, Destaque, Visitas, Actions
- Paginação server-side (page, pageSize); pageSize options: 12/24/48
- Filtros: nome, cidade
- Empty state com CTA criar (admin)

--------------------------------------------------------------------------------

7) Fluxo de upload de imagens (presign → PUT → metadata)

Passos por arquivo:
1. `POST /api/v1/media/presign` — Body: `{ Filename, ContentType, FornecedorId }` (Auth)
2. Receber `{ uploadUrl, blobName, publicUrl? }`
3. `PUT uploadUrl` com header `Content-Type` correto e corpo do arquivo
4. `POST /api/v1/media` (Admin) com `CreateMediaDto` para criar metadados
5. Atualizar UI com thumbnail

UX: progress bars, retries, bulk upload, preview

Observação: confirmar CORS nas SAS URLs com infra

--------------------------------------------------------------------------------

8) Autenticação — detalhes

Armazenamento de tokens
- `accessToken` em memória (AuthService)
- `refreshToken` em `localStorage` (documentar risco XSS; ideal migrar para httpOnly cookie no futuro)

Fluxo
- Login: `POST /api/v1/auth/login` → `{ accessToken, refreshToken, expiresIn, user }`
- Refresh: `POST /api/v1/auth/refresh` → novos tokens
- Logout: `POST /api/v1/auth/logout` → limpar tokens localmente

Interceptor (AuthInterceptor)
- Adiciona Authorization header nas requests
- Em 401 (não para auth/refresh): tenta refresh (uma vez), reenvia request se obtiver novo token; se falhar, redireciona para login

Guards: `AuthGuard`, `RoleGuard` (verifica `Admin`)

---

### AuthInterceptor — comportamento recomendado (detalhado)

- Anexar `Authorization: Bearer <accessToken>` a todas as requisições exceto as da rota `/api/v1/auth`.
- Gerenciar `401 Unauthorized` com um único fluxo de refresh:
  - Quando receber 401 em uma requisição não-auth, o interceptor deve **pausar** novas requisições que exigem autenticação e iniciar uma única chamada `POST /api/v1/auth/refresh` com o `refreshToken` armazenado.
  - Implementar uma fila (queue) para requests pendentes: enquanto o refresh estiver em andamento, as requisições aguardam; quando o refresh completar com sucesso, repetir as requisições pendentes com o novo access token.
  - Se o refresh falhar (401/invalid), limpar tokens, esvaziar fila com erro e redirecionar para `/auth/login`.
  - Evitar loops: se a tentativa de refresh for feita e retornar 401, não tentar novamente.

Pseudo-código (Fluxo):
```
onRequest(req):
  if isAuthRequest(req): return next(req)
  attach accessToken
  return next(req).catch(err => {
    if err.status == 401 and !isRefreshing:
      isRefreshing = true
      refreshToken().then(newTokens => {
         update tokens
         isRefreshing = false
         retry pending requests
      }).catch(() => {
         logout();
      })
    enqueue request and return promise that will be resolved after refresh
  })
```

Opções de armazenamento dos tokens (recomendação)
- `accessToken`: manter em memória (no `AuthService.currentUser`/BehaviorSubject). Evita exposição por XSS.
- `refreshToken`: persistir em `localStorage` por enquanto (faça revisão de segurança e mitigação XSS), planejar migração para cookie httpOnly Secure SameSite quando possível.

---

### AuthService — esqueleto sugerido (TypeScript)

```ts
export interface AuthResponse { accessToken: string; refreshToken: string; expiresIn: number; user: any }

@Injectable({ providedIn: 'root' })
export class AuthService {
  currentUser$ = new BehaviorSubject<User | null>(null)
  private accessToken?: string

  login(email: string, password: string) { return this.http.post<AuthResponse>(...)
    .pipe(tap(res => { this.accessToken = res.accessToken; localStorage.setItem('refresh', res.refreshToken); this.currentUser$.next(res.user); })) }

  refresh() { const rt = localStorage.getItem('refresh'); return this.http.post<AuthResponse>('/auth/refresh', { refreshToken: rt }) }

  logout() { const rt = localStorage.getItem('refresh'); this.http.post('/auth/logout', { refreshToken: rt }).subscribe(); localStorage.removeItem('refresh'); this.accessToken = undefined; this.currentUser$.next(null) }
}
```

---

### Error handling & ValidationProblem mapping

- Quando o backend retornar `400 ValidationProblem`, o `ErrorInterceptor` deve extrair `errors` do payload e mapear para `FormControl.setErrors({ server: 'mensagem' })` para exibir mensagens inline.
- Exemplo de payload `ValidationProblem` (ASP.NET):
```json
{
  "type":"https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title":"One or more validation errors occurred.",
  "status":400,
  "errors":{
    "Nome":["O campo Nome é obrigatório."],
    "Email":["Email inválido."]
  }
}
```

Implementação recomendada:
- No `ErrorInterceptor`, quando `status === 400` e payload contém `errors`, propagar um objeto `{ fieldErrors }` para o componente do formulário que aplicará `setErrors` nos controles correspondentes.

---

### Upload: exemplos práticos (curl)

- Obter presign (Angular usa HttpClient):
```
curl -X POST "${API_BASE_URL}/api/v1/media/presign" -H "Authorization: Bearer <token>" -H "Content-Type: application/json" -d '{"filename":"foto.jpg","contentType":"image/jpeg","fornecedorId":"<GUID>"}'
```
- Fazer PUT para `uploadUrl` (exemplo usando curl):
```
curl -X PUT "<uploadUrl>" -H "x-ms-blob-type: BlockBlob" -H "Content-Type: image/jpeg" --data-binary @foto.jpg
```
Nota: o header `x-ms-blob-type: BlockBlob` é necessário para Azure Blob via REST; o `uploadUrl` gerado deve conter SAS com permissões.

---

### CORS e SAS URLs

- Verificar que a Storage Account (Azure) tem CORS configurado para permitir a origem do frontend (Netlify) e os métodos `PUT, GET, OPTIONS` e cabeçalhos `Content-Type, x-ms-blob-type`.

---

### E2E / Smoke tests recomendados

- E2E (Cypress) flows:
  1. Login como admin → criar fornecedor → abrir detalhe → abrir media manager → upload 1 arquivo → verificar thumbnail
  2. Login não-admin → tentar acessar `/admin` → verificar bloqueio
  3. Public: acessar `/fornecedores`, abrir detalhe, enviar contato → confirmar 202

- Script de smoke-test (bash/curl) para rodar após deploy (pode ser job em CI):
```bash
#!/usr/bin/env bash
BASE="${API_BASE_URL}"
# check public list
curl -s "${BASE}/fornecedores?page=1&pageSize=1" | jq . >/dev/null || exit 1
# check swagger reachable (if exposed)
curl -s -o /dev/null -w "%{http_code}" "${BASE%/api/v1}/swagger/index.html" | grep -E "200|302" || exit 1
echo "smoke ok"
```

---

### Segurança e recomendações operacionais

- Forçar HTTPS em produção (Netlify já disponibiliza TLS)
- Implementar CSP e revisar XSS nas páginas que exibem conteúdo do usuário
- Planejar migração de `refreshToken` para cookie httpOnly e SameSite
- Não logar tokens em console/telemetria

--------------------------------------------------------------------------------

9) Contrato de API — endpoints principais

Base: `${API_BASE_URL}/api/v1`

Auth
- `POST /auth/login`
- `POST /auth/register`
- `POST /auth/refresh`
- `POST /auth/logout`

Fornecedores
- `GET /fornecedores?page=&pageSize=`
- `GET /fornecedores/slug/{slug}`
- `POST /fornecedores/{id}/contact` (ContactDto)

Admin Fornecedores
- `POST /admin/fornecedores`
- `PUT /admin/fornecedores/{id}`
- `PATCH /admin/fornecedores/{id}/destaque`
- `DELETE /admin/fornecedores/{id}`

Media
- `POST /media/presign` (Auth)
- `POST /media` (Admin)
- `DELETE /media/{id}` (Admin)

Exemplos rápidos (copiar/colar)
- Login
```json
{ "email":"admin@example.com", "password":"Secret123!" }
```
- Create fornecedor
```json
{
  "nome":"Ateliê Flores & Cia",
  "slug":"atelie-flores-cia",
  "descricao":"...",
  "cidade":"São Paulo",
  "telefone":"(11)99999-9999",
  "email":"contato@atelieflores.com",
  "website":"https://atelieflores.com",
  "destaque": true,
  "seloFornecedor": false,
  "rating": 4.7
}
```

--------------------------------------------------------------------------------

10) UX / Acessibilidade / Notificações

- Use markup semântico, `aria-label` nos botões, foco visível e leitores de tela compatíveis
- Toast para sucessos/erros
- Modal confirm para deletes

--------------------------------------------------------------------------------

11) Netlify / Build / Dev proxy

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

proxy.conf.json (dev)
```json
{
  "/api": { "target": "https://localhost:5001", "secure": false, "changeOrigin": true }
}
```

Env vars Netlify:
- `API_BASE_URL` = `https://guia-noivas.somee.com/api/v1`

--------------------------------------------------------------------------------

12) QA / Critérios de aceitação

- Login/refresh/logout automático
- Guards bloqueiam rotas admin
- CRUD admin com validação e mensagens do servidor
- Upload via presign funcionando
- Contact form responde 202

--------------------------------------------------------------------------------

13) Backlog técnico (tarefas e estimativas)

- Inicialização + Netlify: 4h
- AuthService + Interceptor + Guards: 10h
- Lista Fornecedores: 10h
- Detalhe + Contato: 6h
- Admin CRUD: 14h
- MediaManager: 14h
- Shared components: 6h
- E2E tests: 8h
- Docs & deploy: 4h

Estimativa total ≈ 76h

--------------------------------------------------------------------------------

14) Próximos passos sugeridos

1. Implementar AuthService + AuthInterceptor
2. Implementar listagem pública e detalhe
3. Implementar CRUD admin
4. Implementar MediaManager

--------------------------------------------------------------------------------

Documento gerado para a squad frontend. Para dúvidas, abrir issue com etiqueta `frontend-spec`.

Fim do documento
