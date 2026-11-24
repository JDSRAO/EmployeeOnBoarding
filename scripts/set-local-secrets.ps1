[CmdletBinding()]
param(
    [string]$ResourceGroup = 'rg-employee-onboarding-dev',
    [string]$DeploymentName = 'employee-onboarding-infra',
    [string]$ApiProject = (Join-Path $PSScriptRoot '..\backend\src\EmployeeOnBoarding.Api'),
    [string]$FunctionsProject = (Join-Path $PSScriptRoot '..\EmployeeOnBoarding.Functions'),
    [string]$FrontendPath = (Join-Path $PSScriptRoot '..\frontend'),
    [string]$ApiBaseUrl = 'http://localhost:5215',
    [switch]$GrantKeyVaultAccess
)

$ErrorActionPreference = 'Stop'

$outputs = az deployment group show `
    --resource-group $ResourceGroup `
    --name $DeploymentName `
    --query properties.outputs `
    -o json | ConvertFrom-Json

if (-not $outputs) {
    throw "Could not read deployment outputs for $DeploymentName in $ResourceGroup."
}

$keyVaultName = $outputs.keyVaultName.value

if ($GrantKeyVaultAccess) {
    Write-Host "Granting current user Key Vault Secrets User on $keyVaultName..."
    $userObjectId = az ad signed-in-user show --query id -o tsv
    $keyVaultId = az keyvault show --name $keyVaultName --query id -o tsv

    az role assignment create `
        --role 'Key Vault Secrets User' `
        --assignee-object-id $userObjectId `
        --assignee-principal-type User `
        --scope $keyVaultId `
        --output none

    Write-Host 'Waiting for role assignment propagation...'
    Start-Sleep -Seconds 15
}

function Get-KeyVaultSecret([string]$name) {
    $value = az keyvault secret show --vault-name $keyVaultName --name $name --query value -o tsv
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($value)) {
        throw "Could not read secret $name from $keyVaultName. Grant Key Vault access and try again."
    }

    return $value
}

$sqlConnectionString = Get-KeyVaultSecret 'SqlConnectionString'
$storageConnectionString = Get-KeyVaultSecret 'StorageConnectionString'
$serviceBusConnectionString = Get-KeyVaultSecret 'ServiceBusConnectionString'
$appInsightsConnectionString = Get-KeyVaultSecret 'ApplicationInsightsConnectionString'

Write-Host 'Writing .NET user secrets and local Functions settings...'
dotnet user-secrets set 'ConnectionStrings:SqlDatabase' $sqlConnectionString --project $ApiProject | Out-Null
dotnet user-secrets set 'ConnectionStrings:BlobStorage' $storageConnectionString --project $ApiProject | Out-Null
dotnet user-secrets set 'ServiceBus:ConnectionString' $serviceBusConnectionString --project $ApiProject | Out-Null

$localSettingsPath = Join-Path $FunctionsProject 'local.settings.json'
if (Test-Path $localSettingsPath) {
    $functionSettings = Get-Content -Path $localSettingsPath -Raw | ConvertFrom-Json
}
else {
    $functionSettings = [pscustomobject]@{
        IsEncrypted = $false
        Values = [pscustomobject]@{}
    }
}

$functionValues = $functionSettings.Values
$functionValues | Add-Member -NotePropertyName 'AzureWebJobsStorage' -NotePropertyValue $storageConnectionString -Force
$functionValues | Add-Member -NotePropertyName 'FUNCTIONS_WORKER_RUNTIME' -NotePropertyValue 'dotnet-isolated' -Force
$functionValues | Add-Member -NotePropertyName 'ConnectionStrings__SqlDatabase' -NotePropertyValue $sqlConnectionString -Force
$functionValues | Add-Member -NotePropertyName 'ServiceBusConnection' -NotePropertyValue $serviceBusConnectionString -Force
$functionValues | Add-Member -NotePropertyName 'ServiceBusQueueName' -NotePropertyValue $outputs.serviceBusQueueName.value -Force
$functionValues | Add-Member -NotePropertyName 'APPLICATIONINSIGHTS_CONNECTION_STRING' -NotePropertyValue $appInsightsConnectionString -Force
$functionSettings | ConvertTo-Json -Depth 10 | Set-Content -Path $localSettingsPath -Encoding utf8

$frontendEnvironment = @(
    "NEXT_PUBLIC_API_BASE_URL=$ApiBaseUrl"
    "NEXT_PUBLIC_APPINSIGHTS_CONNECTION_STRING=$appInsightsConnectionString"
) -join "`n"

$frontendEnvironmentPath = Join-Path $FrontendPath '.env.local'
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText($frontendEnvironmentPath, $frontendEnvironment, $utf8NoBom)

Write-Host ''
Write-Host 'Local configuration ready' -ForegroundColor Green
Write-Host "API base URL: $ApiBaseUrl"
Write-Host "Frontend .env.local: $(Join-Path $FrontendPath '.env.local')"
