# Outside the Box Remote Config reference

Snapshot: 23 November 2026. Source: [FirebaseRemoteConfigValue.json](../Assets/OutsideTheBox/Documentation/FirebaseRemoteConfigValue.json).

This is the complete checked-in export, not a read of the currently published Firebase Console value. Firebase parameter: `game_config_json`. LiveOpsConfigData normalization can clamp values, and individual consumers determine when a setting takes effect.

Unity uses the authored [LiveOpsConfig.asset](../Assets/OutsideTheBox/Resources/OutsideTheBox/Configs/LiveOpsConfig.asset), packaged [default_live_config.json](../Assets/OutsideTheBox/Resources/OutsideTheBox/default_live_config.json), cached valid payloads, and then a successful remote fetch. See [GDD section 11](OutsideTheBox_GDD.md#11-firebase-systems-and-remote-config) for precedence and application behavior.

Values named Coins refer to the gem wallet. Client app keys, ad-unit IDs, topic names and product IDs are configuration identifiers; private service-account keys, signing passwords and OAuth client secrets do not belong in this document.

Arrays retain their original order. Chapter timer/energy arrays use chapters 1–6 followed by Endless/Daily; cosmetic arrays use standard skin indices 00–09, except the flattened per-category product ID array. Product prices here are fallback display values, not store checkout price controls.

## Payload

| Field | Type | Snapshot value |
| --- | --- | --- |
| `version` | number | `13` |

## Timer and revives

| Field | Type | Snapshot value |
| --- | --- | --- |
| `timersEnabled` | boolean | `true` |
| `baseLevelSeconds` | number | `32.0` |
| `chapterLevelSeconds` | array | `[40,45,50,55,60,65,70]` |
| `secondsPerSolutionBeat` | number | `4.5` |
| `minimumLevelSeconds` | number | `45.0` |
| `maximumLevelSeconds` | number | `180.0` |
| `reviveSeconds` | number | `30.0` |
| `maxRevivesPerLevel` | number | `2` |

## Energy

| Field | Type | Snapshot value |
| --- | --- | --- |
| `energyTargetMultiplier` | number | `1.0` |
| `chapterEnergyTargetMultipliers` | array | `[1.0,0.45,0.42,0.40,0.39,0.38,0.36]` |
| `pushEnergyMultiplier` | number | `1.35` |
| `minimumEnergyTarget` | number | `20.0` |
| `minimumLaterChapterEnergyTarget` | number | `12.0` |
| `energyFailureEnabled` | boolean | `true` |
| `energyFailureMultiplier` | number | `1.6` |
| `energyRevivePercent` | number | `0.4` |
| `energyWarningFraction` | number | `0.8` |
| `energyRevivePlacement` | string | `"energy_revive"` |

## Economy and gem IAP

| Field | Type | Snapshot value |
| --- | --- | --- |
| `startingCoins` | number | `150` |
| `coinReviveCost` | number | `50` |
| `campaignCompletionCoins` | number | `2` |
| `coinsPerNewStar` | number | `1` |
| `dailyCompletionCoins` | number | `8` |
| `endlessCompletionCoins` | number | `3` |
| `rewardedCoinAmount` | number | `10` |
| `gemPurchasesEnabled` | boolean | `true` |
| `gemPacks` | array | `[{"productId":"otb_gems_50","name":"POCKET GEMS","gems":50,"fallbackPrice":"INR 10"},{"productId":"otb_gems_150","name":"GEM POUCH","gems":150,"fallbackPrice":"INR 29"},{"productId":"otb_gems_500","name":"GEM CHEST","gems":500,"fallbackPrice":"INR 89"},{"productId":"otb_gems_1000","name":"GEM VAULT","gems":1000,"fallbackPrice":"INR 169"},{"productId":"otb_gems_1500","name":"GEM TREASURY","gems":1500,"fallbackPrice":"INR 229"},{"productId":"otb_gems_3000","name":"GEM HOARD","gems":3000,"fallbackPrice":"INR 399"}]` |

## Cosmetics gifts and store presentation

| Field | Type | Snapshot value |
| --- | --- | --- |
| `cosmeticGemPrices` | array | `[0,100,120,130,0,0,0,0,0,0]` |
| `cosmeticRewardedViews` | number | `1` |
| `chapterGiftsEnabled` | boolean | `true` |
| `chapterGiftColors` | array | `["#A8C4D4","#B3C7B0","#D1BDA8","#C2B3D1","#A6C7C4","#D1C7A3"]` |
| `cosmeticRarities` | array | `[0,0,1,1,1,2,2,2,3,3]` |
| `cosmeticRarityColors` | array | `["#B8CFDF","#55CCFF","#C68AFF","#FFD166"]` |
| `cosmeticArtworkResource` | string | `"OutsideTheBox/Art/Cosmetics/CosmeticAtlas"` |
| `storeRevealSeconds` | number | `0.28` |
| `storeRevealStagger` | number | `0.035` |
| `storePreviewMotionAmount` | number | `0.035` |
| `storePreviewMotionSpeed` | number | `1.2` |
| `cosmeticIapProductIds` | array | `["","","","","otb_ball_skin_04","otb_ball_skin_05","otb_ball_skin_06","otb_ball_skin_07","otb_ball_skin_08","otb_ball_skin_09","","","","","otb_magnet_skin_04","otb_magnet_skin_05","otb_magnet_skin_06","otb_magnet_skin_07","otb_magnet_skin_08","otb_magnet_skin_09","","","","","otb_hole_skin_04","otb_hole_skin_05","otb_hole_skin_06","otb_hole_skin_07","otb_hole_skin_08","otb_hole_skin_09","","","","","otb_walls_skin_04","otb_walls_skin_05","otb_walls_skin_06","otb_walls_skin_07","otb_walls_skin_08","otb_walls_skin_09","","","","","otb_glass_skin_04","otb_glass_skin_05","otb_glass_skin_06","otb_glass_skin_07","otb_glass_skin_08","otb_glass_skin_09","","","","","otb_background_skin_04","otb_background_skin_05","otb_background_skin_06","otb_background_skin_07","otb_background_skin_08","otb_background_skin_09"]` |
| `cosmeticIapFallbackPrices` | array | `["","","","","INR 29","INR 39","INR 49","INR 59","INR 79","INR 99"]` |

## Notifications

| Field | Type | Snapshot value |
| --- | --- | --- |
| `localNotificationsEnabled` | boolean | `true` |
| `firebasePushEnabled` | boolean | `true` |
| `firebasePushTopic` | string | `"otb_updates"` |
| `firebasePushLogTokenInDevelopment` | boolean | `true` |
| `giftReminderHours` | number | `24.0` |
| `gameReminderHours` | number | `24.0` |
| `giftReminderMessage` | string | `"Your chapter gift is waiting. Open the store to claim it!"` |
| `gameReminderMessage` | string | `"Ready for another magnetic puzzle? Your next level is waiting."` |
| `gameReminderMessages` | array | `["A fresh perspective can unlock a new path. Ready for your next puzzle?","Small moves can lead to big discoveries. Try one more magnetic puzzle.","Take a little puzzle break. Your next challenge is waiting.","Every great solution starts with a little curiosity. Follow yours today.","Stuck yesterday? A fresh look might reveal the way today.","Think outside the box. Your next clever move is waiting.","One puzzle, one bright idea. See what you can discover today.","A new angle can change everything. Find your next path.","Make a little time for curiosity. Come back for a magnetic challenge.","The next breakthrough could be one move away. Give it a try.","Pause the busy day and play with a new possibility.","You bring the ideas. We bring the puzzles. Ready to play?","A little patience. A clever move. A satisfying finish. Your next puzzle awaits.","Keep exploring. There is always another way to think outside the box."]` |

## Signature mechanics

| Field | Type | Snapshot value |
| --- | --- | --- |
| `signatureMechanicsEnabled` | boolean | `true` |
| `echoMagnetEnabled` | boolean | `true` |
| `echoRecordSeconds` | number | `3.0` |
| `echoSampleRate` | number | `20` |
| `echoMaximumSamples` | number | `96` |
| `echoStrengthMultiplier` | number | `0.62` |
| `echoLoopDelay` | number | `0.2` |
| `echoEnergyMultiplier` | number | `0.45` |
| `ferrofluidMemoryEnabled` | boolean | `true` |
| `ferrofluidMaximumPoints` | number | `72` |
| `ferrofluidPointSpacing` | number | `0.16` |
| `ferrofluidGuidanceStrength` | number | `0.8` |
| `magneticImprintsEnabled` | boolean | `true` |
| `magneticImprintDuration` | number | `4.0` |
| `magneticImprintStrength` | number | `5.0` |
| `magneticImprintActivationRadius` | number | `0.7` |
| `fieldLensesEnabled` | boolean | `true` |
| `fieldLensDirectionBlend` | number | `1.0` |
| `polarityLocksEnabled` | boolean | `true` |
| `orbitGoalsEnabled` | boolean | `true` |
| `orbitGoalRadiusMultiplier` | number | `2.5` |
| `orbitGoalMinimumSpeed` | number | `0.35` |
| `imprintPulseSpeed` | number | `9.0` |
| `fieldLensRotationSpeed` | number | `22.0` |
| `orbitGoalPulseSpeed` | number | `4.0` |
| `orbitGoalPulseAmount` | number | `0.035` |
| `orbitGoalRotationSpeed` | number | `35.0` |

## FTUE HUD loading and transitions

| Field | Type | Snapshot value |
| --- | --- | --- |
| `ftueEnabled` | boolean | `true` |
| `ftueLevelCount` | number | `2` |
| `ftueTooltipSeconds` | number | `4.5` |
| `uiAnimationSeconds` | number | `0.28` |
| `timerMilestoneSeconds` | number | `30.0` |
| `timerWarningSeconds` | number | `10.0` |
| `hudPulseSeconds` | number | `0.55` |
| `hudPulseScale` | number | `0.2` |
| `uiPopupStartScale` | number | `0.88` |
| `uiButtonPressScale` | number | `0.94` |
| `uiButtonSpringFrequency` | number | `18.0` |
| `uiButtonSpringDamping` | number | `0.72` |
| `uiButtonHoverAmount` | number | `0.0` |
| `uiButtonHoverSpeed` | number | `0.65` |
| `uiAmbientPulseSpeed` | number | `1.2` |
| `uiStaggerSeconds` | number | `0.045` |
| `uiRevealOffset` | number | `34.0` |
| `uiBackdropMotionSpeed` | number | `1.0` |
| `uiScreenSlideDistance` | number | `88.0` |
| `uiScreenStartScale` | number | `0.975` |
| `uiScreenOvershoot` | number | `0.06` |
| `loadingMinimumSeconds` | number | `0.35` |
| `loadingFadeSeconds` | number | `0.16` |
| `loadingStartupMessage` | string | `"SYNCING PLAYER DATA"` |
| `loadingLevelMessage` | string | `"BUILDING LEVEL"` |
| `loadingMenuMessage` | string | `"RETURNING TO MENU"` |
| `mainMenuTransition` | object | `{"style":"SlideFadeScale","enterDirection":"Right","exitDirection":"Left","enterEasing":"EaseOutCubic","exitEasing":"SmoothStep","duration":0.34,"exitDuration":0.187,"distance":220.0,"startScale":0.975,"exitScale":0.97,"animateOutgoing":true}` |
| `gameplayTransition` | object | `{"style":"SlideFadeScale","enterDirection":"Right","exitDirection":"Left","enterEasing":"EaseOutCubic","exitEasing":"SmoothStep","duration":0.34,"exitDuration":0.187,"distance":220.0,"startScale":0.975,"exitScale":0.97,"animateOutgoing":true}` |
| `chapterSelectTransition` | object | `{"style":"SlideFadeScale","enterDirection":"Right","exitDirection":"Left","enterEasing":"EaseOutCubic","exitEasing":"SmoothStep","duration":0.34,"exitDuration":0.187,"distance":220.0,"startScale":0.975,"exitScale":0.97,"animateOutgoing":true}` |
| `levelSelectTransition` | object | `{"style":"SlideFadeScale","enterDirection":"Right","exitDirection":"Left","enterEasing":"EaseOutCubic","exitEasing":"SmoothStep","duration":0.34,"exitDuration":0.187,"distance":220.0,"startScale":0.975,"exitScale":0.97,"animateOutgoing":true}` |
| `settingsTransition` | object | `{"style":"SlideFadeScale","enterDirection":"Right","exitDirection":"Left","enterEasing":"EaseOutCubic","exitEasing":"SmoothStep","duration":0.34,"exitDuration":0.187,"distance":220.0,"startScale":0.975,"exitScale":0.97,"animateOutgoing":true}` |
| `winTransition` | object | `{"style":"SlideFadeScale","enterDirection":"Right","exitDirection":"Left","enterEasing":"EaseOutCubic","exitEasing":"SmoothStep","duration":0.34,"exitDuration":0.187,"distance":220.0,"startScale":0.975,"exitScale":0.97,"animateOutgoing":true}` |
| `timeoutTransition` | object | `{"style":"SlideFadeScale","enterDirection":"Right","exitDirection":"Left","enterEasing":"EaseOutCubic","exitEasing":"SmoothStep","duration":0.34,"exitDuration":0.187,"distance":220.0,"startScale":0.975,"exitScale":0.97,"animateOutgoing":true}` |
| `pauseTransition` | object | `{"style":"ScaleFade","enterDirection":"None","exitDirection":"None","enterEasing":"EaseOutBack","exitEasing":"EaseInCubic","duration":0.28,"exitDuration":0.16,"distance":0.0,"startScale":0.88,"exitScale":0.94,"animateOutgoing":true}` |
| `tutorialTransition` | object | `{"style":"ScaleFade","enterDirection":"None","exitDirection":"None","enterEasing":"EaseOutBack","exitEasing":"EaseInCubic","duration":0.28,"exitDuration":0.16,"distance":0.0,"startScale":0.88,"exitScale":0.94,"animateOutgoing":true}` |
| `signInTransition` | object | `{"style":"SlideFadeScale","enterDirection":"Right","exitDirection":"Left","enterEasing":"EaseOutCubic","exitEasing":"SmoothStep","duration":0.34,"exitDuration":0.187,"distance":220.0,"startScale":0.975,"exitScale":0.97,"animateOutgoing":true}` |
| `accountTransition` | object | `{"style":"SlideFadeScale","enterDirection":"Right","exitDirection":"Left","enterEasing":"EaseOutCubic","exitEasing":"SmoothStep","duration":0.34,"exitDuration":0.187,"distance":220.0,"startScale":0.975,"exitScale":0.97,"animateOutgoing":true}` |
| `leaderboardTransition` | object | `{"style":"SlideFadeScale","enterDirection":"Right","exitDirection":"Left","enterEasing":"EaseOutCubic","exitEasing":"SmoothStep","duration":0.34,"exitDuration":0.187,"distance":220.0,"startScale":0.975,"exitScale":0.97,"animateOutgoing":true}` |

## Connectivity and other live settings

| Field | Type | Snapshot value |
| --- | --- | --- |
| `onlineRequired` | boolean | `true` |
| `connectionCheckSeconds` | number | `6.0` |
| `connectionRetrySeconds` | number | `3.0` |
| `connectionTimeoutSeconds` | number | `4` |
| `connectionFailureThreshold` | number | `2` |
| `connectionCheckUrl` | string | `"https://www.gstatic.com/generate_204"` |
| `connectionFallbackUrl` | string | `"https://connectivitycheck.gstatic.com/generate_204"` |

## Mobile performance

| Field | Type | Snapshot value |
| --- | --- | --- |
| `mobileAdaptivePerformanceEnabled` | boolean | `true` |
| `mobileBatteryFrameRate` | number | `30` |
| `mobileBalancedFrameRate` | number | `60` |
| `mobileSmoothFrameRate` | number | `60` |
| `mobilePhysicsTickRate` | number | `60` |
| `mobileLowMemoryThresholdMb` | number | `3072` |
| `mobileHighMemoryThresholdMb` | number | `6144` |
| `mobileLowGpuMemoryThresholdMb` | number | `768` |
| `mobileHighGpuMemoryThresholdMb` | number | `2048` |
| `mobileLowProcessorCount` | number | `4` |
| `mobileHighProcessorCount` | number | `8` |
| `mobileLowEndRenderScale` | number | `0.72` |
| `mobileBalancedRenderScale` | number | `0.88` |
| `mobileHighEndRenderScale` | number | `1.0` |
| `mobileMinimumRenderScale` | number | `0.65` |
| `mobileRenderScaleStep` | number | `0.08` |
| `mobilePerformanceSampleSeconds` | number | `4.0` |
| `mobileSlowFrameThreshold` | number | `0.82` |
| `mobileRecoveryFrameThreshold` | number | `0.96` |
| `mobileSlowSampleCount` | number | `3` |
| `mobileRecoverySampleCount` | number | `8` |
| `mobileLowQualityLevel` | number | `0` |
| `mobileBalancedQualityLevel` | number | `1` |
| `mobileHighQualityLevel` | number | `2` |

## Ads analytics and consent

| Field | Type | Snapshot value |
| --- | --- | --- |
| `analyticsEnabled` | boolean | `true` |
| `adsEnabled` | boolean | `true` |
| `simulateAdsInEditor` | boolean | `true` |
| `interstitialEveryCompletions` | number | `2` |
| `interstitialMinimumSeconds` | number | `0.0` |
| `bannerEnabled` | boolean | `true` |
| `bannerUseAdaptiveSize` | boolean | `false` |
| `bannerRespectSafeArea` | boolean | `true` |
| `bannerLayoutPaddingDp` | number | `4.0` |
| `bannerPosition` | string | `"BottomCenter"` |
| `bannerPlacement` | string | `"global_banner"` |
| `adLoadRetrySeconds` | number | `8.0` |
| `childDirected` | boolean | `false` |
| `doNotSellPersonalInformation` | boolean | `false` |
| `levelPlayAdapterDebug` | boolean | `false` |
| `validateLevelPlayIntegrationInDevelopment` | boolean | `true` |
| `levelPlayAppKey` | string | `"287e41f2d"` |
| `rewardedAdUnitId` | string | `"mi265bamv9sd3y9k"` |
| `interstitialAdUnitId` | string | `"tknyyycp6t3x64r4"` |
| `bannerAdUnitId` | string | `"xleippljsk902w4m"` |
| `revivePlacement` | string | `"level_revive"` |
| `currencyPlacement` | string | `"free_gems"` |
| `interstitialPlacement` | string | `"level_complete"` |

## Firebase Remote Config

| Field | Type | Snapshot value |
| --- | --- | --- |
| `firebaseRemoteConfigEnabled` | boolean | `true` |
| `remoteConfigJsonKey` | string | `"game_config_json"` |
| `remoteFetchMinutes` | number | `60` |
| `remoteFetchTimeoutSeconds` | number | `12.0` |

## Authentication

| Field | Type | Snapshot value |
| --- | --- | --- |
| `googlePlayGamesEnabled` | boolean | `true` |
| `editorGooglePlayTestLoginEnabled` | boolean | `true` |
| `unityServicesEnabled` | boolean | `true` |
| `guestSignInEnabled` | boolean | `true` |
| `usernamePasswordEnabled` | boolean | `true` |
| `guestAccountUpgradeEnabled` | boolean | `true` |
| `usernameMinLength` | number | `3` |
| `usernameMaxLength` | number | `20` |
| `passwordMinLength` | number | `8` |
| `passwordMaxLength` | number | `30` |

## Cloud progress and scoring

| Field | Type | Snapshot value |
| --- | --- | --- |
| `cloudProgressEnabled` | boolean | `true` |
| `unityLeaderboardsEnabled` | boolean | `true` |
| `cloudProgressKey` | string | `"player_progress_v1"` |
| `leaderboardTimeWeight` | number | `0.7` |
| `leaderboardEnergyWeight` | number | `0.3` |
| `threeStarPerformanceThreshold` | number | `0.65` |
| `twoStarPerformanceThreshold` | number | `0.35` |
| `leaderboardEnergyRangeMultiplier` | number | `2.0` |
| `leaderboardHintPenalty` | number | `0.1` |
| `leaderboardRevivePenalty` | number | `0.15` |
| `leaderboardCompletionPoints` | number | `100000` |
| `leaderboardPerformanceBonusPoints` | number | `1000` |
| `allTimeLeaderboardId` | string | `"all_time_performance"` |
| `chapterLeaderboardIds` | array | `["chapter_01_performance","chapter_02_performance","chapter_03_performance","chapter_04_performance","chapter_05_performance","chapter_06_performance"]` |
| `campaignLeaderboardId` | string | `"campaign_stars"` |
| `endlessLeaderboardId` | string | `"endless_depth"` |

Total exported top-level fields: 199.
