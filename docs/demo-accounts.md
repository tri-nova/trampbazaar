# Demo Accounts

Provision the reusable demo dataset with:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\seed-demo-data.ps1
```

The script checks `TB_SQLSERVER_CONNECTION`, then `trampbazaar.Api/appsettings.Development.Local.json`, then `trampbazaar.Api/appsettings.Local.json`. If you want a different SQL Server target:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\seed-demo-data.ps1 -ConnectionString "Server=tcp:HOST,1433;Database=TrampBazaar;User Id=...;Password=...;Encrypt=False;TrustServerCertificate=True"
```

Provisioned credentials:

- Web buyer: `batu@example.com` / `Password123!`
- Seller persona: `ayse@example.com` / `Password123!`
- Admin: `admin@example.com` / `Password123!`

Provisioned demo state:

- `batu` has one published listing, one paid package record, and two notifications
- `ayse` owns a direct-sale listing and an active auction listing
- `batu` and `ayse` already have a conversation thread with messages
- one open complaint exists for admin moderation screens
- if `005_account_profile_and_billing.sql` is applied, `batu` and `ayse` also get seeded profile/contact and billing address data for the new `Hesabim` forms
- if `006_customer_account_modules.sql` is applied, demo orders, ledger entries, favorites, stock alerts, and price alerts are also seeded

Recommended login flow during demo:

1. Enter the web app with `batu@example.com`.
2. Show `Account`, `Listings`, `ListingDetail`, `Conversations`, and `Notifications`.
3. Switch to admin with `admin@example.com`.
4. Show `Users`, `Listings`, `Payments`, and `Complaints`.
