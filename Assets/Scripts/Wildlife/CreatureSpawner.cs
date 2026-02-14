using UnityEngine;
using OrbWanderer.Core;
using OrbWanderer.Data;
using OrbWanderer.World;

namespace OrbWanderer.Wildlife
{
    /// <summary>
    /// Spawns wild creatures on islands that the player can feed orbs to befriend.
    /// Each island type spawns different creature types.
    /// Wild creatures wander around until fed enough orbs to join the player.
    /// </summary>
    public class CreatureSpawner : MonoBehaviour
    {
        [Header("Spawning")]
        [SerializeField] private float spawnRadius = 8f;
        [SerializeField] private int maxCreaturesPerIsland = 3;
        [SerializeField] private float spawnInterval = 30f;

        [Header("Wild Creature Settings")]
        [SerializeField] private float wanderSpeed = 1.5f;
        [SerializeField] private float wanderRadius = 5f;
        [SerializeField] private int orbsToTame = 5;

        private RegionType islandType;
        private int activeCreatures;
        private float spawnTimer;

        public void Initialize(RegionType type)
        {
            islandType = type;
            spawnTimer = 2f; // Initial spawn delay
        }

        private void Update()
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0 && activeCreatures < maxCreaturesPerIsland)
            {
                SpawnWildCreature();
                spawnTimer = spawnInterval;
            }
        }

        private void SpawnWildCreature()
        {
            CreatureType type = GetCreatureTypeForIsland();

            Vector2 spawnPos = (Vector2)transform.position +
                Random.insideUnitCircle.normalized * Random.Range(3f, spawnRadius);

            var creatureObj = new GameObject("Wild_" + type);
            creatureObj.transform.position = spawnPos;
            creatureObj.transform.SetParent(transform);

            var sr = creatureObj.AddComponent<SpriteRenderer>();
            sr.sprite = GetCreatureSprite(type, 1);
            sr.sortingOrder = 4;

            var collider = creatureObj.AddComponent<CircleCollider2D>();
            collider.radius = 0.8f;
            collider.isTrigger = true;

            var wild = creatureObj.AddComponent<WildCreature>();
            wild.Initialize(type, wanderSpeed, wanderRadius, orbsToTame, transform.position);
            wild.OnTamed += HandleCreatureTamed;

            activeCreatures++;
        }

        private void HandleCreatureTamed(WildCreature wild)
        {
            activeCreatures--;
        }

        private CreatureType GetCreatureTypeForIsland()
        {
            return islandType switch
            {
                RegionType.Ocean => Random.value > 0.5f ? CreatureType.Jellyfish : CreatureType.Whale,
                RegionType.Cave => CreatureType.Spider,
                RegionType.Volcano => CreatureType.Firebird,
                RegionType.Desert => Random.value > 0.5f ? CreatureType.Firebird : CreatureType.Spider,
                RegionType.Tundra => CreatureType.IceGolem,
                RegionType.Mountain => Random.value > 0.5f ? CreatureType.IceGolem : CreatureType.Spider,
                RegionType.Swamp => Random.value > 0.5f ? CreatureType.Jellyfish : CreatureType.Spider,
                RegionType.SkyIslands => CreatureType.Firebird,
                RegionType.Forest => Random.value > 0.5f ? CreatureType.Spider : CreatureType.Jellyfish,
                RegionType.Grassland => (CreatureType)Random.Range(0, 5),
                _ => CreatureType.Jellyfish
            };
        }

        private Sprite GetCreatureSprite(CreatureType type, int level)
        {
            return type switch
            {
                CreatureType.Jellyfish => AlienSpriteGenerator.CreateJellyfishSprite(level),
                CreatureType.Whale => AlienSpriteGenerator.CreateWhaleSprite(level),
                CreatureType.Spider => AlienSpriteGenerator.CreateSpiderSprite(level),
                CreatureType.Firebird => AlienSpriteGenerator.CreateFirebirdSprite(level),
                CreatureType.IceGolem => AlienSpriteGenerator.CreateIceGolemSprite(level),
                _ => AlienSpriteGenerator.CreateJellyfishSprite(level)
            };
        }
    }

    /// <summary>
    /// A wild creature wandering on an island.
    /// Player can feed it orbs to tame it and add to their crew.
    /// Shows a hunger/trust indicator above its head.
    /// </summary>
    public class WildCreature : MonoBehaviour
    {
        private CreatureType creatureType;
        private float wanderSpeed;
        private float wanderRadius;
        private int orbsNeeded;
        private int orbsFed;
        private Vector2 homePosition;
        private Vector2 wanderTarget;
        private float wanderTimer;
        private bool isTamed;
        private SpriteRenderer hungerIndicator;

        public CreatureType Type => creatureType;
        public bool IsTamed => isTamed;
        public float TrustProgress => orbsNeeded > 0 ? (float)orbsFed / orbsNeeded : 0;
        public System.Action<WildCreature> OnTamed;

        public void Initialize(CreatureType type, float speed, float radius, int orbsToTame, Vector2 home)
        {
            creatureType = type;
            wanderSpeed = speed;
            wanderRadius = radius;
            orbsNeeded = orbsToTame;
            homePosition = home;
            wanderTarget = homePosition;

            CreateHungerIndicator();
        }

        private void Update()
        {
            if (isTamed) return;
            UpdateWander();
            UpdateIndicator();
        }

        private void UpdateWander()
        {
            wanderTimer -= Time.deltaTime;

            if (wanderTimer <= 0 || Vector2.Distance(transform.position, wanderTarget) < 0.3f)
            {
                wanderTarget = homePosition + Random.insideUnitCircle * wanderRadius;
                wanderTimer = Random.Range(2f, 5f);
            }

            Vector2 dir = (wanderTarget - (Vector2)transform.position).normalized;
            transform.position += (Vector3)(dir * wanderSpeed * Time.deltaTime);

            // Flip sprite
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && Mathf.Abs(dir.x) > 0.1f)
                sr.flipX = dir.x < 0;
        }

        /// <summary>
        /// Feed this creature an orb. Returns true if it's now tamed.
        /// </summary>
        public bool FeedOrb(OrbData orb)
        {
            if (isTamed) return false;

            orbsFed++;

            // Specialty orbs count as 2
            bool isSpecialty = creatureType switch
            {
                CreatureType.Jellyfish => orb.orbType == OrbType.Aqua,
                CreatureType.Whale => orb.orbType == OrbType.Aqua || orb.orbType == OrbType.Terra,
                CreatureType.Spider => orb.orbType == OrbType.Shadow,
                CreatureType.Firebird => orb.orbType == OrbType.Ember,
                CreatureType.IceGolem => orb.orbType == OrbType.Frost,
                _ => false
            };

            if (isSpecialty) orbsFed++;

            if (orbsFed >= orbsNeeded)
            {
                Tame();
                return true;
            }

            // Visual feedback — creature hops happily
            StartCoroutine(HappyBounce());
            return false;
        }

        private void Tame()
        {
            isTamed = true;

            // Remove hunger indicator
            if (hungerIndicator != null)
                Destroy(hungerIndicator.gameObject);

            OnTamed?.Invoke(this);

            // Convert to CreatureCompanion
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var companion = gameObject.AddComponent<CreatureCompanion>();
                companion.Initialize(GetDefaultName(), creatureType, player.transform);

                // Remove wild behavior
                Destroy(this);
            }
        }

        private string GetDefaultName()
        {
            return creatureType switch
            {
                CreatureType.Jellyfish => "Glimmer",
                CreatureType.Whale => "Titan",
                CreatureType.Spider => "Weaver",
                CreatureType.Firebird => "Blaze",
                CreatureType.IceGolem => "Frost",
                _ => "Companion"
            };
        }

        private void CreateHungerIndicator()
        {
            var indicatorObj = new GameObject("TrustIndicator");
            indicatorObj.transform.SetParent(transform, false);
            indicatorObj.transform.localPosition = new Vector3(0, 1.5f, 0);

            hungerIndicator = indicatorObj.AddComponent<SpriteRenderer>();
            hungerIndicator.sortingOrder = 10;
            UpdateIndicator();
        }

        private void UpdateIndicator()
        {
            if (hungerIndicator == null) return;

            int size = 16;
            var tex = new Texture2D(size, 4);
            tex.filterMode = FilterMode.Point;

            float progress = TrustProgress;
            for (int x = 0; x < size; x++)
            {
                float t = x / (float)size;
                Color c;
                if (t <= progress)
                    c = Color.Lerp(Color.red, Color.green, progress);
                else
                    c = new Color(0.2f, 0.2f, 0.2f, 0.5f);

                for (int y = 0; y < 4; y++)
                    tex.SetPixel(x, y, c);
            }

            tex.Apply();
            hungerIndicator.sprite = Sprite.Create(tex, new Rect(0, 0, size, 4),
                new Vector2(0.5f, 0.5f), size);
        }

        private System.Collections.IEnumerator HappyBounce()
        {
            float elapsed = 0;
            Vector3 startPos = transform.position;
            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                float bounce = Mathf.Sin(elapsed / 0.3f * Mathf.PI) * 0.5f;
                transform.position = startPos + Vector3.up * bounce;
                yield return null;
            }
            transform.position = startPos;
        }
    }
}
