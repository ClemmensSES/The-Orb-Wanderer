using UnityEngine;
using OrbWanderer.Core;
using OrbWanderer.Data;

namespace OrbWanderer.World
{
    /// <summary>
    /// Generates a single island with themed terrain, decorations, and spawn points.
    /// Each island is a region in the world.
    /// </summary>
    public class IslandGenerator : MonoBehaviour
    {
        [Header("Island Data")]
        [SerializeField] private RegionData regionData;

        [Header("Generation Settings")]
        [SerializeField] private float islandRadius = 12f;
        [SerializeField] private int terrainResolution = 64;
        [SerializeField] private float noiseScale = 0.15f;
        [SerializeField] private int decorationCount = 20;

        private float noiseSeedX;
        private float noiseSeedY;

        public RegionData RegionData => regionData;
        public float IslandRadius => islandRadius;

        public void Initialize(RegionData data, float radius)
        {
            regionData = data;
            islandRadius = radius;
            noiseSeedX = Random.Range(0f, 1000f);
            noiseSeedY = Random.Range(0f, 1000f);
        }

        public void GenerateIsland()
        {
            if (regionData == null) return;

            CreateTerrainSprite();
            CreateBeachRing();
            CreateDecorations();
            CreateOrbSpawnPoints();
            CreateWildlifeSpawnPoints();
            CreateEdgeCollider();
        }

        private void CreateTerrainSprite()
        {
            int size = terrainResolution;
            var tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Point;
            float center = size / 2f;
            float radius = size / 2f - 2;

            Color baseColor = GetBiomeBaseColor();
            Color accentColor = GetBiomeAccentColor();

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float normalizedDist = dist / radius;

                    // Organic island shape using noise
                    float noise = Mathf.PerlinNoise(
                        (x + noiseSeedX) * noiseScale,
                        (y + noiseSeedY) * noiseScale
                    );
                    float edgeThreshold = 0.85f + noise * 0.2f;

                    if (normalizedDist > edgeThreshold)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else if (normalizedDist > edgeThreshold - 0.12f)
                    {
                        // Beach/shore ring
                        Color beachColor = GetBeachColor();
                        float beachBlend = (normalizedDist - (edgeThreshold - 0.12f)) / 0.12f;
                        tex.SetPixel(x, y, Color.Lerp(baseColor, beachColor, beachBlend));
                    }
                    else
                    {
                        // Interior terrain with noise variation
                        float detailNoise = Mathf.PerlinNoise(
                            (x + noiseSeedX + 500) * 0.3f,
                            (y + noiseSeedY + 500) * 0.3f
                        );
                        Color terrainColor = Color.Lerp(baseColor, accentColor, detailNoise * 0.4f);

                        // Darken slightly toward center for depth
                        float centerDarkening = 1f - normalizedDist * 0.1f;
                        terrainColor *= centerDarkening;
                        terrainColor.a = 1f;

                        tex.SetPixel(x, y, terrainColor);
                    }
                }
            }

            tex.Apply();

