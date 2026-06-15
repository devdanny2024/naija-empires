# Naija Empires → TestFlight (GitHub Actions, cloud build)

Builds the Unity iOS app on GitHub's macOS runners and uploads to TestFlight. **No Mac needed.**
You only do this credential setup once; after that it's "Actions → Run workflow".

Bundle ID: **`com.buildafr.naijaempires`** · Repo: `devdanny2024/naija-empires`

---

## 1. Configure iOS settings in Unity (once)
In the editor: **Naija Empires → Configure iOS Build Settings** (sets bundle id, landscape, icon).
Then commit + push `ProjectSettings/ProjectSettings.asset`.

## 2. App Store Connect — create the app record (once)
- appstoreconnect.apple.com → **Apps → +** → New App → iOS → bundle id `com.buildafr.naijaempires`
  (register the identifier first at developer.apple.com → Identifiers if it isn't there).

## 3. Apple App Store Connect API key (for signing + upload)
- App Store Connect → **Users and Access → Integrations → App Store Connect API → +**
- Role **App Manager**. Download the **`.p8`** (one-time download). Note the **Key ID** and **Issuer ID**.
- Add repo secrets (Settings → Secrets and variables → Actions → New repository secret):
  | Secret | Value |
  |---|---|
  | `APP_STORE_CONNECT_KEY_ID` | the Key ID |
  | `APP_STORE_CONNECT_ISSUER_ID` | the Issuer ID |
  | `APP_STORE_CONNECT_KEY` | **base64 of the .p8** → `base64 -i AuthKey_XXXX.p8` (or `certutil -encode` on Windows) |
  | `APPLE_TEAM_ID` | your 10-char Team ID (developer.apple.com → Membership) |

## 4. Unity license for the build machine (game-ci)
Personal license is fine. One-time activation to get a license file:
1. Run the **Activation** helper once (game-ci docs: https://game.ci/docs/github/activation) — it
   produces a `.alf`, you upload it to https://license.unity3d.com/manual to get a **`.ulf`**.
2. Add secrets:
   | Secret | Value |
   |---|---|
   | `UNITY_LICENSE` | the full contents of the `.ulf` file |
   | `UNITY_EMAIL` | your Unity account email |
   | `UNITY_PASSWORD` | your Unity account password |

## 5. Run it
GitHub → **Actions → iOS TestFlight → Run workflow** (or push a tag `ios-v0.1`).
First green run uploads to **TestFlight → wait for processing → add yourself as a tester**.

---

### Honest notes
- iOS CI usually needs **1–2 debugging passes** the first time (signing / scheme / Xcode version).
  Re-run from the Actions tab; share the failing log and I'll fix the workflow.
- The build boots straight into a **single-player skirmish** (no menu yet); the small
  "MULTIPLAYER (test)" box is a leftover scaffold. Multiplayer isn't wired yet — this build is to
  feel out the **single-player RTS + touch controls** on device.
- Photon **App Id** is not committed (multiplayer is offline-only in this build), so nothing
  sensitive is public.
