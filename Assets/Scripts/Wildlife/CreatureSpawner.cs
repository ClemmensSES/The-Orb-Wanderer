using UnityEngine;
using OrbWanderer.Core;
using OrbWanderer.Data;
using OrbWanderer.World;

namespace OrbWanderer.Wildlife
{
    /// <summary>
    /// Spawns wild 3D creatures on islands that the player can feed orbs to befriend.
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
            spawnTimer = 2f;
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

            // Spawn on XZ plane
            Vector2 circle = Random.insideUnitCircle.normalized * Random.Range(3f, spawnRadius);
            Vector3 spawnPos = transform.position + new Vector3(circle.x, 0.5f, circle.y);

            var creatureObj = new GameObject("Wild_" + type);
            creatureObj.transform.position = spawnPos;
            creatureObj.transform.SetParent(transform);

            // Apply 3D creature model
            AlienSpriteGenerator.ApplyCreatureModel(creatureObj, type, 1);

            // 3D collider for interaction
            var collider = creatureObj.AddComponent<SphereCollider>();
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
    }

    /// <summary>
    /// A wild 3D creature wandering on an island.
    /// Player can feed it orbs to tame it and add to their crew.
    /// </summary>
    public class WildCreature : MonoBehaviour
    {
        private CreatureType creatureType;
        private float wanderSpeed;
        private float wanderRadius;
        private int orbsNeeded;
        private int orbsFed;
        private Vector3 homePosition;
        private Vector3 wanderTarget;
        private float wanderTimer;
        private bool isTamed;
        private Transform trustIndicator;

        public CreatureType Type => creatureType;
        public bool IsTamed => isTamed;
        public float TrustProgress => orbsNeeded > 0 ? (float)orbsFed / orbsNeeded : 0;
        public System.Action<WildCreature> OnTamed;

        public void Initialize(CreatureType type, float speed, float radius, int orbsToTame, Vector3 home)
        {
            creatureType = type;
            wanderSpeed = speed;
            wanderRadius = radius;
            orbsNeeded = orbsToTame;
            homePosition = home;
            wanderTarget = homePosition;

            CreateTrustIndicator();
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

            if (wanderTimer <= 0 || Vector3.Distance(transform.position, wanderTarget) < 0.3f)
            {
                // Pick random point on XZ plane around home
                Vector2 circle = Random.insideUnitCircle * wanderRadius;
                wanderTarget = homePosition + new Vector3(circle.x, 0, circle.y);
                wanderTimer = Random.Range(2f, 5f);
            }

            Vector3 dir = (wanderTarget - transform.position);
            dir.y = 0; // Stay on XZ plane
            dir = dir.normalized;

            transform.position += dir * wanderSpeed * Time.deltaTime;

            // Rotate to face movement direction
            if (dir.magnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 5f * Time.deltaTime);
            }
        }

        public bool FeedOrb(OrbData orb)
        {
            if (isTamed) return false;

            orbsFed++;

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

            StartCoroutine(HappyBounce());
            return false;
        }

        private void Tame()
        {
            isTamed = true;

            if (trustIndicator != null)
                Destroy(trustIndicator.gameObject);

            OnTamed?.Invoke(this);

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var companion = gameObject.AddComponent<CreatureCompanion>();
                companion.Initialize(GetDefaultName(), creatureType, player.transform);
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

        private void CreateTrustIndicator()
        {
            var indicatorObj = new GameObject("TrustIndicator");
            indicatorObj.transform.SetParent(transform, false);
            indicatorObj.transform.localPosition = new Vector3(0, 2f, 0);

            // Progress bar using a stretched cube
            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "ProgressBar";
            bar.transform.SetParent(indicatorObj.transform, false);
            bar.transform.localScale = new Vector3(1f, 0.1f, 0.1f);
            bar.GetComponent<Renderer>().material =
                AlienSpriteGenerator.CreateEmissiveMaterial(Color.red, 1f);
            Object.Destroy(bar.GetComponent<Collider>());

            trustIndicator = indicatorObj.transform;

            // Billboard so it always faces camera
            indicatorObj.AddComponent<World.BillboardLabel>();

            UpdateIndicator();
        }

        private void UpdateIndicator()
        {
            if (trustIndicator == null) return;

            var bar = trustIndicator.Find("ProgressBar");
            if (bar == null) return;

            float progress = TrustProgress;
            Color barColor = Color.Lerp(Color.red, Color.green, progress);
            bar.GetComponent<Renderer>().material =
                AlienSpriteGenerator.CreateEmissiveMaterial(barColor, 1f);
            bar.localScale = new Vector3(Mathf.Max(0.1f, progress), 0.1f, 0.1f);
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
