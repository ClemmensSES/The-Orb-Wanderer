using UnityEngine;
using OrbWanderer.Core;
using OrbWanderer.Data;
using OrbWanderer.World;

namespace OrbWanderer.Wildlife
{
    /// <summary>
    /// A creature companion that follows the player, levels up, evolves,
    /// and has special abilities like fetching orbs, smashing obstacles,
    /// climbing walls, or floating to high places.
    /// </summary>
    public class CreatureCompanion : MonoBehaviour
    {
        [Header("Creature Info")]
        [SerializeField] private string creatureName;
        [SerializeField] private CreatureType creatureType;
        [SerializeField] private CreatureEvolution evolution = new CreatureEvolution();

        [Header("Following")]
        [SerializeField] private float followDistance = 2f;
        [SerializeField] private float followSpeed = 5f;
        [SerializeField] private float orbitSpeed = 1f;

        [Header("Abilities")]
        [SerializeField] private float fetchCooldown = 5f;
        [SerializeField] private float abilityCooldown = 8f;

        private Transform playerTransform;
        private SpriteRenderer spriteRenderer;
        private float followTimer;
        private float fetchTimer;
        private float abilityTimer;
        private float orbitAngle;
        private bool isFetching;
        private Transform fetchTarget;
        private bool isUsingAbility;

        public string CreatureName => creatureName;
        public CreatureType Type => creatureType;
        public CreatureEvolution Evolution => evolution;
        public bool IsBusy => isFetching || isUsingAbility;

        public System.Action<CreatureCompanion, int> OnLeveledUp;
        public System.Action<CreatureCompanion, int> OnEvolved;
        public System.Action<CreatureCompanion, GameObject> OnFetchedOrb;
        public System.Action<CreatureCompanion, GameObject> OnUsedAbility;

        public void Initialize(string name, CreatureType type, Transform player)
        {
            creatureName = name;
            creatureType = type;
            playerTransform = player;

            evolution = new CreatureEvolution();
            evolution.OnLevelUp += (level) => {
                UpdateVisuals();
                OnLeveledUp?.Invoke(this, level);
            };
            evolution.OnEvolve += (stage) => {
                UpdateVisuals();
                OnEvolved?.Invoke(this, stage);
            };

            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

            UpdateVisuals();
        }

        private void Update()
        {
            if (playerTransform == null) return;

            // XP from following
            followTimer += Time.deltaTime;
            if (followTimer >= 1f)
            {
                followTimer = 0f;
                evolution.AddXP(CreatureXPRewards.FollowingPlayer);
            }

            // Timers
            fetchTimer -= Time.deltaTime;
            abilityTimer -= Time.deltaTime;

            if (isFetching)
                UpdateFetching();
            else if (isUsingAbility)
                UpdateAbility();
            else
                UpdateFollowing();

            // Auto-fetch nearby orbs
            if (!isFetching && fetchTimer <= 0 && evolution.GetOrbFetchRange() > 0)
                TryAutoFetch();

            UpdateAnimation();
        }

        private void UpdateFollowing()
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);

            if (dist > followDistance * 3f)
            {
                // Teleport if too far
                transform.position = (Vector2)playerTransform.position +
                    Random.insideUnitCircle.normalized * followDistance;
            }
            else if (dist > followDistance)
            {
                // Move toward player
                float speed = followSpeed * evolution.GetSpeedMultiplier();
                Vector2 dir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
                transform.position += (Vector3)(dir * speed * Time.deltaTime);
            }
            else
            {
                // Orbit gently around player
                orbitAngle += orbitSpeed * Time.deltaTime;
                Vector2 orbitPos = (Vector2)playerTransform.position +
                    new Vector2(Mathf.Cos(orbitAngle), Mathf.Sin(orbitAngle)) * followDistance * 0.7f;
                transform.position = Vector2.Lerp(transform.position, orbitPos, Time.deltaTime * 2f);
            }

