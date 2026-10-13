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

- `Infrastructure.Tests` uses the EF InMemory provider by default for speed. Any test asserting on
  relational behavior — indexes, cascade paths, check constraints, transactions — runs against the
  Compose SQL Server instead, and says so in the test name.
- `Api.Tests` uses `WebApplicationFactory<Program>`. `Program` is internal to `Api`,
  which is why that project declares `InternalsVisibleTo`.
- Replace the hosted service's database with a test double or a disposable database. Never point a
  test at the development database.

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
