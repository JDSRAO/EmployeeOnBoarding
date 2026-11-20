[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroup,
    [Parameter(Mandatory = $true)]
    [string]$SqlServerName,
    [string]$DatabaseName = 'EmployeeOnBoarding',
    [string]$SqlAdminLogin = 'sqladmin',
    [Parameter(Mandatory = $true)]
    [securestring]$SqlAdminPassword,
    [Parameter(Mandatory = $true)]
    [string]$DacpacPath,
    [switch]$KeepFirewallRule
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $DacpacPath)) {
    throw "Dacpac not found at $DacpacPath. Build the SQL project first."
}

if (-not (Get-Command sqlpackage -ErrorAction SilentlyContinue)) {
    throw 'sqlpackage was not found. Install it with: dotnet tool install --global Microsoft.SqlPackage'
}

$firewallRuleName = 'EmployeeOnBoardingDeployment'
$clientIp = (Invoke-RestMethod -Uri 'https://api.ipify.org?format=json').ip
$serverFqdn = az sql server show `
    --resource-group $ResourceGroup `
    --name $SqlServerName `
    --query fullyQualifiedDomainName `
    -o tsv

$plainPassword = [System.Net.NetworkCredential]::new('', $SqlAdminPassword).Password
Add-Type -AssemblyName System.Data -ErrorAction SilentlyContinue
$connectionBuilder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new()
$connectionBuilder['Server'] = "tcp:$serverFqdn,1433"
$connectionBuilder['Initial Catalog'] = $DatabaseName
$connectionBuilder['Persist Security Info'] = $false
$connectionBuilder['User ID'] = $SqlAdminLogin
$connectionBuilder['Password'] = $plainPassword
$connectionBuilder['MultipleActiveResultSets'] = $false
$connectionBuilder['Encrypt'] = $true
$connectionBuilder['TrustServerCertificate'] = $false
$connectionBuilder['Connection Timeout'] = 60
$connectionString = $connectionBuilder.ConnectionString

Write-Host "Allowing deployment client $clientIp through the SQL firewall..."
az sql server firewall-rule create `
    --resource-group $ResourceGroup `
    --server $SqlServerName `
    --name $firewallRuleName `
    --start-ip-address $clientIp `
    --end-ip-address $clientIp `
    --output none

try {
    Write-Host 'Publishing dacpac...'
    sqlpackage `
        /Action:Publish `
        "/SourceFile:$DacpacPath" `
        "/TargetConnectionString:$connectionString" `
        /p:BlockOnPossibleDataLoss=false

    if ($LASTEXITCODE -ne 0) {
        throw 'sqlpackage publish failed.'
    }

    Write-Host 'Database schema deployed.' -ForegroundColor Green
}
finally {
    if (-not $KeepFirewallRule) {
        Write-Host 'Removing temporary SQL firewall rule...'
        az sql server firewall-rule delete `
            --resource-group $ResourceGroup `
            --server $SqlServerName `
            --name $firewallRuleName `
            --yes `
            --output none
    }
}
