# Release notes — 1.3.0 (version code 26)

Bundle: `Builds/emberline-1.3.0-code26.aab`, signed with the upload key. Code 26 because code 25 was uploaded with Play Billing 6.2.1; 26 carries Unity IAP 4.15.1 (Billing 8.3.0).

---

## Play Console "What's new"

Paste this into the release's release-notes field. Play's limit is 500
characters per language; this is 457.

```
Yorune is reborn.

• A Japanese valley: thatched farmhouses, storehouses, watchtowers, torii, stone lanterns, cherry and maple trees
• A soundtrack at last: koto and shamisen for exploring, taiko for battle, and living ambience
• Nights you can see: lantern light, glow and sharper edges
• Smoother frames in big fights
• Optional rewarded ads: revive once with Second Wind, or double your spoils
• Remove Ads purchase, and delete your account from Settings
```

---

## Full notes

**The valley.** The village, the raiders' camp and the shrines were built from a
European fantasy kit. They are now Japanese: thatched and tiled minka with deep
roofs and verandas, white kura storehouses, yagura watchtowers over a stake
palisade, vermilion torii, kasuga stone lanterns and a roadside shrine. Cherry
trees by the shrine, black pines at the road gate, maples along the road north.

**Sound.** Exploration, combat and boss themes, and ambience that follows where
you stand — village by day, crickets at night, the river at the falls, wind on
high ground, rain when it rains. Music dips under spoken lines. Fixed: every
place sounded like the marsh, and music could go silent after a mission ended.

**Light.** Lanterns, shoji and campfires now cast real light at night, with a
soft glow around bright sources and anti-aliased edges on the medium and high
graphics settings. The fill and back light are computed in the main pass,
which also removed most of the scene's extra draw passes.

**Performance.** Distance culling for grass and small props, automatic
level-of-detail for the cast and the new trees, and fewer shadow casters. On
the reference phone the village view went from ~830 to ~500 draw calls and the
eight-enemy fight from ~640k to ~320k triangles.

**Ads and purchases.** Rewarded ads are optional and appear only on three
buttons: Second Wind (revive once per mission), double shards after a victory,
double spoils after a march. In the EU, UK and Switzerland a consent form asks
first, and Settings → AD PRIVACY changes the choice. Remove Ads is a one-time
purchase and comes back by itself after a reinstall.

**Account.** Settings → DELETE ACCOUNT erases your account and cloud save.

**Privacy policy.** Updated 21 October 2026 to describe sign-in, cloud save,
analytics, ads and purchases:
https://chaubeyprashant.github.io/emberline_privacy_policy/

---

## Before uploading

- Version code 26 must exceed every code already on Play (any track).
- Compliance steps in `docs/ADMOB_COMPLIANCE.md` §8: app-ads.txt and the Website
  field, Play Console declarations (ads, advertising ID, data safety, target
  audience 13+, data deletion), the GDPR message in AdMob, `remove_ads` and
  `100_coins` active.
