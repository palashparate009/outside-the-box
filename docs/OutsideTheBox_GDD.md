# Outside the Box Game Design Document

**Implementation edition — 23 November 2026**

Outside the Box is a portrait mobile physics puzzle game. The player moves a magnet around the outside of a glass chamber to guide a ball through obstacles and into a pocket. Fast solutions matter most; efficient energy use, controlled movement, and understanding each chamber's active mechanics improve the result.

This GDD covers the current game, its supporting services, content pipeline, and production dependencies. It distinguishes implemented behavior, configurable tuning, and remaining release work. Values below are the checked-in configuration snapshot, rather than a statement about the current state of external consoles.

## 1. Product identity and design pillars

| Item | Current project |
| --- | --- |
| Game name | Outside the Box |
| Genre | Single-player physics puzzle |
| Primary platform | Android, portrait presentation |
| Engine | Unity 6000.3.22f1 |
| Application ID | `com.outsidethebox.puzzle` |
| Project version | `0.1.0`; Android version code `10` in the checked-in Player Settings |
| Campaign | 60 levels across six chapters of ten levels |
| Additional modes | Daily Challenge and Endless Vault |
| Business model | Free play with banner, interstitial, and rewarded advertising; cosmetic and gem IAP |
| Text system | TextMesh Pro with the ROGENZ SDF visual style |
| Online policy | Internet required by the current live configuration |

The design pillars are readable magnetic control, increasing route complexity, a clear identity for each chapter, and time-led performance scoring. Higher difficulty should come from route planning and deliberate combinations, rather than filling the board with every unlocked mechanic. Cosmetics change presentation without improving puzzle performance.

There is no multiplayer session or real-time competitive match system. Online services support identity, progress, purchases, analytics, configuration, notifications, and asynchronous leaderboards.

## 2. Player journey and core loop

1. Launch through the loading and authentication flow. Restore the player's account and cloud progress before allowing conflicting progress writes.
2. Reach the main menu and choose campaign Play, Daily Challenge, Chapters, Leaderboard, Settings, account controls, or Store.
3. Load a chamber, run the optional board arrival cinematic, and show any unlearned tutorials that apply to the actual level.
4. Drag and hold the magnet on the outside rails. Release to coast, redirect the field, and use the active mechanic controls to reach the pocket.
5. Finish before the timer or energy failure limit. Pause, restart, undo, hint, and revive actions support the attempt under their respective rules.
6. See the result screen with stars, performance percentage, time, energy, and gem reward. Replay, return to levels, or advance.
7. Unlock further campaign levels, claim chapter gifts in the store, improve personal bests, and optionally submit leaderboard scores.

### Main menu and navigation

The home screen provides a styled gem balance button with a plus affordance. It opens the gem purchase section directly. The separate Store entry supports cosmetic browsing and indicates unclaimed chapter gifts.

There is no home-screen Back button. Android Back or the device back gesture on the home screen opens a quit confirmation. Other screens use contextual Back navigation. Quit confirmation saves progress before leaving; cancelling returns to the current flow. In the Editor, confirming quit does not close Unity or discard an editing session.

## 3. Core gameplay and simulation

### Magnet and ball

The magnet stays outside the glass and moves along the top, bottom, left, and right rails. The ball remains inside. Holding the magnet applies its field; moving the magnet changes the direction and placement of that field. Releasing lets the ball coast and gives the player time to reposition.

Pull attracts the ball. Push reverses the force in chapters or levels where polarity is enabled. Push costs more energy under the current tuning. The view draws magnetic field lines and feedback to connect player input to the ball's movement.

The visible game uses continuous movement, velocity, acceleration, drag, collision resolution, and geometry defined by the board. It also has a deterministic grid rules layer used by the solver, hints, and content validation. A solver route is a planning aid; it does not mean the visible ball moves in discrete grid jumps.

`GameSession` coordinates attempt state. `BoardViewImplementation` presents and simulates the board, while `MagnetInput` handles player input. `GameRules`, `LevelSolver`, and the rail rules share deterministic checks used by generation and hints.

### Walls, gates, and the goal

Connected wall runs form corridors, turns, and chambers. Wall collision should remove movement into the wall while preserving useful movement along it. Gate arrows communicate allowed entry direction. Moving walls create time-dependent openings. The board's glass frame and background should surround the arena evenly, while camera fitting keeps the board centered and clears the HUD and native banner region.

A normal pocket captures the ball when the goal-entry conditions are met. A locked goal or orbit goal has additional conditions, so touching its visible location alone is not always sufficient. The goal drop animation shrinks and sinks the ball into the pocket before completion presentation.

### Player actions

