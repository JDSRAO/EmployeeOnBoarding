# Deployment Guide

This guide covers three paths:

1. Manual deployment with the helper scripts (best for a trainer preparing a lab).
2. GitHub Actions deployment using OpenID Connect (recommended for the sample repository).
3. Azure DevOps YAML deployment using an Azure Resource Manager service connection.

All paths create the same resources from `infra/main.bicep`.

## 1. Prerequisites

- An Azure subscription where you can create resource groups and role assignments.
- Azure CLI with Bicep support.
- .NET 10 SDK.
- Node.js 22 or later.
- `sqlpackage` for dacpac deployment:

  ```powershell
  dotnet tool install --global Microsoft.SqlPackage
  ```

- PowerShell 7 or Windows PowerShell 5.1 for the scripts.
- An Azure region that supports Static Web Apps Free. The sample defaults to `eastus2`.

The deployment identity needs:

- `Contributor` on the target resource group.
- `User Access Administrator` (or equivalent RBAC role-assignment permission) on the resource group, because Bicep creates Key Vault role assignments for the app managed identities.

## 2. Default resource names

The naming uses `environmentName` and a deterministic eight-character suffix.

| Resource | Pattern | Default example |
| --- | --- | --- |
| Resource group | user supplied | `rg-employee-onboarding-dev` |
| App Service plan | `plan-eob-<env>-<suffix>` | `plan-eob-dev-a1b2c3d4` |
| API App Service | `app-eob-api-<env>-<suffix>` | `app-eob-api-dev-a1b2c3d4` |
| Worker App Service | `app-eob-worker-<env>-<suffix>` | `app-eob-worker-dev-a1b2c3d4` |
| Static Web App | `swa-eob-<env>-<suffix>` | `swa-eob-dev-a1b2c3d4` |
| SQL server | `sql-eob-<env>-<suffix>` | `sql-eob-dev-a1b2c3d4` |
| SQL database | fixed | `EmployeeOnBoarding` |
| Storage account | `steob<env><suffix>` | `steobdeva1b2c3d4` |
| Service Bus namespace | `sb-eob-<env>-<suffix>` | `sb-eob-dev-a1b2c3d4` |
| Key Vault | `kv-eob-<env>-<suffix>` | `kv-eob-dev-a1b2c3d4` |
| Application Insights | `appi-eob-<env>-<suffix>` | `appi-eob-dev-a1b2c3d4` |
| Log Analytics | `log-eob-<env>-<suffix>` | `log-eob-dev-a1b2c3d4` |

`environmentName` and `namePrefix` are limited to five characters so Key Vault names stay within the 24-character limit.

## 3. Manual deployment with the scripts

### 3.1 Sign in and choose a subscription

```powershell
az login
az account set --subscription "<subscription-id-or-name>"
```

### 3.2 Build the database project

```powershell
dotnet build backend/database/EmployeeOnBoarding.Database/EmployeeOnBoarding.Database.sqlproj -c Release
```

The dacpac is written to:

```text
backend/database/EmployeeOnBoarding.Database/bin/Release/EmployeeOnBoarding.Database.dacpac
```

### 3.3 Deploy infrastructure

```powershell
$password = Read-Host -AsSecureString "SQL administrator password"

.\scripts\deploy-infrastructure.ps1 `
  -ResourceGroup rg-employee-onboarding-dev `
  -Location eastus2 `
  -SqlAdminPassword $password
```

The script:

- Creates the resource group if needed.
- Detects the current public IP and allows it through the SQL firewall.
- Deploys `infra/main.bicep`.
- Creates all Key Vault secrets.
- Prints the frontend, API, Key Vault and data-service names.

The SQL password must be at least 12 characters. For this sample, avoid semicolons in the password because it is embedded in a SQL connection string.

### 3.4 Deploy the database schema

Use the SQL server name printed by the previous script, without `.database.windows.net`:

```powershell
.\scripts\deploy-database.ps1 `
  -ResourceGroup rg-employee-onboarding-dev `
  -SqlServerName sql-eob-dev-a1b2c3d4 `
  -DacpacPath backend/database/EmployeeOnBoarding.Database/bin/Release/EmployeeOnBoarding.Database.dacpac `
  -SqlAdminPassword $password
