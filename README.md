# \# ParkEase — Smart Parking Management System

# 

# Full-stack parking management platform: ASP.NET Core Web API backend with JWT role-based auth, EF Core (Code-First), real-time slot booking and billing, and a React (TypeScript) frontend.

# 

# \## Tech Stack

# 

# \*\*Backend:\*\* ASP.NET Core 8 Web API · Entity Framework Core (Code-First) · SQL Server · JWT Authentication · BCrypt · Swagger/OpenAPI

# \*\*Frontend:\*\* React + TypeScript (Vite) · Axios · React Router

# \*\*Notifications:\*\* Brevo (transactional email) — channel-agnostic design, SMS-ready pending India DLT registration

# 

# \## Architecture

# 

# Layered architecture: Controllers → Services → Repositories (EF Core) → DbContext, with DTOs at every API boundary (no EF entities ever exposed directly). See `/backend/ParkEase.Api` for the full structure.

# 

# \## Roles

# 

# | Role | Access |

# |---|---|

# | \*\*Admin\*\* | Full access — all lots, all bookings, system-wide reports |

# | \*\*Operator\*\* | Manages only their own lots/slots and related bookings |

# | \*\*Driver\*\* | Books slots, views their own booking history |

# 

# \## Getting Started

# 

# \### Backend

# 

# ```bash

# cd backend/ParkEase.Api

# dotnet restore

# dotnet ef database update

# dotnet run

# ```

# 

# Swagger UI: `https://localhost:<port>/swagger`

# 

# \*\*Configuration:\*\* Copy connection string, JWT key, and Brevo API key into `appsettings.Development.json` (gitignored — never commit secrets). See `appsettings.json` for the expected keys.

# 

# \*\*Seeded Admin account\*\* (created automatically on first run):

# \- Email: `admin@parkease.com`

# \- Password: \*(set on first registration — see seeding logic in `Program.cs`)\*

# 

# \### Frontend

# 

# ```bash

# cd frontend

# npm install

# npm run dev

# ```

# 

# \## API Documentation

# 

# Full endpoint documentation, request/response examples, and auth requirements are available via Swagger UI when the backend is running. A Postman collection is also included at `/docs/ParkEase.postman\_collection.json`.

# 

# \## Git Workflow

# 

# `main` ← `develop` ← (`backend` | `frontend`) ← `feature/\*`

# 

# Feature branches are cut per task, merged into their component branch (`backend`/`frontend`), which periodically merges into `develop`. `main` receives merges from `develop` only at release milestones.

# 

# \## Notes on Design Decisions

# 

# \- \*\*Email over SMS for notifications:\*\* SMS delivery to Indian numbers requires DLT (telecom regulator) sender/template registration, which has a multi-day approval window. Email (via Brevo) is fully live; an `SmsNotificationService` implementing the same `INotificationService` interface exists and is ready to activate with a one-line change in `Program.cs`.

# \- \*\*Delete behavior:\*\* All foreign key relationships use `Restrict` rather than the EF Core default `Cascade`, to prevent accidental data loss (e.g., deleting an Operator should never silently wipe their lots/bookings).

