# Naija Empires → TestFlight via Unity Build Automation (UBA), Windows-only

The Personal license can't activate on game-ci/GitHub Actions, so we use **Unity Build Automation**
(builds with your Unity *account*, no license file needed). iOS signing is done with a distribution
certificate + provisioning profile we generate from Windows. Bundle ID: `com.buildafr.naijaempires`.

Working folder for iOS files: `D:\Downloads\Work\naija-ios\`

---

## A. iOS Distribution Certificate  (CSR already generated)
1. developer.apple.com → **Certificates, IDs & Profiles → Certificates → +**
2. Choose **Apple Distribution** → Continue.
3. Upload **`ios_distribution.csr`** → Continue → **Download** the `.cer` (save into `D:\Downloads\Work\naija-ios\`).
4. Tell Claude when it's downloaded → Claude converts `.cer` + `ios_distribution.key` → **`ios_distribution.p12`** (with a password) using OpenSSL.

## B. App Store Provisioning Profile
1. developer.apple.com → **Profiles → +**
2. Distribution → **App Store Connect** (App Store) → Continue.
3. App ID = `com.buildafr.naijaempires` → select the Distribution cert from step A → name it
   `Naija Empires App Store` → Generate → **Download** the `.mobileprovision` (same folder).

## C. Link the Unity project to Unity Cloud  (in the editor)
1. Open `D:\Work\naija-empires\unity-m0` in Unity.
2. **Edit → Project Settings → Services** (or the Unity Cloud panel) → sign in with your Unity account.
3. **Create/Link** a Unity Cloud project (name "Naija Empires"). This is what lets UBA build it.
4. Commit/push the new `ProjectSettings/` changes (Claude can do this).

## D. Unity Build Automation configuration  (cloud.unity.com)
1. **cloud.unity.com** → pick the org/project → **DevOps → Build Automation**.
2. **Configurations → New** (or "Set up"):
   - **Source control:** connect **GitHub** → authorize → repo `devdanny2024/naija-empires`, branch `master`.
   - **Project path / subfolder:** `unity-m0`  ← important, the Unity project is in this subfolder.
   - **Platform:** iOS · **Unity version:** 6000.4.11f1 (or auto-detect).
   - **Bundle ID:** `com.buildafr.naijaempires`.
   - **Credentials:** upload **`ios_distribution.p12`** (+ its password) and the **`.mobileprovision`**.
3. Save → **Build**.

## E. Deliver to TestFlight
- If UBA offers "publish to App Store Connect / TestFlight": add the **App Store Connect API key**
  (`.p8` + Key ID `3PX66XB92A` + Issuer ID `63b4e270-…`) and enable auto-publish.
- Otherwise: download the `.ipa` and upload from Windows with **iTMSTransporter** (Java, cross-platform)
  or the App Store Connect API. Claude can script the iTMSTransporter upload.

---

### Reality check
- UBA free tier has limited build minutes — fine for occasional test builds.
- First iOS build usually needs 1–2 fixes (signing mismatch, bundle id, Unity version). When a build
  fails, copy the UBA log and Claude will diagnose.
- Apple `.p8` from `D:\Downloads\Work\icons\AuthKey_3PX66XB92A.p8` (already a GitHub secret too).
