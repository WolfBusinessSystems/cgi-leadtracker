# CGI LeadTracker — Guia de Deploy e Banco de Dados

API .NET 9 que sincroniza leads do RD Station e envia conversões para Meta e Google Ads.

## Pré-requisitos

- .NET 9 SDK (build/migrations) e ASP.NET Core 9 Runtime + Hosting Bundle (no servidor IIS).
- **MySQL 8.0+** acessível pela aplicação (provider: Pomelo.EntityFrameworkCore.MySql).
- Ferramentas EF Core (para gerar/aplicar migrations manualmente): `dotnet tool install --global dotnet-ef`.

## 1. Configuração e segredos

Nenhum segredo fica no `appsettings.json` (versionado). Em produção, use **um** destes:

- **`appsettings.Production.json`** ao lado do binário (já no `.gitignore`). Copie de
  [`src/CGI.LeadTracker.API/appsettings.Production.json.example`](src/CGI.LeadTracker.API/appsettings.Production.json.example) e preencha.
- **Variáveis de ambiente** (recomendado em servidor). Aninhamento vira `__`:
  | Config                                | Variável de ambiente                    |
  | ------------------------------------- | --------------------------------------- |
  | `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection`  |
  | `Auth:Secret`                         | `Auth__Secret`                          |
  | `Seed:AdminPassword`                  | `Seed__AdminPassword`                   |
  | `RdStation:ClientSecret`              | `RdStation__ClientSecret`               |
  | `RdStation:RefreshToken`              | `RdStation__RefreshToken`               |
  | `Meta:AccessToken`                    | `Meta__AccessToken`                     |
  | `GoogleAds:RefreshToken`              | `GoogleAds__RefreshToken`               |

  Variáveis de ambiente têm precedência sobre os arquivos.

- **Desenvolvimento local**: a connection string fica em **user-secrets** (fora do repositório), não em arquivo:
  ```bash
  dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
    "Server=HOST;Port=3306;Database=NOME;User ID=USUARIO;Password=SENHA;SslMode=Preferred;" \
    --project src/CGI.LeadTracker.API
  ```

Formato da connection string (MySQL): `Server=HOST;Port=3306;Database=NOME;User ID=USUARIO;Password=SENHA;SslMode=Preferred;`

Validações de start (a aplicação **aborta** se falharem):
- `Auth:Secret` deve ter ≥ 32 caracteres e não pode ser placeholder.
- No primeiro start em produção, `Seed:AdminPassword` é obrigatório (a não ser que `Database:SeedOnStartup=false`).

## 2. Banco de dados — migrations

O schema é definido por EF Core migrations (`src/CGI.LeadTracker.Infrastructure/Migrations`). Dois modos:

### Modo A — automático no start (padrão, deploy simples)
Com `Database:MigrateOnStartup=true` (padrão), a aplicação aplica as migrations pendentes ao subir.
Indicado para **uma instância** (ex.: IIS in-process). Não habilite em múltiplas instâncias simultâneas.

### Modo B — controlado por DBA / pipeline
Defina `Database:MigrateOnStartup=false` e aplique o schema fora da aplicação:

```bash
# Gera um script idempotente (seguro re-rodar) que cria/atualiza o schema
dotnet ef migrations script --idempotent \
  --project src/CGI.LeadTracker.Infrastructure \
  --startup-project src/CGI.LeadTracker.API \
  --output migrate.sql
```

Ou aplique direto numa conexão:

```bash
dotnet ef database update \
  --project src/CGI.LeadTracker.Infrastructure \
  --startup-project src/CGI.LeadTracker.API \
  --connection "Server=...;Database=CGILeadTracker;..."
```

## 3. Seed do administrador

Controlado por `Database:SeedOnStartup` (padrão `true`) e idempotente:
- Se o usuário de `Seed:AdminEmail` já existir, não faz nada.
- Senão, cria com `Seed:AdminPassword` (PBKDF2, 100k iterações). Em **Development**, se a senha não estiver
  configurada, usa `Admin@123` apenas para facilitar o setup local — nunca em produção.

**Troque a senha após o primeiro login** pelo endpoint autenticado:

```
POST /api/auth/change-password      (requer header Authorization: Bearer <token>)
{ "currentPassword": "Admin@123", "newPassword": "SuaNovaSenhaForte" }
```

Regras: nova senha com no mínimo 8 caracteres e diferente da atual; a senha atual é validada
antes da troca. As senhas nunca são gravadas em log (o `LoggingBehavior` registra só o nome do comando).

## 4. Rodar localmente

```bash
# Lê a connection string do user-secrets (ver seção 1); migra e semeia sozinho
dotnet run --project src/CGI.LeadTracker.API
# Docs interativas (Scalar) só em Development:
#   https://localhost:7118/scalar/v1
# Health:
#   https://localhost:7118/health
```

Login inicial:
```
POST /api/auth/login
{ "email": "admin@presenca.com.br", "password": "Admin@123" }
```

## 5. Deploy no IIS

```bash
dotnet publish src/CGI.LeadTracker.API -c Release -o ./publish
```

- O [`web.config`](src/CGI.LeadTracker.API/web.config) já define `ASPNETCORE_ENVIRONMENT=Production` e `hostingModel=inprocess`.
- Publique a pasta `./publish` no site/app do IIS.
- Garanta que a identidade do App Pool tenha acesso ao SQL Server (ou use usuário/senha na connection string).
- Crie a pasta `logs/` gravável (o `web.config` escreve `stdout` lá).
- Confirme o deploy acessando `GET /health` → `Healthy`.

## Conformidade de licenças (dependências)

Nenhuma dependência exige licença comercial paga para uso em produção:

- **MediatR**: fixado na **12.5.0**, última versão sob licença gratuita Apache-2.0 (a 13.x+ passou a
  exigir licença comercial da Lucky Penny Software). Não usar `dotnet add package MediatR` sem `--version`,
  pois traria a versão paga mais recente.
- **AutoMapper** (também comercial a partir da 15.x): **removido**. O mapeamento domínio→DTO é manual em
  [`LeadMapper`](src/CGI.LeadTracker.API/Application/Adapters/LeadMapper.cs).
- Demais pacotes (Pomelo MySQL, FluentValidation, FluentResults, Serilog, Google.Ads.GoogleAds) são
  open-source sem restrição de uso em produção.

## Pendências conhecidas (antes de produção plena)

- **Sem retry de conversões com falha**: um `ConversionEvent` marcado `Failed` só é reenviado se a etapa
  for re-publicada. O ideal é um padrão *outbox* com reprocessamento.
- **Gestão de usuários**: há login e troca de senha do próprio usuário; ainda não há cadastro/listagem
  de usuários nem reset de senha por administrador (só o admin semeado).
- **Campos da API do RD Station** seguem a suposição do código (`v1.0/deals`, custom fields por label) —
  valide contra uma resposta real da sua conta.
- **HTTPS**: terminação TLS no IIS/reverse proxy; `UseHttpsRedirection` já está ativo.
