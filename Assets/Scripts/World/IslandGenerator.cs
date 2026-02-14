using UnityEngine;
using OrbWanderer.Core;
using OrbWanderer.Data;

namespace OrbWanderer.World
{
    /// <summary>
    /// Generates a single 3D island with procedural terrain mesh,
    /// biome-themed decorations (trees, waterfalls, rocks, crystals),
    /// and spawn points for orbs and wildlife.
    /// </summary>
    public class IslandGenerator : MonoBehaviour
    {
        [Header("Island Data")]
        [SerializeField] private RegionData regionData;

        [Header("Generation Settings")]
        [SerializeField] private float islandRadius = 12f;
        [SerializeField] private int terrainResolution = 32;
        [SerializeField] private float noiseScale = 0.15f;
        [SerializeField] private int decorationCount = 20;
        [SerializeField] private float terrainHeight = 2f;

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

            CreateTerrainMesh();
            CreateBeachRing();
            CreateDecorations();
            CreateOrbSpawnPoints();
            CreateWildlifeSpawnPoints();
            CreateEdgeCollider();
        }

        private void CreateTerrainMesh()
        {
            int res = terrainResolution;
            var terrainObj = new GameObject("Terrain");
            terrainObj.transform.SetParent(transform, false);
            terrainObj.transform.localPosition = Vector3.zero;

            var mf = terrainObj.AddComponent<MeshFilter>();
            var mr = terrainObj.AddComponent<MeshRenderer>();
            var mc = terrainObj.AddComponent<MeshCollider>();

            // Generate circular island mesh
            int vertCount = (res + 1) * (res + 1);
            var vertices = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];
            var colors = new Color[vertCount];

            Color baseColor = GetBiomeBaseColor();
            Color accentColor = GetBiomeAccentColor();
            Color beachColor = GetBeachColor();

            for (int z = 0; z <= res; z++)
            {
                for (int x = 0; x <= res; x++)
                {
                    int idx = z * (res + 1) + x;
                    float xNorm = (float)x / res * 2f - 1f;
                    float zNorm = (float)z / res * 2f - 1f;

                    float worldX = xNorm * islandRadius;
                    float worldZ = zNorm * islandRadius;
                    float dist = Mathf.Sqrt(xNorm * xNorm + zNorm * zNorm);

                    // Noise for organic shape
                    float noise = Mathf.PerlinNoise(
                        (x + noiseSeedX) * noiseScale,
                        (z + noiseSeedY) * noiseScale);
                    float edgeThreshold = 0.85f + noise * 0.15f;

                    float height = 0f;
                    Color vertColor = baseColor;

                    if (dist < edgeThreshold)
                    {
                        // Island interior
                        float heightNoise = Mathf.PerlinNoise(
                            (x + noiseSeedX + 200) * noiseScale * 2f,
                            (z + noiseSeedY + 200) * noiseScale * 2f);
                        float edgeFalloff = 1f - Mathf.Pow(dist / edgeThreshold, 3);
                        height = heightNoise * terrainHeight * edgeFalloff;

                        // Color variation
                        float detailNoise = Mathf.PerlinNoise(
                            (x + noiseSeedX + 500) * 0.3f,
                            (z + noiseSeedY + 500) * 0.3f);
                        vertColor = Color.Lerp(baseColor, accentColor, detailNoise * 0.4f);

                        // Beach near edges
                        if (dist > edgeThreshold - 0.15f)
                        {
                            float beachBlend = (dist - (edgeThreshold - 0.15f)) / 0.15f;
                            vertColor = Color.Lerp(vertColor, beachColor, beachBlend);
                            height *= (1f - beachBlend * 0.7f);
                        }
                    }
                    else
                    {
                        // Below water level (sunken edge)
                        height = -0.5f;
                        vertColor = new Color(0.15f, 0.3f, 0.5f, 1f);
                    }

                    vertices[idx] = new Vector3(worldX, height, worldZ);
                    uvs[idx] = new Vector2((float)x / res, (float)z / res);
                    colors[idx] = vertColor;
                }
            }

