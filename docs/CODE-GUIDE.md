# ParkEase — Code Guide (for studying and code review)

Read this top to bottom once, then follow the "reading order" with the code open. Rules and
behaviour are in [FLOW.md](FLOW.md); the data model is in [ER-diagram.md](ER-diagram.md).

## 1. The big picture

```
React (Vite)  --HTTP/JSON + JWT-->  ASP.NET Core API  --EF Core-->  SQL Server
                                        |
                                        +-- BookingMaintenanceWorker (background timer)
                                        +-- SMS (Twilio / log) and email (Brevo)
```

One sentence to say in a review: *a driver reserves a slot for one of their registered vehicles, checks in
(billing starts), and exits (fee calculated); every step texts and emails them, and the database itself
prevents double booking.*

## 2. Repository layout

```
backend/
  ParkEase.Api/
    Program.cs                    start-up: DI, auth, CORS, Swagger, middleware, worker, dev migrate+seed
    Controllers/                  thin HTTP layer
    Services/                     all business rules
      Notifications/              SMS + email pipeline
    Models/                       EF entities + enums
    DTOs/                         request/response shapes (entities never leave the API)
    Validators/                   attributes for [ValidPhone], [ValidVehicleNumber], [EnumName]
    Data/                         ApplicationDbContext, DbSeeder
    Middleware/                   GlobalExceptionMiddleware
    Authorization/                AuthPolicies (named role policies)
    Migrations/                   EF Core migrations
  ParkEase.Api.Tests/             xUnit tests (170)
frontend/src/
  api/                            axios client, endpoint functions, error helper
  context/                        AuthContext (login state), ToastContext
  components/                     reusable UI (seat map, bookings table, navbar...)
  pages/{driver,operator,admin,auth}/
  lib/booking.ts                  small pure helpers (fee estimate, formatting)
  types/index.ts                  TypeScript mirrors of the DTOs
docs/                             FLOW, ER diagram, this guide, Postman collection
```

## 3. Backend: the layers

**Controller → Service → DbContext.** Controllers do three things only: read the user from the JWT, call a
service, turn the result into an HTTP response.

* `ApiControllerBase` has `GetUserId()`, `GetActor()` (user id + role) and `ErrorResult()`, which maps a failed
  result to 400 / 403 / 404 / 409 with a `{ "message": ... }` body. Every controller inherits it.
* Services return `ServiceResult<T>` (`Services/ServiceResult.cs`): either a value or an error with a
  `ServiceErrorCode` (`Validation`, `NotFound`, `Forbidden`, `Conflict`). Expected failures are values, not
  exceptions. Unexpected exceptions are caught by `GlobalExceptionMiddleware` and returned as a clean 500.
* `Actor(UserId, Role)` is passed to services so they can scope by role. An operator asking for a booking in
  someone else's lot gets `NotFound` (not `Forbidden`) so ids can't be probed.

### Request pipeline (`Program.cs`, in order)
exception middleware → CORS → authentication (JWT bearer) → authorization (policies in `AuthPolicies`) →
controllers. In Development it also runs `Migrate()` and `DbSeeder.Seed()`. HTTPS redirect is skipped in
Development so the Vite dev server can call the API over http.

### Key files, and what each one is for

| File | Why it matters |
|---|---|
| `Services/BookingService.cs` | The heart: `ReserveAsync`, `CheckInAsync`, `ExitAsync`, `CancelAsync`, `ExpireStaleReservationsAsync`, `SendOverstayWarningsAsync`. |
| `Services/FeeCalculator.cs` | Pure function: hours round up (min 1), 24 h threshold, 1.5x overstay. Easy to unit test. |
| `Data/ApplicationDbContext.cs` | Relationships (all `Restrict`), unique indexes, UTC converter for every `DateTime`. |
| `Services/ParkingLotService.cs` | Lots and slots, ownership checks, maintenance and delete rules. |
| `Services/VehicleService.cs` | Add / list / archive vehicles; plate normalization. |
| `Services/AuthService.cs` | BCrypt hashing, JWT creation, phone stored as +E.164. |
| `Services/Notifications/NotificationService.cs` | Sends SMS + email for an event, saves two rows, never throws. |
| `Services/Notifications/NotificationTemplates.cs` | The wording of every message, in one place. |
| `Services/BookingMaintenanceWorker.cs` | `BackgroundService` ticking on a `PeriodicTimer`; runs expiry + warnings. |
| `Data/DbSeeder.cs` | Demo admin/operator/driver, two lots, vehicles. |

