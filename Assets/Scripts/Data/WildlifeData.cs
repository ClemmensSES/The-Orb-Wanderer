using UnityEngine;
using OrbWanderer.Core;

namespace OrbWanderer.Data
{
    [CreateAssetMenu(fileName = "NewWildlife", menuName = "OrbWanderer/Wildlife Data")]
    public class WildlifeData : ScriptableObject
    {
        [Header("Identity")]
        public string wildlifeName;
        public WildlifeType wildlifeType;
        public WildlifeBehavior behavior;
        public WildlifeSize size;

        [Header("Visuals")]
        public Sprite icon;
        public Sprite worldSprite;
        public RuntimeAnimatorController animatorController;

        [Header("Habitat")]
        public RegionType[] nativeRegions;
        [Range(0f, 1f)]
        public float spawnChance = 0.3f;

        [Header("Befriending")]
        [Tooltip("Orb recipe required to initiate friendship")]
        public OrbRecipe befriendRecipe;

        [Tooltip("How many interactions needed to reach max friendship")]
        public int interactionsToMax = 10;

        [Tooltip("Difficulty rating 1-10, harder wildlife are better helpers")]
        [Range(1, 10)]
        public int difficultyRating = 1;

        [Header("Mount Properties")]
        public bool isRideable;

        [Tooltip("Equipment needed to ride this wildlife")]
        public EquipmentRequirement[] ridingRequirements;

        [Range(1f, 20f)]
        public float mountSpeed = 5f;

        [Tooltip("Can this mount traverse water?")]
        public bool canSwim;

        [Tooltip("Can this mount fly?")]
        public bool canFly;

        [Tooltip("Can this mount climb steep terrain?")]
        public bool canClimb;

        [Header("Orb Finding")]
        [Tooltip("Multiplier for orb detection range when this wildlife is your companion")]
        [Range(1f, 5f)]
        public float orbDetectionMultiplier = 1f;

        [Tooltip("Bonus drop rate for orbs when this wildlife is active companion")]
        [Range(0f, 1f)]
        public float orbDropRateBonus = 0f;

        [Tooltip("Specific orb types this wildlife is good at finding")]
        public OrbType[] specializedOrbTypes;

        [TextArea(2, 4)]
        public string description;

        /// <summary>
        /// Better wildlife (harder to obtain) provide better orb-finding bonuses.
        /// </summary>
        public float GetEffectiveOrbBonus()
        {
            return orbDropRateBonus + (difficultyRating * 0.02f);
        }
    }

    [System.Serializable]
    public class OrbRecipe
    {
        public OrbRequirement[] requirements;

        public bool CanCraft(System.Collections.Generic.Dictionary<OrbType, int> available)
        {
            if (requirements == null) return true;

            foreach (var req in requirements)
            {
                if (!available.ContainsKey(req.orbType) || available[req.orbType] < req.amount)
                    return false;
            }
            return true;
        }
    }

    [System.Serializable]
    public class OrbRequirement
    {
        public OrbType orbType;
        public int amount;
    }

    [System.Serializable]
    public class EquipmentRequirement
    {
        public EquipmentType equipmentType;
        public EquipmentTier minimumTier;
    }
}
