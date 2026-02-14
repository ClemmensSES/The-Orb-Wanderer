using UnityEngine;
using OrbWanderer.Core;
using OrbWanderer.Data;

namespace OrbWanderer.World
{
    /// <summary>
    /// Spawns collectible orbs on an island with visual effects.
    /// Different orb types spawn based on the island's region type.
    /// Some orbs are in hard-to-reach spots requiring creature abilities.
    /// </summary>
    public class OrbSpawner : MonoBehaviour
    {
        [Header("Spawning")]
        [SerializeField] private float spawnRadius = 10f;
        [SerializeField] private int maxOrbs = 8;
        [SerializeField] private float respawnInterval = 15f;

        [Header("Special Orbs")]
        [SerializeField] private float highOrbChance = 0.2f;    // Needs jellyfish
        [SerializeField] private float hiddenOrbChance = 0.15f;  // Needs spider
        [SerializeField] private float frozenOrbChance = 0.15f;  // Needs whale/firebird
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

            // Pick an orb type based on rarity weights
            OrbData orbData = PickRandomOrb();
            if (orbData == null) return;

            Vector2 spawnPos = (Vector2)transform.position +
                Random.insideUnitCircle.normalized * Random.Range(2f, spawnRadius * 0.8f);

            // Determine orb placement type
            OrbPlacement placement = DeterminePlacement();

            var orbObj = new GameObject("Orb_" + orbData.orbName);
            orbObj.transform.position = spawnPos;
            orbObj.transform.SetParent(transform);

            // Visual orb sprite
            var sr = orbObj.AddComponent<SpriteRenderer>();
            sr.sprite = AlienSpriteGenerator.CreateOrbSprite(orbData.glowColor);
            sr.sortingOrder = 3;

            // Collider for pickup
            var collider = orbObj.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;
            collider.isTrigger = true;

            // Collection behavior
            var collectible = orbObj.AddComponent<CollectibleOrb>();
            // Set orb data via serialized field
            var so = new UnityEngine.Serialization.FormatterConverter();

            // Glow effect (child object)
            CreateGlowEffect(orbObj.transform, orbData.glowColor);

            // Special placement
            switch (placement)
            {
                case OrbPlacement.High:
                    orbObj.transform.position += Vector3.up * highOrbHeight;
                    // Add floating particle trail
                    CreateFloatingIndicator(orbObj.transform, "UP");
                    sr.color = new Color(1, 1, 1, 0.7f);
                    break;

                case OrbPlacement.Hidden:
                    // Slightly transparent until spider reveals
                    sr.color = new Color(1, 1, 1, 0.4f);
                    CreateFloatingIndicator(orbObj.transform, "?");
                    break;

                case OrbPlacement.Frozen:
                    // Encased in ice
                    CreateIceEncasing(orbObj.transform);
                    collider.enabled = false; // Can't collect until broken
                    break;
            }

            // Bobbing animation
            var bobber = orbObj.AddComponent<OrbBobAnimation>();
            bobber.bobSpeed = 2f + Random.Range(-0.5f, 0.5f);
            bobber.bobHeight = 0.15f;

            activeOrbs++;

            // Track destruction
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

        private void CreateGlowEffect(Transform parent, Color color)
        {
            var glow = new GameObject("Glow");
            glow.transform.SetParent(parent, false);
            var sr = glow.AddComponent<SpriteRenderer>();

            int size = 24;
            var tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Bilinear;
            float center = size / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = Mathf.Max(0, 1f - dist / center) * 0.3f;
                    tex.SetPixel(x, y, new Color(color.r, color.g, color.b, alpha));
                }
            tex.Apply();

            sr.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size / 2f);
            sr.sortingOrder = 2;
            glow.transform.localScale = Vector3.one * 1.5f;
        }

        private void CreateFloatingIndicator(Transform parent, string text)
        {
            // Simple visual indicator using a sprite
            var indicator = new GameObject("Indicator");
            indicator.transform.SetParent(parent, false);
            indicator.transform.localPosition = new Vector3(0, 1f, 0);
            var sr = indicator.AddComponent<SpriteRenderer>();

            int size = 8;
            var tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Point;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, Color.clear);

            // Arrow up or question mark indicator
            Color c = Color.yellow;
            if (text == "UP")
            {
                tex.SetPixel(3, 7, c); tex.SetPixel(4, 7, c);
                tex.SetPixel(2, 6, c); tex.SetPixel(5, 6, c);
                tex.SetPixel(3, 3, c); tex.SetPixel(4, 3, c);
                tex.SetPixel(3, 4, c); tex.SetPixel(4, 4, c);
                tex.SetPixel(3, 5, c); tex.SetPixel(4, 5, c);
            }
            else
            {
                tex.SetPixel(3, 7, c); tex.SetPixel(4, 7, c);
                tex.SetPixel(5, 6, c); tex.SetPixel(4, 5, c);
                tex.SetPixel(3, 4, c); tex.SetPixel(3, 2, c);
                tex.SetPixel(4, 2, c);
            }

            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sr.sortingOrder = 11;
        }

        private void CreateIceEncasing(Transform parent)
        {
            var ice = new GameObject("IceEncasing");
            ice.transform.SetParent(parent, false);
            var sr = ice.AddComponent<SpriteRenderer>();

            int size = 20;
            var tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Point;
            float center = size / 2f;
            Color iceColor = new Color(0.6f, 0.85f, 1f, 0.6f);

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist <= center - 1)
                    {
                        float edge = dist / (center - 1);
                        Color c = iceColor;
                        c.a = 0.3f + edge * 0.4f;
                        tex.SetPixel(x, y, c);
                    }
                    else
                        tex.SetPixel(x, y, Color.clear);
                }

            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sr.sortingOrder = 4;
        }

        public void NotifyOrbDestroyed()
        {
            activeOrbs = Mathf.Max(0, activeOrbs - 1);
        }

        private enum OrbPlacement { Normal, High, Hidden, Frozen }
    }

    /// <summary>Simple bobbing animation for orbs.</summary>
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
            transform.Rotate(0, 0, 20f * Time.deltaTime);
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