            // Build triangles
            var triangles = new int[res * res * 6];
            int tri = 0;
            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    int i = z * (res + 1) + x;
                    triangles[tri++] = i;
                    triangles[tri++] = i + res + 1;
                    triangles[tri++] = i + 1;
                    triangles[tri++] = i + 1;
                    triangles[tri++] = i + res + 1;
                    triangles[tri++] = i + res + 2;
                }
            }

            var mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.colors = colors;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            mf.mesh = mesh;
            mc.sharedMesh = mesh;

            // Create vertex-colored material
            var mat = new Material(Shader.Find("Standard"));
            mat.color = baseColor;
            mr.material = mat;
        }

        private void CreateBeachRing()
        {
            int foamCount = Mathf.RoundToInt(islandRadius * 3);
            var foamParent = new GameObject("BeachFoam");
            foamParent.transform.SetParent(transform, false);

            var foamMat = AlienSpriteGenerator.CreateEmissiveMaterial(
                new Color(0.9f, 0.95f, 1f), 0.3f);

            for (int i = 0; i < foamCount; i++)
            {
                float angle = (i / (float)foamCount) * Mathf.PI * 2f;
                float noise = Mathf.PerlinNoise(angle * 2f + noiseSeedX, noiseSeedY) * 0.3f;
                float r = islandRadius * (0.88f + noise);

                var foam = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                foam.name = "Foam";
                foam.transform.SetParent(foamParent.transform, false);
                foam.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * r, -0.1f, Mathf.Sin(angle) * r);
                foam.transform.localScale = Vector3.one * Random.Range(0.2f, 0.5f);
                foam.GetComponent<Renderer>().material = foamMat;
                Object.Destroy(foam.GetComponent<Collider>());
            }
        }

        private void CreateDecorations()
        {
            var decoParent = new GameObject("Decorations");
            decoParent.transform.SetParent(transform, false);

            for (int i = 0; i < decorationCount; i++)
            {
                Vector3 pos = GetRandomIslandPosition3D(0.7f);
                CreateBiomeDecoration(decoParent.transform, pos, i);
            }

            // Add waterfalls for appropriate biomes
            if (regionData.regionType == RegionType.Mountain ||
                regionData.regionType == RegionType.Forest ||
                regionData.regionType == RegionType.SkyIslands)
            {
                int waterfallCount = Random.Range(1, 3);
                for (int i = 0; i < waterfallCount; i++)
                {
                    Vector3 pos = GetRandomIslandPosition3D(0.5f);
                    CreateWaterfall(decoParent.transform, pos);
                }
            }
        }

        private void CreateBiomeDecoration(Transform parent, Vector3 pos, int index)
        {
            if (regionData == null) return;

            switch (regionData.regionType)
            {
                case RegionType.Grassland:
                case RegionType.Forest:
                case RegionType.Swamp:
                    CreateTree(parent, pos, regionData.regionType);
                    break;
                case RegionType.Mountain:
                case RegionType.Volcano:
                    CreateRock(parent, pos, regionData.regionType);
                    break;
                case RegionType.Cave:
                case RegionType.SkyIslands:
                    CreateCrystal(parent, pos, regionData.regionType);
                    break;
                case RegionType.Desert:
                    CreateCactus(parent, pos);
                    break;
                case RegionType.Ocean:
                    CreateCoral(parent, pos);
                    break;
                case RegionType.Tundra:
                    if (Random.value > 0.5f)
                        CreateTree(parent, pos, regionData.regionType);
                    else
                        CreateRock(parent, pos, regionData.regionType);
                    break;
            }
        }

        private void CreateTree(Transform parent, Vector3 pos, RegionType biome)
        {
            var treeObj = new GameObject("Tree");
            treeObj.transform.SetParent(parent, false);
            treeObj.transform.localPosition = pos;

            Color trunkColor = new Color(0.4f, 0.25f, 0.15f);
            Color leafColor = biome switch
            {
                RegionType.Forest => new Color(0.1f, 0.4f, 0.15f),
                RegionType.Swamp => new Color(0.25f, 0.4f, 0.2f),
                RegionType.Tundra => new Color(0.7f, 0.8f, 0.85f),
                _ => new Color(0.2f, 0.6f, 0.25f)
            };

            float scale = Random.Range(0.8f, 1.5f);

            // Trunk
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(treeObj.transform, false);
            trunk.transform.localPosition = new Vector3(0, 0.8f * scale, 0);
            trunk.transform.localScale = new Vector3(0.2f * scale, 0.8f * scale, 0.2f * scale);
            trunk.GetComponent<Renderer>().material = AlienSpriteGenerator.CreateMaterial(trunkColor);
            Object.Destroy(trunk.GetComponent<Collider>());

            // Canopy (sphere)
            var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.name = "Canopy";
            canopy.transform.SetParent(treeObj.transform, false);
            canopy.transform.localPosition = new Vector3(0, 2f * scale, 0);
            canopy.transform.localScale = new Vector3(1.2f * scale, 1.5f * scale, 1.2f * scale);
            canopy.GetComponent<Renderer>().material = AlienSpriteGenerator.CreateMaterial(leafColor);
            Object.Destroy(canopy.GetComponent<Collider>());

            // Extra canopy layers for fullness
            var canopy2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy2.name = "Canopy2";
            canopy2.transform.SetParent(treeObj.transform, false);
            canopy2.transform.localPosition = new Vector3(0.3f * scale, 1.7f * scale, 0.2f * scale);
            canopy2.transform.localScale = new Vector3(0.9f * scale, 1f * scale, 0.9f * scale);
            canopy2.GetComponent<Renderer>().material = AlienSpriteGenerator.CreateMaterial(leafColor * 0.9f);
            Object.Destroy(canopy2.GetComponent<Collider>());
        }

        private void CreateRock(Transform parent, Vector3 pos, RegionType biome)
        {
            var rockObj = new GameObject("Rock");
            rockObj.transform.SetParent(parent, false);
            rockObj.transform.localPosition = pos;

            Color rockColor = biome switch
            {
                RegionType.Volcano => new Color(0.4f, 0.2f, 0.15f),
                RegionType.Tundra => new Color(0.7f, 0.75f, 0.8f),
                _ => new Color(0.5f, 0.5f, 0.5f)
            };

            float scale = Random.Range(0.5f, 1.2f);
            var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = "RockMesh";
            rock.transform.SetParent(rockObj.transform, false);
            rock.transform.localPosition = new Vector3(0, 0.25f * scale, 0);
            rock.transform.localScale = new Vector3(0.8f * scale, 0.5f * scale, 0.6f * scale);
            rock.transform.localRotation = Quaternion.Euler(
                Random.Range(-10f, 10f), Random.Range(0f, 360f), Random.Range(-10f, 10f));
            rock.GetComponent<Renderer>().material = AlienSpriteGenerator.CreateMaterial(rockColor);
            Object.Destroy(rock.GetComponent<Collider>());

            // Volcano biome: add lava glow at base
            if (biome == RegionType.Volcano && Random.value > 0.5f)
            {
                var lava = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                lava.name = "LavaGlow";
                lava.transform.SetParent(rockObj.transform, false);
                lava.transform.localPosition = new Vector3(0, 0.05f, 0);
                lava.transform.localScale = Vector3.one * 0.3f * scale;
                lava.GetComponent<Renderer>().material =
                    AlienSpriteGenerator.CreateEmissiveMaterial(new Color(0.9f, 0.3f, 0.1f), 3f);
                Object.Destroy(lava.GetComponent<Collider>());
            }
        }

        private void CreateCrystal(Transform parent, Vector3 pos, RegionType biome)
        {
            var crystalObj = new GameObject("Crystal");
            crystalObj.transform.SetParent(parent, false);
            crystalObj.transform.localPosition = pos;

            Color crystalColor = biome switch
            {
                RegionType.Cave => new Color(0.6f, 0.3f, 0.8f),
                _ => new Color(0.8f, 0.7f, 1f)
            };

            float scale = Random.Range(0.4f, 1f);

            // Crystal shard (elongated cube rotated)
            int shards = Random.Range(1, 4);
            for (int i = 0; i < shards; i++)
            {
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = "Shard";
                shard.transform.SetParent(crystalObj.transform, false);
                shard.transform.localPosition = new Vector3(
                    Random.Range(-0.15f, 0.15f), 0.4f * scale * (i + 1), Random.Range(-0.15f, 0.15f));
                shard.transform.localScale = new Vector3(
                    0.15f * scale, 0.6f * scale, 0.15f * scale);
                shard.transform.localRotation = Quaternion.Euler(
                    Random.Range(-15f, 15f), Random.Range(0f, 360f), Random.Range(-15f, 15f));
                shard.GetComponent<Renderer>().material =
                    AlienSpriteGenerator.CreateEmissiveMaterial(crystalColor, 2f);
                Object.Destroy(shard.GetComponent<Collider>());
            }

            // Point light for crystal glow
            var lightObj = new GameObject("CrystalLight");
            lightObj.transform.SetParent(crystalObj.transform, false);
            lightObj.transform.localPosition = new Vector3(0, 0.5f, 0);
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = crystalColor;
            light.intensity = 0.8f;
            light.range = 2f;
        }

        private void CreateCactus(Transform parent, Vector3 pos)
        {
            var cactusObj = new GameObject("Cactus");
            cactusObj.transform.SetParent(parent, false);
            cactusObj.transform.localPosition = pos;

            var cactusColor = new Color(0.3f, 0.6f, 0.3f);
            var mat = AlienSpriteGenerator.CreateMaterial(cactusColor);
            float scale = Random.Range(0.6f, 1.2f);

            // Main trunk
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(cactusObj.transform, false);
            trunk.transform.localPosition = new Vector3(0, 0.7f * scale, 0);
            trunk.transform.localScale = new Vector3(0.2f * scale, 0.7f * scale, 0.2f * scale);
            trunk.GetComponent<Renderer>().material = mat;
            Object.Destroy(trunk.GetComponent<Collider>());

            // Left arm
            var leftArm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leftArm.name = "LeftArm";
            leftArm.transform.SetParent(cactusObj.transform, false);
            leftArm.transform.localPosition = new Vector3(-0.25f * scale, 0.9f * scale, 0);
            leftArm.transform.localScale = new Vector3(0.12f * scale, 0.3f * scale, 0.12f * scale);
            leftArm.transform.localRotation = Quaternion.Euler(0, 0, 30);
            leftArm.GetComponent<Renderer>().material = mat;
            Object.Destroy(leftArm.GetComponent<Collider>());

            // Right arm
            var rightArm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rightArm.name = "RightArm";
            rightArm.transform.SetParent(cactusObj.transform, false);
            rightArm.transform.localPosition = new Vector3(0.25f * scale, 0.6f * scale, 0);
            rightArm.transform.localScale = new Vector3(0.12f * scale, 0.25f * scale, 0.12f * scale);
            rightArm.transform.localRotation = Quaternion.Euler(0, 0, -30);
            rightArm.GetComponent<Renderer>().material = mat;
            Object.Destroy(rightArm.GetComponent<Collider>());
        }

        private void CreateCoral(Transform parent, Vector3 pos)
        {
            var coralObj = new GameObject("Coral");
            coralObj.transform.SetParent(parent, false);
            coralObj.transform.localPosition = pos;

            Color coralColor = new Color(0.9f, 0.4f, 0.5f);
            float scale = Random.Range(0.3f, 0.8f);

            // Coral branches
            int branches = Random.Range(3, 6);
            for (int i = 0; i < branches; i++)
            {
                var branch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                branch.name = "Branch";
                branch.transform.SetParent(coralObj.transform, false);
                float angle = Random.Range(0f, 360f);
                float lean = Random.Range(-20f, 20f);
                branch.transform.localPosition = new Vector3(
                    Random.Range(-0.15f, 0.15f), 0.25f * scale, Random.Range(-0.15f, 0.15f));
                branch.transform.localScale = new Vector3(
                    0.06f * scale, 0.25f * scale, 0.06f * scale);
                branch.transform.localRotation = Quaternion.Euler(lean, angle, lean);
                Color branchColor = Color.Lerp(coralColor, new Color(1f, 0.6f, 0.3f), Random.value * 0.3f);
                branch.GetComponent<Renderer>().material = AlienSpriteGenerator.CreateMaterial(branchColor);
                Object.Destroy(branch.GetComponent<Collider>());
            }
        }

        private void CreateWaterfall(Transform parent, Vector3 pos)
        {
            var waterfallObj = new GameObject("Waterfall");
            waterfallObj.transform.SetParent(parent, false);
            waterfallObj.transform.localPosition = pos;

            // Rock face behind waterfall
            var rockFace = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rockFace.name = "RockFace";
            rockFace.transform.SetParent(waterfallObj.transform, false);
            rockFace.transform.localPosition = new Vector3(0, 1.5f, -0.3f);
            rockFace.transform.localScale = new Vector3(1.5f, 3f, 0.5f);
            rockFace.GetComponent<Renderer>().material =
                AlienSpriteGenerator.CreateMaterial(new Color(0.45f, 0.45f, 0.4f));
            Object.Destroy(rockFace.GetComponent<Collider>());

            // Water stream (semi-transparent blue cylinder)
            var waterMat = new Material(Shader.Find("Standard"));
            waterMat.color = new Color(0.3f, 0.6f, 0.9f, 0.6f);
            waterMat.SetFloat("_Mode", 3); // Transparent
            waterMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            waterMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            waterMat.SetInt("_ZWrite", 0);
            waterMat.DisableKeyword("_ALPHATEST_ON");
            waterMat.EnableKeyword("_ALPHABLEND_ON");
            waterMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            waterMat.renderQueue = 3000;
            waterMat.EnableKeyword("_EMISSION");
            waterMat.SetColor("_EmissionColor", new Color(0.2f, 0.4f, 0.7f) * 0.5f);

            var stream = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stream.name = "WaterStream";
            stream.transform.SetParent(waterfallObj.transform, false);
            stream.transform.localPosition = new Vector3(0, 1.5f, 0);
            stream.transform.localScale = new Vector3(0.4f, 1.5f, 0.3f);
            stream.GetComponent<Renderer>().material = waterMat;
            Object.Destroy(stream.GetComponent<Collider>());

            // Splash pool at base
            var pool = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pool.name = "SplashPool";
            pool.transform.SetParent(waterfallObj.transform, false);
            pool.transform.localPosition = new Vector3(0, 0.05f, 0.3f);
            pool.transform.localScale = new Vector3(1.2f, 0.05f, 0.8f);
            pool.GetComponent<Renderer>().material = waterMat;
            Object.Destroy(pool.GetComponent<Collider>());

            // Waterfall animation component
            waterfallObj.AddComponent<WaterfallAnimator>();
        }

        private void CreateOrbSpawnPoints()
        {
            var spawnsParent = new GameObject("OrbSpawnPoints");
            spawnsParent.transform.SetParent(transform, false);

            int orbSpawnCount = 8 + (int)(islandRadius * 0.5f);
            for (int i = 0; i < orbSpawnCount; i++)
            {
                Vector3 pos = GetRandomIslandPosition3D(0.75f);
                var spawn = new GameObject("OrbSpawn_" + i);
                spawn.transform.SetParent(spawnsParent.transform, false);
                spawn.transform.localPosition = pos;
            }
        }

        private void CreateWildlifeSpawnPoints()
        {
            var spawnsParent = new GameObject("WildlifeSpawnPoints");
            spawnsParent.transform.SetParent(transform, false);

            int wildlifeSpawnCount = 4 + (int)(islandRadius * 0.3f);
            for (int i = 0; i < wildlifeSpawnCount; i++)
            {
                Vector3 pos = GetRandomIslandPosition3D(0.6f);
                var spawn = new GameObject("WildlifeSpawn_" + i);
                spawn.transform.SetParent(spawnsParent.transform, false);
                spawn.transform.localPosition = pos;
            }
        }

        private void CreateEdgeCollider()
        {
            var colliderObj = new GameObject("IslandBoundary");
            colliderObj.transform.SetParent(transform, false);
            var sphere = colliderObj.AddComponent<SphereCollider>();
            sphere.radius = islandRadius * 0.9f;
            sphere.isTrigger = true;
            try { colliderObj.tag = "Region"; }
            catch (UnityException) { /* Tag not registered yet */ }
            colliderObj.layer = LayerMask.NameToLayer("Default");
        }

        private Vector3 GetRandomIslandPosition3D(float maxRadiusRatio)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(0.1f, maxRadiusRatio) * islandRadius;
            float x = Mathf.Cos(angle) * dist;
            float z = Mathf.Sin(angle) * dist;

            // Sample terrain height at this position
            float heightNoise = Mathf.PerlinNoise(
                (x / islandRadius * terrainResolution / 2f + noiseSeedX + 200) * noiseScale * 2f,
                (z / islandRadius * terrainResolution / 2f + noiseSeedY + 200) * noiseScale * 2f);
            float normalizedDist = dist / islandRadius;
            float edgeFalloff = 1f - Mathf.Pow(normalizedDist / 0.85f, 3);
            float y = heightNoise * terrainHeight * Mathf.Max(0, edgeFalloff);

            return new Vector3(x, y, z);
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
                _ => new Color(0.9f, 0.85f, 0.7f)
            };
        }
    }

    /// <summary>
    /// Simple animation for waterfall particles: moves child objects down and resets.
    /// </summary>
    public class WaterfallAnimator : MonoBehaviour
    {
        private Transform waterStream;
        private float offset;

        private void Start()
        {
            waterStream = transform.Find("WaterStream");
            offset = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            if (waterStream == null) return;

            // Gentle pulsing scale for the water stream
            float pulse = 1f + Mathf.Sin(Time.time * 3f + offset) * 0.05f;
            var scale = waterStream.localScale;
            scale.x = 0.4f * pulse;
            waterStream.localScale = scale;
        }
    }
}
