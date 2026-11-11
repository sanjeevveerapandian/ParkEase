# Deploying ParkEase

You are putting three things online and connecting them:

```
Browser --> [1] Frontend (static site) --> [2] API (ASP.NET Core) --> [3] SQL Server database
```

The link you put on a resume is the frontend's address. Do the steps **in this order** (database, API,
frontend, then connect them) because each step needs the address from the one before.

## What is already verified, and what is not

Verified before you start (in a throwaway SQL Server container, running the exact image `Dockerfile` builds):

* the schema builds from nothing with the migrations;
* in Production mode nothing is seeded unless you ask, and the old default `Admin@123` password does not exist;
* with demo data enabled, the full end-to-end suite passes (61 checks, no server errors);
* missing or weak settings stop the app at start-up with a clear message;
* CORS allows only the origin you configure.

**Not** verified, because it needs your accounts: the Azure and Vercel portals themselves, real email from a
hosted IP, real SMS. Portal screens change, so treat the click paths below as a guide, not a script.

## Before you start (10 minutes)

1. **Make a JWT signing key** (any 32+ random characters). In a terminal:

   ```bash
   node -e "console.log(require('crypto').randomBytes(48).toString('base64'))"
   ```

   Keep the output; it becomes `Jwt__Key`. Use a **new** one for production, never the one in your
   `appsettings.Development.json`.
2. **Choose an admin password** for the hosted site (strong, not `Admin@123`). It becomes `Seed__AdminPassword`.
3. **Rotate your Brevo API key** (Brevo dashboard, SMTP & API, create a new key). The old one has appeared in
   logs and chats; don't reuse it for a public site.
4. Push to GitHub (already done). Hosts deploy from it.

## Step 1: Database (Azure SQL, free offer)

1. Create an Azure account. Students: Azure for Students needs no credit card; others are asked for a card.
2. In the portal, create **SQL database**. Choose **Apply the free offer** when it is offered.
3. Create a new server: SQL authentication, an admin login and password (save them).
4. Under *Networking* allow **Azure services and resources to access this server**. Add your own IP too if you
   want to look at the data from your PC.
5. For "behavior when free limits are exhausted" choose **Auto-pause the database until next month**, so you
   can never be billed by surprise.
6. Open the database, *Connection strings*, copy the **ADO.NET (SQL authentication)** string and put your
   password into it. Append `Connection Timeout=60;` to the end.

