using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OrbWanderer.Core;
using OrbWanderer.Data;
using OrbWanderer.World;

namespace OrbWanderer.UI
{
    /// <summary>
    /// Renders the world map overlay with fog-of-war.
    /// Locked regions are grayed out, discovered regions show their outline,
    /// unlocked regions are fully colored.
    /// </summary>
    public class WorldMapUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Canvas mapCanvas;
        [SerializeField] private RectTransform mapContainer;
        [SerializeField] private GameObject regionTilePrefab;
        [SerializeField] private GameObject playerMarkerPrefab;

        [Header("Colors")]
        [SerializeField] private Color lockedColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color discoveredColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
        [SerializeField] private Color completedOverlay = new Color(1f, 1f, 0f, 0.2f);

        [Header("Map Settings")]
        [SerializeField] private float tileSize = 50f;
        [SerializeField] private float zoomSpeed = 0.1f;
        [SerializeField] private float minZoom = 0.5f;
        [SerializeField] private float maxZoom = 3f;

        private Dictionary<string, RegionMapTile> regionTiles = new Dictionary<string, RegionMapTile>();
        private GameObject playerMarker;
        private float currentZoom = 1f;
        private Vector2 panOffset;
        private bool isOpen;

        public bool IsOpen => isOpen;
        public System.Action<RegionData> OnRegionSelected;

        private void Start()
        {
            var worldMap = WorldMapManager.Instance;
            if (worldMap == null) return;

            worldMap.OnRegionStateChanged += HandleRegionStateChanged;
            worldMap.OnRegionUnlocked += HandleRegionUnlocked;
            worldMap.OnRegionDiscovered += HandleRegionDiscovered;

            BuildMap();
            CloseMap();
        }

        private void Update()
        {
            if (!isOpen) return;

            HandleMapInput();
            UpdatePlayerMarker();
        }

        public void ToggleMap()
        {
            if (isOpen) CloseMap();
            else OpenMap();
        }

        public void OpenMap()
        {
            isOpen = true;
            mapCanvas.gameObject.SetActive(true);
            RefreshAllTiles();

            // Pause gameplay
            Time.timeScale = 0f;
        }

        public void CloseMap()
        {
            isOpen = false;
            mapCanvas.gameObject.SetActive(false);
            Time.timeScale = 1f;
        }

        private void BuildMap()
        {
            var worldMap = WorldMapManager.Instance;
            if (worldMap == null) return;

            foreach (var region in worldMap.GetAllRegions())
            {
                CreateRegionTile(region);
            }

            // Create player position marker
            if (playerMarkerPrefab != null)
            {
                playerMarker = Instantiate(playerMarkerPrefab, mapContainer);
            }
        }

        private void CreateRegionTile(RegionData region)
        {
            if (regionTilePrefab == null) return;

            var tileObj = Instantiate(regionTilePrefab, mapContainer);
            var rectTransform = tileObj.GetComponent<RectTransform>();

            // Position tile on the grid
            Vector2 pos = new Vector2(
                region.mapPosition.x * tileSize,
                region.mapPosition.y * tileSize
            );
            rectTransform.anchoredPosition = pos;
            rectTransform.sizeDelta = new Vector2(
                region.mapSize.x * tileSize,
                region.mapSize.y * tileSize
            );

            var tile = new RegionMapTile
            {
                regionData = region,
                gameObject = tileObj,
                rectTransform = rectTransform,
                image = tileObj.GetComponent<Image>(),
                button = tileObj.GetComponent<Button>(),
                label = tileObj.GetComponentInChildren<Text>()
            };

            // Set up button click
            if (tile.button != null)
            {
                var regionRef = region; // Capture for closure
                tile.button.onClick.AddListener(() => HandleTileClicked(regionRef));
            }

            regionTiles[region.regionName] = tile;
            UpdateTileVisuals(tile);
        }

