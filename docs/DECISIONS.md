# Decision log

Scope override (2026-10-08): the original Phase 1 rule of no new features and no behavior change is void. The user approved a full refactor into a robust app: real JWT auth plus a multiplayer redesign. This file records the binding decisions.

## D1: Auth model

Name plus password, access JWT only. Passwords hashed server side. Short lived access token, no refresh tokens. Old login by name is deleted. Breaking change.

## D2: Multiplayer shape

Shared live session plus leaderboard. Several players join one session id, answer the same numbers on a shared server clock, see live scores. Breaking change to hub protocol and DB.

## D3: Server owns the rules

Score, ranges, current number, clock, and expiry live server side. Client input is validated and never trusted. Late submits after expiry are rejected. Scoring stays plus 10 per correct answer, no penalty, same rule as before.

## D4: Reconnect resumes state

Reconnect mid session restores score and used numbers from server side per player state. No fresh deal, no session end on disconnect.

## D5: Guardrails are strict and blocking

Format check, C# analyzers, ESLint, Prettier, typecheck, and tests run in precommit hooks and CI. Violations fail the commit or build.

## D6: Test scope

Backend xUnit unit plus hub plus integration tests against real Postgres and Redis. Frontend Vitest plus Testing Library component tests. No E2E unless later requested.

## D7: Breaking changes allowed

API routes, hub messages, DTOs, and tables may change. Dev DB reset expected. No compat shims.

## D8: Test project

Refactor `backend/Backend.Tests` in place: convert NUnit to xUnit, fix the missing project and package references, add it to the solution, align target frameworks.

## D9: Deploy target

Local Docker Compose only. Env config, healthchecks, no real secrets. Document the production path without building it.

## D10: Working rules carried forward

No em dashes in any text. Tests first where possible. A test must be shown red before the fix and green after. Show actual test and CI output. Stop and report if a fix grows past its plan.

## D11: Join flow

Open session lobby. Creator starts a session, it appears as open in the games list, others click join. No session codes.

## D12: Multiplayer scoring

Everyone correct scores. Every correct answer before the number advances gets plus 10. No first only ordering.

## D13: Token storage

HttpOnly cookie. Backend sets the JWT in an HttpOnly cookie, JS never reads it. Needs cookie auth plus CSRF handling on the hub and API.

## D14: Target framework

net8.0 LTS for both projects. Downgrade the test project from net9.0. Matches backend csproj and Docker today.

## D15: Leaderboard

Session scores only. Live ranking inside the session plus final session scores. No cross session totals or global board.

## D16: Integration tests use Testcontainers

Postgres and Redis come from Testcontainers, self provisioning locally and in CI. No compose dependency for `dotnet test`. Requires Docker.

## D17: Precommit hooks are committed scripts

Hooks live in `hooks/` and run via `git config core.hooksPath hooks`. Zero extra dependencies. The hook runs format verify, lint, and typecheck. CI runs those plus build and tests. Document setup in the README.

## D18: Number advance cadence

Fixed timer cadence. The server advances the shared number on a fixed timer and the session ends when duration expires. Slow players never hold up the game.

## D19: Session end rule

Duration expires. The server clock ends the session, not a round count.

## D20: Late join

Join anytime. Score counts from join. No lobby locks.

## D21: Token lifetime

8 hours. Re login about daily.

## D22: Auth transport and CSRF

JWT in HttpOnly cookie fizzbuzz_auth, SameSite Lax, Secure SameAsRequest. JwtBearer reads the cookie for API and hub. CSRF via ASP.NET antiforgery, token endpoint plus X-XSRF-TOKEN header on mutating game endpoints. Register, login, logout exempt.

## D23: Hashing and JWT packages

PasswordHasher from the shared framework, zero new packages for hashing. Microsoft.AspNetCore.Authentication.JwtBearer pinned to 8.*. Microsoft.AspNetCore.Mvc.Testing pinned to 8.* for integration tests.

## D24: Postgres authoritative, Redis minimal

Postgres holds sessions, participants, scores, answered rounds. Unique constraint on (SessionId, PlayerId, Round) stops double scoring at the DB. Redis keeps only used numbers per session. Full schema in docs/CONTRACTS.md.

## D25: Single ticker service

One BackgroundService advances numbers on fixed cadence and finishes expired sessions. Single backend replica assumed. Documented as a limitation.

## D26: Resume keyed by player

Reconnect resumes score and answered rounds from DB keyed by player and session, never by connection id.

## D27: Health, rate limits, errors

Health endpoints /health/live and /health/ready with custom DB and Redis checks, zero new packages. Strict rate limit on register and login, basic limit on hub endpoint. Global exception middleware returns ProblemDetails, no stack traces.

## D28: Fail closed config

Boot refuses on missing connection strings, JWT signing key under 32 chars, or empty CORS origins. Signing key via environment only. No default secrets in repo.

## D29: Bounded number picker

Picker tries up to 100 times to avoid used numbers, then allows a repeat. Random.Shared. Divisor minimum 1 at creation plus defensive skip in validation.

## Hardening deltas (2026-10-08, BackendHardening lane)

No contract changes. Keys in docs/CONTRACTS.md Config section are unchanged, so the schema sections are untouched.

Conventional choices the brief left open, all inside D22, D27, D28: rate limits are fixed window per IP, auth-strict 5 per minute on register and login (login reaches the limit after 4 failures because the 5th permit is consumed by a probe ordering detail, see report), hub-basic 60 per minute on /sessionHub, 429 body is ProblemDetails. Antiforgery uses AddControllersWithViews so the AutoValidateAntiforgeryToken filter resolves, X-XSRF-TOKEN header, Lax cookie Secure SameAsRequest, register login logout exempt by convention matching route template or action name. Cors policy name is GameCors. Health ready response is {status, checks:[{name, status, description}]}. StatusCodePages maps empty 4xx to ProblemDetails. Middleware order is exception handler, status code pages, https, cors, auth, antiforgery, rate limiter, controllers, hub, health. Compose uses required var interpolation for POSTGRES_PASSWORD and Jwt__SigningKey and keeps change-me placeholders in .env.example.

## D30: Lobby UX

Open section on games list. The games page gains an Open sessions list above all games. Smallest change, fits current GameList.

## D31: Play wait UX

Waiting plus live leaderboard. After answering, the player sees a submitted state, current standings, and a countdown to the next number.

## D32: Auth expiry UX

Redirect to login, preserve route. On 401 the frontend pushes to login with a return URL so the user logs in and resumes.

## D33: Test scope confirmed

Add Testcontainers integration plus Vitest. Real Postgres and Redis hub and API tests, plus frontend component tests. Full D6 scope.

## D34: Migration strategy

Additive migration, keep history. Keep InitialCreate, add AddMultiplayerGameplay. Matches current lane work.

## D35: InMemory does not enforce unique indexes

The (SessionId, PlayerId, Round) duplicate protection and the RecordAnswerAsync race path are not proven by unit tests because EF InMemory ignores unique indexes. The Testcontainers Postgres integration test must include a concurrent duplicate submit case before RecordAnswerAsync is called race safe.

## D36: NumberAdvanced carries advancesAtUtc

Payload gains advancesAtUtc, server now plus the tick interval, so the client countdown required by D31 uses a server timestamp instead of a hardcoded interval. Root implements during backend integration. Frontend falls back to an indeterminate waiting state if the field is missing.
