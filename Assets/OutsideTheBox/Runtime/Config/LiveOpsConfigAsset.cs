using UnityEngine;

namespace OutsideTheBox
{
    [CreateAssetMenu(menuName = "Outside The Box/Live Ops Config", fileName = "LiveOpsConfig")]
    public sealed class LiveOpsConfigAsset : ScriptableObject
    {
        [Tooltip("Inspector-editable defaults. Firebase Remote Config overrides these values at runtime.")]
        public LiveOpsConfigData values = new();

        public LiveOpsConfigData CreateRuntimeCopy()
        {
            var copy = new LiveOpsConfigData();
            if (values != null) JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(values), copy);
            copy.Normalize();
            return copy;
        }

        /// <summary>
        /// Replaces the values on a runtime instance without retaining references to the source data.
        /// The project asset is only used for defaults; Firebase overrides are applied to a
        /// HideFlags.DontSave clone created by LiveOpsConfigStore.
        /// </summary>
        public void ReplaceRuntimeValues(LiveOpsConfigData source)
        {
            values = new LiveOpsConfigData();
            if (source != null) JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source), values);
            values.Normalize();
        }

        private void OnValidate()
        {
            values ??= new LiveOpsConfigData();
            values.Normalize();
        }
    }
}
