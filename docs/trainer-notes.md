# Trainer Notes

This is a 60-minute, demo-led session for fresh graduate engineers. The application is intentionally small so every Azure service can be connected back to a visible behavior.

## 1. Learning outcomes

By the end of the session, learners should be able to explain:

- The difference between hosting a static frontend and a backend API.
- Why managed data services (SQL, Blob, Service Bus) are preferable to running infrastructure manually.
- How PaaS hosting, managed identity and Key Vault work together.
- Why asynchronous messaging is used and what “at-least-once” means.
- How Infrastructure as Code and CI/CD make deployments repeatable.
- How Application Insights connects a request across services.

## 2. Suggested 60-minute agenda

| Time | Topic | Azure services in focus |
| --- | --- | --- |
| 0–5 | Welcome, goal and architecture overview | All |
| 5–12 | Walk the Mermaid architecture and request flow | Static Web Apps, App Service, SQL, Blob, Service Bus |
| 12–20 | Demo the frontend and add an employee with a photo | Static Web Apps, Blob Storage |
| 20–30 | Inspect the API, SQL row and blob URL | App Service, SQL Database, Blob Storage |
| 30–42 | Stop the worker, create events, inspect the queue, restart the worker | Service Bus, App Service worker |
| 42–50 | Show Key Vault references, managed identity, App Insights | Key Vault, Application Insights |
| 50–58 | Show Bicep and the pipeline run | All |
| 58–60 | Recap, trade-offs, questions | All |

If time is tight, skip the optional poison-message demo and use the queue metrics plus notification feed as the asynchronous story.

## 3. Before the session

### 3.1 Deploy and validate

1. Deploy infrastructure with `scripts/deploy-infrastructure.ps1`.
2. Publish the dacpac with `scripts/deploy-database.ps1`.
3. Run the GitHub Actions or Azure DevOps pipeline to deploy the API, worker and frontend.
4. Open the frontend and confirm the four seed employees appear.
5. Add a test employee with an image and confirm a notification appears.
6. Delete the test employee.

Alternatively run the API and worker locally as described in [local-development.md](local-development.md), but keep the deployed worker stopped if you want local consumption to be deterministic.

### 3.2 Prepare portal tabs

- Resource group overview.
- App Service for the API: Overview, Configuration, Identity, Log stream.
- App Service for the worker: Log stream, Deployment center.
- SQL database: Query editor (or use a local SQL client).
- Storage account: Containers → `profile-pictures`.
- Service Bus namespace: Queues → `employee-events`.
- Key Vault: Secrets and Access control (IAM).
- Application Insights: Application map, Transaction search, Live metrics.
- Static Web App overview.
- GitHub Actions run or Azure DevOps pipeline run.
- The repository open at `EmployeeOnBoarding` in the editor.

### 3.3 Capture deployment values

```powershell
az deployment group show `
  --resource-group rg-employee-onboarding-dev `
  --name employee-onboarding-infra `
  --query properties.outputs
```

Keep these handy:

```powershell
$rg = 'rg-employee-onboarding-dev'
$api = '<api-app-name>'
$worker = '<worker-app-name>'
$sql = '<sql-server-name>'
$storage = '<storage-account-name>'
$sb = '<service-bus-namespace>'
$kv = '<key-vault-name>'
$ai = '<application-insights-name>'
```

### 3.4 Reset the demo

```sql
DELETE FROM dbo.Notifications;
```

Optionally delete uploaded blobs so the storage container starts clean. Do not delete the four seeded employees unless you plan to add them live.

## 4. Demo script

### 4.1 Welcome and mental model (0–5 minutes)

Open with the business problem, not the technology:

> “New joiners need a profile, a department, a photo and an onboarding notification. We will build that with managed Azure services and see where each service fits.”

Show the architecture diagram in the repository or [architecture.md](architecture.md). Trace one request:

1. Browser loads the frontend.
2. Frontend calls the API.
3. API writes SQL and Blob.
4. API publishes to Service Bus.
5. Worker creates a notification.
6. Everything sends telemetry.

Say explicitly: **Static Web Apps hosts files; App Service runs code; data services store data; Key Vault protects secrets; Application Insights observes it all.**

### 4.2 Frontend and Static Web Apps (5–12 minutes)

Open the deployed frontend URL.

Talking points:

- The Next.js app is exported to static HTML, CSS and JavaScript at build time and hosted on Azure Static Web Apps.
- Static Web Apps serves the files; it does not run the .NET API or a Next.js server.
- HTTPS and a globally distributed endpoint are provided by the platform.
- The frontend has no database connection. It only calls the API over HTTPS.
- This is a common cloud-native split: static frontend, managed API backend.
- Static Web Apps has a free tier, which keeps the lab inexpensive.

Show `frontend/components/EmployeeDirectory.tsx` and `frontend/lib/api.ts`. Point out that the API base URL comes from `NEXT_PUBLIC_API_BASE_URL` at build time.

Open the browser developer tools Network tab and refresh. Show the request to the API.

### 4.3 Add an employee with a profile picture (12–20 minutes)

Click **Add employee**.

Use a realistic example:

- Name: `Jamie Lee`
- Email: `jamie.lee@example.com`
- Department: `Engineering`
- Image: any small JPEG or PNG

Before submitting, point out the callout text in the form: the image goes to Blob Storage, only the URL goes to SQL.

Submit and watch the employee appear.

Ask the group: “Where did the image go, and where did the URL go?” Then prove it.

### 4.4 Prove the Blob and SQL split (20–25 minutes)

Open the storage account → **Containers → profile-pictures**. Browse into the `employees/yyyy/MM/` prefix and open the blob. Click the blob URL to show it is publicly readable in this sample.

Run a SQL query:

```sql
SELECT Name, Email, Department, ProfilePictureUrl, UpdatedAtUtc
FROM dbo.Employees
ORDER BY UpdatedAtUtc DESC;
```

Talking points:

- SQL has a row, not a multi-megabyte image.
- Blob Storage is cheaper for binary objects and can be fronted by a CDN.
- The URL is the reference; the object lives in object storage.
- Security trade-off: this sample uses public blob read for teaching. Production would use private containers, SAS or an image proxy.
- Deleting the employee deletes the blob through the API.

### 4.5 API, App Service and SQL (25–30 minutes)

Open Swagger at `https://<api-app>/swagger`.

