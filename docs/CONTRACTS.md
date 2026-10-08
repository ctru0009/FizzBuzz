# Wire contracts (binding)

Fixed by root on 2026-10-08 from D1-D21. Backend and frontend lanes implement exactly this. Breaking changes allowed per D7. JSON is camelCase both ways. No em dashes in any text.

## Auth API

Cookie: `fizzbuzz_auth`, HttpOnly, SameSite=Lax, Secure SameAsRequest. JwtBearer reads this cookie for API and hub. 8h expiry, sub claim is player id, name claim is player name.

- POST /api/players/register Body {name, password}. Name required, 1..50 chars. Password required, 8..100 chars. Returns 201 {id, name} plus cookie. 400 on validation, 409 when the name is taken. Antiforgery exempt.
- POST /api/players/login Body {name, password}. Returns 200 {id, name} plus cookie. 400 on validation, 401 on bad credentials. Antiforgery exempt. Rate limited, 5 per minute per IP.
- POST /api/players/logout. Clears the cookie, returns 200. AllowAnonymous. Antiforgery exempt.
- GET /api/players/me. Returns 200 {id, name} or 401.
- GET /api/antiforgery/token. AllowAnonymous. Returns 200 {token}. Frontend sends the value in header X-XSRF-TOKEN on every POST, PUT, PATCH, DELETE except register, login, logout.

Passwords hashed with PasswordHasher from the shared framework. JwtBearer package pinned to 8.*.

## Games API

- GET /api/games. AllowAnonymous. Returns 200 [{id, name, authorName, startRange, endRange, createdAt, rules:[{divisibleBy, replacementWord}]}].
- POST /api/games. Auth plus antiforgery. Body {name, startRange, endRange, rules}. No authorName, no playerId, the server takes both from the auth claim. Validation: name required max 100, ranges 1..10000 with start less than end, 1..10 rules, divisibleBy minimum 1, replacementWord required max 50. Returns 201 with the created game.

## Sessions API

Session status is Open or Finished. The creator joins automatically with score 0.

- POST /api/sessions. Auth plus antiforgery. Body {gameId, durationSeconds}. durationSeconds 30..1800. Game must exist. Returns 201 {id, gameId, status, startTimeUtc, endTimeUtc, currentNumber, currentRound, scores:[{playerId, playerName, score}]}. currentRound starts at 1.
- GET /api/sessions/open. AllowAnonymous. Returns 200 [{id, gameId, gameName, playerCount, endsAtUtc}].
- GET /api/sessions/{id}. Auth. Returns the same shape as POST, or 404.

## Hub /sessionHub

Authorize required, cookie auth. Group per session named `session:{id}`. Errors are HubExceptions with these exact messages: "Session not found.", "Session has finished.", "Join the session before answering.", "Answer is for an old round.", "Already answered this round.".

Client calls server:

- JoinSession(sessionId: int) returns {sessionId, number, round, endsAtUtc, scores:[{playerId, playerName, score}], answeredCurrentRound: bool}. Creates the participant row on first join (score kept on rejoin, D4 resume). Adds the connection to the group. Rejects finished sessions.
- SubmitAnswer(sessionId: int, round: int, answer: string). Answer required, max 100 chars. Server checks in order: session exists and Open, caller joined, round equals current round, no existing answer row for this player and round. Validates the answer against DB rules and the server current number, never a client number. Correct scores plus 10. Wrong scores plus 0. Then broadcasts ScoresUpdated. Returns {correct: bool, score: int}.
- LeaveSession(sessionId: int). Removes the connection from the group. Run and score stay.

Server calls client:

- NumberAdvanced {number, round, endsAtUtc, advancesAtUtc}.
- ScoresUpdated {scores:[{playerId, playerName, score}]}.
- SessionEnded {finalScores:[{playerId, playerName, score}]}.

Duplicate submit for the same round is rejected by the unique constraint plus a HubException. Stale round is rejected. Late submit after EndTime is rejected because the ticker finishes the session first.

## Game loop

A single BackgroundService ticker runs every NumberIntervalSeconds (config default 15, valid 5..120). Each tick, for every Open session: if now is past EndTime, mark Finished and broadcast SessionEnded; else pick the next unused number, bump CurrentRound, persist, broadcast NumberAdvanced. Number picker tries up to 100 times to avoid used numbers, then allows a repeat. Random.Shared. Rules logic unchanged: concatenate replacement words where number mod divisibleBy is 0, empty means the answer must be the number itself, comparison case insensitive.

## State and storage

Postgres is authoritative: Session gains Status, CurrentNumber, CurrentRound. New SessionParticipant {SessionId, PlayerId, Score, JoinedAt} with unique (SessionId, PlayerId). New SessionAnswer {SessionId, PlayerId, Round, IsCorrect, AnsweredAt} with unique (SessionId, PlayerId, Round). Player gains PasswordHash. One new migration holds all of it. Redis keeps only used numbers per session at key `session:{id}:used`, JSON int list, TTL is session remaining plus 5 minutes grace.

## Config (all validated at startup, boot refuses on failure)

- ConnectionStrings:DefaultConnection, required, nonempty.
- ConnectionStrings:Redis, required, nonempty.
- Jwt:Issuer, default "fizzbuzz". Jwt:Audience, default "fizzbuzz". Jwt:SigningKey, required from environment only, minimum 32 chars, no default anywhere. Jwt:LifetimeHours, default 8.
- Game:NumberIntervalSeconds, default 15, valid 5..120.
- Cors:AllowedOrigins, required nonempty array. Dev default http://localhost:3000.

DI extension points (names are binding): Auth lane creates `backend/Auth/AuthServiceExtensions.cs` with AddGameAuth. Gameplay lane creates `backend/Gameplay/GameplayServiceExtensions.cs` with AddGameplay. Hardening lane calls both from Program.cs and owns all middleware order. JwtOptions lives in backend/Auth, GameOptions in backend/Gameplay.

## Cross cutting

Errors over HTTP are ProblemDetails, no stack traces, no raw DB text. Health: GET /health/live always 200, GET /health/ready checks Postgres and Redis, both anonymous. Rate limits: strict fixed window on register and login, basic fixed window on the hub endpoint. CORS from config, credentials allowed, middleware order is exception handler, https, cors, auth, rate limiter, controllers, hub, health.
