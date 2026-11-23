# Outside the Box project inventory

Snapshot: 23 November 2026. This appendix lists repository components supporting the [GDD](OutsideTheBox_GDD.md). Installed modules and editor packages are not automatically player-facing gameplay features.

## Unity and package dependencies

Engine: Unity 6000.3.22f1. [Package manifest](../Packages/manifest.json).

| Package | Manifest version |
| --- | --- |
| `com.unity.ide.visualstudio` | `2.0.26` |
| `com.unity.inputsystem` | `1.20.0` |
| `com.unity.purchasing` | `5.5.0` |
| `com.unity.services.authentication` | `3.5.2` |
| `com.unity.services.cloudsave` | `3.4.0` |
| `com.unity.services.leaderboards` | `2.3.3` |
| `com.unity.services.levelplay` | `9.5.1` |
| `com.unity.test-framework` | `1.6.0` |
| `com.unity.ugui` | `2.0.0` |

The manifest also includes standard Unity engine modules such as physics, audio, UI, image conversion, Android JNI, particles and web requests. Their presence does not mean the game implements a separate AI, VR, cloth or vehicle feature.

Google Play Games is supplied under `Assets/GooglePlayGames/com.google.play.games`, version 2.1.0. Android dependency resolution tooling is present under `Assets/ExternalDependencyManager` and `Assets/MobileDependencyResolver`.

## Android service dependencies

Source: [OutsideTheBoxLiveOpsDependencies.xml](../Assets/OutsideTheBox/Editor/OutsideTheBoxLiveOpsDependencies.xml).

| Maven dependency | Declared version |
| --- | --- |
| `com.google.firebase:firebase-analytics` | `23.2.0` |
| `com.google.firebase:firebase-config` | `23.1.0` |
| `com.google.firebase:firebase-messaging` | `25.0.2` |
| `com.google.android.ump:user-messaging-platform` | `4.0.0` |

LevelPlay and installed network adapters bring additional resolved Android dependencies. See the resolved Gradle templates and adapter assets for their exact build versions.

## Runtime source inventory

