# Outside the Box analytics event catalog

Snapshot: 23 November 2026. This catalog locates literal event calls in the runtime sources. Each call site defines its event-specific parameters; a call site can emit zero or many times during a session. It does not mean an event has arrived in a deployed Firebase project.

## Collection and common context

The [AnalyticsService gateway](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs) adds `screen`, `level`, `chapter`, `mode`, `attempt`, `config_version`, and `test_build`. Collection is controlled by `analyticsEnabled`. Editor play mode prints previews; Android uses Firebase Analytics via compatible Unity SDK types or the native bridge.

The opaque Unity player ID can be set as the analytics user ID. Password/token/receipt keys and username/email/auth-code/advertising-ID keys are filtered. Parameters are bounded to 25 total, strings to 100 characters, and numeric NaN/infinity values are excluded. Do not add raw input contents to button IDs or event values.

## Button and screen coverage

[AnalyticsButtonTracker](../Assets/OutsideTheBox/Runtime/UI/AnalyticsButtonTracker.cs) logs `button_click` for buttons passed through the game binding helpers. Parameters are `button_id` (hierarchy path) and `source_screen`. New buttons must use those helpers or explicitly bind the tracker. Screen transitions call `AnalyticsService.Screen`, which emits `screen_view` only when the screen name changes.

## Literal event names

