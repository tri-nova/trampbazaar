param(
    [Parameter(Mandatory = $true)]
    [string]$ConnectionString,
    [int]$StartAt = 1,
    [int]$EndAt = 8
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$sqlRoot = Join-Path $PSScriptRoot "..\Database\SqlServer"
$scripts = Get-ChildItem $sqlRoot -Filter "*.sql" |
    Where-Object { $_.BaseName -match '^\d{3}_' } |
    Sort-Object Name |
    Where-Object {
        $number = [int]($_.BaseName.Split('_')[0])
        $number -ge $StartAt -and $number -le $EndAt
    }

if ($scripts.Count -eq 0) {
    throw "No SQL scripts found between $StartAt and $EndAt."
}

$connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
try {
    $connection.Open()

    foreach ($script in $scripts) {
        Write-Host ("Running {0}" -f $script.Name) -ForegroundColor Cyan
        $sql = Get-Content $script.FullName -Raw
        $batches = [regex]::Split($sql, '(?im)^\s*GO\s*$') | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

        foreach ($batch in $batches) {
            $command = $connection.CreateCommand()
            $command.CommandTimeout = 180
            $command.CommandText = $batch
            [void]$command.ExecuteNonQuery()
        }
    }
}
finally {
    if ($connection.State -ne [System.Data.ConnectionState]::Closed) {
        $connection.Close()
    }
}

Write-Host "SQL migration run completed." -ForegroundColor Green
