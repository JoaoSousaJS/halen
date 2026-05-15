# Halen — Telehealth Platform

A full-stack telehealth platform built with .NET 8 and React 19. It supports patient registration, doctor onboarding with KYC verification, appointment booking, prescription management, and real-time notifications via SignalR.

## Stack

| Layer | Technology |
|-------|------------|
| Backend | .NET 8, ASP.NET Core, EF Core, PostgreSQL 16, MediatR (CQRS), FluentValidation |
| Frontend | React 19, TypeScript, Vite, TanStack Query |
| Messaging | Apache Kafka (KRaft mode) → SignalR push notifications |
| Auth | JWT with ASP.NET Core Identity, role-based (Patient, Doctor, Admin) |
| Infra | Docker Compose (Postgres, Kafka, API) |
| Testing | xUnit, Testcontainers, Vitest, Playwright, Storybook |

## Architecture

Clean Architecture monorepo with CQRS (commands mutate, queries read):

```
src/
  backend/
    Halen.Domain/             # Entities, enums, value objects
    Halen.Application/        # Commands, queries, validators, interfaces
    Halen.Infrastructure/     # EF Core, JWT, Kafka, file storage
    Halen.API/                # Controllers, middleware, Program.cs
    Halen.UnitTests/
    Halen.IntegrationTests/

  frontend/
    src/features/             # auth, patient, doctor, admin dashboards
    src/shared/               # API clients, AuthProvider, hooks
    tests/                    # Playwright e2e tests

infra/
  docker-compose.yml          # Postgres + Kafka + API
  .env                        # Environment variables
```

## Features

- **Auth** — Register, login, JWT with role-based claims
- **Patient dashboard** — Browse doctors, book appointments, view prescriptions
- **Doctor onboarding** — KYC document upload, admin review (approve/reject), resubmission
- **Doctor dashboard** — View schedule, complete appointments, issue prescriptions
- **Admin dashboard** — User management, doctor account creation, KYC review
- **Real-time notifications** — Kafka events → SignalR push to connected clients
- **Appointments** — Book, cancel, complete with serializable transactions for double-booking prevention
- **Prescriptions** — Issue, cancel, list (doctor and patient views)

## Prerequisites

- [Docker](https://docs.docker.com/get-docker/) and Docker Compose
- [Node.js 20+](https://nodejs.org/) (for frontend development)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for backend development outside Docker)

## Quick Start with Docker

1. **Clone the repository:**
   ```bash
   git clone <repo-url>
   cd telehealth-platform
   ```

2. **Start all services:**
   ```bash
   cd infra
   docker compose up --build
   ```

   This starts:
   - **PostgreSQL 16** on port `5432`
   - **Kafka** (KRaft mode, no Zookeeper) on port `9094`
   - **Halen API** on port `8080`

   The API runs EF Core migrations automatically on startup and seeds an admin account.

3. **Start the frontend dev server** (in a separate terminal):
   ```bash
   cd src/frontend
   npm install
   npm run dev
   ```

   The frontend runs on `http://localhost:5173` and proxies API requests to the backend at `localhost:8080`.

4. **Open the app:**
   - App: http://localhost:5173
   - API (direct): http://localhost:8080

## Default Credentials

The admin account is seeded on first startup:

| Field | Value |
|-------|-------|
| Email | `admin@halen.dev` |
| Password | `Admin1234!` |

## Environment Variables

All configuration lives in `infra/.env`. The defaults work out of the box for local development:

```env
POSTGRES_USER=halen
POSTGRES_PASSWORD=halen_dev
POSTGRES_DB=halen
JWT_SECRET=dev_secret_change_this_in_production_min_32_chars
SEED_ADMIN_EMAIL=admin@halen.dev
SEED_ADMIN_PASSWORD=Admin1234!
KAFKA_BOOTSTRAP_SERVERS=kafka:9092
```

## Running Tests

```bash
# Backend unit + integration tests (integration tests use Testcontainers, needs Docker running)
cd src/backend
dotnet test

# Frontend unit tests
cd src/frontend
npm test

# Playwright e2e tests (starts Vite dev server automatically, needs API running via Docker)
cd src/frontend
npm run e2e

# TypeScript type check
cd src/frontend
npx tsc --noEmit

# Storybook
cd src/frontend
npm run storybook        # dev server on port 6006
npm run build-storybook  # static build
```

## Development Without Docker

If you prefer running the backend directly:

1. Start Postgres and Kafka separately (or use Docker just for those):
   ```bash
   cd infra
   docker compose up postgres kafka
   ```

2. Run the API:
   ```bash
   cd src/backend/Halen.API
   dotnet run
   ```

   Set environment variables or use `appsettings.Development.json` for connection strings.