| Area | Source files |
| --- | --- |
| Config | [GameConfig](../Assets/OutsideTheBox/Runtime/Config/GameConfig.cs), [GameStrings](../Assets/OutsideTheBox/Runtime/Config/GameStrings.cs), [LevelCatalog](../Assets/OutsideTheBox/Runtime/Config/LevelCatalog.cs), [LevelDefinition](../Assets/OutsideTheBox/Runtime/Config/LevelDefinition.cs), [LiveOpsConfigAsset](../Assets/OutsideTheBox/Runtime/Config/LiveOpsConfigAsset.cs), [MechanicIconLibrary](../Assets/OutsideTheBox/Runtime/Config/MechanicIconLibrary.cs), [RuntimePrefabRegistry](../Assets/OutsideTheBox/Runtime/Config/RuntimePrefabRegistry.cs) |
| Core | [ChapterMechanicPlan](../Assets/OutsideTheBox/Runtime/Core/ChapterMechanicPlan.cs), [FocusedLevelGenerator](../Assets/OutsideTheBox/Runtime/Core/FocusedLevelGenerator.cs), [GameAutoBootstrap](../Assets/OutsideTheBox/Runtime/Core/GameAutoBootstrap.cs), [GameRules](../Assets/OutsideTheBox/Runtime/Core/GameRules.cs), [GameSession](../Assets/OutsideTheBox/Runtime/Core/GameSession.cs), [GameSessionPipePreview](../Assets/OutsideTheBox/Runtime/Core/GameSessionPipePreview.cs), [GridTypes](../Assets/OutsideTheBox/Runtime/Core/GridTypes.cs), [LevelMechanicLayout](../Assets/OutsideTheBox/Runtime/Core/LevelMechanicLayout.cs), [LevelSolver](../Assets/OutsideTheBox/Runtime/Core/LevelSolver.cs), [RailChallengeGenerator](../Assets/OutsideTheBox/Runtime/Core/RailChallengeGenerator.cs), [RailChallengeRules](../Assets/OutsideTheBox/Runtime/Core/RailChallengeRules.cs) |
| Input | [MagnetInput](../Assets/OutsideTheBox/Runtime/Input/MagnetInput.cs) |
| Performance | [MobilePerformanceController](../Assets/OutsideTheBox/Runtime/Performance/MobilePerformanceController.cs) |
| Services | [CloudProgressWriteQueue](../Assets/OutsideTheBox/Runtime/Services/CloudProgressWriteQueue.cs), [CosmeticIapService](../Assets/OutsideTheBox/Runtime/Services/CosmeticIapService.cs), [FeedbackService](../Assets/OutsideTheBox/Runtime/Services/FeedbackService.cs), [GemPurchaseLedger](../Assets/OutsideTheBox/Runtime/Services/GemPurchaseLedger.cs), [GoogleUmpConsent](../Assets/OutsideTheBox/Runtime/Services/GoogleUmpConsent.cs), [InternetConnectionState](../Assets/OutsideTheBox/Runtime/Services/InternetConnectionState.cs), [LiveOpsManager](../Assets/OutsideTheBox/Runtime/Services/LiveOpsManager.cs), [LiveOpsServices](../Assets/OutsideTheBox/Runtime/Services/LiveOpsServices.cs), [LocalizationService](../Assets/OutsideTheBox/Runtime/Services/LocalizationService.cs), [LocalTelemetryService](../Assets/OutsideTheBox/Runtime/Services/LocalTelemetryService.cs), [SaveService](../Assets/OutsideTheBox/Runtime/Services/SaveService.cs), [UnityPlayerService](../Assets/OutsideTheBox/Runtime/Services/UnityPlayerService.cs) |
| UI | [AnalyticsButtonTracker](../Assets/OutsideTheBox/Runtime/UI/AnalyticsButtonTracker.cs), [GameplayHudPresentation](../Assets/OutsideTheBox/Runtime/UI/GameplayHudPresentation.cs), [GameUIController](../Assets/OutsideTheBox/Runtime/UI/GameUIController.cs), [GameUIControllerGems](../Assets/OutsideTheBox/Runtime/UI/GameUIControllerGems.cs), [GameUIControllerStore](../Assets/OutsideTheBox/Runtime/UI/GameUIControllerStore.cs), [GameUIControllerSystemDialogs](../Assets/OutsideTheBox/Runtime/UI/GameUIControllerSystemDialogs.cs), [GameUIMechanicIcons](../Assets/OutsideTheBox/Runtime/UI/GameUIMechanicIcons.cs), [GameUIRailChallenges](../Assets/OutsideTheBox/Runtime/UI/GameUIRailChallenges.cs), [GemPackCardView](../Assets/OutsideTheBox/Runtime/UI/GemPackCardView.cs), [GemStoreView](../Assets/OutsideTheBox/Runtime/UI/GemStoreView.cs), [LevelBoardDiorama](../Assets/OutsideTheBox/Runtime/UI/LevelBoardDiorama.cs), [LevelPipeTransition](../Assets/OutsideTheBox/Runtime/UI/LevelPipeTransition.cs), [LevelPipeTransition3D](../Assets/OutsideTheBox/Runtime/UI/LevelPipeTransition3D.cs), [LevelPipeTransitionBase](../Assets/OutsideTheBox/Runtime/UI/LevelPipeTransitionBase.cs), [MechanicLegendView](../Assets/OutsideTheBox/Runtime/UI/MechanicLegendView.cs), [PipePreviewMeshes](../Assets/OutsideTheBox/Runtime/UI/PipePreviewMeshes.cs), [ResponsiveCanvasScaler](../Assets/OutsideTheBox/Runtime/UI/ResponsiveCanvasScaler.cs), [SafeAreaFitter](../Assets/OutsideTheBox/Runtime/UI/SafeAreaFitter.cs), [ServiceScreenView](../Assets/OutsideTheBox/Runtime/UI/ServiceScreenView.cs), [TutorialPopupView](../Assets/OutsideTheBox/Runtime/UI/TutorialPopupView.cs), [UIFloatMotion](../Assets/OutsideTheBox/Runtime/UI/UIFloatMotion.cs), [UIGemGraphic](../Assets/OutsideTheBox/Runtime/UI/UIGemGraphic.cs), [UIGradient](../Assets/OutsideTheBox/Runtime/UI/UIGradient.cs), [UIIconGraphic](../Assets/OutsideTheBox/Runtime/UI/UIIconGraphic.cs), [UIJuice](../Assets/OutsideTheBox/Runtime/UI/UIJuice.cs), [UIPipeGraphic](../Assets/OutsideTheBox/Runtime/UI/UIPipeGraphic.cs), [UIRadialGlow](../Assets/OutsideTheBox/Runtime/UI/UIRadialGlow.cs), [UIRoundRectGraphic](../Assets/OutsideTheBox/Runtime/UI/UIRoundRectGraphic.cs), [UIScreenMotion](../Assets/OutsideTheBox/Runtime/UI/UIScreenMotion.cs) |
| View | [BoardView](../Assets/OutsideTheBox/Runtime/View/BoardView.cs), [BoardViewImplementation](../Assets/OutsideTheBox/Runtime/View/BoardViewImplementation.cs), [BoardViewMechanicIcons](../Assets/OutsideTheBox/Runtime/View/BoardViewMechanicIcons.cs), [BoardViewRailChallenges](../Assets/OutsideTheBox/Runtime/View/BoardViewRailChallenges.cs), [BoardViewSignatureMechanics](../Assets/OutsideTheBox/Runtime/View/BoardViewSignatureMechanics.cs), [RailChallengeMarker](../Assets/OutsideTheBox/Runtime/View/RailChallengeMarker.cs) |

