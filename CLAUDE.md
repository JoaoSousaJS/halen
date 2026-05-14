# Halen Telehealth Platform

Learning project for .NET 8 backend development. Explain patterns and design decisions as code is written — this is not just code generation, it's teaching.

## Stack

- **Backend**: .NET 8, ASP.NET Core, EF Core + PostgreSQL 16, MediatR (CQRS), FluentValidation
- **Frontend**: React 18 + TypeScript + Vite
- **Infra**: Docker Compose (Postgres, Kafka in KRaft mode, API) in `infra/`
- **Auth**: JWT + ASP.NET Core Identity, role-based (Patient, Doctor, Admin)

## Architecture

Clean Architecture monorepo:

```
src/backend/
  Halen.Domain/           # Entities, enums, value objects — no external deps
  Halen.Application/      # CQRS commands/queries, interfaces, validators, pipeline
  Halen.Infrastructure/   # EF Core, JWT, Kafka, mock email/payment
  Halen.API/              # Controllers, Program.cs, middleware
  Halen.UnitTests/
  Halen.IntegrationTests/

src/frontend/
  src/features/           # auth, patient, doctor, admin dashboards
  src/shared/             # api clients, AuthProvider, error helpers
```

## Key conventions

- Primary constructor syntax for DI (C# 12)
- CQRS: commands mutate, queries read — each has one handler
- Result records carry `ErrorKind?` for controller mapping (not string matching)
- `IAppDbContext` is the Application-layer abstraction over `HalenDbContext`
- Enums stored as strings in Postgres via `HasConversion<string>()`
- `MapInboundClaims = false` on JWT — keeps short claim names
- Global exception handler in Program.cs maps `UnauthorizedAccessException` -> 401, `ValidationException` -> 400

## Commands

```bash
# Build backend
cd src/backend && dotnet build

# Run with Docker
cd infra && docker compose up --build

# TypeScript check
cd src/frontend && npx tsc --noEmit

# Run backend tests
cd src/backend && dotnet test
```

## Skills

Project-local skills in `.agents/skills/` — read them before writing code:
- `dotnet-best-practices` — .NET 8 patterns and conventions
- `vercel-react-best-practices` — React/TypeScript guidance
- `code-reviewer` — 3-round structured review protocol (writes to `reviews/`)

## Feature implementation checklist

Every new feature must follow this sequence:

1. **Read skills** — load `.agents/skills/dotnet-best-practices` and `.agents/skills/vercel-react-best-practices` before writing code
2. **Backend** — domain entities, CQRS commands/queries, validators, controller
3. **Frontend** — API client types, React components, state management
4. **Unit tests** — handler tests (xUnit + Moq) in `Halen.UnitTests/`, validator tests
5. **Integration tests** — controller tests with `WebApplicationFactory` in `Halen.IntegrationTests/`, hitting real Postgres
6. **Storybook stories** — component stories in `*.stories.tsx` co-located with components
7. **Playwright e2e tests** — user flow tests in `src/frontend/e2e/`
8. **Code review** — run the 3-round review protocol

Do not skip steps or mark a feature as complete without tests, stories, and e2e coverage.

## Review process

Use the code-reviewer skill for a 3-round structured review:
1. Round 1: Reviewer writes findings to `reviews/<date>-<slug>.md`
2. Main assistant responds with ACCEPT/PUSHBACK per finding
3. Round 2: Reviewer re-examines pushbacks (REVISED/STANDING)
4. Main assistant responds again
5. Round 3: Reviewer verifies fixes, writes final verdict

## What's built (as of 2026-05-14)

- Auth: register, login, JWT with role claims
- Admin: create doctor, seed admin on startup
- Appointments: book (with serializable transaction for double-booking prevention), cancel (role-based ownership), complete (doctor-only), list
- Frontend: login, register, patient/doctor/admin dashboards with appointments UI
- Docker Compose: Postgres + Kafka + API, auto-migrations on startup
