using UnityEngine;
using OrbWanderer.Core;
using OrbWanderer.Data;
using OrbWanderer.Wildlife;

namespace OrbWanderer.World
{
    /// <summary>
    /// Top-level world generator: creates 3D islands, ocean, and places
    /// the player in the starting grassland region.
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
                allRegions = Resources.LoadAll<RegionData>("GameData/Regions");

            if (allRegions == null || allRegions.Length == 0)
            {
                Debug.LogWarning("No regions found! Run OrbWanderer > Create Default Data first.");
                return;
            }

            CreateSunlight();
            CreateOcean();
            var allOrbs = Resources.LoadAll<OrbData>("GameData/Orbs");

            for (int i = 0; i < allRegions.Length; i++)
            {
                var region = allRegions[i];
                Vector3 worldPos = CalculateIslandPosition(region, i);
                float radius = baseIslandRadius + Random.Range(-islandRadiusVariation, islandRadiusVariation);

                CreateIsland(region, worldPos, radius, allOrbs);
            }

            ApplyPlayerModel();
            Debug.Log($"3D alien world generated! {allRegions.Length} islands created.");
        }

        private void CreateSunlight()
        {
            // Add directional light for the 3D world
            var lightObj = new GameObject("SunLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.85f);
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;

            // Ambient light
            RenderSettings.ambientLight = new Color(0.3f, 0.35f, 0.45f);
            RenderSettings.fogColor = new Color(0.15f, 0.2f, 0.35f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 50f;
            RenderSettings.fogEndDistance = 200f;
        }

        private Vector3 CalculateIslandPosition(RegionData region, int index)
        {
            return new Vector3(
                region.mapPosition.x * islandSpacing,
                0f,
                region.mapPosition.y * islandSpacing);
        }

        private void CreateIsland(RegionData region, Vector3 position, float radius, OrbData[] allOrbs)
        {
            var islandObj = new GameObject("Island_" + region.regionName);
            islandObj.transform.position = position;
            islandObj.transform.SetParent(transform);

            var generator = islandObj.AddComponent<IslandGenerator>();
            generator.Initialize(region, radius);
            generator.GenerateIsland();

            var orbSpawner = islandObj.AddComponent<OrbSpawner>();
            OrbData[] regionOrbs = GetOrbsForRegion(region, allOrbs);
            orbSpawner.Initialize(region.regionType, regionOrbs, radius * 0.7f);

            var creatureSpawner = islandObj.AddComponent<CreatureSpawner>();
            creatureSpawner.Initialize(region.regionType);

            CreateIslandLabel(islandObj.transform, region.regionName, radius);
        }

        private OrbData[] GetOrbsForRegion(RegionData region, OrbData[] allOrbs)
        {
            if (allOrbs == null || allOrbs.Length == 0) return new OrbData[0];

            // Find orbs matching this region's native type
            var regionOrbs = System.Array.FindAll(allOrbs, o =>
                o.nativeRegion == region.regionType || o.orbType == OrbType.Prismatic);

            if (regionOrbs.Length == 0)
                return new OrbData[] { allOrbs[0] };

            return regionOrbs;
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
            // 3D floating text label above the island
            var labelObj = new GameObject("Label_" + name);
            labelObj.transform.SetParent(island, false);
            labelObj.transform.localPosition = new Vector3(0, 4f, 0);

            // Use a quad with text texture (works on all render pipelines)
            int texWidth = name.Length * 6 + 4;
            int texHeight = 10;
            var tex = new Texture2D(texWidth, texHeight);
            tex.filterMode = FilterMode.Point;

            // Background
            for (int y = 0; y < texHeight; y++)
                for (int x = 0; x < texWidth; x++)
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0.5f));

            // Simple dot-based text
            Color textColor = Color.white;
            for (int i = 0; i < name.Length; i++)
            {
                int baseX = 2 + i * 6;
                for (int py = 2; py < 8; py++)
                    for (int px = 0; px < 4; px++)
                    {
                        if (HasPixel(name[i], px, py - 2))
                            tex.SetPixel(baseX + px, py, textColor);
                    }
            }
            tex.Apply();

            // Create a quad mesh to display the label texture
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "LabelQuad";
            quad.transform.SetParent(labelObj.transform, false);
            quad.transform.localScale = new Vector3(texWidth / 4f, texHeight / 4f, 1f);
            Object.Destroy(quad.GetComponent<Collider>());

            var mat = new Material(Shader.Find("Standard"));
            mat.mainTexture = tex;
            mat.color = Color.white;
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            quad.GetComponent<Renderer>().material = mat;

            // Billboard behavior so the label always faces the camera
            labelObj.AddComponent<BillboardLabel>();
        }

        private bool HasPixel(char c, int x, int y)
        {
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

        private void ApplyPlayerModel()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            // Apply 3D model to player
            AlienSpriteGenerator.ApplyAlienPlayerModel(player);

            // Position player on starting island
            foreach (var region in allRegions)
            {
                if (region.regionType == RegionType.Grassland)
                {
                    player.transform.position = new Vector3(
                        region.mapPosition.x * islandSpacing,
                        1f,
                        region.mapPosition.y * islandSpacing);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Billboard effect: always faces the camera so labels are readable from any angle.
    /// </summary>
    public class BillboardLabel : MonoBehaviour
    {
        private void LateUpdate()
        {
            if (Camera.main != null)
            {
                transform.LookAt(
                    transform.position + Camera.main.transform.rotation * Vector3.forward,
                    Camera.main.transform.rotation * Vector3.up);
            }
        }
    }
}