`ProceduralLevelGenerator` is defined in LevelSolver.cs. `AnalyticsService`, `EconomyService`, `PerformanceBalance`, `EnergyBalance`, `PlayGamesService` and `LevelPlayAdService` live in LiveOpsServices.cs. `ChapterGiftService`, `LocalGameNotifications` and the localization implementation live in SaveService.cs. The small LocalizationService.cs file is a compatibility placeholder.

## Editor source and commands

| Source | Commands or responsibility |
| --- | --- |
| [EditorPerformanceSetup.cs](../Assets/OutsideTheBox/Editor/EditorPerformanceSetup.cs) | Import or editor setup support; see source |
| [FirebaseAndroidConfigBuilder.cs](../Assets/OutsideTheBox/Editor/FirebaseAndroidConfigBuilder.cs) | Tools/Outside The Box/Generate Firebase Android Resources |
| [GeneratedArtImporter.cs](../Assets/OutsideTheBox/Editor/GeneratedArtImporter.cs) | Import or editor setup support; see source |
| [ProjectBuilder.cs](../Assets/OutsideTheBox/Editor/ProjectBuilder.cs) | Tools/Outside The Box/Build Project Content; Tools/Outside The Box/Rebuild Prefab Library |
| [ReleaseValidator.cs](../Assets/OutsideTheBox/Editor/ReleaseValidator.cs) | Tools/Outside The Box/Validate Android Release; Tools/Outside The Box/Configure Release Build Flags; Tools/Outside The Box/Player Balance Report; Tools/Outside The Box/Export Firebase Remote Config JSON; Tools/Outside The Box/Resolve Android Live Ops Dependencies; Tools/Outside The Box/Validate Live Ops Setup |

Broad content/prefab rebuild commands can rewrite manually edited assets. Validate release and live-ops reports without automatically rebuilding all content.

## Authored prefab inventory

All prefabs below are in `Assets/OutsideTheBox/Prefabs` and are referenced or composed through the project registry and UI/board controllers.

