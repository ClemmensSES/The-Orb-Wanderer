using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using OrbWanderer.Core;
using OrbWanderer.Data;
using OrbWanderer.Inventory;

namespace OrbWanderer.Equipment
{
    /// <summary>
    /// Manages the player's crafted equipment.
    /// Equipment affects what wildlife can be ridden, survival in harsh regions, etc.
    /// </summary>
    public class EquipmentManager : MonoBehaviour
    {
        public static EquipmentManager Instance { get; private set; }

        [SerializeField] private EquipmentData[] allEquipment;

        private Dictionary<EquipmentType, EquipmentData> equippedItems = new Dictionary<EquipmentType, EquipmentData>();
        private HashSet<string> craftedEquipment = new HashSet<string>();

        // Events
        public System.Action<EquipmentData> OnEquipmentCrafted;
        public System.Action<EquipmentData> OnEquipmentEquipped;
        public System.Action<EquipmentType> OnEquipmentUnequipped;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Attempt to craft equipment by spending orbs.
        /// </summary>
        public bool TryCraftEquipment(EquipmentData equipment)
        {
            if (craftedEquipment.Contains(equipment.equipmentName))
                return false; // Already crafted

            var satchel = SatchelManager.Instance;
            if (satchel == null) return false;

            if (!satchel.HasRecipeOrbs(equipment.craftingRecipe))
                return false;

            satchel.SpendRecipeOrbs(equipment.craftingRecipe);
            craftedEquipment.Add(equipment.equipmentName);
            OnEquipmentCrafted?.Invoke(equipment);

            // Auto-equip if nothing of that type is equipped, or if this is an upgrade
            if (!equippedItems.ContainsKey(equipment.equipmentType) ||
                equipment.IsUpgradeOf(equippedItems[equipment.equipmentType]))
            {
                EquipItem(equipment);
            }

            return true;
        }

        public void EquipItem(EquipmentData equipment)
        {
            equippedItems[equipment.equipmentType] = equipment;

            // Apply satchel bonus if applicable
            if (equipment.extraSatchelSlots > 0)
            {
                SatchelManager.Instance?.AddBonusCapacity(equipment.extraSatchelSlots);
            }

            OnEquipmentEquipped?.Invoke(equipment);
        }

        public bool HasEquipment(EquipmentType type)
        {
            return equippedItems.ContainsKey(type);
        }

        public bool HasEquipment(EquipmentType type, EquipmentTier minimumTier)
        {
            if (!equippedItems.TryGetValue(type, out var equipped))
                return false;
            return equipped.tier >= minimumTier;
        }

        /// <summary>
        /// Check if the player meets all equipment requirements (e.g., for riding wildlife).
        /// </summary>
        public bool MeetsRequirements(EquipmentRequirement[] requirements)
        {
            if (requirements == null || requirements.Length == 0)
                return true;

            foreach (var req in requirements)
            {
                if (!HasEquipment(req.equipmentType, req.minimumTier))
                    return false;
            }
            return true;
        }

        public EquipmentData GetEquipped(EquipmentType type)
        {
            return equippedItems.TryGetValue(type, out var data) ? data : null;
        }

        /// <summary>
        /// Get the maximum wildlife size the player can currently ride.
        /// Based on best saddle equipped.
        /// </summary>
        public WildlifeSize GetMaxRideableSize()
        {
            var saddle = GetEquipped(EquipmentType.Saddle);
            if (saddle == null) return WildlifeSize.Tiny; // Can only ride tiny without saddle
            return saddle.maxRideableSize;
        }

        public float GetSpeedMultiplier()
        {
            float mult = 1f;
            foreach (var kvp in equippedItems)
            {
                mult *= kvp.Value.speedMultiplier;
            }
            return mult;
        }

        public float GetDetectionRangeBonus()
        {
            float bonus = 0f;
            foreach (var kvp in equippedItems)
            {
                bonus += kvp.Value.detectionRangeBonus;
            }
            return bonus;
        }

        public EquipmentData[] GetAllCraftableEquipment()
        {
            return allEquipment;
        }

        public EquipmentData[] GetAvailableCrafts()
        {
            var satchel = SatchelManager.Instance;
            if (satchel == null) return new EquipmentData[0];

            return allEquipment.Where(e =>
                !craftedEquipment.Contains(e.equipmentName) &&
                satchel.HasRecipeOrbs(e.craftingRecipe)).ToArray();
        }

        public bool IsCrafted(EquipmentData equipment)
        {
            return craftedEquipment.Contains(equipment.equipmentName);
        }
    }
}