Run `GET /api/employees`. Show the JSON shape and the `profilePictureUrl`.

Open the API App Service:

- **Overview**: Linux, .NET 10, running.
- **Configuration**: point out Key Vault references. Do not reveal or expand secret values.
- **Identity**: system-assigned managed identity is on.
- **Log stream**: show Serilog output such as request and event-publish messages.

Talking points:

- App Service manages the VM, runtime patching, TLS certificate, scaling and health checks.
- Deployment is a zip package; the pipeline does not provision a server.
- The API uses Dapper and direct SQL. The schema lives in an SSDT project, which is the “database-first” part.
- The API and worker share one App Service plan. They are separate apps because they scale and fail independently.

Optional command:

```powershell
az webapp log tail --resource-group $rg --name $api
```

### 4.6 Service Bus and the worker (30–42 minutes)

This is the most important demo for cloud-native thinking. Make the asynchronous behavior visible.

#### Stop the worker

```powershell
az webapp stop --resource-group $rg --name $worker
```

Explain that the worker is now offline, but the API should still accept writes.

#### Create or edit two employees

Use the UI. Each save publishes an event.

#### Inspect the queue

```powershell
az servicebus queue show `
  --resource-group $rg `
  --namespace-name $sb `
  --name employee-events `
  --query "{active:countDetails.activeMessageCount,deadLetter:countDetails.deadLetterMessageCount}"
```

The active count should be greater than zero. No new notifications appear yet. Open the Notifications page and let the polling show that nothing new has arrived.

#### Start the worker and watch it drain

```powershell
az webapp start --resource-group $rg --name $worker
az webapp log tail --resource-group $rg --name $worker
```

The worker logs should show the event type, event ID and employee ID. The notification feed updates within a few seconds.

Talking points:

- The API did not wait for notification creation.
- Service Bus is a buffer. It absorbs a burst or an offline consumer.
- Messages are delivered at least once. The worker uses a unique `EventId` in SQL so a duplicate delivery does not create a duplicate notification.
- The queue also has duplicate detection based on `MessageId`.
- After five failed deliveries the message is dead-lettered. That is a real operational tool.
- If this were a direct call from API to worker, an outage would fail the user request or lose the notification.

Optional poison-message demo:

1. Open the queue in the portal and use Service Bus Explorer to send a message with body `{not-json` and content type `application/json`.
2. Tail worker logs and show the deserialization failure.
3. After five deliveries, inspect the dead-letter count. Explain that the bad message is isolated instead of blocking the queue.

### 4.7 Key Vault and managed identity (42–47 minutes)

Open Key Vault → **Objects → Secrets**. Show the names only, not the values.

Show App Service → **Configuration**:

```text
@Microsoft.KeyVault(SecretUri=https://kv-eob-dev-xxxxxx.vault.azure.net/secrets/SqlConnectionString/)
```

Show App Service → **Identity** and then Key Vault → **Access control (IAM)**. Point out the two App Service identities with `Key Vault Secrets User`.

Talking points:

- Connection strings are not in Git, appsettings or the pipeline.
- The app never receives a Key Vault token in application code; App Service resolves the reference using the managed identity.
- There is no client secret to rotate for the identity.
- The deployment-created role assignment is the bridge between identity and secret access.
- If asked about local development: user secrets store the same values on the developer machine, outside the repository.

Optional command to prove the secret exists without printing its value:

```powershell
az keyvault secret list --vault-name $kv --query "[].name"
```

### 4.8 Application Insights (47–52 minutes)

Open Application Insights → **Application map**. Show the API, SQL, Blob and Service Bus dependencies and the worker.

Open **Transaction search** and filter to the last 30 minutes. Find the employee create request. Show the end-to-end transaction, dependencies and duration.

Open **Failures** and **Live metrics**. If you triggered the poison message, show the failed dependency or exception.

