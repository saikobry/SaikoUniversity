# scripts — Repository Tooling

## Purpose

Executable helpers that maintainers and CI run against the repo. Anything a human would otherwise
type more than once, or that must behave identically on a laptop and in a pipeline, lives here.

## Ownership

- Every script under `tools/`
- Script naming, structure, and authoring rules
- The conventions that keep scripts safe to run repeatedly

## Local Contracts

### Naming and placement

- **Filenames are lowercase kebab-case with a `.ps1` extension:** `clean-whitespace.ps1`,
  `update-openapi.ps1`. Never `Update-OpenApi.ps1`.
- Kebab-case is not a stylistic preference here. `core.ignorecase` is `true`, so Git treats
  `Update-OpenApi.ps1` and `update-openapi.ps1` as the same file on Windows and as different files on
  Linux CI. Mixed-case script names are a cross-platform collision. All-lowercase removes the class
  of bug.
- It also matches every other lowercase-hyphen surface in this repo: `docker-compose.yml`,
  `.gitignore`, `.env`, and the Azure resource names.
- **Verb-Noun applies to function names inside a script**, not to filenames:
  `function Get-OpenApiDocument`, not `Update-OpenApi.ps1`. The idiom belongs where it means
  something.
- One task per script. No script that does three unrelated things.
- Utility scripts live in `tools/`. Reserve the `scripts/` root for orchestrators that call several
  tools in sequence.
- A script is not a place for logic that belongs in the application. It may orchestrate the
  application; it must not reimplement any of it.

### Structure

Every script follows the same skeleton:

```powershell
[CmdletBinding()]                    # named params, -Verbose, common parameters
param(
    [string]$BaseUrl = 'http://localhost:5080'
)

$ErrorActionPreference = 'Stop'      # fail loudly, never continue on error
Set-StrictMode -Version Latest      # catch uninitialized variables

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '../..')
Push-Location $repoRoot
try {
    # do the work
}
finally {
    # always clean up: stop processes, Pop-Location
}
```

- Resolve the repo root from `$PSScriptRoot`. Never hardcode an absolute path or rely on the
  caller's working directory.
- Always `Push-Location` and `Pop-Location` in a `finally`. Always clean up processes the script
  started.
- Support `-WhatIf` or a `-DryRun` switch for anything that writes files or deletes content.

### Safety

- **Idempotent.** Running a script twice must be a no-op the second time.
- **Non-destructive by default.** A script that deletes or rewrites tracked files prints what it
  touched and how many files it changed. It never deletes.
- **No secret output.** Never `Write-Host` a connection string, token, or password. Print the
  variable name instead.
- **Bounded waits.** Any poll loop has a timeout and throws a clear message naming the thing that
  never appeared.
- **Clean up from a `finally`, and make cleanup itself 5.1-safe.** A throw inside `finally`
  replaces the original error and orphans whatever the script started. Use `taskkill /T /F` to reach
  child processes, never `Process.Kill($true)`, which is .NET Core only and does not exist in
  Windows PowerShell 5.1. `dotnet run` spawns the app as a child, so killing only the parent leaves
  the port held.
- **Stream child output back to the caller.** A script that redirects a child's output to a log file
  and prints nothing reads as a hang. Tail the log and echo new lines, or use `-NoNewWindow` so the
  child writes into the caller's console.

### Portability

- Target **Windows PowerShell 5.1** (`powershell -File`), not PowerShell 7 (`pwsh`), unless the
  script is explicitly documented as requiring 7. A default Windows install has no `pwsh`.
- Never rely on the user's profile. Callers run with `-NoProfile`.
- Prefer built-in cmdlets over external tools. If an external tool is required, check for it and
  fail with an actionable message naming the install step.

## Work Guidance

- Write the script so its own failure message tells the reader what to do next.
- One tool per concern. `Update-OpenApi.ps1` writes the document; a separate `Publish-OpenApi.ps1`
  would publish it.
- If a script needs the API running, boot it yourself unless one is already listening, and shut it
  down again on the way out.
- Set `ASPNETCORE_ENVIRONMENT` explicitly in any script that starts the API. Without it the host
  runs as Production and skips `appsettings.Development.json`.
- When adding a script, add its invocation to this repo's root `AGENTS.md` Verification section.

## Verification

- Run the script twice. The second run must report zero changes.
- Run it from a different working directory to confirm the `$PSScriptRoot` resolution holds.
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/<name>.ps1` must succeed on a
  clean checkout with only Docker and the .NET SDK installed.

## Child DOX Index

- `tools/clean-whitespace.ps1` — strips trailing whitespace and normalizes each file to exactly one
  trailing newline, preserving per-file CRLF or LF. **The filter lists at the top of the script are the
  single source of truth for what it touches; update this index when you change them.**
  - Extensions: `.cs` `.ts` `.tsx` `.sql` `.md` `.json` `.yml` `.yaml` `.css` `.razor` `.ps1`
  - **`.ps1` gets EOF normalization only, never per-line trimming.** Trailing whitespace inside a
    here-string is part of that string's value. Neither a regex nor a line-state machine can
    reliably distinguish a here-string delimiter from the same characters inside a quoted literal,
    so `.ps1` files are exempt from line trimming entirely. A malformed script is worse than an
    untidy one. Do not add per-line `.ps1` handling without solving this properly.
  - Exact names: `Dockerfile` `.gitignore` `.dockerignore` `.env.example` `AGENTS.md`
  - Wildcards: `Directory.*.props` `*.slnx`
  - Skips these directories: `bin` `obj` `node_modules` `generated` `.git` `.vs` `.vscode`
    `artifacts` `TestResults`
  - Skips files whose first five lines carry a generated-code marker. The marker must be in the
    header region: matching anywhere in the file false-positives on documentation that merely
    mentions "generated by" in prose.
- `tools/update-openapi.ps1` — boots the API in Development, captures `/openapi/v1.json`, writes it
  to `artifacts/openapi/saiko-university.json`, and shuts the API down. Reuses an already-running
  API on the target port instead of starting a second one.