| Action | Implemented role |
| --- | --- |
| Drag and hold | Position the magnet and apply its field |
| Release | Stop the held field and allow coasting |
| Pull / Push | Change polarity when available |
| Echo | Arm or control the echo recording mechanic when present |
| Undo | Restore a recorded movement state from the current attempt |
| Restart | Begin that chamber again with fresh attempt state |
| Hint | Spend a remaining hint and show a solver-recommended magnet position |
| Pause | Suspend player control and present pause options |
| Revive | Continue an eligible failed attempt using gems or rewarded advertising |

New saves begin with five hints. Hints are an inventory count, separate from the gem wallet. A hinted clear cannot earn three stars.

## 4. Modes and progression

### Campaign

Completing levels advances sequential access. Chapters contain ten levels each, and completing the 60-level campaign unlocks Endless Vault. Best stars, energy, performance ratings, times, and scores are stored per campaign level. A replay can improve personal bests without reducing an existing best result.

| Chapter | Levels | Identity | Configured board size | Time limit | Energy target multiplier |
| --- | --- | --- | --- | --- | --- |
| 1 Magnetic Basics | 1–10 | Learn smooth pulls, release, corners, and efficient routes | 5 × 5 | 40 seconds | 1.00 |
| 2 Frozen Glass | 11–20 | Lower friction and greater coasting | 6 × 6 | 45 seconds | 0.45 |
| 3 Polarity Lab | 21–30 | Pull and push control | 7 × 7 | 50 seconds | 0.42 |
| 4 Gate Factory | 31–40 | Gates, with selected moving-wall or signature variations | 8 × 8 | 55 seconds | 0.40 |
| 5 Field Distortion | 41–50 | Field zones, with selected imprints or lenses | 9 × 9 | 60 seconds | 0.39 |
| 6 Master Chambers | 51–60 | Polarity locks, with one selected variation | 10 × 10 | 65 seconds | 0.38 |
| 7 Endless Vault | After campaign | A rotating focused mechanic selection | Endless chapter settings | 70 seconds | 0.36 |

Chapter 1 retains its original energy allowance as the learning chapter. Later chapters use substantially reduced targets. Actual energy targets also depend on the generated route and chapter energy settings.

### Endless Vault

Endless chambers use a deterministic seed derived from `endlessSeedBase` and depth. Depth, best depth, and energy results persist. Difficulty increases through the shared level-generation systems. The active mechanics rotate; the vault does not automatically enable every earlier mechanic together.

The `endless_depth` leaderboard stores depth progress. Endless clears currently grant three gems.

### Daily Challenge

A UTC date key in `yyyyMMdd` format determines the shared daily seed. The generator uses `dailySeedBase`, a minimum difficulty of 16, and a difficulty cycle of 35. Daily chambers reuse the focused route, mechanic, rail, and goal-placement systems rather than a separate low-complexity generator.

Daily progress records the completed day, streak, and energy. The first completion of a day grants eight gems; repeating that completed day's challenge does not repeat this reward. The current UI copy says “one attempt,” but the entry path does not enforce a strict one-entry-per-day lock. Treat replay limits as an unresolved design decision rather than a delivered restriction. There is no dedicated daily leaderboard ID in the checked-in configuration.

## 5. Difficulty and level design

### Focused chapter model

The standard magnet, route geometry, and chapter physics remain the foundation. Additional board mechanics are selected by `ChapterMechanicPlan`. Later chapters retain their specialty and add at most one supporting variation. On a level with a rail challenge, that rail challenge occupies the supporting slot.

Chapter 4 uses Gates as the main mechanic. Its selected non-rail variations include Echo, Memory, and Moving Walls. Chapter 5 uses Field Zones, with selected Imprints or Lenses. Chapter 6 uses Polarity Locks, with selected Echo, Memory, or Orbit. The first three chapters express their main identity through the base controls and physics instead of adding many extra board objects.

Daily and endless chambers rotate a small mechanic selection from their seed. The legend displays the mechanics actually active in that chamber. Unlock levels and chapter defaults are inputs to selection; an unlocked mechanic is not promised in every later level.

### Routes and generation

`FocusedLevelGenerator` creates connected wall layouts and seeks longer, turning routes as the level number increases. Its route target starts from six and grows by approximately `round((levelNumber - 1) × 0.46)`. This is a generation target, not a guarantee that a human must perform exactly that number of drags.

The content pipeline considers reachable routes, detours, direct-line blocking, wall grouping, gate direction, rail availability, mechanic placement, and goal clearance. Solver and rule checks support content validation. The runtime can regenerate obsolete procedural content; authored wall-segment layouts have a separate preservation path.

`contentVersion` is 7 and `levelDesignRevision` is 2 in the current asset. Version checks prevent an old suspended ball position from being restored into a redesigned chamber. Permanent currency, purchases, stars, and progression are separate from that suspended attempt.

### Rail challenges

