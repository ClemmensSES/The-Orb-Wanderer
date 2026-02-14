using UnityEngine;
using OrbWanderer.Core;

namespace OrbWanderer.Data
{
    [CreateAssetMenu(fileName = "NewRegion", menuName = "OrbWanderer/Region Data")]
    public class RegionData : ScriptableObject
    {
        [Header("Identity")]
        public string regionName;
        public RegionType regionType;

        [Header("Map")]
        [Tooltip("Position on the world map grid")]
        public Vector2Int mapPosition;

        [Tooltip("Size of this region in map tiles")]
        public Vector2Int mapSize = Vector2Int.one;

        public Color mapColor = Color.green;
        public Sprite mapIcon;

        [Header("Unlock Requirements")]
        [Tooltip("Orb combination needed to unlock this region")]
        public OrbRecipe unlockRecipe;

        [Tooltip("Regions that must be unlocked before this one becomes discoverable")]
        public RegionData[] prerequisiteRegions;

        [Header("Content")]
        [Tooltip("Orb types that can be found in this region")]
        public OrbSpawnEntry[] orbSpawns;

        [Tooltip("Wildlife that can be found in this region")]
        public WildlifeData[] localWildlife;

        [Header("Environment")]
        [Tooltip("Does the player need special equipment to survive here?")]
        public EquipmentRequirement[] survivalRequirements;

        [Range(0f, 1f)]
        public float dangerLevel = 0f;

        [TextArea(2, 4)]
        public string description;

        public bool ArePrerequisitesMet(System.Collections.Generic.HashSet<string> unlockedRegions)
        {
            if (prerequisiteRegions == null || prerequisiteRegions.Length == 0)
                return true;

            foreach (var prereq in prerequisiteRegions)
            {
                if (prereq != null && !unlockedRegions.Contains(prereq.regionName))
                    return false;
            }
            return true;
        }
    }

    [System.Serializable]
    public class OrbSpawnEntry
    {
        public OrbData orbData;

        [Range(0f, 1f)]
        public float spawnWeight = 0.5f;

        [Tooltip("Maximum number of this orb available in the region at once")]
        public int maxActive = 5;
    }
}
