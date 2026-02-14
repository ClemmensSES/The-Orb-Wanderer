using UnityEngine;
using OrbWanderer.Core;

namespace OrbWanderer.Data
{
    [CreateAssetMenu(fileName = "NewOrb", menuName = "OrbWanderer/Orb Data")]
    public class OrbData : ScriptableObject
    {
        [Header("Identity")]
        public string orbName;
        public OrbType orbType;
        public OrbRarity rarity;

        [Header("Visuals")]
        public Sprite icon;
        public Color glowColor = Color.white;
        [Range(0.5f, 3f)]
        public float visualScale = 1f;

        [Header("Collection")]
        [Tooltip("Base chance to find this orb in a compatible region (0-1)")]
        [Range(0f, 1f)]
        public float baseDropRate = 0.5f;

        [Tooltip("Regions where this orb can be found")]
        public RegionType[] foundInRegions;

        [Header("Value")]
        [Tooltip("How many of this orb count as 1 unit for unlock recipes")]
        public int stackSize = 99;

        [TextArea(2, 4)]
        public string description;

        public int GetValueWeight()
        {
            return rarity switch
            {
                OrbRarity.Common => 1,
                OrbRarity.Uncommon => 3,
                OrbRarity.Rare => 8,
                OrbRarity.Epic => 20,
                OrbRarity.Legendary => 50,
                _ => 1
            };
        }
    }
}
