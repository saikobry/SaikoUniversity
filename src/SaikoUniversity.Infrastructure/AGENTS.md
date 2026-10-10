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

### Auditing and deletion

- `AuditableEntityInterceptor` stamps `CreatedAt` and `UpdatedAt` from `IClock` on every entity
  deriving from `Entity`. It also clears `CreatedAt` from the modified set so an update never
  rewrites the creation timestamp.
- **There is no soft delete.** Do not add `IsDeleted`, `DeletedAt`, a `SoftDeleteInterceptor`, or a
  global query filter without revisiting this section.
- Retirement and deletion are different. A program that must stop accepting students is
  `ProgramLifecycle.Retired`; it stays visible to reports and transcripts. Reference data uses
  `IsActive`. Rows are only physically removed when a mistake needs undoing and nothing references
  them, which `DeleteBehavior.Restrict` on every foreign key enforces.
- Deletion history belongs to the audit log, not to a column on the row. A nullable `DeletedAt`
  cannot answer "when was this deleted and then restored" once the flag is cleared, and it is a
  denormalisation of what the log records anyway.
- `ExecuteDeleteAsync` and raw SQL bypass interceptors entirely. If a deletion guarantee is ever
  needed, enforce it in the database with `DENY DELETE` for the application role, not in C#.

### Seeding

- `DevelopmentSeeder` runs only when the environment is Development.
- Seed deterministic, realistic data: plausible faculty names, well-formed student numbers, and a
  believable mix of domestic and international students. The institution is fictional, so the data
  must stay internally consistent without claiming to represent a real school.
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