| Prefab | Asset |
| --- | --- |
| AccountPanel | [AccountPanel.prefab](../Assets/OutsideTheBox/Prefabs/AccountPanel.prefab) |
| Ball | [Ball.prefab](../Assets/OutsideTheBox/Prefabs/Ball.prefab) |
| Board | [Board.prefab](../Assets/OutsideTheBox/Prefabs/Board.prefab) |
| BoardSurface | [BoardSurface.prefab](../Assets/OutsideTheBox/Prefabs/BoardSurface.prefab) |
| Camera | [Camera.prefab](../Assets/OutsideTheBox/Prefabs/Camera.prefab) |
| ChapterBackground | [ChapterBackground.prefab](../Assets/OutsideTheBox/Prefabs/ChapterBackground.prefab) |
| ChapterSelect | [ChapterSelect.prefab](../Assets/OutsideTheBox/Prefabs/ChapterSelect.prefab) |
| CloudSyncPanel | [CloudSyncPanel.prefab](../Assets/OutsideTheBox/Prefabs/CloudSyncPanel.prefab) |
| Container | [Container.prefab](../Assets/OutsideTheBox/Prefabs/Container.prefab) |
| DirectionArrow | [DirectionArrow.prefab](../Assets/OutsideTheBox/Prefabs/DirectionArrow.prefab) |
| EventSystem | [EventSystem.prefab](../Assets/OutsideTheBox/Prefabs/EventSystem.prefab) |
| Exit | [Exit.prefab](../Assets/OutsideTheBox/Prefabs/Exit.prefab) |
| FeedbackBurst | [FeedbackBurst.prefab](../Assets/OutsideTheBox/Prefabs/FeedbackBurst.prefab) |
| GameplayHUD | [GameplayHUD.prefab](../Assets/OutsideTheBox/Prefabs/GameplayHUD.prefab) |
| GameRoot | [GameRoot.prefab](../Assets/OutsideTheBox/Prefabs/GameRoot.prefab) |
| GemBalanceButton | [GemBalanceButton.prefab](../Assets/OutsideTheBox/Prefabs/GemBalanceButton.prefab) |
| GemPackCard | [GemPackCard.prefab](../Assets/OutsideTheBox/Prefabs/GemPackCard.prefab) |
| GemStorePanel | [GemStorePanel.prefab](../Assets/OutsideTheBox/Prefabs/GemStorePanel.prefab) |
| GlassPiece | [GlassPiece.prefab](../Assets/OutsideTheBox/Prefabs/GlassPiece.prefab) |
| GoalGlow | [GoalGlow.prefab](../Assets/OutsideTheBox/Prefabs/GoalGlow.prefab) |
| HUD | [HUD.prefab](../Assets/OutsideTheBox/Prefabs/HUD.prefab) |
| LeaderboardPanel | [LeaderboardPanel.prefab](../Assets/OutsideTheBox/Prefabs/LeaderboardPanel.prefab) |
| LevelButton | [LevelButton.prefab](../Assets/OutsideTheBox/Prefabs/LevelButton.prefab) |
| LevelPipeTransition | [LevelPipeTransition.prefab](../Assets/OutsideTheBox/Prefabs/LevelPipeTransition.prefab) |
| LevelPipeTransition3D | [LevelPipeTransition3D.prefab](../Assets/OutsideTheBox/Prefabs/LevelPipeTransition3D.prefab) |
| LevelSelect | [LevelSelect.prefab](../Assets/OutsideTheBox/Prefabs/LevelSelect.prefab) |
| LoadingOverlay | [LoadingOverlay.prefab](../Assets/OutsideTheBox/Prefabs/LoadingOverlay.prefab) |
| Magnet | [Magnet.prefab](../Assets/OutsideTheBox/Prefabs/Magnet.prefab) |
| MagneticField | [MagneticField.prefab](../Assets/OutsideTheBox/Prefabs/MagneticField.prefab) |
| MainMenu | [MainMenu.prefab](../Assets/OutsideTheBox/Prefabs/MainMenu.prefab) |
| MechanicButtonIcon | [MechanicButtonIcon.prefab](../Assets/OutsideTheBox/Prefabs/MechanicButtonIcon.prefab) |
| MechanicLegend | [MechanicLegend.prefab](../Assets/OutsideTheBox/Prefabs/MechanicLegend.prefab) |
| MechanicTutorialIcon | [MechanicTutorialIcon.prefab](../Assets/OutsideTheBox/Prefabs/MechanicTutorialIcon.prefab) |
| MechanicWorldIcon | [MechanicWorldIcon.prefab](../Assets/OutsideTheBox/Prefabs/MechanicWorldIcon.prefab) |
| OfflinePanel | [OfflinePanel.prefab](../Assets/OutsideTheBox/Prefabs/OfflinePanel.prefab) |
| OneWayGate | [OneWayGate.prefab](../Assets/OutsideTheBox/Prefabs/OneWayGate.prefab) |
| PausePanel | [PausePanel.prefab](../Assets/OutsideTheBox/Prefabs/PausePanel.prefab) |
| QuitConfirmationPanel | [QuitConfirmationPanel.prefab](../Assets/OutsideTheBox/Prefabs/QuitConfirmationPanel.prefab) |
| RailChallengeMarker | [RailChallengeMarker.prefab](../Assets/OutsideTheBox/Prefabs/RailChallengeMarker.prefab) |
| ScreenTransition | [ScreenTransition.prefab](../Assets/OutsideTheBox/Prefabs/ScreenTransition.prefab) |
| SettingsPanel | [SettingsPanel.prefab](../Assets/OutsideTheBox/Prefabs/SettingsPanel.prefab) |
| SignInPanel | [SignInPanel.prefab](../Assets/OutsideTheBox/Prefabs/SignInPanel.prefab) |
| SpritePiece | [SpritePiece.prefab](../Assets/OutsideTheBox/Prefabs/SpritePiece.prefab) |
| StorePanel | [StorePanel.prefab](../Assets/OutsideTheBox/Prefabs/StorePanel.prefab) |
| TimeOutPanel | [TimeOutPanel.prefab](../Assets/OutsideTheBox/Prefabs/TimeOutPanel.prefab) |
| Tooltip | [Tooltip.prefab](../Assets/OutsideTheBox/Prefabs/Tooltip.prefab) |
| TutorialOverlay | [TutorialOverlay.prefab](../Assets/OutsideTheBox/Prefabs/TutorialOverlay.prefab) |
| UIButton | [UIButton.prefab](../Assets/OutsideTheBox/Prefabs/UIButton.prefab) |
| UICanvas | [UICanvas.prefab](../Assets/OutsideTheBox/Prefabs/UICanvas.prefab) |
| UIPanel | [UIPanel.prefab](../Assets/OutsideTheBox/Prefabs/UIPanel.prefab) |
| UIRect | [UIRect.prefab](../Assets/OutsideTheBox/Prefabs/UIRect.prefab) |
| UISafeArea | [UISafeArea.prefab](../Assets/OutsideTheBox/Prefabs/UISafeArea.prefab) |
| UIScroll | [UIScroll.prefab](../Assets/OutsideTheBox/Prefabs/UIScroll.prefab) |
| UIText | [UIText.prefab](../Assets/OutsideTheBox/Prefabs/UIText.prefab) |
| Wall | [Wall.prefab](../Assets/OutsideTheBox/Prefabs/Wall.prefab) |
| WinPanel | [WinPanel.prefab](../Assets/OutsideTheBox/Prefabs/WinPanel.prefab) |

