using UnityEngine;
using UnityEngine.UI;

namespace OutsideTheBox
{
    public sealed partial class GameUIController
    {
        private Image tutorialMechanicIcon;
        private MechanicLegendView mechanicLegend;
        private Image modeMechanicIcon, echoMechanicIcon;
        private GameplayHudPresentation gameplayPresentation;
        private void RefreshTutorialMechanicIcon(TutorialStepDefinition step)
        {
            if (tutorialMechanicIcon == null && prefabs.mechanicTutorialIconPrefab != null && tutorialTitleText != null)
            {
                tutorialMechanicIcon = tutorialTitleText.transform.parent.Find("Mechanic Icon")?.GetComponent<Image>() ??
                    prefabs.Spawn(prefabs.mechanicTutorialIconPrefab, "Mechanic Icon",
                        tutorialTitleText.transform.parent).GetComponent<Image>();
                // Reserve the left of the existing title row; body/buttons and
                // all other manually authored popup settings remain untouched.
                var title = tutorialTitleText.rectTransform;
                title.anchorMin = new Vector2(.26f, title.anchorMin.y);
            }
            if (tutorialMechanicIcon != null)
            {
                tutorialMechanicIcon.sprite = MechanicIconLibrary.Current?.Get(MechanicIconLibrary.ForTutorial(step.id));
                tutorialMechanicIcon.gameObject.SetActive(tutorialMechanicIcon.sprite != null);
            }
        }

        private bool RefreshMechanicLegend()
        {
            if (session?.CurrentLevel == null || !session.Config.focusedChapterDesignEnabled || gameplayPanel == null || prefabs.mechanicLegendPrefab == null ||
                MechanicIconLibrary.Current == null)
            {
                if (mechanicLegend != null) mechanicLegend.gameObject.SetActive(false);
                return false;
            }
            if (modeMechanicIcon == null && modeButton != null && prefabs.mechanicButtonIconPrefab != null)
                modeMechanicIcon = modeButton.transform.Find("Polarity Icon")?.GetComponent<Image>() ??
                    prefabs.Spawn(prefabs.mechanicButtonIconPrefab, "Polarity Icon", modeButton.transform).GetComponent<Image>();
            if (modeMechanicIcon != null) modeMechanicIcon.sprite = MechanicIconLibrary.Current.Get(
                session.State.mode == ForceMode.Pull ? MechanicIconId.Pull : MechanicIconId.Push);
            if (echoMechanicIcon == null && echoButton != null && prefabs.mechanicButtonIconPrefab != null)
            {
                echoMechanicIcon = echoButton.transform.Find("Echo Icon")?.GetComponent<Image>() ??
                    prefabs.Spawn(prefabs.mechanicButtonIconPrefab, "Echo Icon", echoButton.transform).GetComponent<Image>();
                echoMechanicIcon.sprite = MechanicIconLibrary.Current.Get(MechanicIconId.Echo);
            }
            if (mechanicLegend == null)
                mechanicLegend = prefabs.Spawn(prefabs.mechanicLegendPrefab, "Mechanic Legend", gameplayPanel.transform)
                    .GetComponent<MechanicLegendView>();
            if (mechanicLegend == null) return false;
            var active = session.CurrentLevel;
            var used = 0;
            if (!active.useNormalPhysics && !active.isEndless && active.levelNumber > session.Config.levelsPerChapter &&
                active.levelNumber <= session.Config.levelsPerChapter * 3)
            {
                var ice = active.levelNumber <= session.Config.levelsPerChapter * 2;
                mechanicLegend.SetSlot(used++, ice ? MechanicIconId.Ice : MechanicIconId.Polarity, ice ? "ICE" : "PULL / PUSH", true);
            }
            else if (MechanicIsVisible(active.primaryMechanic, active))
                mechanicLegend.SetSlot(used++, MechanicIconLibrary.ForMechanic(active.primaryMechanic), MechanicLabel(active.primaryMechanic), true);
            if (active.railChallenge != null && active.railChallenge.kind != RailChallengeKind.None)
                mechanicLegend.SetSlot(used++, MechanicIconLibrary.ForRail(active.railChallenge.kind), RailLabel(active.railChallenge.kind), true);
            else if (MechanicIsVisible(active.supportingMechanic, active))
                mechanicLegend.SetSlot(used++, MechanicIconLibrary.ForMechanic(active.supportingMechanic), MechanicLabel(active.supportingMechanic), true);
            while (used < 3) mechanicLegend.SetSlot(used++, MechanicIconId.Magnet, string.Empty, false);
            mechanicLegend.gameObject.SetActive(mechanicLegend.icons[0].transform.parent.gameObject.activeSelf);
            if (signatureStatusText != null) signatureStatusText.gameObject.SetActive(false);
            return true;
        }

