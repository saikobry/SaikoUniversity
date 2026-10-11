# Saiko University

A university enrollment and tracking platform for a fictional institution, built as a portfolio-scale
reference implementation of a production-shaped .NET and React architecture.

The goal is not a toy CRUD app. It is a realistic registrar system: terms and section offerings,
enrollment with capacity and deadline rules, grade recording, role-scoped access, audit trails, and
server-side reporting — the parts that make real university software hard.

## Status

Early scaffolding. The architecture, tooling, and local environment are in place; the domain model is
the next milestone.

| Milestone | State |
|---|---|
| Solution structure, 4 layers, 4 test projects | Done |
| Central Package Management, warnings as errors | Done |
| Local SQL Server 2025 in Docker with healthcheck | Done |
| OpenAPI document + Scalar reference UI | Done |
| Health endpoint with real database round-trip | Done |
| Dockerfile, multi-stage, unprivileged, compose for API + SQL | Done |
| Repository tooling (whitespace, OpenAPI snapshot) | Done |
| DOX agent contract hierarchy | Done |
| Domain entities, EF configurations, migrations | Done |
| Seed data | Done |
| Curriculum vertical slice: programs API, lifecycle, audit interceptor | Done |
| Authentication and role-based access | Planned |
| Reporting, audit log | Planned |
| Frontend `web/` scaffold and typed API client | Next |
| CI/CD and Azure deployment | Planned |

## Stack

**Backend** — .NET 10, ASP.NET Core minimal APIs, EF Core 10, SQL Server 2025, xUnit, FluentAssertions

**Frontend** — React 19, TypeScript, Vite, TanStack Query / Router / Table *(not yet scaffolded)*

**Local environment** — Docker Desktop with WSL2, `mssql/server:2025-latest`

## Architecture

Four layers with references pointing inward only. A layer never references a layer above it.

```
Api              HTTP edge: controllers, middleware, DI composition
  └── Infrastructure   EF Core, SQL Server, migrations, seeding
        └── Application  use-case services, DTOs, port interfaces
              └── Domain    entities, enums, invariants — zero dependencies
```

`Domain` has no package references at all. Not EF Core, not ASP.NET Core. That constraint is what
keeps business rules testable without a database.

Port interfaces in `Application` — `IAppDbContext`, `ICurrentUser`, `IClock` — let the use-case layer
compile and be tested without EF Core. Only `Infrastructure` and `Api` know a database exists.

Every layer has a matching test project. `Domain.Tests` cannot reference EF Core, which is enforced by
the project graph rather than by convention.

## Repository layout

```
src/
  Domain/            entities, enums, value objects
  Application/       use-case services, DTOs, ports
  Infrastructure/   EF Core, migrations, seeding
  Api/               controllers, middleware, composition root
tests/
  <Layer>.Tests/            one per layer
scripts/tools/                       repository tooling
.agents/skills/                      agent skill definitions
web/                                 frontend (planned)
```

Each directory carries an `AGENTS.md` that states its contracts — what it owns, what must never
appear in it, and how it is verified. Read the nearest one before editing anything under it.

## Getting started

### Prerequisites

- .NET SDK 10
- Docker Desktop with WSL2
- Node.js 20+ (once the frontend exists)

### Run the database

```powershell
Copy-Item .env.example .env   # then set MSSQL_SA_PASSWORD in .env
docker compose up -d sql
```

SQL Server 2025 starts on port 1433 with a healthcheck. The compose file waits for it to answer
`SELECT 1` before starting anything that depends on it.

### Run the API

```powershell
dotnet run --project src/Api
```

| URL | What |
|---|---|
| `/scalar/v1` | Interactive API reference |
| `/openapi/v1.json` | OpenAPI 3.1 document |
| `/health` | Health check with a real database round-trip |

The committed development connection string targets the local container. Production credentials come
from Azure Key Vault and are never committed.

### Run the API in a container

```powershell
docker compose up -d --build
```

Inside the compose network the database answers to `sql`, not `localhost`. The connection string is
supplied through a `ConnectionStrings__Default` environment override, so no code branches on
environment.

The API is published as `saiko-university-api` and listens on 8080 inside the container, which is what
Azure Container Apps expects. The host port is parameterized via `API_PORT` in `.env`.

### Verification

```powershell
dotnet build SaikoUniversity.slnx
dotnet test SaikoUniversity.slnx
docker compose ps

powershell -ExecutionPolicy Bypass -File scripts/tools/clean-whitespace.ps1
powershell -ExecutionPolicy Bypass -File scripts/tools/update-openapi.ps1
```

`clean-whitespace.ps1` is idempotent: a second run reports zero changes. `update-openapi.ps1` boots
the API, captures the document to `artifacts/openapi/saiko-university.json`, and shuts it down.

## Configuration

| Key | Purpose |
|---|---|
| `ConnectionStrings:Default` | Database target. Local Compose container by default. |
| `ASPNETCORE_ENVIRONMENT` | `Development` loads `appsettings.Development.json`. |

Secrets are never committed. `.env` is gitignored and holds the local container password;
`.env.example` documents the variable with no value.

## Development conventions

- Nullable enabled, implicit usings, warnings as errors, enforced code style
- Central Package Management: versions live in `Directory.Packages.props`
- Conventional commits, generated by the `commit-message` skill
- Scripts are lowercase kebab-case, PowerShell 5.1 compatible, idempotent, and safe to re-run

## Naming

The product is Saiko University. Layers use bare names — `Domain`, `Application`,
`Infrastructure`, `Api` — and type names stay role-based: `AppDbContext`, not
`SaikoUniversityDbContext`. The tradeoff is stated in the root `AGENTS.md`.

## License

MIT
