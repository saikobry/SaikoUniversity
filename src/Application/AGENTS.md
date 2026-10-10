# Application

## Purpose

Use-case orchestration. Each service here answers one question a caller can ask — "enroll this
student", "list sections for this term", "what is the enrollment fill rate" — by coordinating the
domain model and the ports the infrastructure layer implements.

## Ownership

- Application services, one per feature area
- Request and response DTOs
- Port interfaces the infrastructure layer implements
- Paging, sorting, and filtering shapes shared by list endpoints

## Local Contracts

- References `Domain` and nothing else in `src`.
- **No EF Core types in signatures.** No `DbSet<T>`, no `IQueryable<T>` leaking out of a public
  method, no `Include` in a service. Read paths return DTOs.
- **No ASP.NET Core types.** No `HttpContext`, no `[FromRoute]`, no `ActionResult`. The transport
  belongs to `Api`.
- Persistence is reached through `Abstractions/`, never injected as a concrete `AppDbContext`.
- Services are registered as scoped and depend on other application services, not on controllers.

### Abstractions

| Port | Responsibility |
|---|---|
| `IAppDbContext` | The narrow set of `DbSet<T>` and `SaveChangesAsync` that services are allowed to use |
| `ICurrentUser` | The authenticated user's id, roles, and claims. Null-safe for anonymous callers. |
| `IClock` | `UtcNow`. Never call `DateTime.UtcNow` directly; tests need to control time. |

`IAppDbContext` exists so this layer compiles without an EF Core dependency. Add a `DbSet<T>` to it
when a new aggregate needs querying, and no more.

### Feature layout

Each feature folder owns its own DTOs:

```
Students/
  StudentService.cs
  Dtos/  StudentDto.cs  StudentFilter.cs  PagedResult.cs
```

- DTOs are `record`, immutable, with explicit constructor or `init` properties.
- Entities are never serialized. Map explicitly.
- `PagedResult<T>` is the single list-response shape: `Items`, `TotalCount`, `Page`, `PageSize`.

## Work Guidance

- A service method is one use case. If a method name contains "and", split it.
- Validation that belongs to the business goes in the domain. Validation of request shape goes here
  or in `Api` contracts.
- Return domain-specific results rather than throwing for expected outcomes. Throw for
  programming errors and infrastructure faults.
- Authorization decisions belong in `Api` policies, not in service bodies.

## Verification

- `dotnet build src/Application`
- `dotnet test tests/Application.Tests`

## Child DOX Index

None. Feature folders are organizational, not contractual boundaries.
