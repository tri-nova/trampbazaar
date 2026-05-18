# Demo Talk Track

## 7-minute flow

1. Open the home page.
   Explain that the stack is split into API, customer web, admin web, and MAUI client.

2. Go to `Listings`.
   Show that the marketplace supports direct sale and auction side by side.

3. Open `Retro Kamera`.
   Explain the buyer-side flow: listing detail, offer creation, and seller messaging.

4. Open `Vintage Pikap`.
   Show the auction variant and explain that bids run through the same authenticated API surface.

5. Log in as `batu@example.com`.
   Jump to `Account` and point out active listings, notifications, and paid package history.

6. Open `Conversations` and `Notifications`.
   Show that a live user flow continues after listing discovery into messaging and follow-up actions.

7. Open `Packages`.
   State clearly whether the environment is using `demo` payment mode or `stripe`.

8. Switch to admin as `admin@example.com`.
   Walk `Dashboard -> Users -> Listings -> Payments -> Complaints`.

## Fast talking points

- Customer and admin surfaces are separate apps on top of one API.
- Auth, listings, offers, bids, conversations, notifications, payments, and complaints are all wired.
- The demo environment can run with real SQL persistence and demo payments.
- The pre-demo scripts now cover both health checks and repeatable seed data.

## If something degrades live

- If SQL is reachable but a screen is sparse, continue from seeded accounts rather than creating fresh data on stage.
- If payments should not be demonstrated, keep the explanation at the package catalog and account payment history.
- If web home data is temporarily unavailable, the page should still render instead of crashing; continue from `Listings` or `Login`.
