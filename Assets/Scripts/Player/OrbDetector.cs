using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using OrbWanderer.Core;
using OrbWanderer.Wildlife;
using OrbWanderer.Equipment;
using OrbWanderer.World;

namespace OrbWanderer.Player
{
    /// <summary>
    /// Detects nearby orbs using 3D OverlapSphere and highlights them.
    /// Detection range increases with better companions and equipment.
    /// </summary>
    public class OrbDetector : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] private float baseDetectionRange = 5f;
        [SerializeField] private float scanInterval = 0.5f;
        [SerializeField] private LayerMask orbLayer;

        [Header("Visual Feedback")]
        [SerializeField] private GameObject detectionRadiusIndicator;
        [SerializeField] private bool showDetectionRadius;

        private float scanTimer;
        private List<CollectibleOrb> detectedOrbs = new List<CollectibleOrb>();

        public System.Action<CollectibleOrb> OnOrbDetected;
        public System.Action<CollectibleOrb> OnOrbLost;

        public float EffectiveRange => CalculateEffectiveRange();
        public IReadOnlyList<CollectibleOrb> DetectedOrbs => detectedOrbs;

        private void Update()
        {
            scanTimer += Time.deltaTime;
            if (scanTimer >= scanInterval)
            {
                scanTimer = 0f;
                ScanForOrbs();
            }

            UpdateDetectionIndicator();
        }

        private void ScanForOrbs()
        {
            float range = EffectiveRange;
            var hits = Physics.OverlapSphere(transform.position, range, orbLayer);

            var previousOrbs = new HashSet<CollectibleOrb>(detectedOrbs);
            detectedOrbs.Clear();

            foreach (var hit in hits)
            {
                var orb = hit.GetComponent<CollectibleOrb>();
                if (orb != null && orb.OrbData != null)
                {
                    detectedOrbs.Add(orb);

                    if (!previousOrbs.Contains(orb))
                        OnOrbDetected?.Invoke(orb);

                    previousOrbs.Remove(orb);
                }
            }

            foreach (var lost in previousOrbs)
                OnOrbLost?.Invoke(lost);

            detectedOrbs.Sort((a, b) =>
            {
                float distA = Vector3.Distance(transform.position, a.transform.position);
                float distB = Vector3.Distance(transform.position, b.transform.position);
                return distA.CompareTo(distB);
            });

            HighlightSpecializedOrbs();
        }

        private float CalculateEffectiveRange()
        {
            float range = baseDetectionRange;

            var companion = CompanionManager.Instance;
            if (companion != null)
                range *= companion.GetOrbDetectionMultiplier();

            var equip = EquipmentManager.Instance;
            if (equip != null)
                range += equip.GetDetectionRangeBonus();

            return range;
        }

        private void HighlightSpecializedOrbs()
        {
            var companion = CompanionManager.Instance;
            if (companion == null) return;

            OrbType[] specialTypes = companion.GetSpecializedOrbTypes();
            if (specialTypes.Length == 0) return;

            var specialSet = new HashSet<OrbType>(specialTypes);

            foreach (var orb in detectedOrbs)
            {
                if (specialSet.Contains(orb.OrbData.orbType))
                    OnOrbDetected?.Invoke(orb);
            }
        }

        private void UpdateDetectionIndicator()
        {
            if (detectionRadiusIndicator == null) return;

            detectionRadiusIndicator.SetActive(showDetectionRadius);
            if (showDetectionRadius)
            {
                float range = EffectiveRange;
                detectionRadiusIndicator.transform.localScale = Vector3.one * range * 2f;
            }
        }

        public CollectibleOrb GetNearestOrb()
        {
            return detectedOrbs.FirstOrDefault();
        }

        public CollectibleOrb GetNearestOrbOfType(OrbType type)
        {
            return detectedOrbs.FirstOrDefault(o => o.OrbData.orbType == type);
        }
    }
}