        private void UpdateTileVisuals(RegionMapTile tile)
        {
            var worldMap = WorldMapManager.Instance;
            if (worldMap == null) return;

            RegionState state = worldMap.GetRegionState(tile.regionData);

            if (tile.image != null)
            {
                switch (state)
                {
                    case RegionState.Locked:
                        tile.image.color = lockedColor;
                        if (tile.label != null) tile.label.text = "???";
                        break;

                    case RegionState.Discovered:
                        tile.image.color = discoveredColor;
                        if (tile.label != null) tile.label.text = tile.regionData.regionName;
                        break;

                    case RegionState.Unlocked:
                        tile.image.color = tile.regionData.mapColor;
                        if (tile.label != null) tile.label.text = tile.regionData.regionName;
                        break;

                    case RegionState.Completed:
                        tile.image.color = tile.regionData.mapColor * 0.8f + completedOverlay;
                        if (tile.label != null) tile.label.text = tile.regionData.regionName + " *";
                        break;
                }
            }

            // Icon overlay
            if (tile.regionData.mapIcon != null && state != RegionState.Locked)
            {
                // Apply icon in sub-image if available
            }
        }

        private void RefreshAllTiles()
        {
            foreach (var tile in regionTiles.Values)
            {
                UpdateTileVisuals(tile);
            }
        }

        private void HandleTileClicked(RegionData region)
        {
            var worldMap = WorldMapManager.Instance;
            if (worldMap == null) return;

            RegionState state = worldMap.GetRegionState(region);

            switch (state)
            {
                case RegionState.Discovered:
                    // Show unlock requirements and attempt unlock
                    OnRegionSelected?.Invoke(region);
                    break;

                case RegionState.Unlocked:
                case RegionState.Completed:
                    // Show region details / fast travel option
                    OnRegionSelected?.Invoke(region);
                    break;

                case RegionState.Locked:
                    // Show "this region is undiscovered" message
                    break;
            }
        }

        private void HandleMapInput()
        {
            // Pinch to zoom (mobile)
            if (Input.touchCount == 2)
            {
                Touch t0 = Input.GetTouch(0);
                Touch t1 = Input.GetTouch(1);

                float prevDist = ((t0.position - t0.deltaPosition) - (t1.position - t1.deltaPosition)).magnitude;
                float currDist = (t0.position - t1.position).magnitude;

                float diff = currDist - prevDist;
                currentZoom = Mathf.Clamp(currentZoom + diff * zoomSpeed * 0.01f, minZoom, maxZoom);
                mapContainer.localScale = Vector3.one * currentZoom;
            }

            // Scroll wheel zoom (editor)
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                currentZoom = Mathf.Clamp(currentZoom + scroll * zoomSpeed * 10f, minZoom, maxZoom);
                mapContainer.localScale = Vector3.one * currentZoom;
            }

            // Pan with drag
            if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved)
            {
                panOffset += Input.GetTouch(0).deltaPosition;
                mapContainer.anchoredPosition = panOffset;
            }
            else if (Input.GetMouseButton(0))
            {
                panOffset += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 5f;
                mapContainer.anchoredPosition = panOffset;
            }
        }

        private void UpdatePlayerMarker()
        {
            if (playerMarker == null) return;

            var player = Player.PlayerController.Instance;
            if (player == null) return;

            // Convert world position to map position
            Vector2 mapPos = new Vector2(
                player.transform.position.x * tileSize / 10f, // Approximate world-to-map scale
                player.transform.position.y * tileSize / 10f
            );

            playerMarker.GetComponent<RectTransform>().anchoredPosition = mapPos;
        }

        #region Event Handlers

        private void HandleRegionStateChanged(RegionData region, RegionState newState)
        {
            if (regionTiles.TryGetValue(region.regionName, out var tile))
            {
                UpdateTileVisuals(tile);
            }
        }

        private void HandleRegionUnlocked(RegionData region)
        {
            // Play unlock animation on tile
            if (regionTiles.TryGetValue(region.regionName, out var tile))
            {
                // Trigger reveal animation
                UpdateTileVisuals(tile);
            }
        }

        private void HandleRegionDiscovered(RegionData region)
        {
            if (regionTiles.TryGetValue(region.regionName, out var tile))
            {
                UpdateTileVisuals(tile);
            }
        }

        #endregion

        private void OnDestroy()
        {
            var worldMap = WorldMapManager.Instance;
            if (worldMap != null)
            {
                worldMap.OnRegionStateChanged -= HandleRegionStateChanged;
                worldMap.OnRegionUnlocked -= HandleRegionUnlocked;
                worldMap.OnRegionDiscovered -= HandleRegionDiscovered;
            }
        }
    }

    public class RegionMapTile
    {
        public RegionData regionData;
        public GameObject gameObject;
        public RectTransform rectTransform;
        public Image image;
        public Button button;
        public Text label;
    }
}
