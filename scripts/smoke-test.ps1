[CmdletBinding()]
param(
    [string]$ResourceGroup = 'rg-employee-onboarding-dev',
    [string]$DeploymentName = 'employee-onboarding-infra'
)

$ErrorActionPreference = 'Stop'

$outputs = az deployment group show `
    --resource-group $ResourceGroup `
    --name $DeploymentName `
    --query properties.outputs `
    -o json | ConvertFrom-Json

$apiUrl = $outputs.apiUrl.value
$frontendUrl = $outputs.frontendUrl.value

Write-Host "Checking $frontendUrl..."
$frontend = Invoke-WebRequest -Uri $frontendUrl -Method Get -UseBasicParsing
if ($frontend.StatusCode -ne 200) {
    throw "Frontend returned HTTP $($frontend.StatusCode)."
}

Write-Host "Frontend: HTTP $($frontend.StatusCode)" -ForegroundColor Green

Write-Host "Checking $apiUrl/health..."
$health = Invoke-RestMethod -Uri "$apiUrl/health" -Method Get
Write-Host "Health: $health" -ForegroundColor Green

Write-Host 'Fetching employees...'
$employees = Invoke-RestMethod -Uri "$apiUrl/api/employees" -Method Get
$employees | Select-Object name, email, department, profilePictureUrl | Format-Table

Write-Host 'Fetching notifications...'
$notifications = Invoke-RestMethod -Uri "$apiUrl/api/notifications?take=10" -Method Get
Write-Host "Notifications returned: $($notifications.Count)"
