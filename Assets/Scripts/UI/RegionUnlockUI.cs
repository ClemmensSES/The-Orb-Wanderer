using UnityEngine;
using UnityEngine.UI;
using OrbWanderer.Core;
using OrbWanderer.Data;
using OrbWanderer.Inventory;
using OrbWanderer.World;

namespace OrbWanderer.UI
{
    /// <summary>
    /// Popup UI shown when the player taps on a discovered (locked) region on the map.
    /// Displays required orbs and allows the player to spend orbs to unlock.
    /// </summary>
    public class RegionUnlockUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Canvas unlockCanvas;
        [SerializeField] private Text regionNameText;
        [SerializeField] private Text regionDescriptionText;
        [SerializeField] private Text regionTypeText;
        [SerializeField] private RectTransform requirementsContainer;
        [SerializeField] private GameObject requirementSlotPrefab;
        [SerializeField] private Button unlockButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text unlockButtonText;

        private RegionData currentRegion;

        private void Start()
        {
            if (unlockButton != null)
                unlockButton.onClick.AddListener(OnUnlockPressed);

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            Close();
        }

        public void ShowRegion(RegionData region)
        {
            currentRegion = region;
            unlockCanvas.gameObject.SetActive(true);

            if (regionNameText != null)
                regionNameText.text = region.regionName;

            if (regionDescriptionText != null)
                regionDescriptionText.text = region.description ?? "";

            if (regionTypeText != null)
                regionTypeText.text = region.regionType.ToString();

            PopulateRequirements(region);
            UpdateUnlockButton(region);
        }

        private void PopulateRequirements(RegionData region)
        {
            // Clear existing slots
            foreach (Transform child in requirementsContainer)
            {
                Destroy(child.gameObject);
            }

            if (region.unlockRecipe == null || region.unlockRecipe.requirements == null)
            {
                // No orbs needed
                var freeObj = Instantiate(requirementSlotPrefab, requirementsContainer);
                var freeText = freeObj.GetComponentInChildren<Text>();
                if (freeText != null) freeText.text = "No orbs required!";
                return;
            }

            var satchel = SatchelManager.Instance;

            foreach (var req in region.unlockRecipe.requirements)
            {
                var slotObj = Instantiate(requirementSlotPrefab, requirementsContainer);

                var texts = slotObj.GetComponentsInChildren<Text>();
                var images = slotObj.GetComponentsInChildren<Image>();

                // Show orb type name and count
                if (texts.Length > 0)
                {
                    int have = satchel != null ? satchel.GetOrbCount(req.orbType) : 0;
                    string color = have >= req.amount ? "#00FF00" : "#FF0000";
                    texts[0].text = $"{req.orbType}: <color={color}>{have}</color> / {req.amount}";
                }

                // Color the icon
                if (images.Length > 1)
                {
                    images[1].color = SatchelUI.GetOrbTypeColor(req.orbType);
                }
            }
        }

        private void UpdateUnlockButton(RegionData region)
        {
            var worldMap = WorldMapManager.Instance;
            var satchel = SatchelManager.Instance;

            if (worldMap == null || satchel == null)
            {
                unlockButton.interactable = false;
                return;
            }

            RegionState state = worldMap.GetRegionState(region);

            if (state == RegionState.Discovered)
            {
                bool canAfford = region.unlockRecipe == null ||
                    region.unlockRecipe.requirements == null ||
                    satchel.HasRecipeOrbs(region.unlockRecipe);

                bool prereqsMet = region.ArePrerequisitesMet(
                    new System.Collections.Generic.HashSet<string>(
                        System.Array.ConvertAll(worldMap.GetUnlockedRegions(), r => r.regionName)));

                unlockButton.interactable = canAfford && prereqsMet;
                unlockButtonText.text = canAfford ? "Unlock Region" : "Need More Orbs";

                if (!prereqsMet)
                    unlockButtonText.text = "Prerequisites Not Met";
            }
            else if (state == RegionState.Unlocked || state == RegionState.Completed)
            {
                unlockButtonText.text = "Travel Here";
                unlockButton.interactable = true;
            }
            else
            {
                unlockButton.interactable = false;
                unlockButtonText.text = "Locked";
            }
        }

        private void OnUnlockPressed()
        {
            if (currentRegion == null) return;

            var worldMap = WorldMapManager.Instance;
            if (worldMap == null) return;

            RegionState state = worldMap.GetRegionState(currentRegion);

            if (state == RegionState.Discovered)
            {
                if (worldMap.TryUnlockRegion(currentRegion))
                {
                    // Region unlocked - refresh UI
                    PopulateRequirements(currentRegion);
                    UpdateUnlockButton(currentRegion);
                }
            }
            else if (state == RegionState.Unlocked || state == RegionState.Completed)
            {
                // Fast travel to region
                Close();
            }
        }

        public void Close()
        {
            unlockCanvas.gameObject.SetActive(false);
            currentRegion = null;
        }
    }
}
