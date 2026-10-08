# Security policy

## Supported versions

Only the latest commit on master is supported. This is a portfolio prototype, not a maintained product.

## Reporting a vulnerability

Open a private security advisory on GitHub, or email the repo owner. Do not file public issues for vulnerabilities.

Include: what is affected, steps to reproduce, and the impact you see. Expect an acknowledgment within a week.

## Known posture

- JWT in an HttpOnly cookie, antiforgery on mutations, rate limits on auth.
- No secrets are committed. All config comes from environment via .env (see .env.example).
- No production deployment exists. Local Compose only.