            var terrainObj = new GameObject("Terrain");
            terrainObj.transform.SetParent(transform, false);
            terrainObj.transform.localPosition = Vector3.zero;
            var sr = terrainObj.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size / (islandRadius * 2f));
            sr.sortingOrder = 0;
        }

        private void CreateBeachRing()
        {
            // Beach particles/foam effect around the edge
            int foamCount = Mathf.RoundToInt(islandRadius * 4);
            var foamParent = new GameObject("BeachFoam");
            foamParent.transform.SetParent(transform, false);

            for (int i = 0; i < foamCount; i++)
            {
                float angle = (i / (float)foamCount) * Mathf.PI * 2f;
                float noise = Mathf.PerlinNoise(angle * 2f + noiseSeedX, noiseSeedY) * 0.3f;
                float r = islandRadius * (0.88f + noise);

                var foam = new GameObject("Foam");
                foam.transform.SetParent(foamParent.transform, false);
                foam.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * r,
                    Mathf.Sin(angle) * r, 0);

                var sr = foam.AddComponent<SpriteRenderer>();
                sr.sprite = CreateDotSprite(4, new Color(0.9f, 0.95f, 1f, 0.4f));
                sr.sortingOrder = 1;
                foam.transform.localScale = Vector3.one * Random.Range(0.3f, 0.7f);
            }
        }

        private void CreateDecorations()
        {
            var decoParent = new GameObject("Decorations");
            decoParent.transform.SetParent(transform, false);

            for (int i = 0; i < decorationCount; i++)
            {
                Vector2 pos = GetRandomIslandPosition(0.7f);
                var deco = new GameObject("Deco_" + i);
                deco.transform.SetParent(decoParent.transform, false);
                deco.transform.localPosition = new Vector3(pos.x, pos.y, 0);

                var sr = deco.AddComponent<SpriteRenderer>();
                sr.sprite = CreateDecorationSprite();
                sr.sortingOrder = 2;
                sr.color = GetDecorationColor();
                deco.transform.localScale = Vector3.one * Random.Range(0.4f, 1.2f);
            }
        }

        private void CreateOrbSpawnPoints()
        {
            var spawnsParent = new GameObject("OrbSpawnPoints");
            spawnsParent.transform.SetParent(transform, false);

            int orbSpawnCount = 8 + (int)(islandRadius * 0.5f);
            for (int i = 0; i < orbSpawnCount; i++)
            {
                Vector2 pos = GetRandomIslandPosition(0.75f);
                var spawn = new GameObject("OrbSpawn_" + i);
                spawn.transform.SetParent(spawnsParent.transform, false);
                spawn.transform.localPosition = new Vector3(pos.x, pos.y, 0);
            }
        }

        private void CreateWildlifeSpawnPoints()
        {
            var spawnsParent = new GameObject("WildlifeSpawnPoints");
            spawnsParent.transform.SetParent(transform, false);

            int wildlifeSpawnCount = 4 + (int)(islandRadius * 0.3f);
            for (int i = 0; i < wildlifeSpawnCount; i++)
            {
                Vector2 pos = GetRandomIslandPosition(0.6f);
                var spawn = new GameObject("WildlifeSpawn_" + i);
                spawn.transform.SetParent(spawnsParent.transform, false);
                spawn.transform.localPosition = new Vector3(pos.x, pos.y, 0);
            }
        }

        private void CreateEdgeCollider()
        {
            var colliderObj = new GameObject("IslandBoundary");
            colliderObj.transform.SetParent(transform, false);
            var circle = colliderObj.AddComponent<CircleCollider2D>();
            circle.radius = islandRadius * 0.9f;
            circle.isTrigger = true;
            try { colliderObj.tag = "Region"; }
            catch (UnityException) { /* Tag not registered yet - run OrbWanderer > Setup Game Scene */ }
            colliderObj.layer = LayerMask.NameToLayer("Default");
        }

        private Vector2 GetRandomIslandPosition(float maxRadiusRatio)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(0.1f, maxRadiusRatio) * islandRadius;
            return new Vector2(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist);
        }

        // --- Biome Color Palettes ---

        private Color GetBiomeBaseColor()
        {
            if (regionData == null) return Color.green;
            return regionData.regionType switch
            {
                RegionType.Grassland => new Color(0.35f, 0.65f, 0.3f),
                RegionType.Forest => new Color(0.2f, 0.45f, 0.2f),
                RegionType.Mountain => new Color(0.55f, 0.5f, 0.45f),
                RegionType.Cave => new Color(0.3f, 0.25f, 0.35f),
                RegionType.Ocean => new Color(0.2f, 0.4f, 0.55f),
                RegionType.Desert => new Color(0.85f, 0.75f, 0.5f),
                RegionType.Tundra => new Color(0.8f, 0.85f, 0.9f),
                RegionType.Volcano => new Color(0.4f, 0.2f, 0.15f),
                RegionType.Swamp => new Color(0.3f, 0.4f, 0.25f),
                RegionType.SkyIslands => new Color(0.7f, 0.65f, 0.85f),
                _ => Color.green
            };
        }

        private Color GetBiomeAccentColor()
        {
            if (regionData == null) return Color.yellow;
            return regionData.regionType switch
            {
                RegionType.Grassland => new Color(0.5f, 0.75f, 0.3f),
                RegionType.Forest => new Color(0.15f, 0.55f, 0.15f),
                RegionType.Mountain => new Color(0.7f, 0.65f, 0.6f),
                RegionType.Cave => new Color(0.5f, 0.3f, 0.6f),
                RegionType.Ocean => new Color(0.3f, 0.55f, 0.7f),
                RegionType.Desert => new Color(0.95f, 0.85f, 0.6f),
                RegionType.Tundra => new Color(0.9f, 0.92f, 1f),
                RegionType.Volcano => new Color(0.8f, 0.3f, 0.1f),
                RegionType.Swamp => new Color(0.25f, 0.5f, 0.15f),
                RegionType.SkyIslands => new Color(0.85f, 0.75f, 1f),
                _ => Color.yellow
            };
        }

        private Color GetBeachColor()
        {
            if (regionData == null) return new Color(0.9f, 0.85f, 0.7f);
            return regionData.regionType switch
            {
                RegionType.Volcano => new Color(0.3f, 0.2f, 0.15f),
                RegionType.Tundra => new Color(0.85f, 0.9f, 0.95f),
                RegionType.Swamp => new Color(0.35f, 0.4f, 0.3f),
                RegionType.Cave => new Color(0.4f, 0.35f, 0.4f),
                RegionType.SkyIslands => new Color(0.8f, 0.75f, 0.9f),
                _ => new Color(0.9f, 0.85f, 0.7f) // Sandy beach
            };
        }

        private Color GetDecorationColor()
        {
            if (regionData == null) return Color.green;
            return regionData.regionType switch
            {
                RegionType.Grassland => new Color(0.2f, 0.7f, 0.3f, 0.8f),
                RegionType.Forest => new Color(0.1f, 0.5f, 0.15f, 0.9f),
                RegionType.Mountain => new Color(0.6f, 0.55f, 0.5f, 0.7f),
                RegionType.Cave => new Color(0.5f, 0.3f, 0.7f, 0.6f),
                RegionType.Ocean => new Color(0.3f, 0.6f, 0.8f, 0.5f),
                RegionType.Desert => new Color(0.7f, 0.6f, 0.3f, 0.6f),
                RegionType.Tundra => new Color(0.7f, 0.8f, 0.9f, 0.5f),
                RegionType.Volcano => new Color(0.9f, 0.4f, 0.1f, 0.7f),
                RegionType.Swamp => new Color(0.3f, 0.5f, 0.2f, 0.7f),
                RegionType.SkyIslands => new Color(0.8f, 0.7f, 1f, 0.6f),
                _ => Color.green
            };
        }

        // --- Sprite Creation ---

        private Sprite CreateDecorationSprite()
        {
            if (regionData == null) return CreateDotSprite(8, Color.green);

            return regionData.regionType switch
            {
                RegionType.Grassland => CreateTreeSprite(new Color(0.2f, 0.6f, 0.25f)),
                RegionType.Forest => CreateTreeSprite(new Color(0.1f, 0.4f, 0.15f)),
                RegionType.Mountain => CreateRockSprite(new Color(0.5f, 0.5f, 0.5f)),
                RegionType.Cave => CreateCrystalSprite(new Color(0.6f, 0.3f, 0.8f)),
                RegionType.Ocean => CreateCoralSprite(new Color(0.9f, 0.4f, 0.5f)),
                RegionType.Desert => CreateCactusSprite(new Color(0.3f, 0.6f, 0.3f)),
                RegionType.Tundra => CreateTreeSprite(new Color(0.7f, 0.8f, 0.85f)),
                RegionType.Volcano => CreateRockSprite(new Color(0.6f, 0.2f, 0.1f)),
                RegionType.Swamp => CreateTreeSprite(new Color(0.25f, 0.4f, 0.2f)),
                RegionType.SkyIslands => CreateCrystalSprite(new Color(0.8f, 0.7f, 1f)),
                _ => CreateDotSprite(8, Color.green)
            };
        }

        private static Sprite CreateDotSprite(int size, Color color)
        {
            var tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Point;
            float center = size / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    tex.SetPixel(x, y, dist <= center - 1 ? color : Color.clear);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateTreeSprite(Color leafColor)
        {
            int size = 16;
            var tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Point;
            Color trunk = new Color(0.4f, 0.25f, 0.15f);

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, Color.clear);

            // Trunk
            for (int y = 0; y < 6; y++)
                for (int x = 6; x <= 9; x++)
                    tex.SetPixel(x, y, trunk);

            // Canopy (circle)
            for (int y = 5; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(8, 11));
                    if (dist <= 5)
                    {
                        float shade = 1f - (dist / 5f) * 0.2f;
                        tex.SetPixel(x, y, leafColor * shade);
                    }
                }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0f), size / 2f);
        }

        private static Sprite CreateRockSprite(Color rockColor)
        {
            int size = 12;
            var tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Point;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, Color.clear);

            // Boxy rock shape
            for (int y = 0; y < 8; y++)
                for (int x = 2; x < 10; x++)
                {
                    if (y > 5 && (x < 3 || x > 8)) continue;
                    float shade = 0.8f + Random.Range(0f, 0.2f);
                    tex.SetPixel(x, y, rockColor * shade);
                }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0f), size / 1.5f);
        }

        private static Sprite CreateCrystalSprite(Color crystalColor)
        {
            int size = 12;
            var tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Point;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, Color.clear);

            // Crystal shard shape
            for (int y = 0; y < size; y++)
            {
                int halfWidth = Mathf.Max(1, (size - y) / 3);
                for (int x = size / 2 - halfWidth; x <= size / 2 + halfWidth; x++)
                {
                    if (x < 0 || x >= size) continue;
                    float glow = 0.7f + (y / (float)size) * 0.3f;
                    Color c = crystalColor * glow;
                    c.a = 0.9f;
                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0f), size / 1.5f);
        }

        private static Sprite CreateCoralSprite(Color coralColor)
        {
            int size = 12;
            var tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Point;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, Color.clear);

            // Branching coral
            for (int y = 0; y < 8; y++)
            {
                tex.SetPixel(5, y, coralColor);
                tex.SetPixel(6, y, coralColor);
                if (y > 3) { tex.SetPixel(3, y, coralColor); tex.SetPixel(8, y, coralColor); }
                if (y > 5) { tex.SetPixel(2, y, coralColor); tex.SetPixel(9, y, coralColor); }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0f), size / 1.5f);
        }

        private static Sprite CreateCactusSprite(Color cactusColor)
        {
            int size = 16;
            var tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Point;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, Color.clear);

            // Main trunk
            for (int y = 0; y < 14; y++)
                for (int x = 6; x <= 9; x++)
                    tex.SetPixel(x, y, cactusColor);

            // Left arm
            for (int x = 2; x <= 6; x++)
                for (int y = 7; y <= 9; y++)
                    tex.SetPixel(x, y, cactusColor);
            for (int y = 9; y <= 12; y++)
                for (int x = 2; x <= 4; x++)
                    tex.SetPixel(x, y, cactusColor);

            // Right arm
            for (int x = 9; x <= 13; x++)
                for (int y = 4; y <= 6; y++)
                    tex.SetPixel(x, y, cactusColor);
            for (int y = 6; y <= 10; y++)
                for (int x = 11; x <= 13; x++)
                    tex.SetPixel(x, y, cactusColor);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0f), size / 2f);
        }
    }
}