| Mechanic | Behavior | First scheduled campaign level under the current rules |
| --- | --- | --- |
| Locked Side | One rail is unavailable; use the other three sides | 8 |
| Limited Rail | Only part of one rail can be used | 14 |
| Timed Lock | A rail alternates between locked and open states | 24 |
| Unlock Switch | Touch a switch to unlock the selected rail and goal for the attempt | 34 |
| Side Change Limit | Switching magnet sides consumes a limited budget; movement on the same side does not | 44 |
| Magnet Cooldown | Sustained field use exhausts charge, followed by a recharge period | 54 |

Later chapter encounters are selected deterministically from the mechanics introduced so far. Restart resets attempt-specific budgets and switch state. Red stripes, locks, green open states, switch marks, countdowns, and charge feedback explain availability. Daily/endless rules can select rail challenges too, and a first encounter there can trigger the relevant tutorial before its campaign introduction.

### Signature mechanics

| Mechanic | Player-facing behavior |
| --- | --- |
| Echo Magnet | Record a magnet path and replay it as a ghost field while controlling the real magnet |
| Ferrofluid Memory | A conductive trail remembers movement and guides momentum on later crossings |
| Magnetic Imprints | Contact activates wall-based pull or push effects for a limited duration |
| Field Lenses | Rotate magnetic force inside their area, with arrows indicating the effect |
| Polarity Locks | Complete a pull/push sequence to open a passage |
| Orbit Goal | Circle the goal ring before the pocket permits capture |

Feature flags and strength, timing, radius, sample, and animation settings are configurable. Orbit placement includes clearance checks so its ring is not obstructed by arena boundaries or walls.

## 6. Time, energy, stars, and leaderboards

### Timer and failure rules

Fixed chapter times take priority whenever `chapterLevelSeconds` contains the chapter index. The older fallback formula is `baseLevelSeconds + solutionBeats × secondsPerSolutionBeat`, clamped to the minimum and maximum. The fallback minimum of 45 seconds does not override Chapter 1's explicit 40-second value.

Held magnetic use consumes energy over time and magnet travel distance. Push multiplies that cost by 1.35. Echo consumes additional energy at its configured multiplier. Collision penalties and chapter parameters also influence an attempt's energy cost.

Energy target is the chapter-adjusted route budget. Its minimum is 20 for Chapter 1 and 12 for later chapters. With energy failure enabled, failure occurs at `energyTarget × 1.6`; the displayed target is therefore not itself the failure cutoff. Running out of time also ends the active attempt unless an eligible revive is used.

### Performance formula

The current normalized rating is calculated as follows:

```text
timeEfficiency   = 1 - clamp01(elapsedSeconds / max(1, timeLimit))
energyEfficiency = 1 - clamp01((energyUsed / max(0.01, energyTarget)) / 2)
baseRating       = 0.70 × timeEfficiency + 0.30 × energyEfficiency
penalty          = (hintUsed ? 0.10 : 0) + 0.15 × revivesUsed
rating           = clamp01(baseRating × clamp01(1 - penalty))
```

The implementation normalizes the configured time and energy weights, so different positive weights do not need to sum to exactly one. When timers are disabled, energy efficiency substitutes for the time term.

| Result | Current rule |
| --- | --- |
| Three stars | Rating at least 0.65, with no hint cap |
| Two stars | Rating at least 0.35 |
| One star | A successful clear below the two-star threshold |
| Hint cap | At most two stars after a hint |
| Performance score | 100,000 completion points plus rounded `1,000 × rating` |

Stars use graphic icons rather than depending on a font containing a star character. The result screen displays the rating as a percentage alongside elapsed time and energy.

### Leaderboards

Unity Leaderboards uses the `production` environment selected explicitly by the player service. The game has nine configured board IDs:

- `all_time_performance`
- `chapter_01_performance` through `chapter_06_performance`
- `campaign_stars`
- `endless_depth`

Campaign performance summaries aggregate stored per-level best scores and associated performance data. Campaign stars and endless depth have separate submission paths. The current system does not provide an authoritative server simulation or anti-cheat scoring backend.

## 7. FTUE and accessibility

The first level teaches moving the magnet, guiding the ball, and energy use. Level 2 explains wall interaction and release-and-redirect play. Chapter, signature, and rail tutorials appear only when applicable and unlearned. Tutorial IDs are saved so already acknowledged explanations do not repeat on every visit.

All modal FTUE uses `TutorialOverlay.prefab`: a compact card with a matching mechanic icon, title, explanation, progress, and confirmation button. The game remains visible behind a neutral black shade at 48% opacity. The full-screen root does not draw an opaque blue background. Only the card animates; the backdrop remains stationary. Gameplay is paused and input blocked until the tutorial closes.

The card's preferred size is 900 × 720 canvas units and it fits inside the available safe area. It is a prefab with Inspector-editable bindings, not a new hierarchy of drawn objects for every tutorial message. Legacy tutorial instances are redirected to this authored presentation.

