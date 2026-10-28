# Depot

Container depot management: yard map, gate in/out, release orders and reports.
A .NET 10 Clean Architecture API in `src/`, an Angular web app in `web/`, and PostgreSQL.

![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![Angular](https://img.shields.io/badge/Angular-22-DD0031?style=flat-square&logo=angular&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-4169E1?style=flat-square&logo=postgresql&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=flat-square&logo=docker&logoColor=white)

The system serves the staff of one depot that keeps **empty containers** for shipping lines. It records containers
coming in (gate in), places them in the yard, releases them against a release order (gate out) and reports on traffic
and stock. The business rules are written down in [`docs/requirements/BR_DEPOT.md`](docs/requirements/BR_DEPOT.md).

## Features

| Area | What it does |
|:-----|:-------------|
| Yard | Blocks, bays, rows and tiers, virtual blocks, bulk layout of a bay range, yard map, moving a container to another place |
| Containers | Catalogue with ISO 6346 number check, container types, shipping lines and customers |
| Gate in | Places a container by block, bay, row and tier; 20 ft in odd bays, 40 ft in even bays covering the two odd neighbours; stacking checks |
| Gate out | Needs a valid release order of the same shipping line with a matching open line (type and size) |
| Release orders | Lines per type and size, delivered and remaining counts, expiry, delivery list |
| Stays | History of every stay of a container, with in-yard filter and search |
| Reports | Daily gate in/out per shipping line; yard stock per line by age (0-9 days, 10+ days) |
| Sign-in | Email and password, JWT access token (15 min) with rotating refresh token (7 days) |

## Tech stack

| Layer | Technology |
|:------|:-----------|
| Architecture | Clean Architecture (Domain, Application, Infrastructure, Api) |
| Runtime | .NET 10, Minimal APIs |
| Use cases | Hand-written CQRS handlers (`ICommandHandler` / `IQueryHandler`), no MediatR |
| Errors | `Result` / `Result<T>` with an `Error`, mapped to HTTP with `ToHttpResult()` |
| Validation | `IBusinessRule` + `BusinessRuleValidator` in the Domain |
| Database | PostgreSQL 17, EF Core 10 (Npgsql), code-first migrations |
| Auth | ASP.NET Identity + JWT Bearer, refresh tokens stored as SHA-256 hashes |
| API docs | OpenAPI + Scalar (Development only) |
| Frontend | Angular 22 (standalone components, signals), Tailwind CSS 4 |
| Quality | Roslyn + SonarAnalyzer (warnings are errors), ESLint (angular-eslint + sonarjs), Prettier |
| Tests | xUnit v3, EF Core InMemory provider |
| Packaging | Docker Compose (PostgreSQL, API, nginx web) |

## Architecture

```
Domain  <-  Application  <-  Infrastructure
                  ^                 ^
                  +------- Api -----+
```

- **Domain** depends on nothing: entities, value types and business rules.
- **Application** holds the use cases and talks to data through `IAppDbContext` (no repositories).
- **Infrastructure** has EF Core, the migrations, Identity and the token service.
- **Api** has the endpoints and the wiring.

Entities are created through `Create(...)` methods that return a `Result`, with `private` setters, so an invalid
object cannot be built. Expected failures are values, not exceptions.

The reasons behind each choice, including where Depot differs from the team template, are in
[`docs/QUYET_DINH_THIET_KE.md`](docs/QUYET_DINH_THIET_KE.md).

## Adding a feature (vertical slice)

The project is built one vertical slice at a time, from the inside out. A slice is one use case taken from the
Domain to the screen. The Application layer mirrors this: one folder per action under
`src/Depot.Application/Features/<Feature>/<Action>/`.

1. **Domain** - the entity and its `IBusinessRule` classes (`src/Depot.Domain/<Group>/`, rules in `Rules/`).
2. **Application** - the command or query, its handler and response (`Features/<Feature>/<Action>/`).
3. **Infrastructure** - the `IEntityTypeConfiguration<T>` and an EF Core migration.
4. **Api** - the endpoint in `src/Depot.Api/Endpoints/`, returning `ToHttpResult()`.
5. **Tests** - Domain tests for the rules, Application tests for the handler on a real `AppDbContext` (InMemory).
6. **Web** - the screen in `web/src/app/features/<feature>/`.
7. **Check** - `sh scripts/check.sh` must pass before the slice is done.

The order the project followed, phase by phase, is in [`docs/QUY_TRINH_TRIEN_KHAI.md`](docs/QUY_TRINH_TRIEN_KHAI.md).

## Project structure

```
Depot/
├── src/
│   ├── Depot.Domain/          # Catalogs, Yard, Containers, Gate, ReleaseOrders, Common (Result, rules)
│   ├── Depot.Application/     # Features/<Feature>/<Action>/, Abstractions
│   ├── Depot.Infrastructure/  # Persistence (AppDbContext, Configurations, Migrations), Identity
│   └── Depot.Api/             # Endpoints, Extensions, Program.cs, Dockerfile
├── tests/
│   ├── Depot.Domain.Tests/
│   └── Depot.Application.Tests/
├── web/                       # Angular app (core, shared, features), Design.md, nginx.conf, Dockerfile
├── database/
│   ├── seed/                  # Sample data (SQL)
│   └── queries/               # Report SQL
├── docs/                      # Requirements, design decisions, process, Postman collection
├── scripts/check.sh           # The quality gate
├── docker-compose.yml
└── Depot.slnx
```

## Getting started

### Run everything with Docker

Needs Docker with Compose.

1. Copy `.env.example` to `.env` and set your own values (database password, `JWT_SECRET` of at least 32 characters,
   the first admin account). `.env` is never committed.
2. Start the database, the API and the web app:

```bash
docker compose up --build
```

3. Open `http://localhost:8080` (change the port with `WEB_PORT`) and sign in with `SEED_ADMIN_EMAIL` / `SEED_ADMIN_PASSWORD`.

The API creates the tables on its first start and seeds the admin account. The web container serves the app and
forwards `/api` to the API, so only the web port (and the database port) are open on your machine.

Sample data is not loaded automatically, because the script clears the business data first. To load it, see
[`database/seed/README.md`](database/seed/README.md).

### Run for development

Prerequisites: .NET 10 SDK, Node.js with npm, Docker (for PostgreSQL).

```bash
docker compose up -d postgres
```

Then run the API (`src/Depot.Api`, `http://localhost:5049`) and the web app (port 4200). The API needs a connection
string (`ConnectionStrings:DefaultConnection`), a JWT secret (`Jwt:Secret`) and the first admin account
(`Seed:AdminEmail`, `Seed:AdminPassword`); keep them in user-secrets (`dotnet user-secrets`), not in Git.

```bash
dotnet run --project src/Depot.Api
cd web && npm install && npm start
```

The dev server talks to the API directly; the production build uses `/api`.

### Environment variables

| Variable | Meaning |
|:---------|:--------|
| `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`, `POSTGRES_PORT` | Database account and port |
| `JWT_SECRET` | Signing key of the access tokens, at least 32 characters |
| `SEED_ADMIN_EMAIL`, `SEED_ADMIN_PASSWORD` | The first admin account, created on API start |
| `WEB_PORT` | Port of the web app (default 8080) |

## API

In Development the API serves OpenAPI and the Scalar reference at `http://localhost:5049/scalar`. Every endpoint
needs a token except `POST /api/auth/login` and the documentation pages.

| Group | Examples |
|:------|:---------|
| Auth | `login`, `refresh`, `logout` |
| Yard | Blocks, bays, rows, `POST /api/blocks/{id}/layout`, `GET /api/blocks/{id}/map` |
| Catalogs | Line operators, container types, customers |
| Containers | `/api/containers`: list, search, create |
| Gate | `POST /api/gate-in`, `POST /api/gate-out`, `/api/container-stays`, `PUT /api/container-stays/{id}/position` moves a container |
| Release orders | `/api/release-orders`: create, list, detail, `GET /{id}/deliveries` |
| Reports | `GET /api/reports/throughput`, `GET /api/reports/yard-stock` |

A Postman collection and environment are in [`docs/postman/`](docs/postman).

## Key business rules

| Rule | Summary |
|:-----|:--------|
| Container number | 11 characters: 3-letter owner code, category U/J/Z, 6 digits, check digit; the check digit is recomputed |
| Yard position | One container per position; a tier is usable only when the tier below is filled |
| Bays | Odd bay takes 20 ft; even bay takes 40 ft and covers the two odd neighbours |
| Gate in | Rejected if the container is already in the yard; entry time not in the future |
| Gate out | Needs a release order of the container's line with an open line of the same type and size |
| Stock report | Counts containers still in the yard by calendar days: 0-9 and 10+ |

The complete list, with ids (BR-YD, BR-CT, BR-GI, BR-GO, BR-RO, BR-RP), is in
[`docs/requirements/BR_DEPOT.md`](docs/requirements/BR_DEPOT.md).

## Quality and tests

```bash
sh scripts/check.sh
```

This runs the build with Roslyn and SonarAnalyzer (warnings are errors), the tests, ESLint and Prettier. A task is
done only when it passes. Rules are never silenced to get green: fix the code instead.

Run the pieces alone:

```bash
dotnet test Depot.slnx
cd web && npm run lint && npm run format:check
```

## Documentation

| File | Content |
|:-----|:--------|
| [`docs/requirements/BR_DEPOT.md`](docs/requirements/BR_DEPOT.md) | Business requirements |
| [`docs/requirements/ERD_DEPOT.md`](docs/requirements/ERD_DEPOT.md) | Data model |
| [`docs/QUYET_DINH_THIET_KE.md`](docs/QUYET_DINH_THIET_KE.md) | Design decisions and assumptions |
| [`docs/QUY_TRINH_TRIEN_KHAI.md`](docs/QUY_TRINH_TRIEN_KHAI.md) | Delivery process, phase by phase |
| [`docs/FRONTEND_ROADMAP.md`](docs/FRONTEND_ROADMAP.md) | Frontend plan |
| [`web/Design.md`](web/Design.md) | Design system the UI follows |
| [`AGENTS.md`](AGENTS.md) | Rules for coding agents and contributors |
