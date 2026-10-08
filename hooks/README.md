# Git hooks

`pre-commit` is the only hook. It fail fasts through these steps:

1. `dotnet format backend/backend.sln --verify-no-changes` (backend format check)
2. `npm run lint` in `frontend/` (ESLint)
3. `npm run format` in `frontend/` (Prettier check, fix with `npm run format:write`)
4. `npm run typecheck` in `frontend/` (`tsc --noEmit`)

Any failure aborts the commit and names the failed step with its fix command.

## Setup

The repo sets this locally, so the hook runs automatically on every commit:

```sh
git config core.hooksPath hooks
```

No extra dependencies. `dotnet`, `node`, and `npm` are the only requirements.

## Manual run

```sh
sh hooks/pre-commit
```
