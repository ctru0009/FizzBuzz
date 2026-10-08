# Contributing

## Getting started

1. Read README.md for the run and test commands.
2. Read docs/CONTRACTS.md before touching any API or hub shape. It is binding.
3. Read docs/DECISIONS.md for the why behind the design.

## Branch and commit style

- Branch from master with a short name: `feat/thing`, `fix/thing`, `docs/thing`.
- One logical change per commit. Write imperative subjects under 72 chars.
- No em dashes in any text you write, including code, comments, and docs.

## Before you push

The pre-commit hook runs automatically via `git config core.hooksPath hooks`:

- `dotnet format backend/backend.sln --verify-no-changes`
- `npm run lint`, `npm run format`, `npm run typecheck` in frontend/

CI runs all of that plus the full test suites. A red build blocks merge.

## Tests

- Backend unit tests live in tests/Backend.Tests, integration tests in tests/Backend.Tests/Integration (tagged `Category=Integration`, needs Docker).
- Frontend tests are Vitest colocated files (`*.test.ts`, `*.test.tsx`).
- New behavior needs a test that fails when the behavior breaks. Show the red run before the green one.

## Pull requests

- Fill in the PR template. Link the issue if there is one.
- Keep the diff focused. Unrelated cleanup goes in its own PR.
- Update docs/CONTRACTS.md and docs/DECISIONS.md when you change wire shapes or design calls.