Settings include music, sound, vibration, Reduce Motion, color-blind symbols, Large Text, performance mode, cosmetic selection, notifications, privacy controls, and account deletion. Reduce Motion bypasses the pipe cinematic and reduces relevant UI motion. Large Text changes the UI reference resolution. Only English localization is currently supplied; the loading and language selection infrastructure can support additional string assets.

## 8. Economy and rewards

The player-facing currency is **gems**. Existing code and Remote Config names such as `startingCoins` and `coinReviveCost` refer to that same wallet, not a second coin currency.

| Source or sink | Snapshot value |
| --- | --- |
| Starting balance | 150 gems, initialized once for a new currency profile |
| First campaign clear | 2 gems |
| Each newly improved campaign star | 1 gem |
| First Daily Challenge completion that day | 8 gems |
| Endless completion | 3 gems |
| Free-gems rewarded ad | 10 gems |
| Gem-funded revive | 50 gems |
| Time revive | 30 seconds under the current settings |
| Maximum revives per level | 2 |
| Energy revive | Reduces usage using the configured 0.4 revive fraction |

Campaign replay rewards pay for improvement rather than repeatedly paying the full first-clear reward. The local save tracks wallet balance and lifetime earned/spent totals. Cosmetic ownership, chapter gifts, and transaction markers persist with player progress.

### Chapter completion gifts

Finishing all ten levels of a campaign chapter creates one gift entry for that chapter. Its cosmetic category is chosen randomly from Ball, Magnet, Hole, Walls, Glass, or Background, then saved. The item is not rerolled every time the store opens.

Gifts have separate free-item artwork and muted colors, with a normal-tier presentation intended to sit below premium store items. They use indices outside the ten standard store skins. Players browse pending gifts and claim them from the store; claiming marks the saved gift as owned. The home/store indicator, completion messaging, and reminder system point out unclaimed gifts. A previously owned store cosmetic remains unlocked independently of these rewards.

## 9. Store and in-app purchases

### Cosmetic catalog

Each of six categories has ten standard styles. Categories map to Ball, Magnet, Hole, Walls, Glass, and Background. The default style is free, three styles use gems, and six styles are premium IAP.

| Index | Style | Rarity | Unlock route and India fallback display price |
| --- | --- | --- | --- |
| 00 | Classic | Normal | Default |
| 01 | Arctic | Normal | 100 gems |
| 02 | Solar | Rare | 120 gems |
| 03 | Nebula | Rare | 130 gems |
| 04 | Ruby | Rare | Premium, INR 29 |
| 05 | Emerald | Epic | Premium, INR 39 |
| 06 | Midnight | Epic | Premium, INR 49 |
| 07 | Ember | Epic | Premium, INR 59 |
| 08 | Cosmic | Legendary | Premium, INR 79 |
| 09 | Aurora | Legendary | Premium, INR 99 |

Every category has its own artwork variants rather than relying entirely on recoloring one image. Preview cropping isolates the item. All, Normal, Rare, Epic, and Legendary filters narrow browsing. Owned and equipped states are distinct; an equipped item has a strong border/highlight and explicit label. Category and filter buttons also indicate selection.

Preview reveal animations scale in strength with rarity and respect Reduce Motion. Runtime cosmetic effects are a presentation layer and do not change collision rules, force, energy, timer, or score.

### Premium product IDs

Cosmetics are Unity IAP **non-consumables**. IDs use `otb_<category>_skin_<index>`, with categories `ball`, `magnet`, `hole`, `walls`, `glass`, and `background`, and premium indices `04`–`09`. This produces 36 cosmetic products. The full exact list appears in the project inventory.

### Gem packs

Gem packs are repeatable Unity IAP **consumables**, separate from permanent cosmetic entitlements.

| Product ID | Pack | Gems | India fallback display price |
| --- | --- | --- | --- |
| `otb_gems_50` | Pocket Gems | 50 | INR 10 |
| `otb_gems_150` | Gem Pouch | 150 | INR 29 |
| `otb_gems_500` | Gem Chest | 500 | INR 89 |
| `otb_gems_1000` | Gem Vault | 1,000 | INR 169 |
| `otb_gems_1500` | Gem Treasury | 1,500 | INR 229 |
| `otb_gems_3000` | Gem Hoard | 3,000 | INR 399 |

The combined catalog has **42 configured products: 36 cosmetics and six gem packs**. Product IDs must match Play Console exactly. Store-returned localized prices take priority over fallback text; changing a fallback price in JSON does not change Google's checkout price.

### Purchase delivery

`CosmeticIapService` fetches products and owned purchases, handles pending purchases, reapplies cosmetic entitlements after profile loading, and exposes localized prices and status to the UI. Gem purchasing requires a ready signed-in cloud profile and enabled gem/cloud settings.

Gem delivery derives the quantity from the stable product ID, checks the purchase's account association where available, and records a hashed transaction marker with the wallet update. It saves progress to Cloud Save before confirming the store purchase. Unconfirmed pending orders retry without re-crediting an already recorded transaction.

