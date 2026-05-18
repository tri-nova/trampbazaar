# Production Rollout Checklist

Use this when moving the API, Web, and AdminWeb to a real server.

## 1. Database

Run SQL scripts in order:

1. `Database/SqlServer/001_initial_setup.sql`
2. `Database/SqlServer/002_listing_offers.sql`
3. `Database/SqlServer/003_grant_admin_role.sql`
4. `Database/SqlServer/004_schema_versioning.sql`
5. `Database/SqlServer/005_account_profile_and_billing.sql`
6. `Database/SqlServer/006_customer_account_modules.sql`
7. `Database/SqlServer/007_schema_tracking_and_performance.sql`
8. `Database/SqlServer/008_support_procedures.sql`

Do not skip `005_account_profile_and_billing.sql`, `006_customer_account_modules.sql`, or `007_schema_tracking_and_performance.sql`. Account and messaging tarafindaki yeni ekranlarin veri modeli ve performans iyilestirmeleri buna baglidir. `008_support_procedures.sql` zorunlu uygulama bagimliligi degil, operasyon ve destek icin tavsiye edilir.

## 2. Secrets and config

Do not deploy with repository-local development files.

Required production values:

- `Server:BaseUrl`
- `ConnectionStrings:SqlServer`
- `Auth:SigningKey`
- `Cors:AllowedOrigins`
- `Payments:Provider`

If `Payments:Provider=stripe`, also set:

- `Payments:Stripe:SecretKey`
- `Payments:Stripe:WebhookSecret`
- `Payments:DefaultSuccessUrl`
- `Payments:DefaultCancelUrl`

Recommended approach:

- API secrets through environment variables or server secret store
- Web/Admin `Api:BaseUrl` through environment-specific config
- development-only overrides in `appsettings.Development.Local.json`, which is already gitignored
- never commit live SQL, Stripe, or signing credentials into the repo

## 3. API startup safety

The API already blocks unsafe production-like startup if:

- `Auth:SigningKey` is missing, too short, or placeholder
- SQL connection string is placeholder
- CORS origins are empty or wildcard
- production uses `Payments:Provider=demo`
- Stripe keys are placeholders

Reference:

- `trampbazaar.Api/Services/ConfigurationSafetyValidator.cs`

## 4. First smoke test after deploy

1. Check `GET /health/live` on API, Web, and AdminWeb.
2. Register a brand new user from the Web app.
3. Log in and open `Hesabim`.
4. Save profile fields.
5. Save billing address fields.
6. Change password.
7. Re-login with the new password.
8. Open `Orders`, `Ledger`, `Favorites`, `StockAlerts`, and `PriceAlerts`.
9. Start a hosted ledger payment session from `AccountPayment`.
10. Open `Listings`, `ListingDetail`, `Conversations`, and `Notifications`.
11. If payments are enabled, verify package purchase flow.
12. Log in to AdminWeb and verify `Users`, `Listings`, `Payments`, and `Complaints`.

## 5. Suggested DB checks

After `008_support_procedures.sql`, these are useful:

```sql
EXEC dbo.usp_Ops_HealthSnapshot;
EXEC dbo.usp_Ops_UserAccountSnapshot @UserNameOrEmail = N'batu@example.com';
```

## 6. Rollback note

If the app is already live and `005_account_profile_and_billing.sql`, `006_customer_account_modules.sql`, or `007_schema_tracking_and_performance.sql` has not been applied yet, deploy the database changes before switching traffic to the new application build.
