# ParkEase setup

Everything you need to run ParkEase locally, configure it, and turn on real SMS.

## Prerequisites and running

Prerequisites: .NET 8 SDK, Node 20+, SQL Server (Express or LocalDB).

### Backend

First create your local settings file (it is gitignored, so your secrets never get committed):

```bash
cd backend/ParkEase.Api
cp appsettings.Development.example.json appsettings.Development.json
```

Then edit `appsettings.Development.json`: set the SQL Server name in the connection string and replace
`Jwt:Key` with 32 or more random characters, for example from
`node -e "console.log(require('crypto').randomBytes(48).toString('base64'))"`. Leave `Brevo` empty and emails
are simulated. Then:

```bash
dotnet restore
dotnet run
```

In the Development environment the app applies migrations and seeds demo data on startup, so there is no
separate `dotnet ef database update` step. Swagger UI is at `/swagger`.

`appsettings.json` shows the keys the app expects, and every one can also be set as an environment variable
(`Jwt__Key` for `Jwt:Key`, and so on):

| Key | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server |
| `Jwt:Key` (32+ characters), `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes` (default 60) | token signing; the app refuses to start if the first three are missing |
| `Cors:AllowedOrigins` | frontend origins allowed to call the API (default `http://localhost:5173`) |
| `Booking:ReservationHoldMinutes` | how long a reservation is held (default 15) |
| `Booking:MaintenanceIntervalSeconds` | worker tick (default 60) |
| `Database:AutoMigrate` | outside Development, set `true` to create/update the schema at start-up (default off) |
| `Seed:AdminPassword`, `Seed:DemoData` | outside Development, an Admin exists only if you set a password, and demo data only if `true` |
| `Swagger:Enabled` | `true` to expose `/swagger` outside Development |
| `Sms:Provider` | `Log` (default, delivers nothing) or `Twilio` |
| `Sms:Twilio:AccountSid`, `AuthToken`, `From` or `MessagingServiceSid` | Twilio credentials |
| `Brevo:ApiKey`, `Brevo:SenderEmail`, `Brevo:SenderName` | email; unset means emails are simulated |

**Demo accounts** (seeded on an empty database):

| Role | Email | Password |
|---|---|---|
| Admin | `admin@parkease.com` | `Admin@123` |
| Operator | `operator@parkease.com` | `Operator@123` |
| Driver | `driver@parkease.com` | `Driver@123` |

The operator owns two lots in Chennai (Mylapore Temple Parking, Porur Junction Parking) with five slots
across them, and the driver has a car (`TN09AB1234`) and a bike (`TN09XY4321`).

### Turning on real SMS

By default SMS is **simulated**: the message is written to the Notifications table with status `Simulated`
and nothing is sent. To send for real through Twilio:

```json
"Sms": {
  "Provider": "Twilio",
  "Twilio": {
    "AccountSid": "AC...",
    "AuthToken": "...",
    "From": "+1..."
  }
}
```

(or set `MessagingServiceSid` instead of `From`). Notes:

* Twilio trial accounts can only text numbers you have verified in the Twilio console.
* Sending to Indian numbers for production use requires sender/template registration under the DLT rules.
* If `Provider` is `Twilio` but the credentials are incomplete, the app falls back to the logging sender.
* Failures never break a booking. They are stored with their error and shown to the driver on the Alerts page.

Phone numbers are accepted as `9876543210`, `+91 98765 43210` and similar, and stored as `+919876543210`.

### Frontend

```bash
cd frontend
npm install
npm run dev
```

The API URL comes from `VITE_API_URL` in `frontend/.env` (see `.env.example`), defaulting to
`http://localhost:5241`.

### Deploying

See [docs/DEPLOY.md](docs/DEPLOY.md): step-by-step for Azure SQL (free offer), the API (Azure App Service or any
container host, a `Dockerfile` is included) and the frontend on Vercel, plus a troubleshooting table. Set
`VITE_DEMO_LOGINS=true` on a public demo to show one-click Driver/Operator logins on the login page.

### Tests

```bash
cd backend
dotnet test ParkEase.Api/ParkEase.Api.slnx
```

The suite covers the booking lifecycle and its rules, the vehicle-to-slot mapping, fee calculation,
expiry and overstay warnings, role scoping, the notification pipeline (including provider failures and
the Twilio request shape), registration/login and the seeder.

