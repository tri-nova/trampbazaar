param(
    [Parameter(Mandatory = $true)]
    [string]$ApiBaseUrl,
    [Parameter(Mandatory = $true)]
    [string]$WebBaseUrl,
    [Parameter(Mandatory = $true)]
    [string]$AdminBaseUrl,
    [string]$UserEmail = "batu@example.com",
    [string]$UserPassword = "Password123!",
    [string]$AdminEmail = "admin@example.com",
    [string]$AdminPassword = "Password123!"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

function Invoke-JsonPost {
    param(
        [string]$Url,
        [object]$Body,
        [hashtable]$Headers = @{}
    )

    return Invoke-RestMethod -Uri $Url -Method Post -ContentType "application/json" -Body ($Body | ConvertTo-Json) -Headers $Headers
}

function Assert-Health {
    param(
        [string]$Url,
        [string]$Name
    )

    $response = Invoke-RestMethod -Uri $Url -Method Get
    if ($response.status -ne "ok") {
        throw "$Name health failed."
    }
    Write-Host ("PASS {0} health" -f $Name) -ForegroundColor Green
}

Assert-Health -Url "$ApiBaseUrl/health/live" -Name "API"
Assert-Health -Url "$WebBaseUrl/health/live" -Name "Web"
Assert-Health -Url "$AdminBaseUrl/health/live" -Name "Admin"

$login = Invoke-JsonPost -Url "$ApiBaseUrl/api/auth/login" -Body @{
    email = $UserEmail
    password = $UserPassword
}

if (-not $login.accessToken) {
    throw "User login failed."
}

$userHeaders = @{ Authorization = "Bearer $($login.accessToken)" }

$profile = Invoke-RestMethod -Uri "$ApiBaseUrl/api/account/profile" -Headers $userHeaders -Method Get
$orders = Invoke-RestMethod -Uri "$ApiBaseUrl/api/account/orders" -Headers $userHeaders -Method Get
$ledger = Invoke-RestMethod -Uri "$ApiBaseUrl/api/account/ledger" -Headers $userHeaders -Method Get
$favorites = Invoke-RestMethod -Uri "$ApiBaseUrl/api/account/favorites" -Headers $userHeaders -Method Get
$stockAlerts = Invoke-RestMethod -Uri "$ApiBaseUrl/api/account/stock-alerts" -Headers $userHeaders -Method Get
$priceAlerts = Invoke-RestMethod -Uri "$ApiBaseUrl/api/account/price-alerts" -Headers $userHeaders -Method Get

Write-Host ("PASS user profile: {0}" -f $profile.email) -ForegroundColor Green
Write-Host ("PASS orders: {0}" -f $orders.Count) -ForegroundColor Green
Write-Host ("PASS ledger entries: {0}" -f $ledger.entries.Count) -ForegroundColor Green
Write-Host ("PASS favorites: {0}" -f $favorites.Count) -ForegroundColor Green
Write-Host ("PASS stock alerts: {0}" -f $stockAlerts.Count) -ForegroundColor Green
Write-Host ("PASS price alerts: {0}" -f $priceAlerts.Count) -ForegroundColor Green

$adminLogin = Invoke-JsonPost -Url "$ApiBaseUrl/api/admin/auth/login" -Body @{
    email = $AdminEmail
    password = $AdminPassword
}

if (-not $adminLogin.accessToken) {
    throw "Admin login failed."
}

$adminHeaders = @{ Authorization = "Bearer $($adminLogin.accessToken)" }
$adminUsers = Invoke-RestMethod -Uri "$ApiBaseUrl/api/admin/users" -Headers $adminHeaders -Method Get
$adminListings = Invoke-RestMethod -Uri "$ApiBaseUrl/api/admin/listings" -Headers $adminHeaders -Method Get
$adminPayments = Invoke-RestMethod -Uri "$ApiBaseUrl/api/admin/payments" -Headers $adminHeaders -Method Get
$adminComplaints = Invoke-RestMethod -Uri "$ApiBaseUrl/api/admin/complaints" -Headers $adminHeaders -Method Get

Write-Host ("PASS admin users: {0}" -f $adminUsers.Count) -ForegroundColor Green
Write-Host ("PASS admin listings: {0}" -f $adminListings.Count) -ForegroundColor Green
Write-Host ("PASS admin payments: {0}" -f $adminPayments.Count) -ForegroundColor Green
Write-Host ("PASS admin complaints: {0}" -f $adminComplaints.Count) -ForegroundColor Green

Write-Host "Server smoke test completed." -ForegroundColor Green