            // Flip sprite to face movement direction
            if (spriteRenderer != null)
            {
                Vector2 moveDir = (Vector2)playerTransform.position - (Vector2)transform.position;
                if (Mathf.Abs(moveDir.x) > 0.1f)
                    spriteRenderer.flipX = moveDir.x < 0;
            }
        }

        private void TryAutoFetch()
        {
            float fetchRange = evolution.GetOrbFetchRange();
            if (fetchRange <= 0) return;

            var colliders = Physics2D.OverlapCircleAll(transform.position, fetchRange);
            foreach (var col in colliders)
            {
                var orb = col.GetComponent<CollectibleOrb>();
                if (orb != null)
                {
                    StartFetch(col.transform);
                    break;
                }
            }
        }

        private void StartFetch(Transform target)
        {
            isFetching = true;
            fetchTarget = target;
            fetchTimer = fetchCooldown;
        }

        private void UpdateFetching()
        {
            if (fetchTarget == null)
            {
                isFetching = false;
                return;
            }

            float speed = followSpeed * evolution.GetSpeedMultiplier() * 1.5f;
            Vector2 dir = ((Vector2)fetchTarget.position - (Vector2)transform.position).normalized;
            transform.position += (Vector3)(dir * speed * Time.deltaTime);

            float dist = Vector2.Distance(transform.position, fetchTarget.position);
            if (dist < 0.5f)
            {
                // Collected the orb — bring it to player
                var orb = fetchTarget.GetComponent<CollectibleOrb>();
                if (orb != null)
                {
                    evolution.AddXP(CreatureXPRewards.FetchedOrb);
                    OnFetchedOrb?.Invoke(this, fetchTarget.gameObject);
                }
                isFetching = false;
                fetchTarget = null;
            }
        }

        /// <summary>
        /// Use the creature's special ability on a target.
        /// Returns true if the ability was used.
        /// </summary>
        public bool UseSpecialAbility(GameObject target)
        {
            if (abilityTimer > 0 || isUsingAbility) return false;

            switch (creatureType)
            {
                case CreatureType.Jellyfish:
                    // Float up to reach high orbs
                    isUsingAbility = true;
                    abilityTimer = abilityCooldown;
                    evolution.AddXP(CreatureXPRewards.ReachedHighOrb);
                    OnUsedAbility?.Invoke(this, target);
                    return true;

                case CreatureType.Whale:
                    // Smash through ice/rock obstacles
                    if (target != null)
                    {
                        isUsingAbility = true;
                        abilityTimer = abilityCooldown;
                        evolution.AddXP(CreatureXPRewards.SmashedObstacle);
                        OnUsedAbility?.Invoke(this, target);
                        return true;
                    }
                    break;

                case CreatureType.Spider:
                    // Build bridge or climb wall
                    isUsingAbility = true;
                    abilityTimer = abilityCooldown;
                    evolution.AddXP(CreatureXPRewards.BuiltBridge);
                    OnUsedAbility?.Invoke(this, target);
                    return true;

                case CreatureType.Firebird:
                    // Melt ice, light dark areas
                    if (target != null)
                    {
                        isUsingAbility = true;
                        abilityTimer = abilityCooldown;
                        evolution.AddXP(CreatureXPRewards.SmashedObstacle);
                        OnUsedAbility?.Invoke(this, target);
                        return true;
                    }
                    break;

                case CreatureType.IceGolem:
                    // Freeze water to create paths, smash through volcanic rock
                    isUsingAbility = true;
                    abilityTimer = abilityCooldown;
                    evolution.AddXP(CreatureXPRewards.SmashedObstacle);
                    OnUsedAbility?.Invoke(this, target);
                    return true;
            }

            return false;
        }

        private void UpdateAbility()
        {
            // Simple ability animation — float up/shake/etc
            float t = abilityTimer / abilityCooldown;
            if (t > 0.8f)
            {
                // Ability active
                transform.localScale = Vector3.one * evolution.GetSizeMultiplier() *
                    (1f + Mathf.Sin(Time.time * 10f) * 0.1f);
            }
            else
            {
                isUsingAbility = false;
                transform.localScale = Vector3.one * evolution.GetSizeMultiplier();
            }
        }

        /// <summary>
        /// Feed an orb to this creature to gain trust and XP.
        /// </summary>
        public void FeedOrb(OrbData orb)
        {
            float xp = CreatureXPRewards.FedOrb;

            // Bonus XP for orbs matching creature specialty
            if (IsSpecialtyOrb(orb.orbType))
                xp *= 2f;

            // Bonus for rare orbs
            if (orb.rarity >= OrbRarity.Rare)
                xp *= 1.5f;

            evolution.AddXP(xp);
        }

        public bool IsSpecialtyOrb(OrbType orbType)
        {
            return creatureType switch
            {
                CreatureType.Jellyfish => orbType == OrbType.Aqua || orbType == OrbType.Zephyr,
                CreatureType.Whale => orbType == OrbType.Aqua || orbType == OrbType.Terra,
                CreatureType.Spider => orbType == OrbType.Shadow || orbType == OrbType.Nature,
                CreatureType.Firebird => orbType == OrbType.Ember || orbType == OrbType.Radiant,
                CreatureType.IceGolem => orbType == OrbType.Frost || orbType == OrbType.Storm,
                _ => false
            };
        }

        private void UpdateVisuals()
        {
            if (spriteRenderer == null) return;

            spriteRenderer.sprite = creatureType switch
            {
                CreatureType.Jellyfish => AlienSpriteGenerator.CreateJellyfishSprite(evolution.currentLevel),
                CreatureType.Whale => AlienSpriteGenerator.CreateWhaleSprite(evolution.currentLevel),
                CreatureType.Spider => AlienSpriteGenerator.CreateSpiderSprite(evolution.currentLevel),
                CreatureType.Firebird => AlienSpriteGenerator.CreateFirebirdSprite(evolution.currentLevel),
                CreatureType.IceGolem => AlienSpriteGenerator.CreateIceGolemSprite(evolution.currentLevel),
                _ => spriteRenderer.sprite
            };

            transform.localScale = Vector3.one * evolution.GetSizeMultiplier();
            spriteRenderer.sortingOrder = 5;
        }

        private void UpdateAnimation()
        {
            // Gentle bobbing for flying/floating creatures
            if (creatureType == CreatureType.Jellyfish || creatureType == CreatureType.Firebird)
            {
                float bob = Mathf.Sin(Time.time * 2f) * 0.15f;
                var pos = transform.localPosition;
                pos.y += bob * Time.deltaTime;
                transform.localPosition = pos;
            }
        }
    }

    /// <summary>
    /// Types of creatures in the alien world.
    /// Each has unique abilities for traversal and orb collection.
    /// </summary>
    public enum CreatureType
    {
        Jellyfish,   // Floats up to reach high orbs, ocean travel
        Whale,       // Smashes obstacles, fast ocean crossing, carries stuff
        Spider,      // Climbs walls, builds bridges, grabs crystal orbs
        Firebird,    // Flies, melts ice, lights dark caves
        IceGolem     // Freezes water paths, smashes volcanic rock, cold immunity
    }
}
