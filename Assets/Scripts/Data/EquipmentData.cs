using UnityEngine;
using OrbWanderer.Core;

namespace OrbWanderer.Data
{
    [CreateAssetMenu(fileName = "NewEquipment", menuName = "OrbWanderer/Equipment Data")]
    public class EquipmentData : ScriptableObject
    {
        [Header("Identity")]
        public string equipmentName;
        public EquipmentType equipmentType;
        public EquipmentTier tier;

        [Header("Visuals")]
        public Sprite icon;

        [Header("Crafting")]
        [Tooltip("Orbs needed to craft this equipment")]
        public OrbRecipe craftingRecipe;

        [Header("Effects")]
        [Tooltip("Speed bonus when equipped (multiplier)")]
        [Range(1f, 3f)]
        public float speedMultiplier = 1f;

        [Tooltip("Allows riding wildlife up to this size")]
        public WildlifeSize maxRideableSize = WildlifeSize.Small;

        [Tooltip("Orb detection range bonus")]
        [Range(0f, 10f)]
        public float detectionRangeBonus = 0f;

        [Tooltip("Extra satchel capacity granted")]
        public int extraSatchelSlots = 0;

        [TextArea(2, 4)]
        public string description;

        public bool IsUpgradeOf(EquipmentData other)
        {
            return other != null
                && equipmentType == other.equipmentType
                && tier > other.tier;
        }
    }
}
