# Development

## Prerequisites

- .NET 8 SDK (`dotnet --version`)
- Node.js 22 (`node --version`)
- Docker (`docker --version`)

## First run

```bash
cp .env.example .env
```

Set `POSTGRES_PASSWORD` and `Jwt__SigningKey` (32+ chars) in `.env`. Then:

```bash
docker compose up --build
```

Open http://localhost:3000.

## Local dev without Docker

Run Postgres 17 and Redis 7 yourself, then set env vars (or export from `.env`):

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=fizzbuzz;Username=postgres;Password=change-me"
export ConnectionStrings__Redis="localhost:6379,abortConnect=false"
export Jwt__SigningKey="a-local-dev-key-that-is-at-least-32-chars"
export Cors__AllowedOrigins__0="http://localhost:3000"
dotnet run --project backend
```

```bash
cd frontend
npm ci
npm run dev
```

## Common commands

```bash
dotnet build backend/backend.sln --configuration Release
dotnet test backend/backend.sln --configuration Release
dotnet format backend/backend.sln --verify-no-changes
```

```bash
cd frontend
npm run lint
npm run format
npm run typecheck
npm run test:run
npm run build
```

## Migrations

```bash
cd backend
dotnet ef migrations add NameHere
dotnet ef database update
```

Migrations apply automatically at startup via `Database.Migrate()`. Dev schema resets are expected, see D7 and D34.

## Troubleshooting

- Backend refuses to boot: read the startup error, it names the missing key. Usually `Jwt__SigningKey` or a connection string.
- Password auth failed on compose up: a stale `db-data` volume holds the old password. Run `docker compose down -v` and up again.
- Port in use: frontend 3000, backend 8080, Postgres 5433, Redis 6379. Stop the conflicting process or change the compose ports.
