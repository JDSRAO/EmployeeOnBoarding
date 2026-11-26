# Local Development Setup

The sample is intentionally cloud-first: the API uses Azure SQL Database, Azure Blob Storage, Azure Service Bus and Key Vault. Local development therefore uses a deployed development environment for data services. This keeps the training focused on the Azure services rather than emulator setup.

If you have not deployed the Azure resources yet, follow [deployment-guide.md](deployment-guide.md) sections 3.1–3.4.

## 1. Prerequisites

- .NET 10 SDK.
- Node.js 22 or later and npm.
- Azure CLI signed in to the training subscription.
- The development resource group already deployed.
- The database schema published.

Verify:

```powershell
dotnet --version
node --version
az account show
```

## 2. Restore and build

```powershell
dotnet restore EmployeeOnBoarding.slnx
dotnet build backend/src/EmployeeOnBoarding.Api/EmployeeOnBoarding.Api.csproj
dotnet build EmployeeOnBoarding.Functions/EmployeeOnBoarding.Functions.csproj
dotnet build backend/database/EmployeeOnBoarding.Database/EmployeeOnBoarding.Database.sqlproj
```

Install frontend packages:

```powershell
cd frontend
npm install
cd ..
```

## 3. Configure .NET user secrets

The API uses .NET user secrets. The helper script also reads the Service Bus and storage settings from Key Vault and writes the Functions values into the ignored `EmployeeOnBoarding.Functions/local.settings.json` file.

```powershell
.\scripts\set-local-secrets.ps1 -GrantKeyVaultAccess
```

The script sets:

| Project | Key |
| --- | --- |
| API | `ConnectionStrings:SqlDatabase` |
| API | `ConnectionStrings:BlobStorage` |
| Functions | `ConnectionStrings__SqlDatabase` |
| Functions | `ServiceBusConnection` |
| Functions | `ServiceBusQueueName` |
| Functions | `AzureWebJobsStorage` |

It also writes `frontend/.env.local` with:

```dotenv
NEXT_PUBLIC_API_BASE_URL=http://localhost:5215
NEXT_PUBLIC_APPINSIGHTS_CONNECTION_STRING=<application-insights-connection-string>
```

The frontend targets the local API by default so browser requests use the local development CORS configuration. Pass `-ApiBaseUrl https://<api-app>.azurewebsites.net` if you intentionally want the local frontend to call the deployed API.

If you cannot grant Key Vault access, retrieve the values manually from the Key Vault secrets:

- `SqlConnectionString`
- `StorageConnectionString`
- `ServiceBusConnectionString`
- `ApplicationInsightsConnectionString`

Then set them:

```powershell
dotnet user-secrets set "ConnectionStrings:SqlDatabase" "<value>" --project backend/src/EmployeeOnBoarding.Api
dotnet user-secrets set "ConnectionStrings:BlobStorage" "<value>" --project backend/src/EmployeeOnBoarding.Api
dotnet user-secrets set "ServiceBus:ConnectionString" "<value>" --project backend/src/EmployeeOnBoarding.Api
```

Set `ConnectionStrings__SqlDatabase`, `ServiceBusConnection`, `ServiceBusQueueName`, and `AzureWebJobsStorage` in `EmployeeOnBoarding.Functions/local.settings.json` under `Values`. Keep the connection string values out of source control.

Do not place these values in `appsettings.json` or commit them.

## 4. Run the services

Use three terminals.

### API

```powershell
dotnet run --project backend/src/EmployeeOnBoarding.Api --launch-profile http
```

- API: `http://localhost:5215`
- Swagger: `http://localhost:5215/swagger`
- Health: `http://localhost:5215/health`

### Functions listener

Install Azure Functions Core Tools v4, then run:

```powershell
cd EmployeeOnBoarding.Functions
func start
```

The function connects to the configured Service Bus queue and SQL database. If the deployed Function App is also running, both consumers compete for messages. Stop the deployed Function App during local testing if local processing should be isolated:

```powershell
az functionapp stop -g rg-employee-onboarding-dev -n <function-app-name>
az functionapp start -g rg-employee-onboarding-dev -n <function-app-name>
```

### Frontend

```powershell
cd frontend
npm run dev
```

Open `http://localhost:3000`.

The API allows `http://localhost:3000` by default through the `Cors:AllowedOrigins` setting in `appsettings.Development.json`.

To test the production static export locally, stop the dev server and run:

```powershell
npm run build
npm run preview
```

The build writes the deployable static site to `frontend/out`. `npm run preview` serves that folder on `http://localhost:3000`.

## 5. Validate the local flow

1. Open `http://localhost:3000`.
2. Add an employee with a profile picture.
3. Check the API Swagger response at `http://localhost:5215/swagger`.
4. Check the employee list and image rendering.
5. Open the Notifications page. The locally running function should create the notification.
6. Inspect the Functions host console for a successful structured log entry.
7. Query SQL:

   ```sql
   SELECT Name, Email, Department, ProfilePictureUrl FROM dbo.Employees;
   SELECT EmployeeName, EventType, Message FROM dbo.Notifications ORDER BY CreatedAtUtc DESC;
   ```

8. Query Blob Storage in the Azure portal or with Azure CLI.

## 6. Configuration reference

### API

| Setting | Source in Azure | Local source |
| --- | --- | --- |
| `ConnectionStrings:SqlDatabase` | Key Vault reference | User secrets |
| `ConnectionStrings:BlobStorage` | Key Vault reference | User secrets |
| `ServiceBus:ConnectionString` | Key Vault reference | User secrets |
| `ServiceBus:QueueName` | App setting | `appsettings.json` |
| `Storage:ContainerName` | App setting | `appsettings.json` |
| `Storage:MaxProfilePictureBytes` | `appsettings.json` | `appsettings.json` |
| `Cors:AllowedOrigins:0` | Static Web App origin | `appsettings.Development.json` |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Key Vault reference | Environment variable or `.env.local` for browser only |

### Functions

| Setting | Source in Azure | Local source |
| --- | --- | --- |
| `ConnectionStrings__SqlDatabase` | Key Vault reference | `local.settings.json` |
| `ServiceBusConnection` | Key Vault reference | `local.settings.json` |
| `ServiceBusQueueName` | App setting | `local.settings.json` |
| `AzureWebJobsStorage` | Storage connection string | `local.settings.json` |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Key Vault reference | Optional environment variable |

### Frontend

| Setting | Purpose |
| --- | --- |
| `NEXT_PUBLIC_API_BASE_URL` | API base URL used by browser requests. Defaults to `http://localhost:5215`. |
| `NEXT_PUBLIC_APPINSIGHTS_CONNECTION_STRING` | Optional browser telemetry. |

`NEXT_PUBLIC_*` values are embedded at build time. Restart `npm run dev` after changing them. Production builds must set the production API URL before `npm run build`.

## 7. Common issues

| Issue | Fix |
| --- | --- |
| API exits at startup with “ConnectionStrings:SqlDatabase is required” | Run `set-local-secrets.ps1` or set user secrets manually. |
| API returns a SQL firewall error | The current public IP was not allowed. Re-run `deploy-infrastructure.ps1` or add a SQL firewall rule for your IP. |
| CORS error from `localhost:3000` | Confirm you are running the API in the `Development` environment and that the origin is listed in `appsettings.Development.json`. |
| Uploaded images do not display | Confirm the storage account allows public blob access and the `profile-pictures` container has public access level **Blob**. |
| Notification does not appear | Confirm the Functions host is running and the deployed Function App is stopped if you expect local consumption. Check the host console for deserialization or SQL errors. |
| Key Vault reads fail locally | The signed-in user needs `Key Vault Secrets User`. Run `set-local-secrets.ps1 -GrantKeyVaultAccess` and wait for propagation. |
| Blob upload fails locally | The API uses the storage account key from Key Vault. Verify the `StorageConnectionString` secret and that the storage account firewall allows your network. |

## 8. Demo data reset

Clear notifications and let the Function rebuild them by editing an employee:

```sql
DELETE FROM dbo.Notifications;
```

Reset the directory and re-publish the dacpac to restore the four seed employees:

```sql
DELETE FROM dbo.Notifications;
DELETE FROM dbo.Employees;
```

```powershell
.\scripts\deploy-database.ps1 `
  -ResourceGroup rg-employee-onboarding-dev `
  -SqlServerName <sql-server-name> `
  -SqlAdminPassword $password `
  -DacpacPath backend/database/EmployeeOnBoarding.Database/bin/Release/EmployeeOnBoarding.Database.dacpac
```

Delete uploaded blobs from the portal or with:

```powershell
az storage blob delete-batch `
  --account-name <storage-account-name> `
  --source profile-pictures `
  --auth-mode login
```