| Event | Emission source |
| --- | --- |
| `account_delete_failed` | [GameSession.cs:1089](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L1089) |
| `account_delete_requested` | [GameSession.cs:1067](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L1067) |
| `account_deleted` | [UnityPlayerService.cs:209](../Assets/OutsideTheBox/Runtime/Services/UnityPlayerService.cs#L209) |
| `account_linked` | [UnityPlayerService.cs:108](../Assets/OutsideTheBox/Runtime/Services/UnityPlayerService.cs#L108) |
| `ad_clicked` | [LiveOpsServices.cs:1378](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs#L1378); [LiveOpsServices.cs:1380](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs#L1380); [LiveOpsServices.cs:1384](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs#L1384) |
| `ad_load_failed` | [LiveOpsServices.cs:1422](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs#L1422); [LiveOpsServices.cs:1450](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs#L1450); [LiveOpsServices.cs:1468](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs#L1468) |
| `ads_blocked_by_consent` | [LiveOpsServices.cs:1096](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs#L1096) |
| `ads_init_failed` | [LiveOpsServices.cs:1296](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs#L1296) |
| `ads_initialized` | [LiveOpsServices.cs:1289](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs#L1289) |
| `app_exit` | [GameSession.cs:1578](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L1578) |
| `app_liveops_ready` | [LiveOpsManager.cs:41](../Assets/OutsideTheBox/Runtime/Services/LiveOpsManager.cs#L41) |
| `app_start` | [LiveOpsManager.cs:34](../Assets/OutsideTheBox/Runtime/Services/LiveOpsManager.cs#L34) |
| `banner_loaded` | [LiveOpsServices.cs:1461](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs#L1461) |
| `banner_shown` | [LiveOpsServices.cs:1382](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs#L1382) |
| `button_click` | [AnalyticsButtonTracker.cs:34](../Assets/OutsideTheBox/Runtime/UI/AnalyticsButtonTracker.cs#L34) |
| `chapter_gift_available` | [SaveService.cs:136](../Assets/OutsideTheBox/Runtime/Services/SaveService.cs#L136) |
| `chapter_gift_claimed` | [SaveService.cs:148](../Assets/OutsideTheBox/Runtime/Services/SaveService.cs#L148) |
| `cloud_progress_load_failed` | [GameSession.cs:303](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L303) |
| `cloud_progress_loaded` | [GameSession.cs:294](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L294) |
| `cloud_progress_save_failed` | [UnityPlayerService.cs:258](../Assets/OutsideTheBox/Runtime/Services/UnityPlayerService.cs#L258) |
| `consent_ready` | [GoogleUmpConsent.cs:160](../Assets/OutsideTheBox/Runtime/Services/GoogleUmpConsent.cs#L160) |
| `cosmetic_equipped` | [GameUIControllerStore.cs:757](../Assets/OutsideTheBox/Runtime/UI/GameUIControllerStore.cs#L757) |
| `cosmetic_purchase_rejected` | [GameUIControllerStore.cs:724](../Assets/OutsideTheBox/Runtime/UI/GameUIControllerStore.cs#L724); [GameUIControllerStore.cs:735](../Assets/OutsideTheBox/Runtime/UI/GameUIControllerStore.cs#L735) |
| `cosmetic_unlocked` | [GameUIControllerStore.cs:750](../Assets/OutsideTheBox/Runtime/UI/GameUIControllerStore.cs#L750) |
| `currency_granted` | [LiveOpsServices.cs:713](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs#L713) |
| `earn_virtual_currency` | [CosmeticIapService.cs:429](../Assets/OutsideTheBox/Runtime/Services/CosmeticIapService.cs#L429); [LiveOpsServices.cs:722](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs#L722) |
| `echo_recording_finished` | [GameSession.cs:715](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L715); [GameSession.cs:1360](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L1360) |
| `echo_recording_started` | [GameSession.cs:1353](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L1353) |
| `echo_toggled` | [GameSession.cs:726](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L726) |
| `endless_leaderboard` | [LiveOpsServices.cs:1020](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs#L1020) |
| `energy_warning` | [GameUIController.cs:459](../Assets/OutsideTheBox/Runtime/UI/GameUIController.cs#L459) |
| `force_mode_changed` | [GameSession.cs:701](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L701) |
| `hint_used` | [GameSession.cs:778](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L778) |
| `iap_confirmed` | [CosmeticIapService.cs:248](../Assets/OutsideTheBox/Runtime/Services/CosmeticIapService.cs#L248) |
| `iap_deferred` | [CosmeticIapService.cs:278](../Assets/OutsideTheBox/Runtime/Services/CosmeticIapService.cs#L278) |
| `iap_delivery_waiting` | [CosmeticIapService.cs:446](../Assets/OutsideTheBox/Runtime/Services/CosmeticIapService.cs#L446) |
| `iap_entitlement_restored` | [CosmeticIapService.cs:334](../Assets/OutsideTheBox/Runtime/Services/CosmeticIapService.cs#L334) |
| `iap_failed` | [CosmeticIapService.cs:265](../Assets/OutsideTheBox/Runtime/Services/CosmeticIapService.cs#L265) |
| `iap_init_failed` | [CosmeticIapService.cs:209](../Assets/OutsideTheBox/Runtime/Services/CosmeticIapService.cs#L209) |
| `iap_pending` | [CosmeticIapService.cs:291](../Assets/OutsideTheBox/Runtime/Services/CosmeticIapService.cs#L291) |
| `iap_products_failed` | [CosmeticIapService.cs:352](../Assets/OutsideTheBox/Runtime/Services/CosmeticIapService.cs#L352) |
| `iap_products_loaded` | [CosmeticIapService.cs:220](../Assets/OutsideTheBox/Runtime/Services/CosmeticIapService.cs#L220) |
| `iap_requested` | [CosmeticIapService.cs:114](../Assets/OutsideTheBox/Runtime/Services/CosmeticIapService.cs#L114) |
| `iap_restore_failed` | [CosmeticIapService.cs:360](../Assets/OutsideTheBox/Runtime/Services/CosmeticIapService.cs#L360) |
| `iap_store_disconnected` | [CosmeticIapService.cs:345](../Assets/OutsideTheBox/Runtime/Services/CosmeticIapService.cs#L345) |
| `iap_unavailable` | [CosmeticIapService.cs:100](../Assets/OutsideTheBox/Runtime/Services/CosmeticIapService.cs#L100) |
| `internet_retry` | [GameUIControllerSystemDialogs.cs:142](../Assets/OutsideTheBox/Runtime/UI/GameUIControllerSystemDialogs.cs#L142) |
| `interstitial_closed` | [LiveOpsServices.cs:1436](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs#L1436) |
| `interstitial_failed` | [LiveOpsServices.cs:1444](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs#L1444) |
| `interstitial_requested` | [LiveOpsServices.cs:1189](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs#L1189) |
| `interstitial_shown` | [LiveOpsServices.cs:1431](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs#L1431) |
| `leaderboard_failed` | [GameSession.cs:1223](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L1223) |
| `leaderboard_loaded` | [GameSession.cs:1215](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L1215) |
| `leaderboard_requested` | [GameSession.cs:1208](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L1208) |
| `leaderboard_score` | [LiveOpsServices.cs:1013](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs#L1013) |
| `leaderboard_submit_failed` | [UnityPlayerService.cs:324](../Assets/OutsideTheBox/Runtime/Services/UnityPlayerService.cs#L324) |
| `leaderboard_submitted` | [UnityPlayerService.cs:320](../Assets/OutsideTheBox/Runtime/Services/UnityPlayerService.cs#L320) |
| `level_end` | [GameSession.cs:639](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L639); [GameSession.cs:1495](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L1495) |
| `level_energy_depleted` | [GameSession.cs:1310](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L1310) |
| `level_restart` | [GameSession.cs:758](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L758) |
| `level_revived` | [GameSession.cs:1262](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L1262) |
| `level_start` | [GameSession.cs:620](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L620) |
| `level_suspended` | [GameSession.cs:906](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L906); [GameSession.cs:921](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L921) |
| `level_timeout` | [GameSession.cs:1286](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L1286) |
| `login` | [UnityPlayerService.cs:91](../Assets/OutsideTheBox/Runtime/Services/UnityPlayerService.cs#L91); [UnityPlayerService.cs:154](../Assets/OutsideTheBox/Runtime/Services/UnityPlayerService.cs#L154); [UnityPlayerService.cs:177](../Assets/OutsideTheBox/Runtime/Services/UnityPlayerService.cs#L177); [UnityPlayerService.cs:185](../Assets/OutsideTheBox/Runtime/Services/UnityPlayerService.cs#L185) |
| `login_failed` | [GameSession.cs:1006](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L1006); [GameSession.cs:1117](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L1117) |
| `login_requested` | [GameSession.cs:988](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L988); [GameSession.cs:1106](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L1106) |
| `logout` | [UnityPlayerService.cs:197](../Assets/OutsideTheBox/Runtime/Services/UnityPlayerService.cs#L197) |
| `navigation_failed` | [GameSession.cs:372](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L372) |
| `notifications_preference` | [SaveService.cs:183](../Assets/OutsideTheBox/Runtime/Services/SaveService.cs#L183) |
| `obstacle_hit` | [GameSession.cs:1396](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L1396) |
| `play_games_login` | [LiveOpsServices.cs:990](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs#L990) |
| `quit_cancelled` | [GameUIControllerSystemDialogs.cs:181](../Assets/OutsideTheBox/Runtime/UI/GameUIControllerSystemDialogs.cs#L181) |
| `quit_confirmation_opened` | [GameUIControllerSystemDialogs.cs:170](../Assets/OutsideTheBox/Runtime/UI/GameUIControllerSystemDialogs.cs#L170) |
| `quit_confirmed` | [GameUIControllerSystemDialogs.cs:191](../Assets/OutsideTheBox/Runtime/UI/GameUIControllerSystemDialogs.cs#L191) |
| `rail_challenge_notice` | [GameSession.cs:1324](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L1324) |
| `remote_config_applied` | [LiveOpsManager.cs:160](../Assets/OutsideTheBox/Runtime/Services/LiveOpsManager.cs#L160) |
| `rewarded_capped` | [LiveOpsServices.cs:1149](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs#L1149) |
| `rewarded_closed` | [LiveOpsServices.cs:1394](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs#L1394) |
| `rewarded_failed` | [LiveOpsServices.cs:1415](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs#L1415) |
| `rewarded_requested` | [LiveOpsServices.cs:1133](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs#L1133); [LiveOpsServices.cs:1143](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs#L1143) |
| `rewarded_shown` | [LiveOpsServices.cs:1376](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs#L1376) |
| `rewarded_unavailable` | [LiveOpsServices.cs:1138](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs#L1138) |
| `screen_view` | [LiveOpsServices.cs](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs) |
| `select_item` | [GameUIControllerGems.cs:141](../Assets/OutsideTheBox/Runtime/UI/GameUIControllerGems.cs#L141); [GameUIControllerStore.cs:711](../Assets/OutsideTheBox/Runtime/UI/GameUIControllerStore.cs#L711) |
| `session_restore_failed` | [UnityPlayerService.cs:75](../Assets/OutsideTheBox/Runtime/Services/UnityPlayerService.cs#L75) |
| `session_restored` | [UnityPlayerService.cs:70](../Assets/OutsideTheBox/Runtime/Services/UnityPlayerService.cs#L70) |
| `setting_changed` | [GameUIController.cs:2733](../Assets/OutsideTheBox/Runtime/UI/GameUIController.cs#L2733); [GameUIController.cs:2743](../Assets/OutsideTheBox/Runtime/UI/GameUIController.cs#L2743) |
| `sign_up` | [UnityPlayerService.cs:114](../Assets/OutsideTheBox/Runtime/Services/UnityPlayerService.cs#L114) |
| `sign_up_failed` | [GameSession.cs:1133](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L1133); [GameSession.cs:1161](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L1161) |
| `sign_up_requested` | [GameSession.cs:1140](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L1140) |
| `spend_virtual_currency` | [LiveOpsServices.cs:731](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs#L731) |
| `startup_presentation_timeout` | [GameUIController.cs:161](../Assets/OutsideTheBox/Runtime/UI/GameUIController.cs#L161) |
| `startup_services_failed` | [GameSession.cs:245](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L245) |
| `store_category_selected` | [GameUIControllerGems.cs:61](../Assets/OutsideTheBox/Runtime/UI/GameUIControllerGems.cs#L61); [GameUIControllerStore.cs:423](../Assets/OutsideTheBox/Runtime/UI/GameUIControllerStore.cs#L423) |
| `store_filter_selected` | [GameUIControllerStore.cs:578](../Assets/OutsideTheBox/Runtime/UI/GameUIControllerStore.cs#L578) |
| `store_opened` | [GameUIControllerGems.cs:41](../Assets/OutsideTheBox/Runtime/UI/GameUIControllerGems.cs#L41) |
| `timer_milestone` | [GameUIController.cs:647](../Assets/OutsideTheBox/Runtime/UI/GameUIController.cs#L647) |
| `timer_warning` | [GameUIController.cs:648](../Assets/OutsideTheBox/Runtime/UI/GameUIController.cs#L648) |
| `tutorial_begin` | [GameUIController.cs:844](../Assets/OutsideTheBox/Runtime/UI/GameUIController.cs#L844) |
| `tutorial_complete` | [GameUIController.cs:892](../Assets/OutsideTheBox/Runtime/UI/GameUIController.cs#L892) |
| `tutorial_first_drag` | [GameSession.cs:1333](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L1333) |
| `tutorial_step_complete` | [GameUIController.cs:878](../Assets/OutsideTheBox/Runtime/UI/GameUIController.cs#L878) |
| `tutorial_step_view` | [GameUIController.cs:913](../Assets/OutsideTheBox/Runtime/UI/GameUIController.cs#L913) |
| `undo_used` | [GameSession.cs:732](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L732) |
| `unlock_achievement` | [GameSession.cs:1554](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs#L1554) |

## Conditional event names

Some calls choose the event name from state instead of a single string literal. These families need to be considered alongside the literal list. See the linked sources for their selection conditions.

| Family | Source |
| --- | --- |
| `internet_restored`, `internet_lost` | [GameUIControllerSystemDialogs.cs](../Assets/OutsideTheBox/Runtime/UI/GameUIControllerSystemDialogs.cs) |
| `rewarded_completed`, `rewarded_not_completed` | [LiveOpsServices.cs](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs) |
| `app_background`, `app_foreground` | [GameSession.cs](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs) |

`popup_open` and `popup_closed` are emitted for pause presentation by [GameUIController.cs](../Assets/OutsideTheBox/Runtime/UI/GameUIController.cs). Calls through the TrackRun and TrackOrder helpers are included in the literal table where their caller passes a fixed event name. The literal table is a source index rather than a guarantee that every dynamically constructed event name is enumerated. To extend the catalog, inspect `AnalyticsService.Log` calls whose first argument is an expression.

## Local balance telemetry

[LocalTelemetryService](../Assets/OutsideTheBox/Runtime/Services/LocalTelemetryService.cs) maintains saved attempts, completions, restarts, hints, collisions and play-time counters. These are consumed by the Player Balance Report. They are separate from the Firebase event stream and do not prove remote ingestion.

## Suggested analysis questions

- Which level or FTUE step loses the most players? Correlate starts, tutorial step views/completions, level ends and exits.
- Does the 70/30 rating produce the intended difficulty? Compare elapsed time, energy, stars, hints and revives by level and config version.
- Are store items confusing? Compare category/filter clicks, selections, purchase requests, errors and delivered entitlements.
- Are rewarded ads completing and delivering benefits? Compare requests, rewards, closes and failures by placement.
- Do account/cloud issues block play? Compare sign-in, restore, sync failure and recovery events.
- Do new settings improve performance? Use config_version and build context with device profiling; analytics alone is not a frame profiler.