**Production gap:** duplicate protection and cloud persistence are implemented client safeguards. The project still needs server-side receipt validation and an authoritative wallet for hardened public paid-currency operation. Cloud Save written by the client is not a substitute for that backend.

## 10. Advertising and consent

Unity LevelPlay is the game's advertising gateway for banners, rewarded ads, and interstitials. A banner supplied by Google AdMob is still requested through LevelPlay mediation. Unity Ads and ironSource/ISX are other configured mediation sources; network eligibility and live fill remain dashboard-controlled. The game does not need a second direct AdMob request loop for the same banner.

### Formats and placements

| Format or placement | Use |
| --- | --- |
| Banner / `global_banner` | Persistent banner in the configured screen-edge region |
| Interstitial / `level_complete` | Completion-boundary interstitial |
| Rewarded / `free_gems` | Optional free gems |
| Rewarded / `level_revive` | Optional timer revive |
| Rewarded / `energy_revive` | Optional energy recovery |

The current interstitial cadence is **every two completions**, with `interstitialMinimumSeconds = 0`. This is an attempt-to-show rule: an ad must be ready, ads enabled, no interstitial already showing, and placement capping must allow it. No-fill, offline conditions, or dashboard caps can prevent an ad from appearing exactly on the second clear. The counter resets when an interstitial displays.

Rewarded benefits are granted through the successful reward callback, not merely because the player opened or closed an ad. The game configuration controls gem and revive benefits; a dashboard reward label alone does not rewrite those values. This matters where a console placement says 50 gems but `rewardedCoinAmount` is 10.

### Banner layout

The checked-in banner position is BottomCenter, safe-area respect is enabled, layout padding is 4 dp, and adaptive size is currently disabled. The service converts native display density and banner height into the Unity screen space, including reduced render resolution. `SafeAreaFitter`, responsive canvas scaling, the HUD bottom stack, and board fitting reserve that region so controls and the board do not sit beneath native ads.

### Consent, tests, and initialization

`GoogleUmpConsent` updates consent information, displays a required form, and exposes privacy choices from Settings. LevelPlay initialization and ad requests are coordinated with the consent result. Child-directed and do-not-sell flags are separate configurable inputs and must match the declared audience and actual policy choices.

Editor ad simulation is enabled in the local payload. It is not proof of device ad serving. Development integration validation, adapter logging switches, dashboard test devices, and per-network test tools support device checks. A closed-test release should use proper tester devices/accounts; test mode and live serving are not inferred from whether Unity's Development Build checkbox is selected.

## 11. Firebase systems and Remote Config

### Service responsibilities

| Service | Responsibility in this project |
| --- | --- |
| Firebase Analytics | Screen, button, gameplay, economy, ad, account, and service events |
| Firebase Remote Config | Deliver one JSON live configuration payload |
| Firebase Cloud Messaging | Receive server/console push notifications |
| Native Android reminders | Schedule local return and gift reminders using packaged artwork |
| Unity Authentication | Player identity and linked account providers |
| Unity Cloud Save | Player progress snapshots |
| Unity Leaderboards | Performance, campaign stars, and endless depth boards |

Firebase Authentication, Firestore, Realtime Database, and a Firebase-hosted wallet are not implemented identity/progress stores in this project. The Google sign-in flow links Play Games to Unity Authentication. Native Firebase Android dependencies provide Analytics, Config, and Messaging; reflection also allows compatible Firebase Unity SDK types where installed.

### Live configuration lifecycle

The Firebase parameter is the string key `game_config_json`. Its value is the JSON payload, currently version 13. The project contains:

- `Assets/OutsideTheBox/Resources/OutsideTheBox/Configs/LiveOpsConfig.asset` for authored defaults.
- `Assets/OutsideTheBox/Resources/OutsideTheBox/default_live_config.json` for packaged JSON fallback.
- `Assets/OutsideTheBox/Documentation/FirebaseRemoteConfigValue.json` for console publishing.
- `LiveOpsConfigData` in `LiveOpsServices.cs` for schema, code defaults, and normalization.

Startup loads local defaults and a last-valid cached payload into a runtime-only ScriptableObject. Firebase fetch/activation can replace those values. Missing fields in older payloads are overlaid on defaults, and values are normalized. Invalid payloads keep the last usable configuration. The runtime cache key is `otb_remote_live_config_v1`. Remote application does not rewrite the authored asset on disk.

The native Android fetch interval is currently 60 minutes, with a 12-second fetch timeout. Applying a payload refreshes applicable service configuration, analytics, player settings, and push registration. Some SDK construction settings or already-created content can require a fresh level or application initialization; not every arbitrary field is an instant hot swap.

The schema covers timers, energy, rewards, IAP catalog entries, gifts, reminders, mechanics, FTUE, UI motion, transitions, mobile performance, ads, Firebase, authentication, cloud progress, leaderboards, and connection checks. [Remote Config reference](REMOTE_CONFIG_REFERENCE.md) lists every exported field and value.

