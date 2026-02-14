using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OrbWanderer.Core;
using OrbWanderer.Inventory;

namespace OrbWanderer.UI
{
    /// <summary>
    /// Displays the player's satchel contents.
    /// Shows orb counts by type with visual icons and capacity bar.
    /// </summary>
    public class SatchelUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Canvas satchelCanvas;
        [SerializeField] private RectTransform contentContainer;
        [SerializeField] private GameObject orbSlotPrefab;

        [Header("Capacity Bar")]
        [SerializeField] private Slider capacityBar;
        [SerializeField] private Text capacityText;
        [SerializeField] private Color normalColor = Color.green;
        [SerializeField] private Color warningColor = Color.yellow;
        [SerializeField] private Color fullColor = Color.red;

        [Header("Quick View")]
        [SerializeField] private RectTransform quickViewBar;
        [SerializeField] private GameObject quickViewSlotPrefab;

        private Dictionary<OrbType, OrbSlotUI> orbSlots = new Dictionary<OrbType, OrbSlotUI>();
        private bool isOpen;

        public bool IsOpen => isOpen;

        private void Start()
        {
            var satchel = SatchelManager.Instance;
            if (satchel != null)
            {
                satchel.OnSatchelChanged += RefreshUI;
                satchel.OnSatchelFull += ShowFullWarning;
            }

            CreateOrbSlots();
            CloseSatchel();
        }

        private void CreateOrbSlots()
        {
            if (orbSlotPrefab == null || contentContainer == null) return;

            foreach (OrbType type in System.Enum.GetValues(typeof(OrbType)))
            {
                var slotObj = Instantiate(orbSlotPrefab, contentContainer);
                var slot = new OrbSlotUI
                {
                    orbType = type,
                    gameObject = slotObj,
                    countText = slotObj.GetComponentInChildren<Text>(),
                    icon = slotObj.GetComponentInChildren<Image>(),
                    nameLabel = slotObj.transform.Find("Name")?.GetComponent<Text>()
                };

                if (slot.nameLabel != null) slot.nameLabel.text = type.ToString();
                if (slot.countText != null) slot.countText.text = "0";

                // Set orb type color
                if (slot.icon != null)
                {
                    slot.icon.color = GetOrbTypeColor(type);
                }

                orbSlots[type] = slot;
            }
        }

        public void ToggleSatchel()
        {
            if (isOpen) CloseSatchel();
            else OpenSatchel();
        }

        public void OpenSatchel()
        {
            isOpen = true;
            satchelCanvas.gameObject.SetActive(true);
            RefreshUI();
        }

        public void CloseSatchel()
        {
            isOpen = false;
            satchelCanvas.gameObject.SetActive(false);
        }

        private void RefreshUI()
        {
            var satchel = SatchelManager.Instance;
            if (satchel == null) return;

            // Update orb counts
            foreach (var kvp in orbSlots)
            {
                int count = satchel.GetOrbCount(kvp.Key);
                if (kvp.Value.countText != null)
                {
                    kvp.Value.countText.text = count.ToString();
                }

                // Dim slots with 0 orbs
                if (kvp.Value.gameObject != null)
                {
                    var cg = kvp.Value.gameObject.GetComponent<CanvasGroup>();
                    if (cg == null) cg = kvp.Value.gameObject.AddComponent<CanvasGroup>();
                    cg.alpha = count > 0 ? 1f : 0.4f;
                }
            }

            // Update capacity bar
            if (capacityBar != null)
            {
                float ratio = (float)satchel.CurrentCount / satchel.TotalCapacity;
                capacityBar.value = ratio;

                var fillImage = capacityBar.fillRect.GetComponent<Image>();
                if (fillImage != null)
                {
                    if (ratio >= 1f) fillImage.color = fullColor;
                    else if (ratio >= 0.8f) fillImage.color = warningColor;
                    else fillImage.color = normalColor;
                }
            }

            if (capacityText != null)
            {
                capacityText.text = $"{satchel.CurrentCount} / {satchel.TotalCapacity}";
            }
        }

        private void ShowFullWarning()
        {
            // Flash the capacity bar red, show a message
            if (capacityBar != null)
            {
                var fillImage = capacityBar.fillRect.GetComponent<Image>();
                if (fillImage != null) fillImage.color = fullColor;
            }
        }

        public static Color GetOrbTypeColor(OrbType type)
        {
            return type switch
            {
                OrbType.Nature => new Color(0.2f, 0.8f, 0.3f),
                OrbType.Terra => new Color(0.6f, 0.4f, 0.2f),
                OrbType.Aqua => new Color(0.2f, 0.5f, 0.9f),
                OrbType.Ember => new Color(0.9f, 0.3f, 0.1f),
                OrbType.Zephyr => new Color(0.7f, 0.9f, 1f),
                OrbType.Frost => new Color(0.6f, 0.8f, 1f),
                OrbType.Shadow => new Color(0.3f, 0.1f, 0.4f),
                OrbType.Radiant => new Color(1f, 0.95f, 0.5f),
                OrbType.Storm => new Color(0.5f, 0.3f, 0.8f),
                OrbType.Prismatic => Color.white,
                _ => Color.gray
            };
        }

        private void OnDestroy()
        {
            var satchel = SatchelManager.Instance;
            if (satchel != null)
            {
                satchel.OnSatchelChanged -= RefreshUI;
                satchel.OnSatchelFull -= ShowFullWarning;
            }
        }
    }

    public class OrbSlotUI
    {
        public OrbType orbType;
        public GameObject gameObject;
        public Text countText;
        public Image icon;
        public Text nameLabel;
    }
}
