# src — Application Code

## Purpose

All production .NET projects for SaikoUniversity. Owns the four-layer architecture and the
dependency direction between those layers.

## Ownership

- Layering contract and reference rules for the four projects
- The shared shape of each layer's directory layout
- Promotion rule: EF Core and HTTP types never cross a layer boundary inward

This directory owns no leaf code. Each child project owns its own files.

## Local Contracts

### Dependency direction

```
Api
  -> Infrastructure
  -> Application
  -> Domain

Domain  (no project references)
```

- References point inward only. A layer never references a layer above it.
- `Domain` has zero dependencies, including no EF Core, no ASP.NET Core, no package references.
- `Application` may reference `Domain`. It must not reference `Infrastructure` or `Api`.
- Only `Infrastructure` and `Api` know about EF Core and SQL Server.
- Only `Api` knows about HTTP: controllers, middleware, model binding.

### Layer boundaries

| Layer | Owns | Must not contain |
|---|---|---|
| Domain | Entities, enums, value objects, domain rules | EF attributes, DTOs, services, HTTP |
| Application | Use-case services, DTOs, port interfaces | EF `DbContext`, SQL, controllers |
| Infrastructure | `AppDbContext`, entity configurations, migrations, seeding, port implementations | Business rules, HTTP concerns |
| Api | Controllers, middleware, DI composition, contracts | EF queries, business rules |

- No entity type is ever returned from a controller. Map entities to DTOs in `Application`.
- No `DbSet<T>` leaks above `Infrastructure`. `Application` talks through port interfaces such as
  `IAppDbContext`, `ICurrentUser`, `IClock`.

### Type naming

- `AppDbContext`, not `SaikoUniversityDbContext`.
- `AddInfrastructure()`, not `AddSaikoUniversityInfrastructure()`.
- DI extension methods live in a `DependencyInjection.cs` at the layer root.

## Work Guidance

- Add a new entity to `Domain`, its configuration to `Infrastructure/Persistence/Configurations`,
  and generate a migration from `Infrastructure`. Never hand-edit a migration.
- Controllers stay thin: bind, call an `Application` service, map the result to a contract record.
- When adding a feature, add the test project coverage in the same change.

## Verification

- `dotnet build SaikoUniversity.slnx` — must succeed with zero warnings
- `dotnet test SaikoUniversity.slnx`

## Child DOX Index

- `Domain/` — entities, enums, value objects, no dependencies
- `Application/` — use-case orchestration, DTOs, port interfaces
- `Infrastructure/` — EF Core, SQL Server, migrations, seeding
- `Api/` — Kestrel host, controllers, middleware, DI composition
