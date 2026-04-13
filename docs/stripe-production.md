# Stripe Production Checklist

Use this before switching `Payments:Provider` to `stripe` in a real environment.

## Required configuration

- Set `Payments:Provider=stripe`
- Set `Payments:Stripe:SecretKey`
- Set `Payments:Stripe:WebhookSecret`
- Set `Payments:DefaultSuccessUrl`
- Set `Payments:DefaultCancelUrl`
- Set `Cors:AllowedOrigins` to explicit HTTPS origins
- Set a strong `Auth:SigningKey`
- Set a real `ConnectionStrings:SqlServer`

The API already rejects unsafe production-like configurations at startup. See `ConfigurationSafetyValidator`.

## Stripe dashboard setup

1. Create a live API key with minimum required scope.
2. Create a webhook endpoint for:
   - `POST /api/payments/webhooks/stripe`
3. Subscribe at minimum to:
   - `checkout.session.completed`
   - `checkout.session.expired`
4. Store the webhook signing secret in `Payments:Stripe:WebhookSecret`.

## Return URL checks

1. Verify the web app sends absolute success and cancel URLs.
2. Verify API fallback URLs resolve to real public HTTPS addresses.
3. Confirm checkout returns to:
   - `/PaymentSuccess`
   - `/PaymentCancel`

## Smoke test in staging

1. Start API, Web, AdminWeb with staging config.
2. Log in from the Web app.
3. Open `/Packages`.
4. Start a package purchase.
5. Confirm redirect to hosted Stripe Checkout.
6. Complete a test payment in Stripe test mode first.
7. Verify:
   - payment row exists
   - status changes from `pending` to `paid`
   - webhook delivery is successful
   - success page resolves correctly

## Go-live checks

- HTTPS terminates correctly in front of the API
- `X-Forwarded-*` headers are preserved by the reverse proxy
- webhook endpoint is reachable from Stripe
- logs do not expose secret values
- replay a webhook event from Stripe dashboard and confirm idempotent handling

## MAUI note

The MAUI client now persists the access token through `SecureStorage` first and only falls back when secure storage is unavailable. That closes the main local token storage gap, but mobile store signing remains a separate release task.