## 12. Analytics and local telemetry

`AnalyticsService` is the central Firebase event gateway. Events include common context: `screen`, `level`, `chapter`, `mode`, `attempt`, `config_version`, and `test_build`. Event-specific parameters add time, energy, stars, score, product, placement, or action details as appropriate.

`AnalyticsButtonTracker` attaches a click listener to bound buttons and logs `button_click` with a hierarchy-based `button_id` and `source_screen`. Screens use `screen_view`. Explicit events cover launch, loading, level starts and ends, tutorials, dragging, polarity, echo, hints, undo, restart, revives, stores, currency, IAP, ads, authentication, cloud sync, connectivity, settings, and quitting. The [event catalog](ANALYTICS_EVENTS.md) identifies the exact literal event names and source locations; conditional event names are listed separately there.

The gateway limits parameter count, bounds strings, rejects non-finite numbers, and filters sensitive parameter keys such as passwords, tokens, receipts, usernames, and email. The user ID is the opaque Unity player ID. Input field contents are not button IDs. Editor play mode writes analytics previews to the Console rather than the Android production stream.

`LocalTelemetryService` separately records attempts, completions, restarts, hints, collisions, and play time in saved telemetry. The Player Balance Report editor window uses this data. Local telemetry is not a remote analytics dashboard.

## 13. Authentication, cloud progress, and saving

### Accounts

Primary implemented account flows are Google Play Games and Unity username/password. Google Play returns a server authorization code that Unity Authentication uses for sign-in or linking. Username/password accounts support sign-up, login, and eligible account upgrades. Validation currently allows usernames of 3–20 characters and passwords of 8–30 with upper case, lower case, a number, and a symbol.

Session restoration uses Unity's cached authentication session. Anonymous restoration and guest-related flags exist, but those flags alone should not be treated as proof of a separate exposed new-guest sign-in flow. The Editor Google Play test-login branch creates a Unity test identity; it is not an Android Google Play Games authentication test.

Sign-out/account switching and account deletion have explicit flows. Real Android sign-in depends on package name, OAuth clients, correct signing certificate SHA-1, Play Games credentials, the server client configuration, and applicable tester/publication access.

### Save model

Local progress is serialized under PlayerPrefs key `otb_save_v1`. It includes unlocked/last levels, stars and best results, endless and daily progress, wallet, cosmetic choices and entitlements, gifts, transaction markers, achievements, settings, learned tutorials, hints, suspended run state, and local telemetry.

Unity Cloud Save stores the profile JSON at `player_progress_v1`. `UnityPlayerService` explicitly initializes Unity services in **production**, independent of which environment is selected in the Editor dashboard. `CloudProgressWriteQueue` serializes profile writes and prevents writes for the wrong or not-yet-loaded player. Account transitions and cloud-sync presentation protect against overwriting a newly loaded profile with an old local one.

Deferred local writes reduce unnecessary flushing. Application pause, quit, completion, and purchase boundaries have save/flush handling. Suspended-run restoration checks mode and content revision before applying attempt state.

### Local achievements

Implemented local achievement checks include first pocket, a three-star performance result, daily solve, endless depth ten, chapter completion, and collecting 90 campaign stars. These are saved achievement IDs and in-game feedback; do not assume they are automatically published Google Play Games achievements.

## 14. Notifications and return engagement

### Local automated reminders

New player settings default notifications to on. Android's operating-system permission still requires player approval where applicable, and a player's explicit opt-out remains respected. Local scheduling also requires enabled reminder settings and a signed-in Unity player.

Return reminders use a 24-hour interval and a saved rotation through 14 distinct messages. Each has packaged title/artwork metadata. A cursor advances after a delivered non-gift reminder and wraps around the catalog. Gift reminders use separate messaging for unclaimed chapter gifts and do not consume the next general quote.

Scheduling adjusts early/late notification times into a daytime window: a calculated time before 09:00 moves to 10:00 that day; a time at or after 21:00 moves to 10:00 the next day. Native handling reschedules after delivery and supports reboot/package-replacement restoration. There is no catch-up burst for every missed notification.

`GameReminderReceiver`, `ReminderPolicy`, and `NotificationArtwork` implement this path. Images are packaged under the notification Android library's assets, with a JSON catalog. The recurring local campaign does **not** require 14 Firebase Console campaigns or hosted image downloads.

Android permission denial, force-stop, battery restrictions, notification settings, and device scheduling policy can prevent or delay delivery. “Not currently running” is compatible with scheduled local delivery; an OS force-stop is a different condition.

### Firebase push

`GamePushMessagingService` receives FCM notifications and supports token registration plus topic subscription. The configured topic is `otb_updates`; development-only token logging assists device tests. Firebase Console can send a test to a device token or a campaign to an appropriate audience/topic.

