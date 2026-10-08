# FizzBuzz frontend

Next.js client for the FizzBuzz multiplayer game. It renders the lobby, the live session view, and the session leaderboard, and talks to the ASP.NET backend over REST and SignalR.

## Commands

Run these from `frontend/`:

```sh
npm run dev        # start the dev server on http://localhost:3000
npm run build      # production build
npm run typecheck  # tsc --noEmit
npm run lint       # ESLint
npm run format      # prettier check
npm run format:write  # apply formatting
npm run test:run    # Vitest suite, single run
```

## Environment

`NEXT_PUBLIC_BASE_URL` points the client at the backend (default `http://localhost:8080` in `docker-compose.yml`). See the root `.env.example` for local values.

## Guardrails

Commits run the checks in `hooks/README.md` via `core.hooksPath`. Binding project decisions live in `docs/DECISIONS.md`.