## Native Android code and build files

| File | Source |
| --- | --- |
| AndroidManifest.xml | [Assets/Plugins/Android/AndroidManifest.xml](../Assets/Plugins/Android/AndroidManifest.xml) |
| AndroidManifest.xml | [Assets/Plugins/Android/GooglePlayGamesManifest.androidlib/AndroidManifest.xml](../Assets/Plugins/Android/GooglePlayGamesManifest.androidlib/AndroidManifest.xml) |
| mainTemplate.gradle | [Assets/Plugins/Android/mainTemplate.gradle](../Assets/Plugins/Android/mainTemplate.gradle) |
| AndroidManifest.xml | [Assets/Plugins/Android/OutsideTheBoxFirebase.androidlib/AndroidManifest.xml](../Assets/Plugins/Android/OutsideTheBoxFirebase.androidlib/AndroidManifest.xml) |
| google-services.xml | [Assets/Plugins/Android/OutsideTheBoxFirebase.androidlib/res/values/google-services.xml](../Assets/Plugins/Android/OutsideTheBoxFirebase.androidlib/res/values/google-services.xml) |
| AndroidManifest.xml | [Assets/Plugins/Android/OutsideTheBoxNotifications.androidlib/AndroidManifest.xml](../Assets/Plugins/Android/OutsideTheBoxNotifications.androidlib/AndroidManifest.xml) |
| build.gradle | [Assets/Plugins/Android/OutsideTheBoxNotifications.androidlib/build.gradle](../Assets/Plugins/Android/OutsideTheBoxNotifications.androidlib/build.gradle) |
| otb_notification.xml | [Assets/Plugins/Android/OutsideTheBoxNotifications.androidlib/res/drawable/otb_notification.xml](../Assets/Plugins/Android/OutsideTheBoxNotifications.androidlib/res/drawable/otb_notification.xml) |
| keep.xml | [Assets/Plugins/Android/OutsideTheBoxNotifications.androidlib/res/raw/keep.xml](../Assets/Plugins/Android/OutsideTheBoxNotifications.androidlib/res/raw/keep.xml) |
| GamePushMessagingService.java | [Assets/Plugins/Android/OutsideTheBoxNotifications.androidlib/src/com/outsidethebox/notifications/GamePushMessagingService.java](../Assets/Plugins/Android/OutsideTheBoxNotifications.androidlib/src/com/outsidethebox/notifications/GamePushMessagingService.java) |
| GameReminderReceiver.java | [Assets/Plugins/Android/OutsideTheBoxNotifications.androidlib/src/com/outsidethebox/notifications/GameReminderReceiver.java](../Assets/Plugins/Android/OutsideTheBoxNotifications.androidlib/src/com/outsidethebox/notifications/GameReminderReceiver.java) |
| NotificationArtwork.java | [Assets/Plugins/Android/OutsideTheBoxNotifications.androidlib/src/com/outsidethebox/notifications/NotificationArtwork.java](../Assets/Plugins/Android/OutsideTheBoxNotifications.androidlib/src/com/outsidethebox/notifications/NotificationArtwork.java) |
| ReminderPolicy.java | [Assets/Plugins/Android/OutsideTheBoxNotifications.androidlib/src/com/outsidethebox/notifications/ReminderPolicy.java](../Assets/Plugins/Android/OutsideTheBoxNotifications.androidlib/src/com/outsidethebox/notifications/ReminderPolicy.java) |
| settingsTemplate.gradle | [Assets/Plugins/Android/settingsTemplate.gradle](../Assets/Plugins/Android/settingsTemplate.gradle) |

