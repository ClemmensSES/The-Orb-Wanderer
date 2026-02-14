using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using OrbWanderer.Core;
using OrbWanderer.Data;
using OrbWanderer.Inventory;

namespace OrbWanderer.World
{
    /// <summary>
    /// Manages the world map state: which regions are locked, discovered, unlocked.
    /// Handles fog-of-war reveal and region unlock logic.
    /// </summary>
    public class WorldMapManager : MonoBehaviour
    {
        public static WorldMapManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private RegionData[] allRegions;
        [SerializeField] private RegionData startingRegion;

        [Header("Map Settings")]
        [SerializeField] private Vector2Int mapGridSize = new Vector2Int(20, 20);

        private Dictionary<string, RegionState> regionStates = new Dictionary<string, RegionState>();
        private HashSet<string> unlockedRegionNames = new HashSet<string>();

        // Events
        public System.Action<RegionData, RegionState> OnRegionStateChanged;
        public System.Action<RegionData> OnRegionUnlocked;
        public System.Action<RegionData> OnRegionDiscovered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeMap();
        }

        private void InitializeMap()
        {
            foreach (var region in allRegions)
            {
                regionStates[region.regionName] = RegionState.Locked;
            }

            // Starting region is always unlocked
            if (startingRegion != null)
            {
                UnlockRegion(startingRegion, skipRecipeCheck: true);
                DiscoverAdjacentRegions(startingRegion);
            }
        }

        /// <summary>
        /// Attempt to unlock a region by spending orbs from the player's satchel.
        /// </summary>
        public bool TryUnlockRegion(RegionData region)
        {
            if (GetRegionState(region) != RegionState.Discovered)
                return false;

            if (!region.ArePrerequisitesMet(unlockedRegionNames))
                return false;

            if (region.unlockRecipe == null || region.unlockRecipe.requirements == null)
            {
                UnlockRegion(region);
                return true;
            }

            var satchel = SatchelManager.Instance;
            if (satchel == null) return false;

            var available = satchel.GetOrbCounts();
            if (!region.unlockRecipe.CanCraft(available))
                return false;

            // Spend the orbs
            foreach (var req in region.unlockRecipe.requirements)
            {
                satchel.RemoveOrbs(req.orbType, req.amount);
            }

            UnlockRegion(region);
            return true;
        }

        private void UnlockRegion(RegionData region, bool skipRecipeCheck = false)
        {
            SetRegionState(region, RegionState.Unlocked);
            unlockedRegionNames.Add(region.regionName);
            OnRegionUnlocked?.Invoke(region);
            DiscoverAdjacentRegions(region);
        }

        /// <summary>
        /// When a region is unlocked, neighboring regions become "Discovered" (visible but locked).
        /// </summary>
        private void DiscoverAdjacentRegions(RegionData unlockedRegion)
        {
            foreach (var region in allRegions)
            {
                if (GetRegionState(region) != RegionState.Locked)
                    continue;

                if (IsAdjacent(unlockedRegion, region))
                {
                    SetRegionState(region, RegionState.Discovered);
                    OnRegionDiscovered?.Invoke(region);
                }
            }
        }

        private bool IsAdjacent(RegionData a, RegionData b)
        {
            var diff = a.mapPosition - b.mapPosition;
            // Consider regions adjacent if within 2 grid units (accounts for larger regions)
            return Mathf.Abs(diff.x) <= (a.mapSize.x + 1) && Mathf.Abs(diff.y) <= (a.mapSize.y + 1)
                && (Mathf.Abs(diff.x) + Mathf.Abs(diff.y)) > 0;
        }

        public RegionState GetRegionState(RegionData region)
        {
            return regionStates.TryGetValue(region.regionName, out var state) ? state : RegionState.Locked;
        }

        public RegionState GetRegionState(string regionName)
        {
            return regionStates.TryGetValue(regionName, out var state) ? state : RegionState.Locked;
        }

        private void SetRegionState(RegionData region, RegionState newState)
        {
            var oldState = GetRegionState(region);
            if (oldState == newState) return;

            regionStates[region.regionName] = newState;
            OnRegionStateChanged?.Invoke(region, newState);
        }

        public void MarkRegionCompleted(RegionData region)
        {
            if (GetRegionState(region) == RegionState.Unlocked)
            {
                SetRegionState(region, RegionState.Completed);
            }
        }

        public RegionData[] GetAllRegions() => allRegions;

        public RegionData[] GetUnlockedRegions()
        {
            return allRegions.Where(r => GetRegionState(r) == RegionState.Unlocked
                                      || GetRegionState(r) == RegionState.Completed).ToArray();
        }

        public RegionData[] GetDiscoveredRegions()
        {
            return allRegions.Where(r => GetRegionState(r) == RegionState.Discovered).ToArray();
        }

        public RegionData GetRegionAtPosition(Vector2Int pos)
        {
            return allRegions.FirstOrDefault(r =>
                pos.x >= r.mapPosition.x && pos.x < r.mapPosition.x + r.mapSize.x &&
                pos.y >= r.mapPosition.y && pos.y < r.mapPosition.y + r.mapSize.y);
        }

        public float GetExplorationProgress()
        {
            if (allRegions == null || allRegions.Length == 0) return 0f;
            int unlocked = allRegions.Count(r =>
                GetRegionState(r) == RegionState.Unlocked || GetRegionState(r) == RegionState.Completed);
            return (float)unlocked / allRegions.Length;
        }

        #region Save/Load

        public WorldMapSaveData GetSaveData()
        {
            var data = new WorldMapSaveData();
            data.regionStates = new List<RegionStatePair>();
            foreach (var kvp in regionStates)
            {
                data.regionStates.Add(new RegionStatePair
                {
                    regionName = kvp.Key,
                    state = kvp.Value
                });
            }
            return data;
        }

        public void LoadSaveData(WorldMapSaveData data)
        {
            regionStates.Clear();
            unlockedRegionNames.Clear();

            foreach (var pair in data.regionStates)
            {
                regionStates[pair.regionName] = pair.state;
                if (pair.state == RegionState.Unlocked || pair.state == RegionState.Completed)
                {
                    unlockedRegionNames.Add(pair.regionName);
                }
            }
        }

        #endregion
    }

    [System.Serializable]
    public class WorldMapSaveData
    {
        public List<RegionStatePair> regionStates;
    }

    [System.Serializable]
    public class RegionStatePair
    {
        public string regionName;
        public RegionState state;
    }
}
