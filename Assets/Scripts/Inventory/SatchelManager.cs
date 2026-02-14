using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using OrbWanderer.Core;
using OrbWanderer.Data;

namespace OrbWanderer.Inventory
{
    /// <summary>
    /// Manages the player's satchel (inventory) of collected orbs.
    /// Tracks orb counts by type and rarity, handles capacity limits.
    /// </summary>
    public class SatchelManager : MonoBehaviour
    {
        public static SatchelManager Instance { get; private set; }

        [Header("Capacity")]
        [SerializeField] private int baseCapacity = 50;

        private Dictionary<OrbType, int> orbCounts = new Dictionary<OrbType, int>();
        private List<OrbEntry> orbEntries = new List<OrbEntry>();
        private int bonusCapacity;

        // Events
        public System.Action<OrbData, int> OnOrbAdded;
        public System.Action<OrbType, int> OnOrbRemoved;
        public System.Action OnSatchelChanged;
        public System.Action OnSatchelFull;

        public int TotalCapacity => baseCapacity + bonusCapacity;
        public int CurrentCount => orbEntries.Count;
        public bool IsFull => CurrentCount >= TotalCapacity;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeOrbCounts();
        }

        private void InitializeOrbCounts()
        {
            foreach (OrbType type in System.Enum.GetValues(typeof(OrbType)))
            {
                orbCounts[type] = 0;
            }
        }

        /// <summary>
        /// Try to add an orb to the satchel. Returns false if satchel is full.
        /// </summary>
        public bool TryAddOrb(OrbData orbData)
        {
            if (IsFull)
            {
                OnSatchelFull?.Invoke();
                return false;
            }

            var entry = new OrbEntry
            {
                orbData = orbData,
                collectedTime = Time.time
            };

            orbEntries.Add(entry);
            orbCounts[orbData.orbType]++;

            OnOrbAdded?.Invoke(orbData, orbCounts[orbData.orbType]);
            OnSatchelChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Remove a specific number of orbs of a given type.
        /// Used when spending orbs on unlocks or befriending wildlife.
        /// </summary>
        public bool RemoveOrbs(OrbType type, int amount)
        {
            if (orbCounts[type] < amount)
                return false;

            int removed = 0;
            for (int i = orbEntries.Count - 1; i >= 0 && removed < amount; i--)
            {
                if (orbEntries[i].orbData.orbType == type)
                {
                    orbEntries.RemoveAt(i);
                    removed++;
                }
            }

            orbCounts[type] -= removed;
            OnOrbRemoved?.Invoke(type, orbCounts[type]);
            OnSatchelChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Remove orbs matching a specific type and rarity.
        /// </summary>
        public bool RemoveOrbs(OrbType type, OrbRarity rarity, int amount)
        {
            int available = orbEntries.Count(e =>
                e.orbData.orbType == type && e.orbData.rarity == rarity);

            if (available < amount) return false;

            int removed = 0;
            for (int i = orbEntries.Count - 1; i >= 0 && removed < amount; i--)
            {
                if (orbEntries[i].orbData.orbType == type &&
                    orbEntries[i].orbData.rarity == rarity)
                {
                    orbEntries.RemoveAt(i);
                    removed++;
                }
            }

            orbCounts[type] -= removed;
            OnOrbRemoved?.Invoke(type, orbCounts[type]);
            OnSatchelChanged?.Invoke();
            return true;
        }

        public int GetOrbCount(OrbType type)
        {
            return orbCounts.TryGetValue(type, out int count) ? count : 0;
        }

        public Dictionary<OrbType, int> GetOrbCounts()
        {
            return new Dictionary<OrbType, int>(orbCounts);
        }

        public bool HasOrbs(OrbType type, int amount)
        {
            return GetOrbCount(type) >= amount;
        }

        /// <summary>
        /// Check if the satchel has all orbs required by a recipe.
        /// </summary>
        public bool HasRecipeOrbs(OrbRecipe recipe)
        {
            if (recipe == null || recipe.requirements == null) return true;
            return recipe.CanCraft(orbCounts);
        }

        /// <summary>
        /// Spend orbs according to a recipe. Returns false if insufficient orbs.
        /// </summary>
        public bool SpendRecipeOrbs(OrbRecipe recipe)
        {
            if (!HasRecipeOrbs(recipe)) return false;

            foreach (var req in recipe.requirements)
            {
                RemoveOrbs(req.orbType, req.amount);
            }
            return true;
        }

        public void AddBonusCapacity(int amount)
        {
            bonusCapacity += amount;
            OnSatchelChanged?.Invoke();
        }

        /// <summary>
        /// Get a summary of orbs grouped by type for UI display.
        /// </summary>
        public List<OrbSummary> GetOrbSummary()
        {
            var summary = new List<OrbSummary>();
            foreach (var kvp in orbCounts)
            {
                if (kvp.Value > 0)
                {
                    summary.Add(new OrbSummary
                    {
                        orbType = kvp.Key,
                        count = kvp.Value,
                        entries = orbEntries.Where(e => e.orbData.orbType == kvp.Key).ToList()
                    });
                }
            }
            return summary.OrderByDescending(s => s.count).ToList();
        }

        #region Save/Load

        public SatchelSaveData GetSaveData()
        {
            var data = new SatchelSaveData();
            data.orbTypeNames = new List<string>();
            data.orbDataNames = new List<string>();
            data.bonusCapacity = bonusCapacity;

            foreach (var entry in orbEntries)
            {
                data.orbTypeNames.Add(entry.orbData.orbType.ToString());
                data.orbDataNames.Add(entry.orbData.name);
            }
            return data;
        }

        #endregion
    }

    [System.Serializable]
    public class OrbEntry
    {
        public OrbData orbData;
        public float collectedTime;
    }

    [System.Serializable]
    public class OrbSummary
    {
        public OrbType orbType;
        public int count;
        public List<OrbEntry> entries;
    }

    [System.Serializable]
    public class SatchelSaveData
    {
        public List<string> orbTypeNames;
        public List<string> orbDataNames;
        public int bonusCapacity;
    }
}
