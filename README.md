# ParkEase — Smart Parking Management System

Full-stack parking platform: an ASP.NET Core Web API with JWT role-based auth and a React + TypeScript
frontend. A driver registers a vehicle, reserves a matching slot, checks in, parks and exits; billing, slot
state and SMS/email notifications follow the booking automatically.

## How it works

1. **Reserve** — the driver picks one of their vehicles and a free slot of the same type (Car or Bike). The
   slot is held for 15 minutes. No billing yet.
2. **Check in** — when the vehicle arrives (driver, or operator at the gate). The slot becomes Occupied and
   billing starts.
3. **Exit** — the fee is calculated (hours rounded up, 1.5x after 24 h), the slot is freed and a receipt is sent.

A hold that nobody checks in for expires on its own; a reservation can be cancelled for free until check-in.
Every step sends an **SMS and an email** and is logged in the Notifications table.

Full detail, with the state diagram, sequence diagram and notification matrix, is in
[docs/FLOW.md](docs/FLOW.md). The data model is in [docs/ER-diagram.md](docs/ER-diagram.md).

## Tech stack

**Backend:** ASP.NET Core 8 · EF Core 8 (code-first, SQL Server) · JWT · BCrypt · Swagger/OpenAPI
**Frontend:** React 19 + TypeScript (Vite) · Tailwind CSS · Axios · React Router
**Notifications:** SMS via Twilio (or a logging stub), email via Brevo
**Tests:** xUnit + EF Core InMemory

## Architecture

Controllers → Services → EF Core `DbContext`, with DTOs at every API boundary (entities are never exposed).

* Services return a `ServiceResult<T>`; the controller layer maps its error code to 400 / 403 / 404 / 409.
* Business rules that must never be violated (one live booking per slot and per vehicle) are enforced by
  filtered unique indexes in SQL Server, not only by code.
* `BookingMaintenanceWorker` expires stale reservations and sends overstay warnings.
* Time comes from `TimeProvider`, so lifecycle logic is unit-tested with a fake clock.

## Roles

| Role | Can do |
|---|---|
| **Driver** | Manage own vehicles, reserve slots, check in / exit / cancel own bookings, see own notifications |
| **Operator** | Manage own lots and slots (add, maintenance, delete), see who is mapped to which slot, check vehicles in/out |
| **Admin** | Everything an operator can do for every lot, plus the system report |

## Getting started

Prerequisites: .NET 8 SDK, Node 20+, SQL Server (Express or LocalDB).

### Backend

```bash
cd backend/ParkEase.Api
dotnet restore
dotnet run
```

In the Development environment the app applies migrations and seeds demo data on startup, so there is no
separate `dotnet ef database update` step. Swagger UI is at `/swagger`.

Put your connection string, JWT key and any provider keys in `appsettings.Development.json`. That file is
gitignored, so never commit secrets. `appsettings.json` shows the keys the app expects:

| Key | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server |
| `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes` | token signing |
| `Cors:AllowedOrigins` | frontend origins allowed to call the API (default `http://localhost:5173`) |
| `Booking:ReservationHoldMinutes` | how long a reservation is held (default 15) |
| `Booking:MaintenanceIntervalSeconds` | worker tick (default 60) |
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

### Tests

```bash
cd backend
dotnet test ParkEase.Api/ParkEase.Api.slnx
```

The suite covers the booking lifecycle and its rules, the vehicle-to-slot mapping, fee calculation,
expiry and overstay warnings, role scoping, the notification pipeline (including provider failures and
the Twilio request shape), registration/login and the seeder.

## API documentation

Swagger UI lists every endpoint with request/response examples and auth requirements. A Postman collection
is in `docs/`.

Main endpoints:

| Method and path | Who | Purpose |
|---|---|---|
| `POST /api/auth/register`, `/api/auth/login` | public | accounts |
| `GET/POST/DELETE /api/vehicles` | Driver | manage vehicles |
| `GET /api/parkinglots/browse`, `GET /api/parkinglots/{id}/slots` | any user | find slots |
| `POST /api/bookings` | Driver | reserve a slot for a vehicle |
| `POST /api/bookings/{id}/check-in`, `/exit`, `/cancel` | owner, lot operator, admin | lifecycle |
| `GET /api/bookings/mine` | Driver | own bookings |
| `GET /api/bookings/lot/{lotId}` | Operator, Admin | vehicle-to-slot mapping for a lot |
| `GET /api/notifications/mine` | Driver | SMS/email log |
| `POST/PATCH/DELETE /api/parkinglots/...` | Operator, Admin | lots, slots, maintenance |
| `GET /api/reports/overview` | Admin | occupancy and revenue |

## Git workflow

`main` ← `develop` ← (`backend` | `frontend`) ← `feature/*`

## Design decisions

* **Reserve, then check in.** Billing starts at check-in, so a driver who is late or never shows is not
  charged, and the slot is not blocked forever (the hold expires).
* **Vehicles are records, not text.** A booking references a registered vehicle, which is what lets the
  system check that a bike isn't put in a car slot and that one vehicle isn't in two places at once.
* **The database is the final referee.** Filtered unique indexes make double-booking impossible even if two
  requests race.
* **Notifications never fail a booking.** Each send is recorded as Sent, Simulated or Failed with the reason.
* **Restrict, not Cascade.** Foreign keys use `Restrict` so booking history can't be wiped by deleting a lot,
  slot or vehicle; vehicles with history are archived instead.
* **Operators see only their own lots.** Anything else returns 404, so ids can't be probed.

## Known limitations

* Real Twilio delivery needs credentials and, in India, DLT registration; it has been exercised only against
  a stubbed HTTP endpoint.
* Registration lets a user choose any role, including Admin, which is convenient for a demo. A production
  deployment should create Admins by seeding or invitation only.
* There is no payment integration; the fee is calculated and recorded but not collected.
