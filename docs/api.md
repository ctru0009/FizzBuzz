# API reference

Base URL: `http://localhost:8080` in local Compose. JSON is camelCase both ways. Errors are ProblemDetails, no stack traces. Binding shapes live in docs/CONTRACTS.md, this guide adds examples.

## Auth

Cookie `fizzbuzz_auth`, HttpOnly, SameSite Lax. Send `X-XSRF-TOKEN` on POST, PUT, PATCH, DELETE except register, login, logout. Get the token from `GET /api/antiforgery/token`.

### POST /api/players/register

Body `{name, password}`. Name 1..50 chars, password 8..100. Returns 201 `{id, name}` plus cookie. 400 on validation, 409 when the name is taken.

```bash
curl -c cookies.txt -H "Content-Type: application/json" \
  -d '{"name":"ann","password":"correct-horse-1"}' \
  http://localhost:8080/api/players/register
```

### POST /api/players/login

Same body. Returns 200 `{id, name}` plus cookie. 401 on bad credentials. Rate limited.

### POST /api/players/logout

Clears the cookie. Returns 200.

### GET /api/players/me

Returns 200 `{id, name}` or 401.

## Games

### GET /api/games

Anonymous. Returns the game list with rules.

### POST /api/games

Auth plus antiforgery. Body `{name, startRange, endRange, rules}`. No authorName or playerId, the server takes both from the auth claim. Returns 201.

```bash
TOKEN=$(curl -s -b cookies.txt -c cookies.txt http://localhost:8080/api/antiforgery/token | node -e "let s='';process.stdin.on('data',d=>s+=d).on('end',()=>console.log(JSON.parse(s).token))")
curl -b cookies.txt -H "Content-Type: application/json" -H "X-XSRF-TOKEN: $TOKEN" \
  -d '{"name":"Classic","startRange":1,"endRange":100,"rules":[{"divisibleBy":3,"replacementWord":"Fizz"},{"divisibleBy":5,"replacementWord":"Buzz"}]}' \
  http://localhost:8080/api/games
```

## Sessions

### POST /api/sessions

Auth plus antiforgery. Body `{gameId, durationSeconds}` (30..1800). Creator auto-joins with score 0. Returns 201 with the snapshot.

### GET /api/sessions/open

Anonymous. Returns open lobby sessions: id, gameId, gameName, playerCount, endsAtUtc.

### GET /api/sessions/{id}

Auth. Returns the snapshot or 404.

## Hub /sessionHub

Cookie auth, `[Authorize]`. Group per session: `session:{id}`.

Client calls:

- `JoinSession(sessionId)` returns the snapshot plus scores plus `answeredCurrentRound`. Rejoin resumes score.
- `SubmitAnswer(sessionId, round, answer)` returns `{correct, score}`. Rejects stale rounds, duplicates, and non-members with exact HubException messages.
- `LeaveSession(sessionId)` removes the connection. Score stays.

Server broadcasts: `NumberAdvanced {number, round, endsAtUtc, advancesAtUtc}`, `ScoresUpdated {scores}`, `SessionEnded {finalScores}`.

## Health

- `GET /health/live` always 200.
- `GET /health/ready` checks Postgres and Redis.
