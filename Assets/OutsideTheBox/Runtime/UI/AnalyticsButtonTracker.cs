using UnityEngine;
using UnityEngine.UI;

namespace OutsideTheBox
{
    // Attached at runtime; authored prefab contents remain untouched.
    [DisallowMultipleComponent]
    public sealed class AnalyticsButtonTracker : MonoBehaviour
    {
        private string actionId;
        private string sourceScreen;

        public static void Bind(Button target)
        {
            if (!Application.isPlaying || target == null) return;
            var tracker = target.GetComponent<AnalyticsButtonTracker>();
            if (tracker == null) tracker = target.gameObject.AddComponent<AnalyticsButtonTracker>();
            var path = target.name;
            var screen = AnalyticsService.CurrentScreen;
            for (var parent = target.transform.parent; parent != null; parent = parent.parent)
            {
                if (parent.name == "Safe Area") break;
                path = parent.name + "/" + path;
                screen = parent.name;
            }
            tracker.actionId = path;
            tracker.sourceScreen = screen;
            target.onClick.RemoveListener(tracker.Track);
            target.onClick.AddListener(tracker.Track);
        }

        private void Track()
        {
            AnalyticsService.Log("button_click", ("button_id", actionId), ("source_screen", sourceScreen));
        }
    }

}
