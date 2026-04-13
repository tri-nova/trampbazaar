# Mobile Release Notes

Current repository automation covers:

- MAUI target build validation in GitHub Actions
- release artifact generation for:
  - Windows unpackaged app
  - Android APK
  - iOS simulator app
  - MacCatalyst app

Workflow files:

- `.github/workflows/maui.yml`
- `.github/workflows/maui-artifacts.yml`

## What is still manual

Store-ready signed packages still require platform credentials outside the repository:

- Android keystore for signed APK/AAB
- Apple signing certificate + provisioning profile for device IPA / App Store delivery
- Windows code-signing certificate if packaged MSIX distribution is needed

## Recommended next secrets

If signed mobile delivery is added later, keep these as GitHub Actions secrets instead of files committed to the repo:

- `ANDROID_KEYSTORE_BASE64`
- `ANDROID_KEYSTORE_PASSWORD`
- `ANDROID_KEY_ALIAS`
- `ANDROID_KEY_PASSWORD`
- `APPLE_CERTIFICATE_BASE64`
- `APPLE_CERTIFICATE_PASSWORD`
- `APPLE_PROVISIONING_PROFILE_BASE64`
- `APPLE_TEAM_ID`
- `WINDOWS_PFX_BASE64`
- `WINDOWS_PFX_PASSWORD`

## Suggested versioning inputs

Keep these aligned before tagged releases:

- `ApplicationDisplayVersion` in `trampbazaar.csproj`
- `ApplicationVersion` in `trampbazaar.csproj`
- MAUI `ApplicationTitle` / `ApplicationId`
- Windows and Tizen manifest identity metadata

## Current limitation

The repository currently produces unsigned or simulator-friendly artifacts by default. This is enough for CI verification and internal smoke testing, but not enough for Play Store, App Store, TestFlight, or signed Windows Store distribution.

The MAUI session token is now stored through secure device storage when available. Release readiness still depends on platform signing and distribution credentials, not on local token persistence.
