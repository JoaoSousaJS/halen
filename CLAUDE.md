# Halen Telehealth Platform

Learning project for .NET 8 backend development. Explain patterns and design decisions as code is written — this is not just code generation, it's teaching.

## Stack

- **Backend**: .NET 8, ASP.NET Core, EF Core + PostgreSQL 16, MediatR (CQRS), FluentValidation
- **Frontend**: React 19 + TypeScript + Vite
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
- `supabase-postgres-best-practices` — Postgres query optimization, indexing, connection pooling, RLS
- `kafka-realtime-dotnet` — Kafka consumer/producer patterns for .NET
- `playwright-best-practices` — Playwright e2e test patterns, selectors, assertions
- `test-driven-development` — TDD workflow: red-green-refactor, test design principles
- `systematic-debugging` — structured debugging methodology for diagnosing issues
- `azure-kubernetes` — AKS cluster planning, networking, security, operations
- `using-superpowers` — advanced agent capabilities for planning and development
- `frontend-design` — UI/UX design guidance for distinctive, production-grade interfaces
- `code-reviewer` — 3-round structured review protocol (writes to `reviews/`)
- `code-planner` — 3-round structured planning protocol (writes to `plans/`). Also loads `brainstorming` and `using-superpowers`.

## Feature implementation checklist

Every new feature must follow this sequence:

1. **Read skills** — load `dotnet-best-practices`, `vercel-react-best-practices`, `supabase-postgres-best-practices`, and `using-superpowers` before writing code. For Kafka features also load `kafka-realtime-dotnet`. For AKS/infra work load `azure-kubernetes`.
2. **Plan** — run the 3-round planning protocol with the `code-planner` skill (which also loads `brainstorming` + `using-superpowers`). Writes to `plans/`.
3. **Backend** — domain entities, CQRS commands/queries, validators, controller
4. **Frontend** — API client types, React components, state management
5. **Design** — use `frontend-design` skill to ensure UI components have polished, intentional aesthetics (not generic)
6. **Unit tests** — load `test-driven-development`, then write handler tests (xUnit + Moq) in `Halen.UnitTests/`, validator tests
7. **Integration tests** — controller tests with `WebApplicationFactory` in `Halen.IntegrationTests/`, hitting real Postgres
8. **Storybook stories** — component stories in `*.stories.tsx` co-located with components
9. **Playwright e2e tests** — load `playwright-best-practices`, then write user flow tests in `src/frontend/tests/`
10. **Code review** — run the 3-round review protocol

Do not skip steps or mark a feature as complete without tests, stories, and e2e coverage. Code review (step 10) must only run after all previous steps (1–9) are complete.

When debugging failing tests or production issues, load `systematic-debugging` before investigating.

## Review process

Use the code-reviewer skill for a 3-round structured review:
1. Round 1: Reviewer writes findings to `reviews/<date>-<slug>.md`
2. Main assistant responds with ACCEPT/PUSHBACK per finding
3. Round 2: Reviewer re-examines pushbacks (REVISED/STANDING)
4. Main assistant responds again
5. Round 3: Reviewer verifies fixes, writes final verdict

## Planning process

Use the code-planner skill for a 3-round structured plan:
1. Round 1: Planner writes plan to `plans/<date>-<slug>.md`
2. Main assistant responds with ACCEPT/PUSHBACK per section
3. Round 2: Planner re-examines pushbacks (REVISED/STANDING)
4. Main assistant responds again
5. Round 3: Planner writes consolidated final plan

Implementation begins only after the plan reaches "Status: Plan complete".

## Parallel execution

When performing independent work (backend tests, frontend tests, Playwright e2e, audits, refactors across different layers), always spawn parallel agents instead of running tasks sequentially. Examples:

- Running backend unit tests + integration tests + frontend vitest + Playwright → 4 parallel agents
- Implementing backend handler + frontend component when they don't depend on each other → parallel
- Reviewing/auditing multiple independent files or layers → parallel

Only serialize when one task's output is required as input for the next (e.g., backend API must exist before writing the frontend client that calls it).

## What's built (as of 2026-05-15)

- Auth: register, login, JWT with role claims
- Admin: create doctor, seed admin on startup
- Appointments: book (with serializable transaction for double-booking prevention), cancel (role-based ownership), complete (doctor-only), list
- Prescriptions: issue, cancel, list (doctor and patient views)
- Kafka events: appointment + prescription events → SignalR notifications (with poison message handling)
- Frontend: login, register, patient/doctor/admin dashboards with appointments + prescriptions UI
- Docker Compose: Postgres + Kafka + API, auto-migrations on startup, centralized secrets via .env
