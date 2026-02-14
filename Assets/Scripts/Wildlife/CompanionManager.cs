using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using OrbWanderer.Core;
using OrbWanderer.Data;
using OrbWanderer.Equipment;

namespace OrbWanderer.Wildlife
{
    /// <summary>
    /// Manages the player's befriended wildlife companions.
    /// Handles active companion selection, mounting, and companion bonuses.
    /// </summary>
    public class CompanionManager : MonoBehaviour
    {
        public static CompanionManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int maxCompanions = 6;
        [SerializeField] private float companionFollowDistance = 2f;
        [SerializeField] private float companionFollowSpeed = 6f;

        private List<CompanionSlot> companions = new List<CompanionSlot>();
        private CompanionSlot activeCompanion;
        private CompanionSlot currentMount;
        private bool isMounted;

        // Events
        public System.Action<WildlifeData> OnCompanionAdded;
        public System.Action<WildlifeData> OnCompanionRemoved;
        public System.Action<WildlifeData> OnCompanionActivated;
        public System.Action<WildlifeData> OnMounted;
        public System.Action OnDismounted;

        public bool IsMounted => isMounted;
        public WildlifeData ActiveCompanionData => activeCompanion?.wildlifeData;
        public WildlifeData CurrentMountData => currentMount?.wildlifeData;

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
        /// Add a newly befriended wildlife as a companion.
        /// </summary>
        public bool AddCompanion(WildlifeData wildlife, WildlifeController controller)
        {
            if (companions.Count >= maxCompanions)
                return false;

            if (companions.Any(c => c.wildlifeData == wildlife))
                return false;

            var slot = new CompanionSlot
            {
                wildlifeData = wildlife,
                controller = controller,
                friendship = FriendshipLevel.Bonded
            };

            companions.Add(slot);
            OnCompanionAdded?.Invoke(wildlife);

            // Auto-activate if first companion
            if (activeCompanion == null)
            {
                SetActiveCompanion(wildlife);
            }

            return true;
        }

        public void SetActiveCompanion(WildlifeData wildlife)
        {
            var slot = companions.FirstOrDefault(c => c.wildlifeData == wildlife);
            if (slot == null) return;

            activeCompanion = slot;
            OnCompanionActivated?.Invoke(wildlife);
        }

        /// <summary>
        /// Attempt to mount the active companion.
        /// Checks equipment requirements and wildlife size.
        /// </summary>
        public MountResult TryMount()
        {
            if (isMounted)
                return MountResult.AlreadyMounted;

            if (activeCompanion == null)
                return MountResult.NoCompanion;

            if (!activeCompanion.wildlifeData.isRideable)
                return MountResult.NotRideable;

            var equipMgr = EquipmentManager.Instance;

            // Check size requirements
            WildlifeSize maxSize = equipMgr != null ?
                equipMgr.GetMaxRideableSize() : WildlifeSize.Tiny;

            if (activeCompanion.wildlifeData.size > maxSize)
                return MountResult.NeedBetterSaddle;

            // Check equipment requirements
            if (activeCompanion.wildlifeData.ridingRequirements != null &&
                equipMgr != null &&
                !equipMgr.MeetsRequirements(activeCompanion.wildlifeData.ridingRequirements))
            {
                return MountResult.MissingEquipment;
            }

            isMounted = true;
            currentMount = activeCompanion;
            OnMounted?.Invoke(currentMount.wildlifeData);
            return MountResult.Success;
        }

        public void Dismount()
        {
            if (!isMounted) return;

            isMounted = false;
            currentMount = null;
            OnDismounted?.Invoke();
        }

        /// <summary>
        /// Get mount movement properties for the player controller.
        /// </summary>
        public MountProperties GetMountProperties()
        {
            if (!isMounted || currentMount == null)
            {
                return new MountProperties
                {
                    speedMultiplier = 1f,
                    canSwim = false,
                    canFly = false,
                    canClimb = false
                };
            }

            var equipMgr = EquipmentManager.Instance;
            float equipSpeedBonus = equipMgr != null ? equipMgr.GetSpeedMultiplier() : 1f;

            return new MountProperties
            {
                speedMultiplier = currentMount.wildlifeData.mountSpeed * equipSpeedBonus,
                canSwim = currentMount.wildlifeData.canSwim,
                canFly = currentMount.wildlifeData.canFly,
                canClimb = currentMount.wildlifeData.canClimb
            };
        }

        /// <summary>
        /// Get the orb-finding bonus from the active companion.
        /// Harder-to-obtain wildlife provide better bonuses.
        /// </summary>
        public float GetOrbDetectionMultiplier()
        {
            if (activeCompanion == null) return 1f;
            return activeCompanion.wildlifeData.orbDetectionMultiplier;
        }

        public float GetOrbDropRateBonus()
        {
            if (activeCompanion == null) return 0f;
            return activeCompanion.wildlifeData.GetEffectiveOrbBonus();
        }

        public OrbType[] GetSpecializedOrbTypes()
        {
            if (activeCompanion == null) return new OrbType[0];
            return activeCompanion.wildlifeData.specializedOrbTypes ?? new OrbType[0];
        }

        public List<WildlifeData> GetAllCompanions()
        {
            return companions.Select(c => c.wildlifeData).ToList();
        }

        public bool HasCompanion(WildlifeData wildlife)
        {
            return companions.Any(c => c.wildlifeData == wildlife);
        }
    }

    [System.Serializable]
    public class CompanionSlot
    {
        public WildlifeData wildlifeData;
        public WildlifeController controller;
        public FriendshipLevel friendship;
    }

    public struct MountProperties
    {
        public float speedMultiplier;
        public bool canSwim;
        public bool canFly;
        public bool canClimb;
    }

    public enum MountResult
    {
        Success,
        AlreadyMounted,
        NoCompanion,
        NotRideable,
        NeedBetterSaddle,
        MissingEquipment
    }
}
