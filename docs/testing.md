# Testing

## Suites

| Suite | Command | Count | Notes |
|---|---|---|---|
| Backend unit | `dotnet test backend/backend.sln --filter "Category!=Integration"` | 61 | xUnit, EF InMemory, fakes. No Docker needed. |
| Backend integration | `dotnet test backend/backend.sln --filter "Category=Integration"` | 21 | Testcontainers Postgres + Redis. Needs Docker. |
| Frontend | `cd frontend && npm run test:run` | 32 | Vitest + Testing Library, fetch and SignalR mocked. |

Run everything: `dotnet test backend/backend.sln --configuration Release` plus `npm run test:run` in frontend/.

## The red-green rule

New behavior needs a test that fails when the behavior breaks. Before finalizing:

1. Break the behavior briefly.
2. Confirm the test goes red.
3. Restore, confirm green.

Both red-green proofs from the refactor: the D35 concurrent duplicate-submit test (message break went red, restore went green) and the frontend 401-redirect test (redirect removal went red, restore went green).

## What InMemory does not prove

EF InMemory ignores unique indexes, so unit-test green does not prove the (SessionId, PlayerId, Round) duplicate protection. The Testcontainers Postgres suite carries a concurrent duplicate-submit case for exactly this. See D35.

## CI

GitHub Actions runs on every push and PR: backend restore, build, format verify, unit tests, integration tests; frontend install, lint, format check, typecheck, tests, build.
