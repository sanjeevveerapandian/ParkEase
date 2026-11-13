# ParkEase

A parking slot booking app. Drivers book a slot, operators run the parking lot, and admins see everything.

This started as a task during my internship at Kanini.

Backend is ASP.NET Core 8 with SQL Server. Frontend is React with TypeScript.

<!-- screenshots go here -->

## How it works

![Architecture](docs/images/architecture.png)

The React app talks to the API. Every request carries a JWT token, and the API checks the user's role before doing anything.

Inside the API it's simple layers: controllers take the request, services do the actual work, and EF Core talks to the database.

After every booking step, the app sends an SMS and an email. If the SMS fails, the booking still goes through. The failure is just saved so the driver can see it.

## A booking, step by step

![Booking lifecycle](docs/images/booking-lifecycle.png)

1. **Reserve.** The driver picks their vehicle and a free slot. The slot is held for 15 minutes.
2. **Check in.** When they arrive, they check in. Billing starts now, not at reservation.
3. **Exit.** The fee is calculated, the slot is freed, and a receipt is sent.

If the driver never shows up, the hold expires on its own and nothing is charged. A background job checks for this every minute.

Fee: hours are rounded up, and after 24 hours it's charged at 1.5x.

## Things I learned building it

- **Let the database stop double booking.** Checking in code isn't enough, because two people can book the same slot at the same moment. I added a unique index in SQL Server, so only one booking can win.
- **Split "reserve" and "check in".** If billing started at booking, a late driver would pay for time they weren't parked. So billing starts at check-in.
- **Never send database entities out of the API.** Every endpoint uses DTOs, so internal fields don't leak.
- **Inject the clock.** I used `TimeProvider` so I could test "15 minutes later" without actually waiting 15 minutes.

## Roles

- **Driver:** add vehicles, book, check in, exit, cancel
- **Operator:** manage their own lots and slots, check vehicles in and out
- **Admin:** everything, plus the occupancy and revenue report

## Run it

You need .NET 8, Node 20 and SQL Server.

```bash
# backend
cd backend/ParkEase.Api
cp appsettings.Development.example.json appsettings.Development.json
# put your SQL Server connection string and a JWT key in that file
dotnet run

# frontend
cd frontend
npm install
npm run dev
```

The backend creates the database and some demo data on first run. Swagger is at `/swagger`.

Demo logins:

| Role | Email | Password |
|---|---|---|
| Admin | admin@parkease.com | Admin@123 |
| Operator | operator@parkease.com | Operator@123 |
| Driver | driver@parkease.com | Driver@123 |

SMS is simulated by default (nothing is actually sent). All settings and how to turn on real SMS are in [docs/SETUP.md](docs/SETUP.md). Full booking rules are in [docs/FLOW.md](docs/FLOW.md).

## What's not done yet

- No payment integration. The fee is calculated but not collected.
- Real SMS is only tested against a fake endpoint.
- Admins can only be created by the seed data.
