using UnityEngine;

namespace OutsideTheBox
{
    public partial class BoardViewImplementation
    {
        private SpriteRenderer AddMechanicIcon(Transform parent, MechanicIconId id, float size = .42f)
        {
            if (prefabs.mechanicWorldIconPrefab == null) return null;
            var sprite = MechanicIconLibrary.Current?.Get(id);
            if (sprite == null) return null;
            var instance = prefabs.Spawn(prefabs.mechanicWorldIconPrefab, "Mechanic Icon " + id, parent);
            spawned.Add(instance);
            var renderer = instance.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            instance.transform.localPosition = Vector3.back * .16f;
            instance.transform.localRotation = Quaternion.Inverse(parent.rotation) * transform.rotation;
            var rotation = instance.transform.localRotation;
            var parentX = parent.TransformVector(rotation * Vector3.right).magnitude;
            var parentY = parent.TransformVector(rotation * Vector3.up).magnitude;
            instance.transform.localScale = new Vector3(
                size * CellSize * transform.TransformVector(Vector3.right).magnitude / Mathf.Max(.0001f, parentX),
                size * CellSize * transform.TransformVector(Vector3.up).magnitude / Mathf.Max(.0001f, parentY), 1f);
            return renderer;
        }
    }
}
