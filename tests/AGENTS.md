# tests — Test Suites

## Purpose

Automated verification for SaikoUniversity. One project per source layer, so a test can depend on
exactly as much of the system as it needs to exercise.

## Ownership

- Test projects, one per layer under `src/`
- Shared test fixtures and builders
- Test-only conventions: naming, arrangement, data setup, cleanup

## Local Contracts

### Project mapping

| Test project | Covers | Can reference |
|---|---|---|
| `Domain.Tests` | Entities, value objects, invariants | `Domain` only |
| `Application.Tests` | Use-case services, DTO mapping | `Application`, `Domain` |
| `Infrastructure.Tests` | EF configuration, migrations, interceptors | `Infrastructure`, `Application`, `Domain` |
| `Api.Tests` | Endpoints, middleware, composition | all four |

- A test project never references a layer it does not test. `Domain.Tests` cannot touch EF Core.
- Test-only helper types are `internal` and exposed to tests via `InternalsVisibleTo`, never `public`
  in a src project.

### Conventions

- Naming: `MethodName_Condition_ExpectedResult`.
- Arrangement / act / assert, one assertion concept per test.
- Use FluentAssertions for readability.
- Prefer `Theory` with `InlineData` over a loop of near-identical tests.
- No shared mutable state between tests. Each test owns its data.
- Every test is independent and order-agnostic.

### Data setup

- `Infrastructure.Tests` uses the EF InMemory provider by default for speed. Anything the provider
  cannot express goes to the Compose SQL Server — see Relational tests below.
- `Api.Tests` uses `WebApplicationFactory<Program>`. `Program` is internal to `Api`,
  which is why that project declares `InternalsVisibleTo`. The factory swaps the database provider and
  shares the production registration seam, so the two graphs differ only in where they store rows.
- Replace the hosted service's database with a test double or a disposable database. Never point a
  test at the development database.

### Relational tests

Some assertions only mean anything against the real engine. InMemory has no unique indexes, no
foreign keys, and no transactions, and it does not translate `EF.Functions.Like` at all. A test that
needs any of those is testing persistence, not a use case.

- Put relational tests in `Infrastructure.Tests`, and say so in the test name — suffix it
  `_OnSqlServer`. `Application` cannot reference `Infrastructure`, so `Application.Tests` is not an
  option even when the code under test lives in `Application`.
- Get a database through `Support/SqlServerDatabase.TryCreateAsync`. It creates a uniquely named
  database, applies migrations, and drops it on dispose. Migrations, not `EnsureCreated`, because the
  schema under test is the migrated one.
- It returns `null` when the Compose container is unreachable. Guard on that and call `Assert.Skip`,
  so `dotnet test SaikoUniversity.slnx` still passes on a machine without Docker. A skip is a
  reporting decision, so make it conditional and visible — a guard that skips unconditionally is
  worse than no test at all, because it looks like coverage.
- Never point a relational test at the development database. That makes a run depend on Docker
  running, on migrations being applied, and on rows an earlier run left behind.

### Test doubles

- Time is controlled through `IClock`, never by sleeping.
- The current user is faked through `ICurrentUser`, never by forging a real token.
- Prefer a hand-written fake over a mocking framework when the interface is small. A mock that
  restates the interface adds nothing.

## Work Guidance

- A failing test names the defect. "Expected 3, found 2" is a failure; "Assert.NotNull(x)" is not.
- Fix or delete a failing test. Never weaken an assertion to make it pass.
- Extract a builder or fixture once the third test needs the same arrangement.
- Coverage is a signal, not a goal. Cover rules and edge cases, not getters.

## Verification

- `dotnet test SaikoUniversity.slnx`
- `dotnet test SaikoUniversity.slnx --coverage` — writes `.coverage` files to `TestResults/`
- Never pass `--nologo` or any other VSTest-era switch to `dotnet test`. This repo runs in
  Microsoft Testing Platform mode, which forwards those switches to the test app instead of
  consuming them. The app rejects them with exit code 5 and the run reports `Zero tests ran` even
  though every test is present and passing.
- A test project with no test files reports `Zero tests ran` and exit code 8. That is a real signal,
  not a runner fault.
- Tests must pass with zero warnings, since warnings are errors project-wide.

## Child DOX Index

None. Each test project is a leaf. Promote one to its own doc when it grows shared infrastructure
that the others need.
