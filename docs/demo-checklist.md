# Demo Checklist

Run this from the repository root before a live demo:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\seed-demo-data.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\demo-check.ps1
```

For a full rehearsal where the apps stay open after the checks:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\demo-check.ps1 -KeepRunning
```

What the script verifies:

- `trampbazaar.Api` starts and answers `GET /health/live`
- `trampbazaar.Web` starts and serves the home page
- the web home page still renders its core CTA even if the dashboard falls back
- `trampbazaar.AdminWeb` starts and serves the admin login page

Seeded accounts:

- Web buyer: `batu@example.com` / `Password123!`
- Seller persona: `ayse@example.com` / `Password123!`
- Admin: `admin@example.com` / `Password123!`

Manual rehearsal after the script passes:

1. Open the web app and verify `login -> listings -> listing detail -> offer or bid -> conversations -> notifications -> complaint`.
2. If packages will be shown, confirm the current payment mode:
   `demo` for internal mock completion
   `stripe` only if keys, webhook, and return URLs are already validated
3. Open the admin app and verify login plus `Users`, `Listings`, `Payments`, and `Complaints`.
4. If MAUI will be shown, run the exact device or desktop build you will use in the demo. CI build coverage exists, but UI smoke coverage is still web and admin focused.

Known demo-safe fallback:

- If the API can start but the database is unavailable, the web home page should still render and show the status banner instead of crashing.