        private static string MechanicLabel(LevelMechanicKind kind) => kind switch
        {
            LevelMechanicKind.MovingWalls => "MOVING WALL", LevelMechanicKind.FieldZones => "FIELD ZONE",
            LevelMechanicKind.PolarityLocks => "POLARITY LOCK", _ => kind.ToString().ToUpperInvariant()
        };
        private static string RailLabel(RailChallengeKind kind) => kind switch
        {
            RailChallengeKind.LockedSide => "LOCKED SIDE", RailChallengeKind.LimitedRail => "LIMITED RAIL",
            RailChallengeKind.TimedLock => "TIMED RAIL", RailChallengeKind.UnlockSwitch => "SWITCH",
            RailChallengeKind.SwitchLimit => "SIDE LIMIT", _ => "COOLDOWN"
        };

        private string FocusedGameplayPrompt(LevelDefinition active)
        {
            if (active.railChallenge != null && active.railChallenge.kind != RailChallengeKind.None &&
                !string.IsNullOrEmpty(session.RailChallengeStatus)) return session.RailChallengeStatus;
            if (active.levelNumber == 1 && !active.isEndless) return "DRAG THE MAGNET AROUND THE BOX";
            if (MechanicIsVisible(LevelMechanicKind.Echo, active)) return "RECORD AN ECHO TO HOLD YOUR ROUTE";
            if (MechanicIsVisible(LevelMechanicKind.Memory, active)) return "REUSE YOUR TRAIL THROUGH THE PASSAGES";
            if (session.RequiresOrbit) return "CIRCLE THE GOAL TO OPEN THE POCKET";
            if (active.polarityLocks?.Count > 0) return "FOLLOW THE LOCK'S PULL / PUSH SEQUENCE";
            if (active.fieldLenses?.Count > 0) return "USE THE LENS TO TURN THE MAGNETIC FORCE";
            if (active.magneticImprints?.Count > 0) return "WAKE THE IMPRINT BESIDE THE PASSAGE";
            if (active.fieldZones?.Count > 0) return "ADJUST YOUR PULL INSIDE THE FIELD ZONE";
            if (active.movingWalls?.Count > 0) return "TIME YOUR MOVE PAST THE SHUTTER";
            if (active.gates?.Count > 0) return "FOLLOW THE ARROWS THROUGH THE GATES";
            if (!active.useNormalPhysics && active.levelNumber <= session.Config.levelsPerChapter * 2 &&
                active.levelNumber > session.Config.levelsPerChapter) return "RELEASE EARLY AND LET THE BALL COAST";
            return "PLAN THE TURNS. RELEASE TO STAY IN CONTROL.";
        }
        private bool MechanicTutorialApplies(int id, LevelDefinition active) => id switch
        {
            201 => !active.useNormalPhysics,
            301 => active.pushUnlocked,
            401 => active.gates?.Count > 0,
            402 => active.movingWalls?.Count > 0,
            501 => active.fieldZones?.Count > 0,
            801 => MechanicIsVisible(LevelMechanicKind.Echo, active),
            802 => MechanicIsVisible(LevelMechanicKind.Memory, active),
            803 => MechanicIsVisible(LevelMechanicKind.Imprints, active),
            804 => MechanicIsVisible(LevelMechanicKind.Lenses, active),
            805 => MechanicIsVisible(LevelMechanicKind.PolarityLocks, active),
            806 => MechanicIsVisible(LevelMechanicKind.Orbit, active),
            _ => true
        };

        private bool MechanicIsVisible(LevelMechanicKind kind, LevelDefinition active)
        {
            var live = LiveOpsConfigStore.Current;
            var signature = live.signatureMechanicsEnabled;
            return kind switch
            {
                LevelMechanicKind.Gates => active.gates?.Count > 0,
                LevelMechanicKind.MovingWalls => active.movingWalls?.Count > 0,
                LevelMechanicKind.FieldZones => active.fieldZones?.Count > 0,
                LevelMechanicKind.Echo => signature && live.echoMagnetEnabled && active.echoMagnetEnabled,
                LevelMechanicKind.Memory => signature && live.ferrofluidMemoryEnabled && active.ferrofluidMemoryEnabled,
                LevelMechanicKind.Imprints => signature && live.magneticImprintsEnabled && active.magneticImprints?.Count > 0,
                LevelMechanicKind.Lenses => signature && live.fieldLensesEnabled && active.fieldLenses?.Count > 0,
                LevelMechanicKind.PolarityLocks => signature && live.polarityLocksEnabled && active.polarityLocks?.Count > 0,
                LevelMechanicKind.Orbit => session.RequiresOrbit,
                _ => false
            };
        }
    }
}
