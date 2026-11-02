# ACNUTRI - Sistema Nutricional Multi-tenant

## Stack
- **API**: C# ASP.NET Core (`servico-csharp/`)
- **Python Service**: Cálculos nutricionais (`servico-python/`)
- **Frontend**: HTML/JS (`frontend/interface/`)
- **Database**: PostgreSQL (`smart_nutri_db`)

---

## Segredos e Configuração

### Desenvolvimento Local (dotnet user-secrets)

```bash
cd servico-csharp
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Database=smart_nutri_db;Username=postgres;Password=SUA_SENHA"
dotnet user-secrets set "Jwt:Key" "sua-chave-jwt-desenvolvimento-min-32-chars"
dotnet user-secrets set "Email:Smtp:UserName" "seu-email@gmail.com"
dotnet user-secrets set "Email:Smtp:Password" "sua-senha-app-gmail"
dotnet user-secrets set "Email:Smtp:FromEmail" "seu-email@gmail.com"
dotnet user-secrets set "Asaas:ApiKey" "sua-api-key-asaas-sandbox"
dotnet user-secrets set "Asaas:WebhookSecret" "seu-webhook-secret"
```

O arquivo `appsettings.Development.json` (no .gitignore) serve como fallback/template.

### Produção (Render)

Configure no **Render Dashboard → Environment**:

| Variável | Descrição |
|----------|-----------|
| `ConnectionStrings__Default` | URL do PostgreSQL do Render |
| `Jwt__Key` | Chave JWT (mín. 32 chars, `openssl rand -base64 32`) |
| `Email__Smtp__UserName` | Email Gmail |
| `Email__Smtp__Password` | App Password Gmail (16 chars) |
| `Email__Smtp__FromEmail` | Mesmo que UserName |
| `Email__FrontendBaseUrl` | `https://achubhome.vercel.app` (sem barra final) |
| `Asaas__ApiKey` | Chave API Asaas (produção) |
| `Asaas__WebhookSecret` | Secret do webhook Asaas |
| `Cors__AllowedOrigins__0` | `https://achubhome.vercel.app` |
| `Cors__AllowedOrigins__1` | `http://localhost:5500` |

---

## Rodar Local

```bash
# Backend C#
cd servico-csharp
dotnet run

# Python Service
cd ../servico-python
pip install -r requirements.txt
python main.py

# Frontend (VS Code Live Server ou npx serve)
cd ../frontend/interface
npx serve
```

Acesse: `http://localhost:5500` (frontend) → API em `http://localhost:5290`

---

## Deploy

- **Frontend**: Vercel (configurado em `vercel.json`)
- **Backend**: Render (Docker/ASP.NET Core)
- **Python**: Render (Docker/Python)
- **Database**: Render PostgreSQL

---

## Documentação Técnica

- `TESTS_SOLICITAR_ACESSO.md` - Casos de teste do fluxo "Solicitar Acesso"
- `CLAUDE.md` - Instruções do projeto
- `docs/` - Documentação adicional