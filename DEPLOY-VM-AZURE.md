# Deploy API na VM Azure

## 🚀 Opção 1: Executar API direto na VM (Mais Rápido)

### 1. Publicar localmente
```powershell
cd C:\fontes\guia-noivas-api\src\GuiaNoivas.Api
dotnet publish -c Release -o publish
```

### 2. Copiar para VM Azure
Use um dos métodos:

**Via RDP (copiar/colar arquivos):**
- Conectar via RDP na VM
- Copiar a pasta `publish/` inteira
- Colar na VM em: `C:\inetpub\guia-noivas-api\`

**Via PowerShell remoto (se habilitado):**
```powershell
# Comprimir localmente
Compress-Archive -Path .\publish\* -DestinationPath guia-noivas-api.zip

# Copiar via SCP/FTP para VM
# Depois descomprimir na VM
```

### 3. Configurar na VM Azure

#### Instalar .NET 9 Runtime (se não tiver)
```powershell
# Na VM, baixar e instalar:
# https://dotnet.microsoft.com/download/dotnet/9.0
# Instalar "ASP.NET Core Runtime 9.0.x - Windows Hosting Bundle"
```

#### Executar API diretamente (teste)
```powershell
cd C:\inetpub\guia-noivas-api
$env:CONNECTION_STRING = "Server=.\SQLEXPRESS;Database=GuiaNoivas;Trusted_Connection=True;MultipleActiveResultSets=true;"
dotnet GuiaNoivas.Api.dll --urls "http://0.0.0.0:5000"
```

Testar no Bruno: `http://<IP-DA-VM>:5000/api/v1/health/live`

### 4. Configurar Firewall da VM
No portal Azure:
- Ir em "Networking" / "Redes"
- Adicionar regra de entrada (inbound):
  - Porta: 5000
  - Protocolo: TCP
  - Origem: Seu IP ou "Any"
  - Nome: "API-GuiaNoivas"

### 5. Executar como Serviço Windows (produção)

Criar arquivo `install-service.ps1`:
```powershell
$serviceName = "GuiaNoivasApi"
$execPath = "C:\inetpub\guia-noivas-api\GuiaNoivas.Api.exe"
$displayName = "Guia Noivas API"

# Parar e remover se existir
if (Get-Service $serviceName -ErrorAction SilentlyContinue) {
    Stop-Service $serviceName
    sc.exe delete $serviceName
}

# Criar serviço
New-Service -Name $serviceName `
    -BinaryPathName "$execPath --urls http://0.0.0.0:5000" `
    -DisplayName $displayName `
    -StartupType Automatic `
    -Description "API do Guia de Noivas Piracicaba"

# Iniciar
Start-Service $serviceName
Get-Service $serviceName
```

Executar na VM (como Administrador):
```powershell
.\install-service.ps1
```

---

## 🌐 Opção 2: Hospedar no IIS da VM

### 1. Instalar IIS na VM
```powershell
# No PowerShell como Admin
Install-WindowsFeature -name Web-Server -IncludeManagementTools
```

### 2. Instalar .NET 9 Hosting Bundle
- Baixar: https://dotnet.microsoft.com/download/dotnet/9.0
- Instalar "ASP.NET Core Runtime 9.0.x - Windows Hosting Bundle"
- Reiniciar IIS: `iisreset`

### 3. Criar Site no IIS
1. Abrir IIS Manager
2. Sites → Add Website
   - Nome: GuiaNoivasApi
   - Caminho físico: `C:\inetpub\guia-noivas-api`
   - Binding: HTTP, porta 80 (ou 5000)
   - Hostname: deixar vazio

### 4. Configurar App Pool
1. Application Pools → GuiaNoivasApi
2. .NET CLR Version: **No Managed Code**
3. Managed Pipeline: Integrated
4. Identity: ApplicationPoolIdentity

### 5. Configurar Connection String
Criar/editar `appsettings.json` na pasta publish:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=GuiaNoivas;Trusted_Connection=True;MultipleActiveResultSets=true;"
  },
  "Jwt": {
    "Secret": "seu-secret-seguro-aqui-minimo-32-caracteres"
  }
}
```

### 6. Abrir Firewall
- Porta 80 (HTTP) ou 5000 (custom)
- No portal Azure: Networking → Add inbound rule

---

## 🧪 Testar do Bruno

Depois de configurado, testar:

```http
# Health Check
GET http://<IP-DA-VM>:5000/api/v1/health/live

# Login
POST http://<IP-DA-VM>:5000/api/v1/auth/login
Content-Type: application/json

{
  "email": "admin@guianoivas.com.br",
  "password": "Admin@123"
}

# Listar fornecedores
GET http://<IP-DA-VM>:5000/api/v1/fornecedores
```

---

## ⚙️ Variáveis de Ambiente (alternativa ao appsettings)

Se preferir usar variáveis de ambiente:

### Via PowerShell (serviço Windows):
```powershell
[System.Environment]::SetEnvironmentVariable('CONNECTION_STRING', 'Server=.\SQLEXPRESS;...', 'Machine')
[System.Environment]::SetEnvironmentVariable('JWT_SECRET', 'seu-secret', 'Machine')

# Reiniciar serviço
Restart-Service GuiaNoivasApi
```

### Via IIS (App Pool Environment Variables):
1. IIS Manager → Application Pools → GuiaNoivasApi
2. Advanced Settings → Environment Variables
3. Adicionar:
   - `CONNECTION_STRING`
   - `JWT_SECRET`

---

## 📊 Logs e Troubleshooting

### Ver logs do serviço
```powershell
# Event Viewer
eventvwr.msc
# Procurar em: Windows Logs → Application

# Ou arquivo de log (se configurado no Serilog)
Get-Content C:\inetpub\guia-noivas-api\Logs\log-*.txt -Tail 50
```

### Status do serviço
```powershell
Get-Service GuiaNoivasApi
Get-Process -Name GuiaNoivas.Api
```

### Testar localmente na VM
```powershell
Invoke-WebRequest -Uri http://localhost:5000/api/v1/health/live
```

---

## 🔒 Segurança (Produção)

1. **HTTPS**: Instalar certificado SSL no IIS
2. **Firewall**: Restringir acesso apenas aos IPs necessários
3. **SQL Server**: Usar autenticação SQL ao invés de Trusted Connection
4. **Secrets**: Usar Azure Key Vault ou variáveis de ambiente

---

## 📝 Checklist Rápido

- [ ] .NET 9 Runtime instalado na VM
- [ ] Pasta publish copiada para VM
- [ ] Connection string configurada
- [ ] Porta aberta no firewall Azure
- [ ] API rodando (serviço ou IIS)
- [ ] Teste do Bruno funcionando

---

**Recomendação**: Use o **Opção 1** (serviço Windows) por ser mais simples e direto.
