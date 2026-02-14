using System.Collections.Generic;
using UnityEngine;
using OrbWanderer.Core;
using OrbWanderer.Data;

namespace OrbWanderer.World
{
    /// <summary>
    /// Controls a single region in the game world.
    /// Manages orb spawning and wildlife placement within the region.
    /// Attach to the root GameObject of each region scene/area.
    /// </summary>
    public class RegionController : MonoBehaviour
    {
        [SerializeField] private RegionData regionData;
        [SerializeField] private Transform[] orbSpawnPoints;
        [SerializeField] private Transform[] wildlifeSpawnPoints;
        [SerializeField] private Collider2D regionBounds;

        [Header("Spawning")]
        [SerializeField] private float orbRespawnInterval = 30f;
        [SerializeField] private float wildlifeRespawnInterval = 60f;

        private List<GameObject> activeOrbs = new List<GameObject>();
        private List<GameObject> activeWildlife = new List<GameObject>();
        private float orbSpawnTimer;
        private float wildlifeSpawnTimer;

        public RegionData RegionData => regionData;

        private void Start()
        {
            if (regionData == null)
            {
                Debug.LogError($"RegionController on {gameObject.name} has no RegionData assigned!");
                return;
            }

            SpawnInitialOrbs();
            SpawnInitialWildlife();
        }

        private void Update()
        {
            orbSpawnTimer += Time.deltaTime;
            wildlifeSpawnTimer += Time.deltaTime;

            if (orbSpawnTimer >= orbRespawnInterval)
            {
                orbSpawnTimer = 0f;
                TrySpawnOrbs();
            }

            if (wildlifeSpawnTimer >= wildlifeRespawnInterval)
            {
                wildlifeSpawnTimer = 0f;
                TrySpawnWildlife();
            }

            CleanupDestroyedObjects();
        }

        private void SpawnInitialOrbs()
        {
            if (regionData.orbSpawns == null) return;

            foreach (var orbSpawn in regionData.orbSpawns)
            {
                int toSpawn = Mathf.Min(orbSpawn.maxActive, orbSpawnPoints.Length);
                for (int i = 0; i < toSpawn; i++)
                {
                    if (Random.value <= orbSpawn.spawnWeight)
                    {
                        SpawnOrb(orbSpawn.orbData, GetRandomSpawnPoint(orbSpawnPoints));
                    }
                }
            }
        }

        private void SpawnInitialWildlife()
        {
            if (regionData.localWildlife == null) return;

            foreach (var wildlife in regionData.localWildlife)
            {
                if (wildlifeSpawnPoints.Length == 0) break;

                if (Random.value <= wildlife.spawnChance)
                {
                    SpawnWildlife(wildlife, GetRandomSpawnPoint(wildlifeSpawnPoints));
                }
            }
        }

        private void TrySpawnOrbs()
        {
            if (regionData.orbSpawns == null) return;

            foreach (var orbSpawn in regionData.orbSpawns)
            {
                int currentCount = CountActiveOrbsOfType(orbSpawn.orbData);
                if (currentCount < orbSpawn.maxActive && Random.value <= orbSpawn.spawnWeight)
                {
                    var point = GetRandomSpawnPoint(orbSpawnPoints);
                    if (point != null)
                    {
                        SpawnOrb(orbSpawn.orbData, point);
                    }
                }
            }
        }

        private void TrySpawnWildlife()
        {
            if (regionData.localWildlife == null || wildlifeSpawnPoints.Length == 0) return;

            foreach (var wildlife in regionData.localWildlife)
            {
                if (Random.value <= wildlife.spawnChance * 0.5f) // Lower respawn rate
                {
                    SpawnWildlife(wildlife, GetRandomSpawnPoint(wildlifeSpawnPoints));
                }
            }
        }

        private void SpawnOrb(OrbData orbData, Transform point)
        {
            if (point == null) return;

            // Instantiate orb prefab - in production, use object pooling
            var orb = new GameObject($"Orb_{orbData.orbName}");
            orb.transform.position = point.position;
            orb.transform.SetParent(transform);

            var collectible = orb.AddComponent<CollectibleOrb>();
            collectible.Initialize(orbData);

            // Add visual components
            var sr = orb.AddComponent<SpriteRenderer>();
            sr.sprite = orbData.icon;
            sr.color = orbData.glowColor;
            orb.transform.localScale = Vector3.one * orbData.visualScale;

            // Add collider for pickup
            var col = orb.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;

            activeOrbs.Add(orb);
        }

        private void SpawnWildlife(WildlifeData wildlifeData, Transform point)
        {
            if (point == null) return;

            var wildlife = new GameObject($"Wildlife_{wildlifeData.wildlifeName}");
            wildlife.transform.position = point.position;
            wildlife.transform.SetParent(transform);

            var controller = wildlife.AddComponent<Wildlife.WildlifeController>();
            controller.Initialize(wildlifeData);

            var sr = wildlife.AddComponent<SpriteRenderer>();
            sr.sprite = wildlifeData.worldSprite;

            var col = wildlife.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 1f;

            var rb = wildlife.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;

            activeWildlife.Add(wildlife);
        }

        private Transform GetRandomSpawnPoint(Transform[] points)
        {
            if (points == null || points.Length == 0) return null;
            return points[Random.Range(0, points.Length)];
        }

        private int CountActiveOrbsOfType(OrbData orbData)
        {
            int count = 0;
            foreach (var orb in activeOrbs)
            {
                if (orb != null)
                {
                    var collectible = orb.GetComponent<CollectibleOrb>();
                    if (collectible != null && collectible.OrbData == orbData)
                        count++;
                }
            }
            return count;
        }

        private void CleanupDestroyedObjects()
        {
            activeOrbs.RemoveAll(o => o == null);
            activeWildlife.RemoveAll(w => w == null);
        }
    }
}
