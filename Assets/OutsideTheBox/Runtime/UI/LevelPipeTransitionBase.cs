using System;
using System.Collections;
using UnityEngine;

namespace OutsideTheBox
{
    public enum PipeTransitionPreviewStyle { TwoDimensional, ThreeDimensional }

    public abstract class LevelPipeTransitionBase : MonoBehaviour
    {
        public CanvasGroup group;
        [Min(0.2f)] public float departureSeconds = 1.1f;
        [Min(0.2f)] public float arrivalSeconds = 1.15f;
        [Min(0.05f)] public float fadeSeconds = 0.16f;
        public bool Completed { get; protected set; }

        public abstract IEnumerator Play(bool arriving, Camera camera, SpriteRenderer source,
            Vector3 worldPoint, float worldDiameter, Func<bool> blocked, Action landed = null);

        public virtual void Hide()
        {
            if (group != null) { group.alpha = 0f; group.blocksRaycasts = false; }
            gameObject.SetActive(false);
        }

        protected virtual void OnDisable()
        {
            if (group != null) { group.alpha = 0f; group.blocksRaycasts = false; }
        }
    }
}
