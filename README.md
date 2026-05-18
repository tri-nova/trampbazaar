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
- GitHub Actions workflows for web/API tests, containers and MAUI target builds
- on-demand MAUI release artifact workflow for desktop/mobile smoke packaging

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
- Store-ready signed mobile packages still require platform signing credentials
- MAUI defaults to live API mode; mock data is now only an explicit debug fallback
- Browser smoke end-to-end automation covers Web and AdminWeb critical flows, but not the MAUI client UI

## Local setup

### 1. Database

Run SQL scripts in order:

1. [001_initial_setup.sql](./Database/SqlServer/001_initial_setup.sql)
2. [002_listing_offers.sql](./Database/SqlServer/002_listing_offers.sql)
3. [003_grant_admin_role.sql](./Database/SqlServer/003_grant_admin_role.sql)
4. [004_schema_versioning.sql](./Database/SqlServer/004_schema_versioning.sql)
5. [005_account_profile_and_billing.sql](./Database/SqlServer/005_account_profile_and_billing.sql)
6. [006_customer_account_modules.sql](./Database/SqlServer/006_customer_account_modules.sql)
7. [007_schema_tracking_and_performance.sql](./Database/SqlServer/007_schema_tracking_and_performance.sql)
8. [008_support_procedures.sql](./Database/SqlServer/008_support_procedures.sql)

### 2. API configuration

Create `trampbazaar.Api/appsettings.Development.Local.json` by copying [appsettings.Local.example.json](./trampbazaar.Api/appsettings.Local.example.json), or set environment variables.

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

For local overrides without committing values:

- copy [trampbazaar.Web/appsettings.Local.example.json](./trampbazaar.Web/appsettings.Local.example.json) to `trampbazaar.Web/appsettings.Development.Local.json`
- copy [trampbazaar.AdminWeb/appsettings.Local.example.json](./trampbazaar.AdminWeb/appsettings.Local.example.json) to `trampbazaar.AdminWeb/appsettings.Development.Local.json`

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

Release publish bundle:

```bash
powershell -ExecutionPolicy Bypass -File .\scripts\publish-server.ps1
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

Current automated coverage includes:

- unit tests for token and authorization rules
- API integration smoke tests for health, auth gate, and database-unavailable behavior
- payment gateway and webhook signature validation tests
- Web and AdminWeb integration smoke tests for render and redirect flows
- Playwright browser smoke tests for Web and AdminWeb shell navigation

MAUI Windows target also builds locally. Full solution build can still fail if Android SDK `android-35` is not installed.

## CI

GitHub Actions workflow:

- [.github/workflows/ci.yml](./.github/workflows/ci.yml)
- [.github/workflows/containers.yml](./.github/workflows/containers.yml)
- [.github/workflows/maui.yml](./.github/workflows/maui.yml)
- [.github/workflows/maui-artifacts.yml](./.github/workflows/maui-artifacts.yml)

It runs:

- restore
- API build
- Web build
- AdminWeb build
- Playwright browser install
- tests

MAUI workflow runs:

- Windows MAUI build
- Android MAUI build
- iOS MAUI build
- MacCatalyst MAUI build

MAUI artifact workflow publishes:

- Windows unpackaged release output
- Android APK artifact
- iOS simulator artifact
- MacCatalyst artifact

Release notes:

- [docs/mobile-release.md](./docs/mobile-release.md)
- [docs/stripe-production.md](./docs/stripe-production.md)
- [docs/production-rollout.md](./docs/production-rollout.md)
- [docs/demo-checklist.md](./docs/demo-checklist.md)
- [docs/demo-accounts.md](./docs/demo-accounts.md)
- [docs/demo-talk-track.md](./docs/demo-talk-track.md)

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

Before the first run, create a local `.env` from [.env.example](./.env.example) and fill in your environment-specific values.

Required:

- `TB_SQLSERVER_CONNECTION`
- `TB_AUTH_SIGNING_KEY`

Optional:

- `TB_PAYMENTS_PROVIDER`
- `TB_PAYMENTS_SUCCESS_URL`
- `TB_PAYMENTS_CANCEL_URL`
- `TB_STRIPE_SECRET_KEY`
- `TB_STRIPE_WEBHOOK_SECRET`
- `TB_WEB_ORIGIN`
- `TB_ADMIN_ORIGIN`
- `TB_INTERNAL_API_BASE_URL`

Database automation:

```bash
powershell -ExecutionPolicy Bypass -File .\scripts\run-db-migrations.ps1 -ConnectionString "Server=tcp:HOST,1433;Database=TrampBazaar;User Id=...;Password=...;Encrypt=True;TrustServerCertificate=False"
```

Post-deploy smoke test:

```bash
powershell -ExecutionPolicy Bypass -File .\scripts\server-smoke-test.ps1 `
  -ApiBaseUrl "https://api.example.com" `
  -WebBaseUrl "https://app.example.com" `
  -AdminBaseUrl "https://admin.example.com"
```

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
- For development, keep secrets in `appsettings.Development.Local.json` or `TB_*` environment variables, not in committed `appsettings.Development.json`.
- Follow [docs/stripe-production.md](./docs/stripe-production.md) before enabling live Stripe traffic.
