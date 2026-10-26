# AdMob compliance — Phase 3

What Google Play and AdMob need before ads go live, set against what the game
actually does (read from the code on 2026-10-21). Items marked **you** happen
outside the repo.

## 1. Privacy policy — blocking

The live policy (https://chaubeyprashant.github.io/emberline_privacy_policy/)
says the game has no ads, no analytics, no accounts, no sign-in, no network and
no purchases. The app ships Firebase sign-in, Firestore cloud save and Firebase
Analytics today, and now AdMob and Play purchases. Play requires the policy to
describe the app; an ads app under this policy is a policy violation.

- Replacement page: `Marketing/PlayStore/compliance/privacy_policy_index.html`
  (same styling; drop-in for that repo's `index.html`).
- **you**: publish it before the first build with ads reaches any track.

## 2. app-ads.txt — **you**

Line (publisher ID from the AdMob app ID in `GoogleMobileAdsSettings.asset`):

```
google.com, pub-2773109772362951, DIRECT, f08c47fec0942fa0
```

File: `Marketing/PlayStore/compliance/app-ads.txt`.

1. Put it at `https://www.ergebinstechnologies.com/app-ads.txt` (Next.js: the
   site's `public/` folder). It currently returns 404. The bare domain's
   redirect to `www` is fine.
2. Play Console → Store presence → Store settings → Website:
   `https://www.ergebinstechnologies.com`. AdMob finds the file through this
   field; it must be the same domain.
3. AdMob → Apps → Emberline → app-ads.txt: "Check for updates". Verification can
   take up to 24 h; unverified apps get limited ad serving.

## 3. Play Console declarations — **you**

**App content → Ads:** Yes, the app contains ads.

**App content → Advertising ID:** Yes — used for Advertising or marketing,
Analytics and Fraud prevention. The built APK requests
`com.google.android.gms.permission.AD_ID` (plus the Privacy Sandbox
`ACCESS_ADSERVICES_*` permissions), all merged in by the Mobile Ads SDK;
checked with `aapt2 dump permissions` on 2026-10-21.

**Target audience and content:** ages 13–15, 16–17, 18+ only. Do not tick any
under-13 group and do not join Designed for Families: the ads and consent code
assume a general audience (`TagForUnderAgeOfConsent = false`). The listing is
rated 12+ (IARC); combat violence fits that.

**Data safety** (collected = leaves the device; shared = sent to a third party):

| Data type | Collected | Shared | Optional | Purpose | Source in code |
| --- | --- | --- | --- | --- | --- |
| Name (display name) | Yes | No | Yes (Google sign-in only) | Account management | `AuthManager.SaveUserToFirestore` |
| Email address | Yes | No | Yes (Google sign-in only) | Account management | same |
| User IDs (Firebase UID) | Yes | No | Yes | Account management, App functionality | `AuthManager`, `CloudSaveManager` |
| Device or other IDs (advertising ID, app instance ID) | Yes | Yes (Google AdMob) | No* | Advertising or marketing, Analytics, Fraud prevention | AdMob, Firebase Analytics |
| App interactions (ad interactions, usage events) | Yes | Yes (AdMob) | No* | Advertising, Analytics | AdMob, Firebase Analytics |
| Approximate location (from IP, by AdMob) | Yes | Yes (AdMob) | No* | Advertising, Fraud prevention | AdMob |
| Other user-generated content: game progress | Yes | No | Yes | App functionality (cloud save) | `CloudSaveManager` |
| Purchase history | Yes | No | Yes | App functionality (Remove Ads) | Unity IAP / Play Billing |

\* Consent-gated in the EEA/UK/CH by UMP; elsewhere collection is required for
ads to serve.

Security practices: data encrypted in transit — **Yes**. Users can request
deletion — **Yes** (section 4). Independent security review — No.

If Firebase Analytics is switched off (it ships as a Firestore dependency and
the game logs no events), drop its rows and the "Analytics" purposes.

## 4. Account deletion — done in code, link **you**

Play requires apps that create accounts to offer deletion in the app and on
the web.

- In app: Settings → **DELETE ACCOUNT** (two taps). `AuthManager.DeleteAccount`
  deletes `users/{uid}` in Firestore, then the Firebase account, then signs out;
  a stale session asks the player to sign in again first.
- **you**: Play Console → App content → Data deletion: give the policy URL
  (its "Delete your account" paragraph) and the email route.

## 5. Consent (UMP) check — needs the phone (Phase 4)

Development builds read `ads_debug.txt` from the app's files folder:

```
eea
reset
device <HASHED_ID>
```

```bash
adb shell "run-as com.ergebins.emberline3d sh -c 'printf \"eea\nreset\n\" > files/ads_debug.txt'"
```

`eea` forces the EU consent form, `reset` forgets the stored choice, `device`
registers a test device for both UMP and AdMob (test ads only). The hashed ID
is printed in logcat by the SDK on first launch ("Use
RequestConfiguration.Builder().setTestDeviceIds"). Expect: the form on first
launch, AD PRIVACY in Settings, and ads requested only after a choice.
(`persistentDataPath` is normally external storage; if `run-as` cannot see the
file, push it to `/sdcard/Android/data/com.ergebins.emberline3d/files/`.)

## 6. Store listing wording — **you**

The description says "No wifi, no timers, and no paywalls" and stresses offline
play. Keep both true by keeping ads optional (rewarded) and the game fully
playable offline. Play will show "Contains ads" and "In-app purchases" on the
listing automatically once declared.

## 7. Phase 4 results on the Galaxy A33 (2026-10-21, dev build)

- Test device hashed ID: `84BBBC162B7D6877F1D78FFFBBD6A735` (UMP and AdMob share it).
  Development builds read it from `ads_debug.txt` (section 5); release builds
  never do, so the A33 must not tap live ads in a store build.
- Rewarded, end to end (`--es adtest 1`): loaded in 8 s, shown as **Test Ad**,
  game paused and silent during it, **reward granted**, time scale and audio
  restored exactly (`[ADTEST] done result=ok`).
- **UMP: no consent form configured** — "Publisher misconfiguration: no form(s)
  configured for the input app ID". **you**: AdMob → Privacy & messaging →
  European regulations → create and publish a message for Emberline. Until
  then EEA/UK players get no form; Google requires one for ads there.
- **Purchases: `NoProductsAvailable`** on a sideloaded build — expected. Play
  only returns products to a build installed from a Play test track, signed with
  the upload key, with `remove_ads` and `100_coins` active in Play Console.
- Not testable yet: the interstitial (no live unit), closing a rewarded ad
  early, offline behaviour (needs airplane mode on the phone — do it by hand:
  the "(AD)" buttons should grey out and the game should be unaffected).

## 8. Phase 5 — release

1. Build: `./release.sh 26 1.3.0` → `Builds/emberline-1.3.0-code26.aab` (Unity IAP 4.15.1, Play Billing 8.3.0), signed
   with the upload key and verified.
2. **you** before any upload, in this order: publish the new privacy policy (§1),
   app-ads.txt + website field (§2), Play Console declarations (§3), GDPR message
   (§7), activate `remove_ads` / `100_coins` in Play Console → Monetize →
   Products.
3. **you**: Play Console → Testing → Internal testing → create release, upload
   the AAB, add yourself as tester, install from the opt-in link.
4. On that install: buy Remove Ads with a licence-tester account (no charge),
   uninstall/reinstall and confirm it comes back by itself; check the consent
   form appears on a fresh install (EEA tester or VPN).
5. Run 48 h. Real users of the internal track see live ads; testers must not
   tap them. Watch AdMob for invalid-traffic warnings and `[ADS] paid` lines.
6. Promote to production (staged rollout 10 % → 50 % → 100 %).
