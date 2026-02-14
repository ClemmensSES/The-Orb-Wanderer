using UnityEngine;
using OrbWanderer.Core;
using OrbWanderer.Data;
using OrbWanderer.Wildlife;

namespace OrbWanderer.World
{
    /// <summary>
    /// Generates the entire alien archipelago world at runtime.
    /// Creates themed islands, ocean, orb spawners, creature spawners,
    /// and connects everything together.
    /// </summary>
    public class AlienWorldGenerator : MonoBehaviour
    {
        [Header("World Layout")]
        [SerializeField] private float islandSpacing = 35f;
        [SerializeField] private float baseIslandRadius = 12f;
        [SerializeField] private float islandRadiusVariation = 4f;

        [Header("References")]
        [SerializeField] private RegionData[] allRegions;

        private void Start()
        {
            GenerateWorld();
        }

        public void SetRegions(RegionData[] regions)
        {
            allRegions = regions;
        }

        public void GenerateWorld()
        {
            if (allRegions == null || allRegions.Length == 0)
            {
                // Try loading from resources
                allRegions = Resources.LoadAll<RegionData>("GameData/Regions");
            }

            if (allRegions == null || allRegions.Length == 0)
            {
                Debug.LogWarning("No regions found! Run OrbWanderer > Create Default Data first.");
                return;
            }

            // Create ocean
            CreateOcean();

            // Load all orbs for spawning
            var allOrbs = Resources.LoadAll<OrbData>("GameData/Orbs");

            // Generate each island
            for (int i = 0; i < allRegions.Length; i++)
            {
                var region = allRegions[i];
                Vector2 worldPos = CalculateIslandPosition(region, i);
                float radius = baseIslandRadius + Random.Range(-islandRadiusVariation, islandRadiusVariation);

                CreateIsland(region, worldPos, radius, allOrbs);
            }

            // Apply player sprite
            ApplyPlayerSprite();

            Debug.Log($"Alien world generated! {allRegions.Length} islands created.");
        }

        private Vector2 CalculateIslandPosition(RegionData region, int index)
        {
            // Use the region's map position scaled by island spacing
            // This creates a nice archipelago layout
            return new Vector2(
                region.mapPosition.x * islandSpacing,
                region.mapPosition.y * islandSpacing
            );
        }

        private void CreateIsland(RegionData region, Vector2 position, float radius, OrbData[] allOrbs)
        {
            var islandObj = new GameObject("Island_" + region.regionName);
            islandObj.transform.position = position;
            islandObj.transform.SetParent(transform);

            // Island terrain
            var generator = islandObj.AddComponent<IslandGenerator>();
            generator.Initialize(region, radius);
            generator.GenerateIsland();

            // Orb spawner
            var orbSpawner = islandObj.AddComponent<OrbSpawner>();
            OrbData[] regionOrbs = GetOrbsForRegion(region, allOrbs);
            orbSpawner.Initialize(region.regionType, regionOrbs, radius * 0.7f);

            // Creature spawner
            var creatureSpawner = islandObj.AddComponent<CreatureSpawner>();
            creatureSpawner.Initialize(region.regionType);

            // Region name floating text
            CreateIslandLabel(islandObj.transform, region.regionName, radius);
        }

        private OrbData[] GetOrbsForRegion(RegionData region, OrbData[] allOrbs)
        {
            var regionOrbs = new System.Collections.Generic.List<OrbData>();
            foreach (var orb in allOrbs)
            {
                if (orb.foundInRegions == null) continue;
                foreach (var orbRegion in orb.foundInRegions)
                {
                    if (orbRegion == region.regionType)
                    {
                        regionOrbs.Add(orb);
                        break;
                    }
                }
            }
            return regionOrbs.ToArray();
        }

        private void CreateOcean()
        {
            var oceanObj = new GameObject("Ocean");
            oceanObj.transform.SetParent(transform);
            oceanObj.transform.position = Vector3.zero;
            var ocean = oceanObj.AddComponent<OceanRenderer>();
            ocean.SetOceanSize(500f);
        }

        private void CreateIslandLabel(Transform island, string name, float radius)
        {
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(island, false);
            labelObj.transform.localPosition = new Vector3(0, -radius - 1.5f, 0);

            // Create text sprite (simple pixel text)
            var sr = labelObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 10;

            int texWidth = name.Length * 6 + 4;
            int texHeight = 10;
            var tex = new Texture2D(texWidth, texHeight);
            tex.filterMode = FilterMode.Point;

            // Background
            for (int y = 0; y < texHeight; y++)
                for (int x = 0; x < texWidth; x++)
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0.5f));

            // Simple dot-based text (just marks the area, not actual font rendering)
            Color textColor = Color.white;
            for (int i = 0; i < name.Length; i++)
            {
                int baseX = 2 + i * 6;
                // Draw simple character marks
                for (int py = 2; py < 8; py++)
                    for (int px = 0; px < 4; px++)
                    {
                        if (HasPixel(name[i], px, py - 2))
                            tex.SetPixel(baseX + px, py, textColor);
                    }
            }

            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, texWidth, texHeight),
                new Vector2(0.5f, 0.5f), texWidth / 3f);
        }

        private bool HasPixel(char c, int x, int y)
        {
            // Minimal 4x6 pixel font for basic readability
            // Only handles uppercase and common chars
            c = char.ToUpper(c);
            long glyph = c switch
            {
                'A' => 0x699F99L, 'B' => 0xE9E9EL, 'C' => 0x698896L, 'D' => 0xE9996L,
                'E' => 0xF8E8FL, 'F' => 0xF8E88L, 'G' => 0x689B96L, 'H' => 0x99F99L,
                'I' => 0xE4446L, 'J' => 0x111196L, 'K' => 0x9ACA9L, 'L' => 0x8888FL,
                'M' => 0x9F999L, 'N' => 0x9DB99L, 'O' => 0x69996L, 'P' => 0xE9E88L,
                'Q' => 0x6999AL, 'R' => 0xE9EA9L, 'S' => 0x68216L, 'T' => 0xF4444L,
                'U' => 0x99996L, 'V' => 0x99966L, 'W' => 0x999F9L, 'X' => 0x96699L,
                'Y' => 0x99644L, 'Z' => 0xF1248FL, ' ' => 0x00000L,
                _ => 0x00000L
            };

            int bitIndex = (5 - y) * 4 + x;
            return ((glyph >> bitIndex) & 1) == 1;
        }

        private void ApplyPlayerSprite()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            var sr = player.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = AlienSpriteGenerator.CreateAlienPlayer();
                sr.sortingOrder = 5;
            }

            // Position player on the starting island (Grassland)
            foreach (var region in allRegions)
            {
                if (region.regionType == RegionType.Grassland)
                {
                    player.transform.position = new Vector3(
                        region.mapPosition.x * islandSpacing,
                        region.mapPosition.y * islandSpacing, 0);
                    break;
                }
            }
        }
    }
}