```

The script adds a temporary firewall rule for the current IP, publishes the dacpac, and removes the rule. Pass `-KeepFirewallRule` if you want to query SQL from the same machine afterward.

### 3.5 Run locally against Azure resources

```powershell
.\scripts\set-local-secrets.ps1 -GrantKeyVaultAccess
```

Then follow [local-development.md](local-development.md). The `-GrantKeyVaultAccess` switch is only needed if the signed-in user cannot already read Key Vault secrets.

### 3.6 Use the deployed apps

The API and worker are deployed by CI/CD, not by the infrastructure script. The infrastructure deployment creates empty App Services. To publish code manually:

```powershell
dotnet publish backend/src/EmployeeOnBoarding.Api/EmployeeOnBoarding.Api.csproj -c Release -o publish/api
dotnet publish backend/src/EmployeeOnBoarding.Worker/EmployeeOnBoarding.Worker.csproj -c Release -o publish/worker
Compress-Archive -Path publish/api/* -DestinationPath publish/api.zip -Force
Compress-Archive -Path publish/worker/* -DestinationPath publish/worker.zip -Force

az webapp deploy -g rg-employee-onboarding-dev -n <api-app-name> --src-path publish/api.zip --type zip
az webapp deploy -g rg-employee-onboarding-dev -n <worker-app-name> --src-path publish/worker.zip --type zip
```

For the frontend, use one of the CI/CD pipelines or the Static Web Apps deployment token described below.

## 4. GitHub Actions deployment

The workflow is `.github/workflows/employee-onboarding.yml`. It validates pull requests and deploys on pushes to `main` or manual runs.

### 4.1 Create the resource group

```powershell
az group create --name rg-employee-onboarding-dev --location eastus2
```

### 4.2 Create an Entra application for OIDC

```powershell
$app = az ad app create --display-name "gh-employee-onboarding" --query appId -o tsv
$sp = az ad sp create --id $app --query id -o tsv
$rgId = az group show --name rg-employee-onboarding-dev --query id -o tsv

az role assignment create --assignee $app --role Contributor --scope $rgId
az role assignment create --assignee $app --role "User Access Administrator" --scope $rgId
```

Create a federated credential for the `main` branch:

```powershell
$credential = @{
  name        = "github-main"
  issuer      = "https://token.actions.githubusercontent.com"
  subject     = "repo:<github-org>/<repository>:ref:refs/heads/main"
  description = "GitHub Actions main branch"
  audiences   = @("api://AzureADTokenExchange")
} | ConvertTo-Json

$credential | Set-Content federated-credential.json -Encoding utf8
az ad app federated-credential create --id $app --parameters federated-credential.json
Remove-Item federated-credential.json
```

Replace `<github-org>/<repository>` with the actual repository. If you add a GitHub environment with approvals, change the subject to match that environment.

### 4.3 Configure GitHub secrets and variables

Secrets:

| Name | Value |
| --- | --- |
| `AZURE_CLIENT_ID` | The `$app` application ID. |
| `AZURE_TENANT_ID` | `az account show --query tenantId -o tsv`. |
| `AZURE_SUBSCRIPTION_ID` | `az account show --query id -o tsv`. |
| `SQL_ADMIN_PASSWORD` | A strong password with at least 12 characters. Avoid semicolons in the sample because the password is embedded in a SQL connection string. |

Variables (optional, defaults are shown):

| Name | Default |
| --- | --- |
| `AZURE_RESOURCE_GROUP` | `rg-employee-onboarding-dev` |
| `AZURE_LOCATION` | `eastus2` |
| `ENVIRONMENT_NAME` | `dev` |
| `NEXT_PUBLIC_APPINSIGHTS_CONNECTION_STRING` | empty (browser telemetry disabled) |

### 4.4 Run the workflow

Push to `main` or run **Actions → Employee Onboarding → Run workflow**.

The workflow:

1. Builds the API, worker, SQL project and frontend.
2. Deploys Bicep.
3. Adds the GitHub runner IP to the SQL firewall.
4. Publishes the dacpac.
5. Deploys the API and worker zips.
6. Calls the API health endpoint and reads the employee list to verify SQL and Key Vault access.
7. Removes the temporary SQL firewall rule.
8. Builds the Next.js static export into `out` and deploys it to Static Web Apps with the API URL baked into the build.

If the smoke test fails immediately after the first deployment, wait a few minutes for the Key Vault role assignments to propagate, then restart the API app or re-run the workflow.

## 5. Azure DevOps deployment

The pipeline is `.azure_devops/azure-pipelines.yml`.

### 5.1 Prepare Azure access

Create the resource group and an Azure Resource Manager service connection with workload identity federation:

```powershell
az group create --name rg-employee-onboarding-dev --location eastus2
```

In Azure DevOps: **Project settings → Service connections → New service connection → Azure Resource Manager → Workload identity federation (automatic)**. Scope it to the subscription or the target resource group and name it `sc-employee-onboarding`. Grant its identity `Contributor` and `User Access Administrator` on the resource group.

### 5.2 Configure the secret variable

Create a pipeline variable or variable group named `sqlAdminPassword` and mark it as secret. Optionally override these YAML variables:

```yaml
azureServiceConnection: sc-employee-onboarding
resourceGroup: rg-employee-onboarding-dev
location: eastus2
environmentName: dev
```

### 5.3 Create the pipeline

Create a pipeline from an existing YAML file and select `.azure_devops/azure-pipelines.yml`.

The pipeline builds on every pull request and deploys only when `Build.Reason` is not `PullRequest`.

### 5.4 Browser telemetry

The Azure DevOps pipeline retrieves the Application Insights connection string from the deployed resource and passes it to the frontend build automatically. No extra pipeline variable is required.

The GitHub workflow reads `NEXT_PUBLIC_APPINSIGHTS_CONNECTION_STRING` from a repository variable. Leave it empty to disable browser telemetry.

## 6. Post-deployment validation

Retrieve the outputs:

```powershell
az deployment group show `
  --resource-group rg-employee-onboarding-dev `
  --name employee-onboarding-infra `
  --query properties.outputs
```

Run the smoke test:

```powershell
.\scripts\smoke-test.ps1
```

Then validate the asynchronous flow in the UI:

1. Open the frontend URL.
2. Add an employee with a JPEG or PNG profile picture.
3. Confirm the employee appears and the image renders.
4. Open **Notifications** and wait up to 10 seconds for the `Created` notification.
5. Edit the employee and confirm an `Updated` notification appears.
6. In the Azure portal, open the SQL database and run:

   ```sql
   SELECT Name, Email, Department, ProfilePictureUrl FROM dbo.Employees;
   SELECT EmployeeName, EventType, Message, CreatedAtUtc FROM dbo.Notifications ORDER BY CreatedAtUtc DESC;
   ```

7. In the storage account, open **Containers → profile-pictures** and confirm the uploaded blob exists.
8. In Service Bus, confirm the `employee-events` queue has no active or dead-letter messages.
9. In Application Insights, open **Transaction search** and find the API request, Blob dependency, SQL dependency and worker trace.

## 7. Troubleshooting

| Symptom | Cause and fix |
| --- | --- |
| App Service returns 500 and logs a Key Vault reference error | The managed identity role assignment has not propagated, or the Key Vault secret URI is wrong. Wait 5–10 minutes, verify the app setting starts with `@Microsoft.KeyVault(SecretUri=https://kv-...`, then restart the app. |
| API starts but SQL login fails | Password mismatch or firewall. Re-check `SqlConnectionString` in Key Vault, confirm the SQL firewall includes the caller, and re-run the deployment with the same password. |
| Browser shows a CORS error | The API's `Cors__AllowedOrigins__0` is the Static Web App hostname. If you added a custom domain, add another allowed origin and restart the API. |
| Images return 404/403 | Confirm the `profile-pictures` container exists and public blob access is enabled. The sample uses public read access for teaching. |
| Notifications never appear | Check worker logs (`az webapp log tail`), the queue's active and dead-letter counts, and the SQL `Notifications` table. A malformed event can end up in the dead-letter queue after five attempts. |
| Database pipeline step cannot connect | The GitHub/ADO runner IP was not added to the SQL firewall, or the password contains a semicolon that broke the connection string. Avoid semicolons in the sample password. |
| Static Web Apps deployment fails | Confirm the deployment token was retrieved successfully, the frontend static export produced `frontend/out/index.html`, and the Node version is 22 or later. Check the build log for `next build` errors. |
| .NET 10 runtime not available | Choose a region where App Service supports the .NET 10 Linux runtime, or update `apiLinuxFxVersion`/`workerLinuxFxVersion` after confirming the supported string in the portal. |

## 8. Cost and cleanup

The sample uses low-cost SKUs:

- Static Web Apps Free.
- One App Service B1 Linux plan hosting both apps.
- SQL Database Basic.
- Service Bus Standard.
- Storage Standard LRS.
- Application Insights and Log Analytics consumption-based.
- Key Vault standard, charged per operation.

Delete everything when the training is over:

```powershell
az group delete --name rg-employee-onboarding-dev --yes --no-wait
```

Key Vault soft delete keeps the vault name reserved for seven days by default. If you need to reuse the exact same names immediately, purge the vault or use a different environment name.