## IAP catalog

Cosmetics are non-consumable permanent entitlements. Gem packs are consumable repeat purchases. Product IDs must match the store; fallback display prices do not set checkout prices.

| Product ID | Item | Product type |
| --- | --- | --- |
| `otb_ball_skin_04` | Ruby Ball | Non-consumable |
| `otb_ball_skin_05` | Emerald Ball | Non-consumable |
| `otb_ball_skin_06` | Midnight Ball | Non-consumable |
| `otb_ball_skin_07` | Ember Ball | Non-consumable |
| `otb_ball_skin_08` | Cosmic Ball | Non-consumable |
| `otb_ball_skin_09` | Aurora Ball | Non-consumable |
| `otb_magnet_skin_04` | Ruby Magnet | Non-consumable |
| `otb_magnet_skin_05` | Emerald Magnet | Non-consumable |
| `otb_magnet_skin_06` | Midnight Magnet | Non-consumable |
| `otb_magnet_skin_07` | Ember Magnet | Non-consumable |
| `otb_magnet_skin_08` | Cosmic Magnet | Non-consumable |
| `otb_magnet_skin_09` | Aurora Magnet | Non-consumable |
| `otb_hole_skin_04` | Ruby Hole | Non-consumable |
| `otb_hole_skin_05` | Emerald Hole | Non-consumable |
| `otb_hole_skin_06` | Midnight Hole | Non-consumable |
| `otb_hole_skin_07` | Ember Hole | Non-consumable |
| `otb_hole_skin_08` | Cosmic Hole | Non-consumable |
| `otb_hole_skin_09` | Aurora Hole | Non-consumable |
| `otb_walls_skin_04` | Ruby Walls | Non-consumable |
| `otb_walls_skin_05` | Emerald Walls | Non-consumable |
| `otb_walls_skin_06` | Midnight Walls | Non-consumable |
| `otb_walls_skin_07` | Ember Walls | Non-consumable |
| `otb_walls_skin_08` | Cosmic Walls | Non-consumable |
| `otb_walls_skin_09` | Aurora Walls | Non-consumable |
| `otb_glass_skin_04` | Ruby Glass | Non-consumable |
| `otb_glass_skin_05` | Emerald Glass | Non-consumable |
| `otb_glass_skin_06` | Midnight Glass | Non-consumable |
| `otb_glass_skin_07` | Ember Glass | Non-consumable |
| `otb_glass_skin_08` | Cosmic Glass | Non-consumable |
| `otb_glass_skin_09` | Aurora Glass | Non-consumable |
| `otb_background_skin_04` | Ruby Background | Non-consumable |
| `otb_background_skin_05` | Emerald Background | Non-consumable |
| `otb_background_skin_06` | Midnight Background | Non-consumable |
| `otb_background_skin_07` | Ember Background | Non-consumable |
| `otb_background_skin_08` | Cosmic Background | Non-consumable |
| `otb_background_skin_09` | Aurora Background | Non-consumable |
| `otb_gems_50` | 50 gems | Consumable |
| `otb_gems_150` | 150 gems | Consumable |
| `otb_gems_500` | 500 gems | Consumable |
| `otb_gems_1000` | 1000 gems | Consumable |
| `otb_gems_1500` | 1500 gems | Consumable |
| `otb_gems_3000` | 3000 gems | Consumable |

