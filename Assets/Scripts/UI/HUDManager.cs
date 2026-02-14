using UnityEngine;
using UnityEngine.UI;
using OrbWanderer.Core;
using OrbWanderer.Inventory;
using OrbWanderer.Wildlife;

namespace OrbWanderer.UI
{
    /// <summary>
    /// Manages the always-visible HUD elements: mini orb counter,
    /// active companion display, mount indicator, and notification toasts.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        public static HUDManager Instance { get; private set; }

        [Header("Orb Counter")]
        [SerializeField] private Text totalOrbText;
        [SerializeField] private Image[] orbTypeIcons;

        [Header("Companion Display")]
        [SerializeField] private GameObject companionPanel;
        [SerializeField] private Image companionIcon;
        [SerializeField] private Text companionNameText;
        [SerializeField] private Slider friendshipBar;

        [Header("Mount Indicator")]
        [SerializeField] private GameObject mountIndicator;
        [SerializeField] private Text mountSpeedText;

        [Header("Notifications")]
        [SerializeField] private RectTransform notificationContainer;
        [SerializeField] private GameObject notificationPrefab;
        [SerializeField] private float notificationDuration = 3f;

        [Header("Region Name")]
        [SerializeField] private Text currentRegionText;
        [SerializeField] private CanvasGroup regionNameGroup;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            var satchel = SatchelManager.Instance;
            if (satchel != null)
            {
                satchel.OnSatchelChanged += UpdateOrbCounter;
                satchel.OnOrbAdded += OnOrbCollected;
            }

            var companion = CompanionManager.Instance;
            if (companion != null)
            {
                companion.OnCompanionActivated += UpdateCompanionDisplay;
                companion.OnMounted += OnMounted;
                companion.OnDismounted += OnDismounted;
            }

            UpdateOrbCounter();
            UpdateCompanionDisplay(null);
        }

        private void UpdateOrbCounter()
        {
            var satchel = SatchelManager.Instance;
            if (satchel == null) return;

            if (totalOrbText != null)
            {
                totalOrbText.text = $"{satchel.CurrentCount}/{satchel.TotalCapacity}";
            }
        }

        private void OnOrbCollected(Data.OrbData orb, int newCount)
        {
            ShowNotification($"+1 {orb.orbName}", SatchelUI.GetOrbTypeColor(orb.orbType));
        }

        private void UpdateCompanionDisplay(Data.WildlifeData wildlife)
        {
            if (companionPanel == null) return;

            if (wildlife == null)
            {
                companionPanel.SetActive(false);
                return;
            }

            companionPanel.SetActive(true);

            if (companionIcon != null && wildlife.icon != null)
                companionIcon.sprite = wildlife.icon;

            if (companionNameText != null)
                companionNameText.text = wildlife.wildlifeName;
        }

        private void OnMounted(Data.WildlifeData mount)
        {
            if (mountIndicator != null) mountIndicator.SetActive(true);

            if (mountSpeedText != null)
                mountSpeedText.text = $"Speed: {mount.mountSpeed:F1}x";
        }

        private void OnDismounted()
        {
            if (mountIndicator != null) mountIndicator.SetActive(false);
        }

        public void ShowRegionName(string regionName)
        {
            if (currentRegionText != null)
                currentRegionText.text = regionName;

            if (regionNameGroup != null)
            {
                regionNameGroup.alpha = 1f;
                // Fade out after a delay - use coroutine in production
            }
        }

        public void ShowNotification(string message, Color color = default)
        {
            if (notificationPrefab == null || notificationContainer == null) return;

            var notifObj = Instantiate(notificationPrefab, notificationContainer);
            var text = notifObj.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = message;
                if (color != default) text.color = color;
            }

            Destroy(notifObj, notificationDuration);
        }

        private void OnDestroy()
        {
            var satchel = SatchelManager.Instance;
            if (satchel != null)
            {
                satchel.OnSatchelChanged -= UpdateOrbCounter;
                satchel.OnOrbAdded -= OnOrbCollected;
            }

            var companion = CompanionManager.Instance;
            if (companion != null)
            {
                companion.OnCompanionActivated -= UpdateCompanionDisplay;
                companion.OnMounted -= OnMounted;
                companion.OnDismounted -= OnDismounted;
            }
        }
    }
}
