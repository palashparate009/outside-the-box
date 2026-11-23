using System.Collections.Generic;
using UnityEngine;

namespace OutsideTheBox
{
    [CreateAssetMenu(menuName = "Outside The Box/Level Catalog", fileName = "LevelCatalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        public List<LevelDefinition> levels = new();
        public int Count => levels?.Count ?? 0;
        public LevelDefinition Get(int zeroBasedIndex) => levels[Mathf.Clamp(zeroBasedIndex, 0, Count - 1)];
    }
}