## Configuration content and release assets

| Area | Location and role |
| --- | --- |
| Campaign definitions | `Assets/OutsideTheBox/Resources/OutsideTheBox/Configs/Levels` and the LevelCatalog asset |
| Core configuration | [GameConfig.asset](../Assets/OutsideTheBox/Resources/OutsideTheBox/Configs/GameConfig.asset) |
| Prefab bindings | [RuntimePrefabRegistry.asset](../Assets/OutsideTheBox/Resources/OutsideTheBox/Configs/RuntimePrefabRegistry.asset) |
| Live defaults | [LiveOpsConfig.asset](../Assets/OutsideTheBox/Resources/OutsideTheBox/Configs/LiveOpsConfig.asset) and [packaged JSON](../Assets/OutsideTheBox/Resources/OutsideTheBox/default_live_config.json) |
| Firebase publishing payload | [FirebaseRemoteConfigValue.json](../Assets/OutsideTheBox/Documentation/FirebaseRemoteConfigValue.json) |
| English strings | [Strings_en.asset](../Assets/OutsideTheBox/Resources/OutsideTheBox/Configs/Localization/Strings_en.asset) |
| Font | `Assets/OutsideTheBox/Fonts/rogenz-demo` and configured TMP font assets |
| Gameplay and cosmetic art | `Assets/OutsideTheBox/Art`, resource artwork, materials and shaders |
| Local notification catalog | [catalog.json](../Assets/Plugins/Android/OutsideTheBoxNotifications.androidlib/assets/otb_notifications/catalog.json), with packaged image assets |
| Google Play release materials | `ReleaseAssets/GooglePlay`: icon variants, feature graphics, and listing text |
| Scene | [OutsideTheBox.unity](../Assets/OutsideTheBox/Scenes/OutsideTheBox.unity) |

## External services and development tools

Unity Editor and Unity Hub manage builds and packages. Google Play Console manages distribution, licensing tests, Play Games, and IAP. Unity Dashboard manages Authentication, Cloud Save, Leaderboards, and Unity Ads. Unity LevelPlay manages mediation. Google AdMob supplies the configured banner network instance. Firebase Console provides Analytics, Remote Config, and Messaging. Google Cloud Auth Platform and Search Console support OAuth identity/branding and website ownership. Android developer options and ADB/Logcat support physical-device diagnostics.

A tool used during development is not necessarily a runtime dependency. The project has no generative-AI service integrated into gameplay. Temporary isolated validation projects and authoring scripts are outside the shipping Assets tree, rather than player-facing content.
