# Changelog API - Funcionalidade Testemunhos

**Data**: 24 de Novembro de 2025  
**Versão**: 1.1.0

## 📋 Resumo

Adicionada funcionalidade completa de **Testemunhos** onde noivas podem deixar depoimentos sobre fornecedores.

---

## 🆕 Novos Endpoints

### 1. **Listar Testemunhos por Fornecedor** (Público)
```http
GET /api/v1/testemunhos/fornecedor/{fornecedorId}
```

**Query Parameters:**
- `page` (opcional, default: 1) - Número da página
- `pageSize` (opcional, default: 10) - Itens por página

**Response 200:**
```json
{
  "data": [
    {
      "id": "guid",
      "nome": "Maria Silva",
      "descricao": "Serviço excelente, recomendo!",
      "createdAt": "2025-11-24T10:30:00Z"
    }
  ],
  "meta": {
    "total": 25,
    "page": 1,
    "pageSize": 10,
    "totalPages": 3
  }
}
```

**Response 404:** Fornecedor não encontrado

---

### 2. **Obter Testemunho Específico** (Público)
```http
GET /api/v1/testemunhos/{id}
```

**Response 200:**
```json
{
  "id": "guid",
  "nome": "Maria Silva",
  "descricao": "Serviço excelente, recomendo!",
  "fornecedorId": "guid",
  "createdAt": "2025-11-24T10:30:00Z"
}
```

**Response 404:** Testemunho não encontrado

---

### 3. **Criar Testemunho** (Público)
```http
POST /api/v1/testemunhos
Content-Type: application/json
```

**Request Body:**
```json
{
  "nome": "Maria Silva",
  "descricao": "Serviço excelente, superou todas as expectativas!",
  "fornecedorId": "guid"
}
```

**Validações:**
- `nome`: obrigatório, máximo 200 caracteres
- `descricao`: obrigatório, máximo 2000 caracteres
- `fornecedorId`: obrigatório, deve existir

**Response 201:**
```json
{
  "id": "guid",
  "nome": "Maria Silva",
  "descricao": "Serviço excelente, superou todas as expectativas!",
  "fornecedorId": "guid",
  "createdAt": "2025-11-24T10:30:00Z"
}
```

**Response 400:** Dados inválidos  
**Response 404:** Fornecedor não encontrado

---

### 4. **Listar Todos Testemunhos** (Admin)
```http
GET /api/v1/admin/testemunhos
Authorization: Bearer {token}
```

**Query Parameters:**
- `page` (opcional, default: 1)
- `pageSize` (opcional, default: 20)
- `fornecedorId` (opcional) - Filtrar por fornecedor

**Response 200:**
```json
{
  "data": [
    {
      "id": "guid",
      "nome": "Maria Silva",
      "descricao": "Serviço excelente!",
      "fornecedorId": "guid",
      "fornecedorNome": "Buffet Estrela",
      "createdAt": "2025-11-24T10:30:00Z"
    }
  ],
  "meta": {
    "total": 150,
    "page": 1,
    "pageSize": 20,
    "totalPages": 8
  }
}
```

**Response 401:** Não autenticado  
**Response 403:** Sem permissão (requer role Admin)

---

### 5. **Remover Testemunho** (Admin)
```http
DELETE /api/v1/admin/testemunhos/{id}
Authorization: Bearer {token}
```

**Response 204:** Testemunho removido com sucesso  
**Response 401:** Não autenticado  
**Response 403:** Sem permissão (requer role Admin)  
**Response 404:** Testemunho não encontrado

---

## 🔄 Endpoints Modificados

### **GET /api/v1/fornecedores/{id}** (Modificado)
### **GET /api/v1/fornecedores/slug/{slug}** (Modificado)

Agora retornam os testemunhos do fornecedor automaticamente:

**Response 200:**
```json
{
  "id": "guid",
  "nome": "Buffet Estrela",
  "slug": "buffet-estrela",
  "descricao": "...",
  "cidade": "Piracicaba",
  "telefone": "19 99999-9999",
  "email": "contato@buffetestrela.com.br",
  "website": "https://buffetestrela.com.br",
  "destaque": true,
  "seloFornecedor": true,
  "ativo": true,
  "rating": 4.8,
  "visitas": 150,
  "createdAt": "2025-01-15T10:00:00Z",
  "updatedAt": null,
  "imagens": [...],
  "categoria": {...},
  "testemunhos": [
    {
      "id": "guid",
      "nome": "Maria Silva",
      "descricao": "Buffet maravilhoso, comida excelente!",
      "createdAt": "2025-11-20T15:30:00Z"
    },
    {
      "id": "guid",
      "nome": "João Santos",
      "descricao": "Atendimento impecável, recomendo!",
      "createdAt": "2025-11-18T10:15:00Z"
    }
  ]
}
```

**Observações:**
- Testemunhos são ordenados do mais recente para o mais antigo
- Campo `testemunhos` pode ser um array vazio `[]` se não houver testemunhos

---

## 📊 Modelo de Dados

### Testemunho
```typescript
interface Testemunho {
  id: string;                    // GUID
  nome: string;                  // Máx 200 caracteres
  descricao: string;             // Máx 2000 caracteres
  fornecedorId: string;          // GUID do fornecedor
  createdAt: string;             // ISO 8601 datetime
}
```

### TestemunhoListDto (usado em listagens)
```typescript
interface TestemunhoListDto {
  id: string;
  nome: string;
  descricao: string;
  createdAt: string;
  // Nota: fornecedorId não está incluído nas listagens por fornecedor
}
```

---

## 🔗 Relacionamentos

- **Fornecedor 1:N Testemunhos** - Um fornecedor pode ter vários testemunhos
- **Testemunho N:1 Fornecedor** - Cada testemunho pertence a apenas um fornecedor
- **Cascade Delete**: Se um fornecedor for deletado, todos seus testemunhos são removidos automaticamente

---

## 💡 Sugestões de Implementação no Frontend

### 1. **Página de Detalhes do Fornecedor**
- Exibir seção "Avaliações e Testemunhos" 
- Mostrar todos os testemunhos retornados no campo `testemunhos`
- Adicionar botão "Deixar Testemunho" que abre modal/formulário

### 2. **Formulário de Novo Testemunho**
```typescript
// Exemplo de chamada POST
const novoTestemunho = {
  nome: "Maria Silva",
  descricao: "Excelente serviço...",
  fornecedorId: fornecedorId
};

const response = await fetch('/api/v1/testemunhos', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(novoTestemunho)
});
```

### 3. **Validações Frontend** (recomendadas)
- Nome: obrigatório, 3-200 caracteres
- Descrição: obrigatório, 10-2000 caracteres
- Limitar caracteres com contador visual
- Sanitizar HTML/JavaScript antes de enviar

### 4. **Paginação de Testemunhos**
Se houver muitos testemunhos, considere:
```typescript
// Carregar mais testemunhos via endpoint específico
const response = await fetch(
  `/api/v1/testemunhos/fornecedor/${fornecedorId}?page=2&pageSize=10`
);
```

### 5. **Admin - Gerenciar Testemunhos**
- Lista todos os testemunhos com filtro por fornecedor
- Botão para remover testemunhos inadequados
- Exibir nome do fornecedor junto ao testemunho

---

## ⚠️ Breaking Changes

**Nenhum breaking change.** Apenas adições à API existente.

Os endpoints de fornecedores continuam funcionando normalmente, apenas com um novo campo `testemunhos` na resposta.

---

## 🐛 Tratamento de Erros

Todos os endpoints retornam ProblemDetails em caso de erro:

```json
{
  "title": "Fornecedor não encontrado",
  "detail": "Fornecedor com ID {guid} não foi encontrado.",
  "status": 404
}
```

---

## 📝 Notas Adicionais

1. **Moderação**: Não há moderação automática de testemunhos. Admins podem remover via endpoint DELETE.

2. **Anonimato**: Campo `nome` é obrigatório, mas não há verificação se é nome real.

3. **Limite de caracteres**: 
   - Nome: 200 caracteres
   - Descrição: 2000 caracteres

4. **Ordenação**: Sempre do mais recente para o mais antigo (CreatedAt DESC)

5. **Performance**: Testemunhos são carregados automaticamente com o fornecedor (eager loading)

---

## 📞 Contato

Para dúvidas sobre a implementação, consulte a especificação completa em `api-spec-dotnet9-sqlserver.md`.