Good to know: the free serverless database **pauses when idle**, and the first request after a pause can take
up to a minute while it wakes. The API retries connections automatically, so the first visit is slow but works.
The free allowance is 100,000 vCore-seconds per month per database
([Microsoft docs](https://learn.microsoft.com/azure/azure-sql/database/free-offer)); a demo uses far less.

## Step 2: API

The API needs these settings. Use these exact names (double underscore = a nested setting):

| Setting | Value |
|---|---|
| `ConnectionStrings__DefaultConnection` | the string from step 1 |
| `Jwt__Key` | the key you generated |
| `Jwt__Issuer` | `ParkEaseApi` |
| `Jwt__Audience` | `ParkEaseClient` |
| `Database__AutoMigrate` | `true` (creates the tables on first start) |
| `Seed__AdminPassword` | the admin password you chose |
| `Seed__DemoData` | `true` for a demo site (adds the demo lots, driver and operator); `false` for a clean one |
| `Cors__AllowedOrigins__0` | your frontend address, filled in at step 4 |
| `Swagger__Enabled` | `true` if you want `/swagger` to be public (nice for reviewers) |
| `Brevo__ApiKey`, `Brevo__SenderEmail` | optional; leave unset and emails are simulated |
| `ASPNETCORE_ENVIRONMENT` | leave unset (Production) |

### Option A: Azure App Service, publishing from Visual Studio (easiest for you)

1. In the portal create an **App Service** (publish: Code, runtime stack **.NET 8**, region near your database).
   Pick the plan with care, see "Plans" below.
2. In the App Service, *Settings, Environment variables*, add every setting from the table above.
3. In Visual Studio: right-click `ParkEase.Api`, **Publish**, **Azure**, choose that App Service, **Publish**.
4. Visit `https://<your-app>.azurewebsites.net/health`. You should see `{"status":"ok"}`.

### Option B: any host that runs containers

The repo has `backend/ParkEase.Api/Dockerfile`. Build context is the `backend/` folder:

```bash
docker build -f ParkEase.Api/Dockerfile -t parkease-api .
```

Point the host at that Dockerfile, set the same settings, and expose port `8080`. This works on Azure Container
Apps, Render, Fly.io and similar; check each one's current free limits yourself.

### Plans: be honest about the free tier

Azure's free App Service plan (F1) has a daily CPU allowance and Microsoft describes it as for trials, not
production; I could not confirm .NET 8 on Linux there, and it has no "Always on", so the app sleeps when idle.
It is fine to try first. If it misbehaves or stops, move to the cheapest paid plan (B1), which Azure for
Students credit can cover.

## Step 3: Frontend (Vercel)

1. Create a Vercel account (sign in with GitHub) and **Add New Project**, import the `ParkEase` repo.
2. Set **Root Directory** to `frontend`. Framework preset **Vite** (auto-detected).
   Production branch: `main` (or `frontend`).
3. Add environment variables:

   | Name | Value |
   |---|---|
   | `VITE_API_URL` | your API address, e.g. `https://<your-app>.azurewebsites.net` (no trailing slash) |
   | `VITE_DEMO_LOGINS` | `true` to show the one-click Driver/Operator buttons on the login page |

4. **Deploy.** You get an address like `https://parkease.vercel.app`.

Vite bakes these values in when it builds, so if you change them later you must **redeploy**.
`frontend/vercel.json` already makes deep links like `/driver/bookings` work on refresh.

## Step 4: Connect them (CORS)

The browser only lets the frontend call the API if the API lists the frontend's address. Set on the API:

`Cors__AllowedOrigins__0` = `https://parkease.vercel.app` (exactly, with no trailing slash and the right
`https://`), then restart the API. If you add a custom domain later, add it as `Cors__AllowedOrigins__1`.

## Step 5: Check it works

* `https://<api>/health` shows `{"status":"ok"}`.
* `https://<api>/swagger` opens (if you enabled it).
* Open the frontend, click **Try as a Driver**, reserve a slot, check in, exit, then open **Alerts**.
* Click **Try as an Operator** and open a lot.
* Log in as `admin@parkease.com` with the password you chose, and open Reports.

## Email and SMS on a hosted site

* **SMS** stays simulated until you set `Sms__Provider=Twilio` plus `Sms__Twilio__AccountSid`, `AuthToken` and
  `From` (see the README). Trial Twilio accounts can only text numbers you verify.
* **Email** uses Brevo. Brevo blocks API calls from IP addresses it doesn't recognise, which is what you saw
  locally. A hosted server has a different IP, so either authorise it in Brevo (*Security, Authorised IPs*;
  Azure's outbound IPs are listed on the App Service's *Networking* page) or switch the restriction off.
  Until then emails show as **Failed** with the reason on the Alerts page, and bookings still work.

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| Login page says "Network Error" | `VITE_API_URL` wrong, or the frontend address isn't in `Cors__AllowedOrigins__0` |
| Login returns a server error | a required setting is missing; read the API's log stream, the start-up message names it |
| First visit takes a minute | the free database or app was asleep; normal |
| `/health` fails and the log mentions the database | firewall (allow Azure services), password, or `Connection Timeout` too short |
| Pages 404 on refresh | `frontend/vercel.json` missing from the deployed branch |
| Changed a `VITE_` value and nothing changed | redeploy the frontend |
| Demo buttons don't show | `VITE_DEMO_LOGINS` isn't exactly `true`, or you didn't redeploy |

## For the resume

* Put the **frontend** link, plus the GitHub link.
* Add one line under the link: "First load may take ~30s while the free tier wakes up."
* Mention what it is in one sentence, e.g. "Smart parking system: vehicle-to-slot reservations, check-in/exit
  billing, SMS and email notifications; ASP.NET Core, EF Core, SQL Server, React, TypeScript."
