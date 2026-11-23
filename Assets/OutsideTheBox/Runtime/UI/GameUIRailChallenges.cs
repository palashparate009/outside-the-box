using UnityEngine;

namespace OutsideTheBox
{
    public sealed partial class GameUIController
    {
        private float nextRailStatusAt;

        private void RefreshRailChallengeStatus()
        {
            if (session == null || tutorialText == null || gameplayPanel == null || !gameplayPanel.activeInHierarchy ||
                Time.unscaledTime < nextRailStatusAt) return;
            nextRailStatusAt = Time.unscaledTime + .2f;
            var status = session.RailChallengeStatus;
            if (!string.IsNullOrEmpty(status) && tutorialText.text != status) tutorialText.text = status;
        }

        private void AddRailChallengeTutorial(LevelDefinition activeLevel)
        {
            if (!LiveOpsConfigStore.Current.ftueEnabled || !session.Config.railChallengesEnabled) return;
            var rule = activeLevel.railChallenge;
            if (rule == null || rule.kind == RailChallengeKind.None) return;
            var index = (int)rule.kind - 1;
            var definitions = session.Config.railChallengeTutorials;
            if (definitions == null || index >= definitions.Length || definitions[index] == null) return;
            var source = definitions[index];
            AddUnlearnedTutorial(new TutorialStepDefinition { id = source.id, title = source.title,
                body = source.body?.Replace("{side}", rule.side.ToString().ToUpperInvariant())
                    .Replace("{budget}", rule.switchBudget.ToString()),
                buttonLabel = source.buttonLabel, completionTooltip = source.completionTooltip });
        }
    }
}
