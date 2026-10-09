# SaikoUniversity.Infrastructure

## Purpose

Everything that touches the outside world: EF Core, SQL Server, migrations, seeding, and the concrete
implementations of the ports declared in `Application`.

## Ownership

- `AppDbContext` and its entity configurations
- EF Core interceptors for auditing and soft delete
- Migrations and the development seeder
- Port implementations, `IClock`, `ICurrentUser`
- Registration of all of the above via `AddInfrastructure()`

## Local Contracts

- The only layer that references `Microsoft.EntityFrameworkCore`.
- `AppDbContext` is named for its role. Never `SaikoUniversityDbContext`.
- Implements `IAppDbContext` from `Application`, so both must agree on the exposed `DbSet<T>`s.

### Entity configuration

- One `IEntityTypeConfiguration<T>` per entity, in `Persistence/Configurations/`.
- All mapping lives in configuration classes. No data annotations in `Domain`, no fluent API inline
  in `OnModelCreating`.
- `OnModelCreating` calls `ApplyConfigurationsFromAssembly`. Adding a configuration file is the only
  step needed for it to register.
- String lengths, precision, indexes, and delete behaviors are declared explicitly. Never rely on a
  convention default.

### Migrations

- Generate with `dotnet ef migrations add <Name> --project src/SaikoUniversity.Infrastructure`.
- Never hand-edit a generated migration.
- Every migration must be reversible. If a migration is intentionally irreversible, say so in its
  name and add a comment explaining the data-loss path.
- Migrations run in development via `Database.Migrate()`. Never `EnsureCreated()` — it does not
  track history and will silently conflict with a later migration.

### Auditing and soft delete

- `AuditableEntityInterceptor` stamps `CreatedAt`, `UpdatedAt`, and the acting user on change.
- `SoftDeleteInterceptor` converts `Remove` into `IsDeleted = true` for entities implementing
  `IAuditable`. This is why students are never hard-deleted.
- Query filters hide soft-deleted rows. Bypass them only in an explicit admin or audit path, and
  say so in a comment when you do.

### Seeding

- `DevelopmentSeeder` runs only when the environment is Development.
- Seed deterministic, realistic data: Japanese faculty names, realistic student numbers, plausible
  Japanese and international student mix.
- Seeding is idempotent. It must be safe to run on every startup.

### Connection strings

- Read from `ConnectionStrings:Default` in `appsettings.Development.json`.
- Local default targets the Compose container on `localhost,1433`.
- The same code runs against Azure SQL in production. No environment branching in `AppDbContext`.

## Work Guidance

- Register everything in `DependencyInjection.cs` so `Program.cs` stays a composition root.
- Prefer server-side aggregation in `Reports/` over pulling rows into memory and computing in C#.
- When adding an index that a reporting query needs, declare it in the entity configuration, not in
  raw SQL.

## Verification

- `dotnet build src/SaikoUniversity.Infrastructure`
- `dotnet test tests/SaikoUniversity.Infrastructure.Tests`
- `docker compose ps` shows `saiko-uni-sql` healthy
- `dotnet ef database update --project src/SaikoUniversity.Infrastructure` applies cleanly

## Child DOX Index

None. `Persistence/`, `Seeding/`, and `Identity/` are organizational until they accumulate their own
contracts.