Kusto examples:

```kusto
requests
| where timestamp > ago(30m)
| project timestamp, name, success, duration, operation_Id
| order by timestamp desc
```

```kusto
dependencies
| where timestamp > ago(30m)
| project timestamp, name, type, target, success, duration
| order by timestamp desc
```

```kusto
traces
| where timestamp > ago(30m)
| where message contains "employee"
| project timestamp, message, severityLevel
| order by timestamp desc
```

Mention:

- Ingestion can lag by a minute or two.
- The browser also sends page-view telemetry if `NEXT_PUBLIC_APPINSIGHTS_CONNECTION_STRING` was set at build time.
- The Application Insights component is backed by a Log Analytics workspace, which is why the resource group has both.

### 4.9 Infrastructure as Code and pipelines (52–58 minutes)

Open `infra/main.bicep` and the `modules/` folder. Walk the resource declarations in order:

1. Monitoring and Key Vault.
2. Storage and Service Bus.
3. SQL.
4. Plan, Static Web App, app services.
5. Secrets and role assignments.

Talking points:

- The same template produced every resource in the demo.
- Redeploying is idempotent.
- Names are deterministic, and outputs feed the scripts and pipelines.
- Secrets are parameters, never literals in the template.
- The pipeline builds, deploys infrastructure, publishes the dacpac, deploys app packages, gets the Static Web Apps token and deploys the frontend.
- The pipeline is the repeatable version of the manual deployment script.

Show a successful pipeline run. If available, show the deployment history for the resource group.

### 4.10 Recap and questions (58–60 minutes)

Close with the service-to-behavior summary:

| Service | What learners saw |
| --- | --- |
| Static Web Apps | The UI loaded from a managed static host and called the API. |
| App Service | The .NET API and worker ran as PaaS apps with health checks and logs. |
| SQL Database | Employee rows and notification rows were queryable with SQL. |
| Blob Storage | The image bytes lived in a container; SQL held only the URL. |
| Service Bus | Events queued while the worker was stopped and drained when it restarted. |
| Key Vault | App settings referenced secrets, and managed identity supplied access. |
| Application Insights | One request was traceable across API, dependencies and worker. |

## 5. Discussion questions

Use 2–3 of these depending on time:

1. Why publish an event instead of inserting the notification directly in the API?
2. What happens if the SQL write succeeds but the Service Bus publish fails? How would an outbox fix it?
3. Why is “exactly once” hard in messaging? What did the worker do to make duplicates harmless?
4. Why put images in Blob Storage instead of a `varbinary(max)` SQL column?
5. What does App Service manage that you would otherwise manage on a VM?
6. Why is a managed identity better than storing a client secret for Key Vault?
7. What would you change before exposing this application to real employees?
8. How would you scale the worker independently from the API?
9. What is the difference between Static Web Apps and App Service in this architecture?
10. Which resource would you check first if notifications stopped appearing?

## 6. Intentional trade-offs to call out

| Simplification | Production direction |
| --- | --- |
| No user authentication or authorization | Add Microsoft Entra ID and role-based access. |
| SQL administrator login in the connection string | Use managed identity authentication to SQL. |
| Public-read blob container | Private container with SAS tokens, CDN or an image proxy. |
| API writes SQL then publishes to Service Bus | Transactional outbox plus a relay, or a messaging-aware persistence pattern. |
| No custom domain or deployment slots | Add custom domains, staging slots and swap-based releases. |
| No private networking | Private endpoints for SQL, Storage, Service Bus and Key Vault. |
| One shared Basic App Service plan | Separate plans, autoscale rules and zone redundancy. |
| Basic SQL tier | Business-critical tier or serverless depending on workload. |
| Polling notification feed | SignalR, Web PubSub or server-sent events. |

## 7. Live troubleshooting

| Symptom | Likely cause | Quick fix |
| --- | --- | --- |
| Frontend can’t reach API | Wrong `NEXT_PUBLIC_API_BASE_URL` in build or CORS origin mismatch | Check the browser console, rebuild the frontend with the deployed API URL, update the API CORS setting. |
| API returns 500 after first deployment | Key Vault role assignments still propagating | Wait, restart the App Service, inspect the Key Vault reference status. |
| Notifications do not appear | Worker stopped, queue has messages, or worker error | Check worker log stream, queue active/dead-letter counts, and SQL notifications. |
| Image upload works but image is broken | Public blob access disabled or container missing | Check container access level and the URL in SQL. |
| Queue never drains | Worker service bus connection string or queue name is wrong | Compare worker app settings with the Service Bus namespace and queue. |
| API health check fails | Key Vault or SQL configuration missing | Open Log stream and check startup exception. |
| Pipeline cannot publish dacpac | SQL firewall for runner missing or password issue | Check the pipeline step and SQL firewall rules. |

## 8. Cleanup

After the session:

```powershell
az group delete --name rg-employee-onboarding-dev --yes --no-wait
```

Explain that deleting the resource group is the cleanest way to remove the lab and stop charges. Key Vault soft delete retains the vault name for seven days; use a new environment name if you need to redeploy immediately.
