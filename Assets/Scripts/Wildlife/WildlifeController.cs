using UnityEngine;
using OrbWanderer.Core;
using OrbWanderer.Data;

namespace OrbWanderer.Wildlife
{
    /// <summary>
    /// Controls individual wildlife behavior in the world.
    /// Handles AI movement, player interaction, and befriending.
    /// </summary>
    public class WildlifeController : MonoBehaviour
    {
        private WildlifeData wildlifeData;
        private FriendshipLevel friendshipLevel = FriendshipLevel.Wild;
        private int interactionCount;
        private bool isBefriended;

        [Header("AI Settings")]
        [SerializeField] private float wanderRadius = 5f;
        [SerializeField] private float wanderInterval = 3f;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float fleeSpeed = 5f;
        [SerializeField] private float detectionRange = 8f;
        [SerializeField] private float interactionRange = 2f;

        private Vector2 wanderTarget;
        private float wanderTimer;
        private Transform playerTransform;
        private bool isInteractable;

        // Events
        public System.Action<WildlifeController> OnBefriended;
        public System.Action<WildlifeController, FriendshipLevel> OnFriendshipChanged;

        public WildlifeData WildlifeData => wildlifeData;
        public FriendshipLevel Friendship => friendshipLevel;
        public bool IsBefriended => isBefriended;

        public void Initialize(WildlifeData data)
        {
            wildlifeData = data;
            wanderTarget = transform.position;

            // Scale speed based on wildlife size
            moveSpeed *= GetSizeSpeedMultiplier();
        }

        private void Update()
        {
            if (wildlifeData == null) return;

            FindPlayer();
            UpdateBehavior();
            UpdateMovement();
        }

        private void FindPlayer()
        {
            if (playerTransform == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }
        }

        private void UpdateBehavior()
        {
            if (isBefriended) return; // Befriended wildlife follows companion logic
            if (playerTransform == null) return;

            float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);

            switch (wildlifeData.behavior)
            {
                case WildlifeBehavior.Passive:
                    Wander();
                    isInteractable = distToPlayer <= interactionRange;
                    break;

                case WildlifeBehavior.Timid:
                    if (distToPlayer < detectionRange)
                    {
                        FleeFromPlayer();
                        isInteractable = false;
                    }
                    else
                    {
                        Wander();
                        isInteractable = distToPlayer <= interactionRange;
                    }
                    break;

                case WildlifeBehavior.Neutral:
                    Wander();
                    isInteractable = distToPlayer <= interactionRange;
                    break;

                case WildlifeBehavior.Aggressive:
                    if (distToPlayer < detectionRange)
                    {
                        ChasePlayer();
                        isInteractable = distToPlayer <= interactionRange;
                    }
                    else
                    {
                        Wander();
                        isInteractable = false;
                    }
                    break;
            }
        }

        private void Wander()
        {
            wanderTimer += Time.deltaTime;
            if (wanderTimer >= wanderInterval)
            {
                wanderTimer = 0f;
                Vector2 randomDir = Random.insideUnitCircle * wanderRadius;
                wanderTarget = (Vector2)transform.position + randomDir;
            }
        }

        private void FleeFromPlayer()
        {
            if (playerTransform == null) return;
            Vector2 awayDir = ((Vector2)transform.position - (Vector2)playerTransform.position).normalized;
            wanderTarget = (Vector2)transform.position + awayDir * detectionRange;
        }

        private void ChasePlayer()
        {
            if (playerTransform == null) return;
            wanderTarget = playerTransform.position;
        }

        private void UpdateMovement()
        {
            float speed = isBefriended ? moveSpeed :
                (wildlifeData.behavior == WildlifeBehavior.Timid ? fleeSpeed : moveSpeed);

            Vector2 currentPos = transform.position;
            Vector2 direction = (wanderTarget - currentPos);

            if (direction.magnitude > 0.1f)
            {
                Vector2 newPos = Vector2.MoveTowards(currentPos, wanderTarget, speed * Time.deltaTime);
                transform.position = newPos;

                // Flip sprite based on movement direction
                if (direction.x != 0)
                {
                    Vector3 scale = transform.localScale;
                    scale.x = direction.x > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
                    transform.localScale = scale;
                }
            }
        }

        /// <summary>
        /// Attempt to befriend this wildlife using orbs.
        /// Called from the player interaction system.
        /// </summary>
        public BefriendResult TryBefriend(Inventory.SatchelManager satchel)
        {
            if (isBefriended)
                return BefriendResult.AlreadyBefriended;

            if (!isInteractable)
                return BefriendResult.TooFar;

            if (wildlifeData.befriendRecipe == null)
            {
                // No recipe needed - just interaction count
                interactionCount++;
                UpdateFriendship();
                return friendshipLevel == FriendshipLevel.Bonded ?
                    BefriendResult.Success : BefriendResult.FriendshipIncreased;
            }

            if (!satchel.HasRecipeOrbs(wildlifeData.befriendRecipe))
                return BefriendResult.InsufficientOrbs;

            satchel.SpendRecipeOrbs(wildlifeData.befriendRecipe);
            interactionCount += 3; // Recipe-based befriending gives more friendship
            UpdateFriendship();

            return friendshipLevel == FriendshipLevel.Bonded ?
                BefriendResult.Success : BefriendResult.FriendshipIncreased;
        }

        private void UpdateFriendship()
        {
            float progress = (float)interactionCount / wildlifeData.interactionsToMax;
            FriendshipLevel newLevel;

            if (progress >= 1f)
                newLevel = FriendshipLevel.Bonded;
            else if (progress >= 0.75f)
                newLevel = FriendshipLevel.Companion;
            else if (progress >= 0.5f)
                newLevel = FriendshipLevel.Friendly;
            else if (progress >= 0.25f)
                newLevel = FriendshipLevel.Acquaintance;
            else
                newLevel = FriendshipLevel.Wild;

            if (newLevel != friendshipLevel)
            {
                friendshipLevel = newLevel;
                OnFriendshipChanged?.Invoke(this, friendshipLevel);
            }

            if (friendshipLevel == FriendshipLevel.Bonded && !isBefriended)
            {
                isBefriended = true;
                OnBefriended?.Invoke(this);
            }
        }

        private float GetSizeSpeedMultiplier()
        {
            return wildlifeData.size switch
            {
                WildlifeSize.Tiny => 0.6f,
                WildlifeSize.Small => 0.8f,
                WildlifeSize.Medium => 1f,
                WildlifeSize.Large => 1.3f,
                WildlifeSize.Massive => 1.6f,
                _ => 1f
            };
        }
    }

    public enum BefriendResult
    {
        Success,
        FriendshipIncreased,
        InsufficientOrbs,
        TooFar,
        AlreadyBefriended
    }
}
