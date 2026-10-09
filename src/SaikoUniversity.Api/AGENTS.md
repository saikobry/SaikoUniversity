# SaikoUniversity.Api

## Purpose

The HTTP edge. Owns the Kestrel host, request contracts, controllers, middleware, and DI
composition. Contains no business rules and no database queries.

## Ownership

- `Program.cs` — the composition root
- Controllers, versioned under `Controllers/v1/`
- Request and response contracts in `Contracts/`
- Middleware: exception handling, correlation IDs, and later authentication
- `appsettings*.json`
- OpenAPI document generation

## Local Contracts

- Controllers are thin. A controller action binds, delegates to an `Application` service, and
  returns. No `AppDbContext`, no `_context` field, no query expressions.
- Entities are never returned. Actions return contract records defined in `Contracts/`.
- Contracts live in `Contracts/`, versioned separately from entities. Renaming an entity property
  must not silently change an API response shape.
- Route prefix is `api/v1`. Swagger is served at `/swagger` in Development only.

### Program.cs

`Program.cs` reads top to bottom as the wiring order:

1. Builder configuration
2. `AddInfrastructure()` and `AddApplication()`
3. Auth and authorization
4. Middleware pipeline
5. `app.MapControllers()`
6. `app.Run()`

Keep it that shape. If it grows past roughly 40 lines, the excess belongs in a `DependencyInjection.cs`
extension.

### Error handling

`ExceptionHandlingMiddleware` converts exceptions into `ProblemDetails` with a correlation ID. Never
let an exception reach the client as a stack trace. Never return `200` for a failed operation.

### Configuration

| Key | Purpose |
|---|---|
| `ConnectionStrings:Default` | Database target. Points at the Compose container locally. |
| `Logging` | Standard ASP.NET Core logging. Category filters use full type names. |

- Secrets are never committed. Use user secrets locally, Key Vault in production.
- CORS is configured for the SPA's dev origin only, and by configuration rather than a wildcard.

## Work Guidance

- Add an endpoint under a new version folder when the shape changes incompatibly. Do not break a
  published contract.
- Cancellation tokens flow from the action signature into every async call.
- Configure the SPA dev server origin, not `*`. A wildcard CORS policy plus credentials does not work
  anyway.
- When adding a feature, add an integration test in `tests/SaikoUniversity.Api.Tests` using
  `WebApplicationFactory`.

## Verification

- `dotnet build src/SaikoUniversity.Api`
- `dotnet test tests/SaikoUniversity.Api.Tests`
- `dotnet run --project src/SaikoUniversity.Api` then open `/swagger` in Development
- `GET /health` returns 200 and reports database connectivity

## Child DOX Index

None. `Controllers/`, `Contracts/`, and `Middleware/` stay organizational for now.