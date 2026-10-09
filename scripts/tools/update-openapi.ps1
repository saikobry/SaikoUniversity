# Regenerates the OpenAPI document from the running API.
#
# The document is produced at runtime by the host, not stored in the repo. This script boots the
# API in Development, captures the document, writes it to artifacts/openapi/saiko-university.json,
# and shuts the API down again.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File scripts/tools/update-openapi.ps1
#   powershell -ExecutionPolicy Bypass -File scripts/tools/update-openapi.ps1 -BaseUrl http://localhost:5080
#   powershell -ExecutionPolicy Bypass -File scripts/tools/update-openapi.ps1 -StartupTimeoutSeconds 120

[CmdletBinding()]
param(
    [string]$BaseUrl = 'http://localhost:5080',
    [string]$OutPath = 'artifacts/openapi/saiko-university.json',
    [int]$StartupTimeoutSeconds = 90,
    [switch]$Quiet
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Stop-ProcessTree {
    param([System.Diagnostics.Process]$Process)

    # Process.Kill($true) does not exist in Windows PowerShell 5.1, it is .NET Core only. taskkill
    # with /T reaches the children, which matters because `dotnet run` launches the app as a
    # separate process that would otherwise survive and hold the port.
    & taskkill.exe /PID $Process.Id /T /F 2>&1 | Out-Null
    $Process.WaitForExit(15000) | Out-Null
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '../..')
$logDir = Join-Path $repoRoot 'artifacts/logs'
$apiOut = Join-Path $logDir 'update-openapi-api.out.log'
$apiErr = Join-Path $logDir 'update-openapi-api.err.log'

Push-Location $repoRoot

$apiProcess = $null

try {
    if (-not (Test-Path $logDir)) {
        New-Item -ItemType Directory -Path $logDir -Force | Out-Null
    }

    $openApiUrl = "$BaseUrl/openapi/v1.json"
    $outputFile = Join-Path $repoRoot $OutPath
    $outputDir = Split-Path -Parent $outputFile
    if (-not (Test-Path $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    }

    $deadline = (Get-Date).AddSeconds($StartupTimeoutSeconds)
    $seenLineCount = 0

    function Test-ApiReady {
        try {
            Invoke-WebRequest -Uri $openApiUrl -UseBasicParsing -TimeoutSec 3 | Out-Null
            return $true
        }
        catch {
            return $false
        }
    }

    function Show-NewApiOutput {
        # Windows PowerShell 5.1 compatible. Reads whatever the child has written since the last
        # check so the caller sees build and startup progress instead of silence.
        if ($Quiet) { return }
        if (-not (Test-Path $apiOut)) { return }
        $lines = @(Get-Content $apiOut -ErrorAction SilentlyContinue)
        if ($lines.Count -le $seenLineCount) { return }
        for ($i = $seenLineCount; $i -lt $lines.Count; $i++) {
            if ($lines[$i].Trim()) { Write-Host "  $($lines[$i])" -ForegroundColor DarkGray }
        }
        $script:seenLineCount = $lines.Count
    }

    if (Test-ApiReady) {
        Write-Host "Reusing the API already listening on $BaseUrl" -ForegroundColor Yellow
    }
    else {
        Write-Host "Starting the API on $BaseUrl (includes a build)..." -ForegroundColor Cyan

        Remove-Item $apiOut, $apiErr -Force -ErrorAction SilentlyContinue
        New-Item -ItemType File -Path $apiOut -Force | Out-Null
        New-Item -ItemType File -Path $apiErr -Force | Out-Null

        # The child inherits this, and it makes the API load appsettings.Development.json.
        # Without it the host defaults to Production and the connection string is missing.
        $env:ASPNETCORE_ENVIRONMENT = 'Development'

        # -NoNewWindow keeps the child in the caller's console. Without it a separate window opens
        # and every line goes to the log files, which reads as a hang.
        $apiProcess = Start-Process -FilePath 'dotnet' `
            -ArgumentList @(
                'run',
                '--project', 'src/SaikoUniversity.Api',
                '--urls', $BaseUrl,
                '--no-launch-profile'
            ) `
            -WorkingDirectory $repoRoot `
            -PassThru `
            -NoNewWindow `
            -RedirectStandardOutput $apiOut `
            -RedirectStandardError $apiErr

        $ready = $false
        while ((Get-Date) -lt $deadline) {
            Start-Sleep -Milliseconds 500
            Show-NewApiOutput

            if ($apiProcess.HasExited) {
                $stdout = Get-Content $apiOut -Raw -ErrorAction SilentlyContinue
                $stderr = Get-Content $apiErr -Raw -ErrorAction SilentlyContinue
                throw "The API exited with code $($apiProcess.ExitCode) during startup.`n--- stdout ---`n$stdout`n--- stderr ---`n$stderr"
            }

            if (Test-ApiReady) {
                $ready = $true
                break
            }
        }

        if (-not $ready) {
            Show-NewApiOutput
            throw "The API did not serve $openApiUrl within $StartupTimeoutSeconds seconds. Increase -StartupTimeoutSeconds if the first build is slow."
        }

        Show-NewApiOutput
        Write-Host 'API is up.' -ForegroundColor Green
    }

    $document = Invoke-RestMethod -Uri $openApiUrl -TimeoutSec 30
    $json = $document | ConvertTo-Json -Depth 100
    [System.IO.File]::WriteAllText($outputFile, $json, [System.Text.UTF8Encoding]::new($false))

    $pathCount = @($document.paths.PSObject.Properties).Count
    Write-Host "Wrote $pathCount path(s) to $OutPath" -ForegroundColor Green
}
finally {
    if ($apiProcess -and -not $apiProcess.HasExited) {
        Write-Host 'Stopping the API...'
        Stop-ProcessTree -Process $apiProcess
    }

    Pop-Location
}