Console push content is whatever was sent; it does not automatically rotate the local 14-message catalog. Rich remote notification images need a reachable image source and the relevant delivery handling. Firebase Storage/Hosting is not required for the packaged local reminder artwork. No Cloud Functions backend or recurring server campaign scheduler is implemented here.

## 15. UI, art, animation, and audio

### Presentation and prefabs

The style combines a blue game backdrop, glass chambers, metallic balls, red/blue magnets, cyan field effects, dark readable information cards, and gold action accents. Category-specific cosmetic images, a store rarity palette, shared mechanic symbols, and graphic UI icons provide consistent visual language.

The project has 56 authored prefabs referenced through `RuntimePrefabRegistry`. Gameplay objects, screens, service modals, gem cards, and mechanic markers are instantiated from those assets. UI controllers still bind listeners, update text and state, and compose some reusable store/control elements from UI prefabs. “Prefab-based” does not mean there is no runtime layout or composition code.

Responsive canvas scaling, device safe-area fitting, native banner reservations, minimum control sizes, and a bottom-row layout system adapt the HUD. The board camera fits between upper HUD and lower controls. Font glyph limitations are avoided for symbols such as stars and pause icons by using graphics. `MechanicInk` removes unwanted dark backgrounds from mechanic icon art.

### Transitions and the pipe cinematic

Standard screen transitions support slide, fade, scale, direction, duration, and easing profiles from live configuration. Tutorial and system dialogs animate their content separately from their screen-sized input blockers. Loading overlays cover startup, level building, and return-to-menu work.

The approved experimental 3D pipe transition is enabled in the current GameConfig asset. It presents a tilted board and a 3D pipe while gameplay remains 2D. Completion drops through the pocket toward a pipe below; level entry arrives from above. Pipes extend to the screen edge. The level-complete screen separates the outgoing and incoming sequences. Menu Play also has an arrival sequence, and the board is hidden until that sequence is ready to prevent a one-frame reveal.

`previewPipeLevelTransitions` can disable the experiment, and `pipeTransitionPreviewStyle` selects the 2D or 3D presentation. Reduce Motion bypasses it. `LevelPipeTransition3D`, `LevelBoardDiorama`, `PipePreviewMeshes`, and the transition prefabs isolate this feature from core puzzle rules.

### Audio and haptics

`FeedbackService` manages separate music and effects sources, move/collision/polarity/win sounds, chapter music, and Android vibration. Assigned clips take priority. Missing effects can use generated sweeps/success audio, and missing chapter music can use cached procedural ambient loops while the fallback is enabled.

Procedural music fallback is currently enabled in GameConfig. The release validator still calls out replacing fallback audio with final licensed audio; assigning final clips and checking their import/volume settings remains a content task. Settings independently mute music, sound effects, and vibration.

## 16. Performance and technical architecture

`MobilePerformanceController` offers Auto, Battery Saver, and Smooth modes. Hardware memory, GPU memory, processor count, low-memory events, and sampled frame performance influence tier and rendering choices. Live configuration controls frame targets, physics tick rate, quality levels, initial/minimum render scales, scale steps, and slow/recovery thresholds.

The snapshot targets 30 FPS for battery mode and 60 FPS for balanced/smooth, with a 60 Hz physics tick. Render scale can decrease during sustained slow frames and recover after a longer stable period. Reduced rendering resolution is accounted for when reserving native ad space. Cached art/materials, reused preview transforms, bounded mechanic samples/trails, and deferred saves reduce recurring work.

These mechanisms do not guarantee every Android device meets its target. Profile physical devices with ads, the 3D transition, notifications, long sessions, and account/purchase flows active.

### Runtime responsibility map

| Layer | Main components |
| --- | --- |
| Bootstrap and attempt state | `GameAutoBootstrap`, `GameSession`, `GameSessionPipePreview` |
| Data and rules | `GameConfig`, `LevelDefinition`, `LevelCatalog`, `GameRules`, rail rules |
| Generation and planning | `LevelSolver`, `FocusedLevelGenerator`, `LevelMechanicLayout`, `ChapterMechanicPlan`, rail generator |
| Board and input | `BoardView` partials, `MagnetInput`, rail and mechanic markers |
| UI | `GameUIController` partials, HUD/card views, safe-area and responsive components |
| Services | Live Ops, analytics, ads, Unity player service, saves, IAP, consent, feedback |
| Native Android | Firebase resources, FCM receiver, local reminder policy/artwork, manifests and Gradle integration |

The source and dependency inventory is provided in [Project inventory](PROJECT_INVENTORY.md). No gameplay AI agent, generative-AI runtime API, or model inference service is implemented. Procedural generation, deterministic solving, and generated fallback audio are local code systems. Art provenance/licensing should be tracked separately from the runtime feature inventory.

## 17. Tools and content pipeline

The Unity menu **Tools → Outside The Box** exposes these commands:

