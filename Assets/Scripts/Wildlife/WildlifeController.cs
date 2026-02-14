using UnityEngine;
using OrbWanderer.Core;
using OrbWanderer.Data;

namespace OrbWanderer.Wildlife
{
    /// <summary>
    /// Controls individual wildlife behavior in the 3D world.
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

        private Vector3 wanderTarget;
        private float wanderTimer;
        private Transform playerTransform;
        private bool isInteractable;

        public System.Action<WildlifeController> OnBefriended;
        public System.Action<WildlifeController, FriendshipLevel> OnFriendshipChanged;

        public WildlifeData WildlifeData => wildlifeData;
        public FriendshipLevel Friendship => friendshipLevel;
        public bool IsBefriended => isBefriended;

        public void Initialize(WildlifeData data)
        {
            wildlifeData = data;
            wanderTarget = transform.position;
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
            if (isBefriended) return;
            if (playerTransform == null) return;

            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

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
                wanderTarget = transform.position + new Vector3(randomDir.x, 0, randomDir.y);
            }
        }

        private void FleeFromPlayer()
        {
            if (playerTransform == null) return;
            Vector3 awayDir = (transform.position - playerTransform.position);
            awayDir.y = 0;
            awayDir = awayDir.normalized;
            wanderTarget = transform.position + awayDir * detectionRange;
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

            Vector3 direction = wanderTarget - transform.position;
            direction.y = 0; // Stay on XZ plane

            if (direction.magnitude > 0.1f)
            {
                Vector3 newPos = Vector3.MoveTowards(transform.position, wanderTarget, speed * Time.deltaTime);
                newPos.y = transform.position.y; // Preserve Y
                transform.position = newPos;

                // Rotate to face movement direction
                Quaternion targetRot = Quaternion.LookRotation(direction.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 5f * Time.deltaTime);
            }
        }

        public BefriendResult TryBefriend(Inventory.SatchelManager satchel)
        {
            if (isBefriended)
                return BefriendResult.AlreadyBefriended;

            if (!isInteractable)
                return BefriendResult.TooFar;

            if (wildlifeData.befriendRecipe == null)
            {
                interactionCount++;
                UpdateFriendship();
                return friendshipLevel == FriendshipLevel.Bonded ?
                    BefriendResult.Success : BefriendResult.FriendshipIncreased;
            }

            if (!satchel.HasRecipeOrbs(wildlifeData.befriendRecipe))
                return BefriendResult.InsufficientOrbs;

            satchel.SpendRecipeOrbs(wildlifeData.befriendRecipe);
            interactionCount += 3;
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
