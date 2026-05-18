param(
    [switch]$KeepRunning
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

$repoRoot = Split-Path -Parent $PSScriptRoot

$projects = @(
    @{
        Name = "api"
        Project = "trampbazaar.Api/trampbazaar.Api.csproj"
        ReadyUrl = "http://localhost:5136/health/live"
        Probe = {
            param($baseUrl)
            $response = Invoke-WebRequest -UseBasicParsing "$baseUrl/health/live"
            [pscustomobject]@{
                Name = "API health"
                Passed = $response.StatusCode -eq 200
                Detail = "GET /health/live -> $($response.StatusCode)"
            }
        }
    },
    @{
        Name = "web"
        Project = "trampbazaar.Web/trampbazaar.Web.csproj"
        ReadyUrl = "http://localhost:50503/"
        Probe = {
            param($baseUrl)
            $content = Get-PageContent -Url $baseUrl
            $hasBrand = [bool]($content -match "TrampBazaar")
            $hasCta = [bool]($content -match "Ilanlari Kesfet")
            $hasFallback = [bool]($content -match "Veritabani baglantisi su anda kullanilamiyor")
            [pscustomobject]@{
                Name = "Web home"
                Passed = $hasBrand -and $hasCta
                Detail = "GET / -> brand=$hasBrand, cta=$hasCta, fallbackBanner=$hasFallback"
            }
        }
    },
    @{
        Name = "admin"
        Project = "trampbazaar.AdminWeb/trampbazaar.AdminWeb.csproj"
        ReadyUrl = "http://localhost:5257/Login"
        Probe = {
            param($baseUrl)
            $response = Invoke-WebRequest -UseBasicParsing "$baseUrl/Login"
            $content = $response.Content
            $hasLoginHeading = $content -match "Yonetim paneli girisi"
            $hasEmail = $content -match "E-posta"
            [pscustomobject]@{
                Name = "Admin login"
                Passed = $response.StatusCode -eq 200 -and $hasLoginHeading -and $hasEmail
                Detail = "GET /Login -> $($response.StatusCode), heading=$hasLoginHeading, emailField=$hasEmail"
            }
        }
    }
)

$startedProcesses = @()
$results = @()

function Wait-HttpReady {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Url,
        [int]$TimeoutSeconds = 45
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        try {
            Invoke-WebRequest -UseBasicParsing -MaximumRedirection 0 $Url | Out-Null
            return
        }
        catch {
            $response = $_.Exception.Response
            if ($response -and ([int]$response.StatusCode -ge 300) -and ([int]$response.StatusCode -lt 400)) {
                return
            }
            Start-Sleep -Milliseconds 750
        }
    } while ((Get-Date) -lt $deadline)

    throw "Timed out waiting for $Url"
}

function Get-PageContent {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Url
    )

    $content = & curl.exe -k -L -s $Url
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($content)) {
        throw "Failed to fetch page content from $Url"
    }

    return $content
}

try {
    foreach ($project in $projects) {
        $process = Start-Process dotnet `
            -ArgumentList "run --project $($project.Project) --no-build" `
            -WorkingDirectory $repoRoot `
            -PassThru

        $startedProcesses += [pscustomobject]@{
            Name = $project.Name
            Process = $process
            ReadyUrl = $project.ReadyUrl
            Probe = $project.Probe
        }
    }

    foreach ($started in $startedProcesses) {
        Wait-HttpReady -Url $started.ReadyUrl
    }

    $results += & $startedProcesses[0].Probe "http://localhost:5136"
    $results += & $startedProcesses[1].Probe "https://localhost:50502"
    $results += & $startedProcesses[2].Probe "http://localhost:5257"

    $failed = @($results | Where-Object { -not $_.Passed })

    Write-Host ""
    Write-Host "Demo check results" -ForegroundColor Cyan
    foreach ($result in $results) {
        $color = if ($result.Passed) { "Green" } else { "Red" }
        $status = if ($result.Passed) { "PASS" } else { "FAIL" }
        Write-Host ("[{0}] {1} - {2}" -f $status, $result.Name, $result.Detail) -ForegroundColor $color
    }

    if ($KeepRunning) {
        Write-Host ""
        Write-Host "Processes are still running because -KeepRunning was specified." -ForegroundColor Yellow
        Write-Host "API:   http://localhost:5136" -ForegroundColor Yellow
        Write-Host "Web:   http://localhost:50503" -ForegroundColor Yellow
        Write-Host "Admin: http://localhost:5257" -ForegroundColor Yellow
    }

    if (@($failed).Count -gt 0) {
        exit 1
    }
}
finally {
    if (-not $KeepRunning) {
        foreach ($started in $startedProcesses) {
            if (-not $started.Process.HasExited) {
                Stop-Process -Id $started.Process.Id -Force
            }
        }
    }
}