| Tool | Purpose |
| --- | --- |
| Build Project Content | Broad content/bootstrap generation |
| Rebuild Prefab Library | Regenerate reusable prefab assets |
| Generate Firebase Android Resources | Read Firebase Android configuration and create native resources |
| Configure Release Build Flags | Set non-development Android App Bundle flags |
| Validate Android Release | Inspect release settings, prefab bindings, content, and known owner tasks |
| Player Balance Report | Inspect saved attempts, failures, hints, completion, and balance telemetry |
| Export Firebase Remote Config JSON | Export the local live configuration for Firebase |
| Resolve Android Live Ops Dependencies | Run Android dependency resolution |
| Validate Live Ops Setup | Inspect local service configuration and integration prerequisites |

`GeneratedArtImporter` standardizes generated art imports. `EditorPerformanceSetup` manages development-time performance setup. The Android launcher build guard validates the exported launcher activity. Firebase resource generation and Gradle/dependency templates support the native integrations.

Broad generation tools can rewrite authored content. They should not be used as a routine fix for a manually tuned screen. Preserve Inspector changes and validate a targeted prefab change before regenerating entire libraries.

Existing content includes the campaign assets, live configuration assets/JSON, English strings, cosmetic and gift art, mechanic icons, materials/shaders, notification artwork/catalog, and Google Play release artwork. `ReleaseAssets/GooglePlay` contains icon variants, the 1024 × 500 feature graphic, and listing text.

## 18. Console ownership and deployment dependencies

| Console | Required configuration and verification |
| --- | --- |
| Google Play Console | Testing tracks/tester access, signing, 42 matching one-time products and active purchase options, prices/regions, license testers, listing, audience, ads, data declarations |
| Play Games Services | Matching Android/server credentials and signing certificate, tester/publication access, package association |
| Google Cloud Auth Platform | Android and server OAuth clients, consent branding, authorized website and ownership verification when requested |
| Unity Dashboard | Correct project, production environment, account providers, Cloud Save access, and all nine leaderboard IDs |
| Firebase Console | Matching Android app/config, Analytics stream, published `game_config_json`, FCM test/campaign configuration |
| Unity LevelPlay | App/ad-unit mapping, placements, network instances, mediation groups, capping/pacing, consent integration, test devices |
| Unity Ads Monetization | Correct Android Game ID and bidding placement IDs linked to this game's LevelPlay instances |
| Google AdMob | Correct app/banner unit, partner-bidding configuration when mediated through LevelPlay, app/store association, approval/app-ads.txt and privacy-message setup as applicable |

A published Play Games configuration, OAuth production status, a closed Play Store track, and ad-network approval are different states. One does not establish the others. Development signing and Google Play App Signing can use different certificates, so working sideload sign-in does not alone prove the Play Store build is configured correctly.

No console was changed while producing this GDD. Previously completed setup work should still be checked against the actual build, account, product IDs, and environment before a new rollout.

## 19. Release status and remaining work

Implemented systems include the campaign/daily/endless loops, focused generation, all listed mechanic families, first-encounter tutorials, results/stars, economy, cosmetics and gifts, gem purchasing, ads and consent integration, telemetry, Firebase services, Unity account/cloud/leaderboard integration, connectivity/quit dialogs, responsive HUD, and the reversible pipe presentation.

Keep the following distinctions visible during release planning:

- Server-side purchase validation and a server-authoritative gem wallet are still required for hardened public paid-currency use.
- Strict one-attempt-per-day gating is not enforced by the current Daily Challenge entry flow.
- Final licensed audio remains a task while procedural fallback is enabled and final clips are not supplied.
- A provider SDK, configured product ID, or active local flag does not prove live console approval, fill, entitlement restoration, or a successful real-device transaction.
- Guest-related configuration, localization infrastructure, and local achievement IDs do not imply a fully exposed guest account flow, multiple translated languages, or published platform achievements.
- Android is the implemented native service target. An equivalent iOS identity, IAP, APNs, consent, and notification configuration is not established by these Android integrations.
- Client-generated scores and client-written progress are not authoritative anti-cheat systems.

For a release candidate, verify sign-in from a Play-installed build, cloud restore/account switching, gameplay and goal entry in all mechanic families, the smallest supported screen with banners, a timer and energy failure/revive, licensed test purchases and duplicate retries, consent/privacy options, push and local reminder behavior, and the intended release flags. This is an acceptance checklist, not a claim that the checks have been performed for the next build.

## 20. Reference appendices

- [Remote Config reference](REMOTE_CONFIG_REFERENCE.md): complete field/value snapshot.
- [Analytics event catalog](ANALYTICS_EVENTS.md): source-linked event inventory and contextual parameters.
- [Project inventory](PROJECT_INVENTORY.md): runtime/editor/native files, prefab names, package versions, menu commands, release assets, and full IAP ID list.

The PDF edition includes these appendices so its recipients can read the complete snapshot without opening the Unity project.