### How a reservation works, step by step (`ReserveAsync`)
1. Load the vehicle: must be the caller's and active, else NotFound.
2. Load the slot, else NotFound.
3. Slot must be `Available`, else Conflict with a specific message (reserved / occupied / maintenance).
4. Vehicle type must equal slot type, else Validation.
5. The vehicle must not already have a live booking, else Conflict.
6. Insert the booking as `Reserved` with `ReservationExpiresAt = now + hold`, set the slot to `Reserved`, save.
7. If SQL Server throws a unique-index violation (error 2601 / 2627) because someone else won a race,
   return Conflict instead of a 500.
8. Ask `INotificationService` to send the confirmation (failures are swallowed).

### Why the database enforces the rules too
Two filtered unique indexes on `VehicleBookings` (`WHERE Status IN (0, 4)`, i.e. Active or Reserved) guarantee
one live booking per slot and per vehicle. Code checks give friendly messages; the indexes guarantee
correctness when two requests arrive at the same moment.

### Notifications in one picture
```
BookingService --NotifyAsync(type, bookingId)--> NotificationService
   loads booking data -> NotificationTemplates.Build(type) -> { sms text, email subject/html }
   -> ISmsSender  (TwilioSmsSender | LogSmsSender)  -> result
   -> IEmailSender (BrevoEmailSender)               -> result
   -> saves 2 Notification rows (Sent / Simulated / Failed + error)
```
Which SMS sender is used is decided once at start-up from `Sms:Provider` (and falls back to the log sender if
Twilio credentials are incomplete). Senders report failure through a `SendResult`; they don't throw.

### Time handling
`TimeProvider` is injected (not `DateTime.UtcNow`), so tests use a fake clock. All dates are stored and
returned as UTC; the frontend formats them in the browser's local time and SMS/email text uses IST.

## 4. Frontend

