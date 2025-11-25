# Guia de Deploy - Somee.com

## 📦 Publicar a aplicação localmente

```powershell
# 1. Navegar até a pasta do projeto
cd C:\fontes\guia-noivas-api\src\GuiaNoivas.Api

# 2. Publicar em Release mode
dotnet publish -c Release -o publish

# 3. Os arquivos estarão em: C:\fontes\guia-noivas-api\src\GuiaNoivas.Api\publish
```

## 🌐 Deploy para Somee.com

### Opção 1: Via FTP (Recomendado)

1. **Acessar via FTP:**
   - Host: `ftp://guia-noivas.somee.com` (ou conforme painel Somee)
   - Usuário: seu usuário Somee
   - Senha: sua senha Somee
   - Porta: 21

2. **Upload dos arquivos:**
   - Conectar no FTP (pode usar FileZilla, WinSCP ou outro cliente)
   - Navegar até a pasta do site (geralmente `/wwwroot` ou `/`)
   - **IMPORTANTE:** Parar o site antes no painel Somee
   - Fazer upload de todos os arquivos da pasta `publish/`
   - **NÃO deletar** a pasta `App_Data` se já existir (contém o banco de dados)

3. **Reiniciar o site:**
   - Voltar ao painel Somee.com
   - Iniciar o site novamente

### Opção 2: Via Painel Somee.com (File Manager)

1. Acessar painel Somee.com
2. Ir em "File Manager"
3. Parar o site (botão "Stop")
4. Fazer upload dos arquivos da pasta `publish/`
5. Iniciar o site (botão "Start")

### Opção 3: Publicação direta via Visual Studio / VS Code

```powershell
# Criar perfil de publicação FTP
# Tools > Publish > Add a publish profile > FTP
# Preencher com dados do Somee.com
```

## 🔧 Configurações no Somee.com

### Variáveis de Ambiente (se necessário)

No painel Somee.com, configurar:
- `CONNECTION_STRING`: String de conexão do SQL Server Express fornecida pelo Somee
- `JWT_SECRET`: Segredo para JWT (gerar um aleatório seguro)
- `STORAGE_CONNECTION_STRING`: (opcional) Azure Blob Storage

### Connection String Somee.com

Geralmente no formato:
```
Server=SomeeServerAddress;Database=YourDatabaseName;User Id=YourUsername;Password=YourPassword;
```

## ✅ Verificar Deploy

Após o deploy, testar:

```bash
# Health check
GET https://guia-noivas.somee.com/api/v1/health/live

# Swagger (se habilitado)
GET https://guia-noivas.somee.com/swagger

# Login
POST https://guia-noivas.somee.com/api/v1/auth/login
Content-Type: application/json

{
  "email": "seu@email.com",
  "password": "suasenha"
}
```

## 🐛 Troubleshooting

### Erro 404
- Verificar se todos os arquivos foram enviados
- Verificar se o site está rodando no painel Somee
- Verificar se a pasta de destino está correta

### Erro 500
- Verificar logs no painel Somee (se disponível)
- Verificar connection string do banco
- Verificar se as migrations foram aplicadas

### Banco de Dados
```powershell
# Aplicar migrations manualmente (via SQL no painel Somee)
# Ou garantir que o código aplica no startup (já configurado no Program.cs)
```

## 📝 Checklist de Deploy

- [ ] Código commitado no Git
- [ ] Build local sem erros (`dotnet build`)
- [ ] Publicação criada (`dotnet publish -c Release`)
- [ ] Site parado no Somee
- [ ] Arquivos enviados via FTP
- [ ] Connection string configurada
- [ ] Site iniciado no Somee
- [ ] Testes de API funcionando
- [ ] CORS funcionando (testar do frontend)

## 🚀 Deploy Rápido (Script PowerShell)

```powershell
# deploy.ps1
$publishPath = ".\src\GuiaNoivas.Api\publish"
$ftpServer = "ftp://guia-noivas.somee.com"
$ftpUser = "SEU_USUARIO"
$ftpPass = "SUA_SENHA"

# Publicar
Write-Host "Publicando aplicação..."
dotnet publish .\src\GuiaNoivas.Api\GuiaNoivas.Api.csproj -c Release -o $publishPath

Write-Host "Arquivos prontos em: $publishPath"
Write-Host "Agora faça upload via FTP para: $ftpServer"
Write-Host "Use FileZilla ou WinSCP para fazer o upload"
```

---

**Nota:** Como o Somee.com é hospedagem gratuita, pode haver limitações de:
- Tempo de CPU
- Memória
- Número de requisições
- Uptime (site pode dormir após inatividade)
