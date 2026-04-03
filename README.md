# TrampBazaar

Hybrid marketplace sample with:

- `trampbazaar.Api`: ASP.NET Core minimal API
- `trampbazaar.Web`: user-facing Razor Pages app
- `trampbazaar.AdminWeb`: admin Razor Pages app
- `trampbazaar`: MAUI client
- `trampbazaar.Tests`: unit tests for critical API auth rules

Operational extras included:

- Dockerfiles for `Api`, `Web`, `AdminWeb`
- `docker-compose.yml` for local production-like smoke runs
- `/health/live` endpoints on API, Web and AdminWeb

## Current status

Implemented:

- User and admin login flows
- Token-protected API for authenticated operations
- Listings, offers, bids, conversations, notifications
- Package purchase flow
- User account dashboard on web
- Admin dashboards for users, listings, complaints, payments
- CI workflow for API/Web/AdminWeb build and tests

Not fully production-complete:

- Real payment credentials are not bundled
- MAUI full cross-platform CI is not configured
- End-to-end browser or API integration tests are not present

## Local setup

### 1. Database

Run SQL scripts in order:

1. [001_initial_setup.sql](./Database/SqlServer/001_initial_setup.sql)
2. [002_listing_offers.sql](./Database/SqlServer/002_listing_offers.sql)
3. [003_grant_admin_role.sql](./Database/SqlServer/003_grant_admin_role.sql)

### 2. API configuration

Edit [appsettings.Development.json](./trampbazaar.Api/appsettings.Development.json).

Required keys:

- `ConnectionStrings:SqlServer`
- `Auth:SigningKey`

Optional payment keys:

- `Payments:Provider`
  Supported: `demo`, `stripe`
- `Payments:DefaultSuccessUrl`
- `Payments:DefaultCancelUrl`
- `Payments:Stripe:SecretKey`
- `Payments:Stripe:WebhookSecret`

Optional security keys:

- `Cors:AllowedOrigins`
- `RateLimiting:PermitLimit`
- `RateLimiting:WindowMinutes`

### 3. Web configuration

Edit:

- [trampbazaar.Web/appsettings.json](./trampbazaar.Web/appsettings.json)
- [trampbazaar.AdminWeb/appsettings.json](./trampbazaar.AdminWeb/appsettings.json)

Set `Api:BaseUrl` to your API address.

## Running locally

API:

```bash
dotnet run --project trampbazaar.Api/trampbazaar.Api.csproj
```

Web:

```bash
dotnet run --project trampbazaar.Web/trampbazaar.Web.csproj
```

Admin:

```bash
dotnet run --project trampbazaar.AdminWeb/trampbazaar.AdminWeb.csproj
```

Tests:

```bash
dotnet test trampbazaar.Tests/trampbazaar.Tests.csproj
```

Health checks:

```text
GET /health/live
```

Available on:

- API `http://localhost:5136/health/live`
- Web `https://localhost:<web-port>/health/live`
- Admin `https://localhost:<admin-port>/health/live`

## Payments

### Demo mode

Set:

```json
"Payments": {
  "Provider": "demo"
}
```

This creates completed internal payment records without external checkout.

### Stripe mode

Set:

```json
"Payments": {
  "Provider": "stripe",
  "Stripe": {
    "SecretKey": "sk_...",
    "WebhookSecret": "whsec_..."
  }
}
```

Web package purchases will redirect to hosted Stripe Checkout.

Configure a Stripe webhook to:

```text
POST /api/payments/webhooks/stripe
```

Relevant events:

- `checkout.session.completed`
- `checkout.session.expired`

If the web app sends return URLs, those are used first. Otherwise API falls back to:

- `Payments:DefaultSuccessUrl`
- `Payments:DefaultCancelUrl`

## Build and verification

Verified locally:

- `dotnet build trampbazaar.Api/trampbazaar.Api.csproj`
- `dotnet build trampbazaar.Web/trampbazaar.Web.csproj`
- `dotnet build trampbazaar.AdminWeb/trampbazaar.AdminWeb.csproj`
- `dotnet test trampbazaar.Tests/trampbazaar.Tests.csproj`

MAUI Windows target also builds locally. Full solution build can still fail if Android SDK `android-35` is not installed.

## CI

GitHub Actions workflow:

- [.github/workflows/ci.yml](./.github/workflows/ci.yml)
- [.github/workflows/containers.yml](./.github/workflows/containers.yml)

It runs:

- restore
- API build
- Web build
- AdminWeb build
- tests

Container workflow:

- builds `api`, `web`, `admin` Docker images on pull requests
- publishes GHCR images on `main`, `master` and `v*` tags
- emits `latest` for default branch pushes and `sha` tags for traceability

## Docker smoke run

Build and start API, web and admin:

```bash
docker compose up --build
```

Default ports:

- API: `http://localhost:8080`
- Web: `http://localhost:8081`
- Admin: `http://localhost:8082`

Before real use, replace these placeholder values in [docker-compose.yml](./docker-compose.yml):

- `ConnectionStrings__SqlServer`
- `Auth__SigningKey`
- `Payments__Provider`
- Stripe keys if `stripe` is enabled

## Registry publishing

The repository includes a GHCR publishing workflow for:

- `ghcr.io/<owner>/trampbazaar-api`
- `ghcr.io/<owner>/trampbazaar-web`
- `ghcr.io/<owner>/trampbazaar-adminweb`

On GitHub-hosted runs it uses the built-in `GITHUB_TOKEN`. Make sure package write access is allowed for the repository actions settings.

## Deployment notes

- Put signing keys and payment secrets in environment-specific configuration, not in committed production files.
- Run behind HTTPS and a reverse proxy that forwards `X-Forwarded-*` headers.
- Set strict `Cors:AllowedOrigins` in production.
- Keep `Payments:Provider=demo` in non-payment environments.
- Use `/health/live` for load balancer or container liveness probes.
- Prefer environment variables or secret stores over server-local JSON files in production.
