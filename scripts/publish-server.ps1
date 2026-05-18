param(
    [string]$Configuration = "Release",
    [string]$OutputRoot = ".\artifacts\server"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$outputRootPath = Join-Path $repoRoot $OutputRoot

$projects = @(
    @{
        Name = "api"
        Project = "trampbazaar.Api\trampbazaar.Api.csproj"
    },
    @{
        Name = "web"
        Project = "trampbazaar.Web\trampbazaar.Web.csproj"
    },
    @{
        Name = "admin"
        Project = "trampbazaar.AdminWeb\trampbazaar.AdminWeb.csproj"
    }
)

foreach ($project in $projects) {
    $projectOutput = Join-Path $outputRootPath $project.Name
    if (Test-Path $projectOutput) {
        Remove-Item $projectOutput -Recurse -Force
    }

    dotnet publish (Join-Path $repoRoot $project.Project) `
        -c $Configuration `
        -o $projectOutput `
        /p:UseAppHost=false

    if ($LASTEXITCODE -ne 0) {
        throw "Publish failed: $($project.Project)"
    }
}

Write-Host "Published server artifacts:" -ForegroundColor Green
foreach ($project in $projects) {
    Write-Host ("- {0}: {1}" -f $project.Name, (Join-Path $outputRootPath $project.Name)) -ForegroundColor Yellow
}
