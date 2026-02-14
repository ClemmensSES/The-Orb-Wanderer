using UnityEngine;
using OrbWanderer.Core;
using OrbWanderer.Data;

namespace OrbWanderer.World
{
    /// <summary>
    /// Spawns shiny 3D collectible orbs on an island.
    /// Different orb types spawn based on the island's region type.
    /// </summary>
    public class OrbSpawner : MonoBehaviour
    {
        [Header("Spawning")]
        [SerializeField] private float spawnRadius = 10f;
        [SerializeField] private int maxOrbs = 8;
        [SerializeField] private float respawnInterval = 15f;

        [Header("Special Orbs")]
        [SerializeField] private float highOrbChance = 0.2f;
        [SerializeField] private float hiddenOrbChance = 0.15f;
        [SerializeField] private float frozenOrbChance = 0.15f;
        [SerializeField] private float highOrbHeight = 3f;

        private RegionType islandType;
        private OrbData[] availableOrbs;
        private int activeOrbs;
        private float respawnTimer;

        public void Initialize(RegionType type, OrbData[] orbs, float radius)
        {
            islandType = type;
            availableOrbs = orbs;
            spawnRadius = radius;
            respawnTimer = 1f;
        }

        private void Update()
        {
            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0 && activeOrbs < maxOrbs)
            {
                SpawnOrb();
                respawnTimer = respawnInterval;
            }
        }

        private void SpawnOrb()
        {
            if (availableOrbs == null || availableOrbs.Length == 0) return;

            OrbData orbData = PickRandomOrb();
            if (orbData == null) return;

            // Spawn on XZ plane around the island
            Vector2 circle = Random.insideUnitCircle.normalized * Random.Range(2f, spawnRadius * 0.8f);
            Vector3 spawnPos = transform.position + new Vector3(circle.x, 0.5f, circle.y);

            OrbPlacement placement = DeterminePlacement();

            var orbObj = new GameObject("Orb_" + orbData.orbName);
            orbObj.transform.position = spawnPos;
            orbObj.transform.SetParent(transform);

            // Apply 3D orb model (shiny sphere with glow)
            AlienSpriteGenerator.ApplyOrbModel(orbObj, orbData.glowColor);

            // 3D sphere collider for pickup
            var collider = orbObj.AddComponent<SphereCollider>();
            collider.radius = 0.5f;
            collider.isTrigger = true;

            // Collection behavior
            var collectible = orbObj.AddComponent<CollectibleOrb>();
            collectible.Initialize(orbData);

            // Special placement
            switch (placement)
            {
                case OrbPlacement.High:
                    orbObj.transform.position += Vector3.up * highOrbHeight;
                    CreateFloatingIndicator(orbObj.transform, true);
                    break;

                case OrbPlacement.Hidden:
                    // Make partially transparent
                    foreach (var r in orbObj.GetComponentsInChildren<Renderer>())
                    {
                        var c = r.material.color;
                        c.a = 0.4f;
                        r.material.color = c;
                    }
                    CreateFloatingIndicator(orbObj.transform, false);
                    break;

                case OrbPlacement.Frozen:
                    CreateIceEncasing(orbObj.transform);
                    collider.enabled = false;
                    break;
            }

            // Bobbing animation
            var bobber = orbObj.AddComponent<OrbBobAnimation>();
            bobber.bobSpeed = 2f + Random.Range(-0.5f, 0.5f);
            bobber.bobHeight = 0.15f;

            activeOrbs++;

            var tracker = orbObj.AddComponent<OrbDestroyTracker>();
            tracker.spawner = this;
        }

        private OrbPlacement DeterminePlacement()
        {
            float roll = Random.value;
            if (roll < highOrbChance) return OrbPlacement.High;
            if (roll < highOrbChance + hiddenOrbChance) return OrbPlacement.Hidden;
            if (roll < highOrbChance + hiddenOrbChance + frozenOrbChance) return OrbPlacement.Frozen;
            return OrbPlacement.Normal;
        }

        private OrbData PickRandomOrb()
        {
            float totalWeight = 0;
            foreach (var orb in availableOrbs)
                totalWeight += orb.baseDropRate;

            float roll = Random.Range(0, totalWeight);
            float current = 0;
            foreach (var orb in availableOrbs)
            {
                current += orb.baseDropRate;
                if (roll <= current) return orb;
            }

            return availableOrbs[0];
        }

        private void CreateFloatingIndicator(Transform parent, bool isUp)
        {
            var indicator = new GameObject("Indicator");
            indicator.transform.SetParent(parent, false);
            indicator.transform.localPosition = new Vector3(0, 1.2f, 0);

            // Small arrow/question mark sphere
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "IndicatorMesh";
            sphere.transform.SetParent(indicator.transform, false);
            sphere.transform.localScale = Vector3.one * 0.15f;
            var mat = AlienSpriteGenerator.CreateEmissiveMaterial(
                isUp ? Color.cyan : Color.yellow, 3f);
            sphere.GetComponent<Renderer>().material = mat;
            Object.Destroy(sphere.GetComponent<Collider>());
        }

        private void CreateIceEncasing(Transform parent)
        {
            var ice = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ice.name = "IceEncasing";
            ice.transform.SetParent(parent, false);
            ice.transform.localPosition = Vector3.zero;
            ice.transform.localScale = Vector3.one * 0.8f;

            var iceMat = new Material(Shader.Find("Standard"));
            iceMat.color = new Color(0.6f, 0.85f, 1f, 0.5f);
            iceMat.SetFloat("_Mode", 3);
            iceMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            iceMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            iceMat.SetInt("_ZWrite", 0);
            iceMat.DisableKeyword("_ALPHATEST_ON");
            iceMat.EnableKeyword("_ALPHABLEND_ON");
            iceMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            iceMat.renderQueue = 3000;
            iceMat.SetFloat("_Glossiness", 0.95f);

            ice.GetComponent<Renderer>().material = iceMat;
            Object.Destroy(ice.GetComponent<Collider>());
        }

        public void NotifyOrbDestroyed()
        {
            activeOrbs = Mathf.Max(0, activeOrbs - 1);
        }

        private enum OrbPlacement { Normal, High, Hidden, Frozen }
    }

    /// <summary>Simple 3D bobbing animation for orbs.</summary>
    public class OrbBobAnimation : MonoBehaviour
    {
        public float bobSpeed = 2f;
        public float bobHeight = 0.15f;
        private Vector3 startPos;
        private float offset;

        private void Start()
        {
            startPos = transform.localPosition;
            offset = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            var pos = startPos;
            pos.y += Mathf.Sin(Time.time * bobSpeed + offset) * bobHeight;
            transform.localPosition = pos;
            // Spin on Y axis
            transform.Rotate(0, 20f * Time.deltaTime, 0);
        }
    }

    /// <summary>Notifies spawner when an orb is destroyed.</summary>
    public class OrbDestroyTracker : MonoBehaviour
    {
        public OrbSpawner spawner;
        private void OnDestroy()
        {
            if (spawner != null) spawner.NotifyOrbDestroyed();
        }
    }
}
