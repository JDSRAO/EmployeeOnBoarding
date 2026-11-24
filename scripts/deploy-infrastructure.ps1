[CmdletBinding()]
param(
    [string]$ResourceGroup = 'rg-employee-onboarding-dev',
    [string]$Location = 'eastus2',
    [string]$EnvironmentName = 'dev',
    [string]$DeploymentName = 'employee-onboarding-infra',
    [Parameter(Mandatory = $true)]
    [securestring]$SqlAdminPassword,
    [switch]$AllowAllSqlIpAddresses
)

$ErrorActionPreference = 'Stop'

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw 'Azure CLI (az) was not found. Install it from https://aka.ms/azure-cli.'
}

Write-Host 'Checking Azure sign-in...'
$account = az account show --query '{name:name,id:id}' -o json | ConvertFrom-Json
if (-not $account) {
    throw 'No Azure subscription is signed in. Run az login first.'
}

Write-Host "Using subscription $($account.name) ($($account.id))"

if ((az group exists --name $ResourceGroup) -ne 'true') {
    Write-Host "Creating resource group $ResourceGroup in $Location..."
    az group create --name $ResourceGroup --location $Location --output none
}

$clientIp = (Invoke-RestMethod -Uri 'https://api.ipify.org?format=json').ip
$plainPassword = [System.Net.NetworkCredential]::new('', $SqlAdminPassword).Password

$parameters = @{
    '$schema'      = 'https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#'
    contentVersion = '1.0.0.0'
    parameters     = @{
        environmentName      = @{ value = $EnvironmentName }
        location             = @{ value = $Location }
        sqlAdminPassword     = @{ value = $plainPassword }
        clientIpAddress      = @{ value = $clientIp }
        allowAllIpAddresses  = @{ value = $AllowAllSqlIpAddresses.IsPresent }
    }
}

$tempParameterFile = [System.IO.Path]::GetTempFileName()
try {
    $json = $parameters | ConvertTo-Json -Depth 10
    [System.IO.File]::WriteAllText($tempParameterFile, $json, [System.Text.UTF8Encoding]::new($false))

    Write-Host "Deploying infrastructure to $ResourceGroup. This can take several minutes..."
    az deployment group create `
        --resource-group $ResourceGroup `
        --name $DeploymentName `
        --template-file (Join-Path $PSScriptRoot '..\infra\main.bicep') `
        --parameters $tempParameterFile `
        --output none

    if ($LASTEXITCODE -ne 0) {
        throw 'Infrastructure deployment failed.'
    }
}
finally {
    Remove-Item -LiteralPath $tempParameterFile -Force -ErrorAction SilentlyContinue
}

$outputs = az deployment group show `
    --resource-group $ResourceGroup `
    --name $DeploymentName `
    --query properties.outputs `
    -o json | ConvertFrom-Json

Write-Host ''
Write-Host 'Deployment complete' -ForegroundColor Green
Write-Host "Frontend:       $($outputs.frontendUrl.value)"
Write-Host "Static Web App: $($outputs.staticWebAppName.value)"
Write-Host "API:            $($outputs.apiUrl.value)"
Write-Host "API app:        $($outputs.apiAppName.value)"
Write-Host "Function app:   $($outputs.workerAppName.value)"
Write-Host "Swagger:        $($outputs.swaggerUrl.value)"
Write-Host "Key Vault:      $($outputs.keyVaultName.value)"
Write-Host "SQL server:     $($outputs.sqlServerName.value).database.windows.net"
Write-Host "Storage:        $($outputs.storageAccountName.value)"
Write-Host "Service Bus:    $($outputs.serviceBusNamespaceName.value)"
Write-Host "App Insights:   $($outputs.applicationInsightsName.value)"