* `api/client.ts`: one axios instance; the request interceptor adds the JWT held in a module variable (set
  through `setAuthToken`, which `AuthContext` calls after loading it from localStorage), and the response
  interceptor clears the token and redirects to `/login` on a 401. `api/endpoints.ts` groups calls (`bookingsApi`, `vehiclesApi`, ...).
  `api/errors.ts` turns any error into a readable message (reads the backend's `{message}`).
* `context/AuthContext.tsx`: holds token, role, name; persisted in localStorage; exposes `login`, `register`,
  `logout`.
* `components/RequireRole.tsx` + routes in `App.tsx`: role guards. The server still enforces everything; the
  guards are for UX.
* Driver booking page (`pages/driver/LotSlotsPage.tsx`): step 1 choose vehicle, step 2 choose slot
  (`SlotSeatMap` dims slots that don't fit the vehicle, using `lib/booking.ts#slotFitsVehicle`), step 3 reserve.
* `pages/driver/MyBookingsPage.tsx`: live bookings as cards with `BookingTimeline`, `HoldCountdown`,
  `LiveEstimate` (all in `components/BookingParts.tsx`); history as a table.
* `pages/operator/LotDetailPage.tsx`: live console — counts per status, click a slot to see which vehicle and
  driver it's mapped to, and check in / exit / cancel, set maintenance, or delete; refreshes every 15 s.
* The fee shown while parked is only an estimate (`lib/booking.ts#estimateFee`); the server's number on exit
  is the real one.

## 5. Tests

`backend/ParkEase.Api.Tests`, run with `dotnet test`. `Support/TestWorld.cs` builds a small world (one lot, two
car slots, one bike slot, two drivers, two operators, an admin, a fake clock, a recording notifier).

| File | Covers |
|---|---|
| `BookingReservationTests` | mapping rules, conflicts, hold length |
| `BookingLifecycleTests` | check-in, exit, cancel, who may do what, listings |
| `BookingMaintenanceTests` | expiry and overstay warning |
| `ParkingLotServiceTests`, `VehicleServiceTests` | lot/slot/vehicle rules |
| `NotificationTests` | dispatch, failures, templates, inbox |
| `TwilioSmsSenderTests` | request shape and error handling against a stub HTTP handler |
| `FeeCalculatorTests`, `InputNormalizationTests` | pure logic and DTO validation |
| `AuthAndSeedTests` | registration, login, seeder |

Known limit: the tests use EF's in-memory provider, which does **not** enforce unique or filtered indexes. The
index behaviour was checked by hand against real SQL Server instead (the end-to-end run).

## 6. Suggested reading order (about 2 hours)

1. `docs/FLOW.md` (10 min): the rules.
2. `Models/*` then `Data/ApplicationDbContext.cs`: the data and constraints.
3. `Program.cs`: how everything is wired.
4. `Controllers/BookingsController.cs` → `Services/BookingService.cs` → `Services/FeeCalculator.cs`.
5. `Services/Notifications/NotificationService.cs` → `NotificationTemplates.cs` → `TwilioSmsSender.cs`.
6. `Services/BookingMaintenanceWorker.cs`.
7. `ParkingLotService.cs`, `VehicleService.cs`, `AuthService.cs`.
8. Frontend: `api/client.ts` → `AuthContext.tsx` → `App.tsx` → `LotSlotsPage.tsx` → `MyBookingsPage.tsx` →
   `LotDetailPage.tsx`.
9. `BookingLifecycleTests.cs`: the behaviour, written as examples.

Tip: put a breakpoint in `BookingService.ReserveAsync` and book from the UI to watch the flow end to end.

## 7. Questions a reviewer is likely to ask

* **Why a `ServiceResult` instead of exceptions?** Business failures (slot taken, wrong vehicle type) are
  normal outcomes, not errors; returning them keeps control flow explicit and cheap, and the controller maps
  them to the right status code in one place.
* **How do you prevent double booking?** Code checks for friendly messages, plus filtered unique indexes so the
  database rejects the loser of a race; the service catches that and returns 409.
* **Why reserve and check in separately?** So billing starts when the vehicle actually arrives, a no-show costs
  nothing, and a slot isn't blocked forever (the hold expires).
* **Why a `Vehicle` entity rather than a plate string?** To enforce type matching and one-live-booking-per-vehicle,
  and to let drivers pick from saved vehicles.
* **What if SMS or email fails?** It's recorded as `Failed` with the reason and the booking is unaffected.
* **Why `Restrict` on foreign keys?** So billing history can't be wiped by deleting a lot, slot or vehicle.
* **Why `TimeProvider`?** Deterministic tests of holds, expiry and overstay.
* **Why 404 instead of 403 for other people's bookings?** Doesn't reveal that the id exists.
* **What would you do next?** Payments; admin creation by invitation instead of open registration; real
  integration tests against SQL Server (Testcontainers); WebSocket/SSE live updates instead of polling; rate
  limiting on login.

## 8. Honest weak spots (say them before they do)

* Admins can't self-register, but there is no invite flow, and the seeded admin password is public in the README.
* Real SMS delivery hasn't been exercised against Twilio, only against a stub.
* The frontend refreshes by polling (15–30 s), not push.
* Tests don't cover the frontend, and the in-memory database doesn't enforce the unique indexes.
* No payment, only fee calculation.
