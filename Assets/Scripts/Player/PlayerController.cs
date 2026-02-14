using UnityEngine;
using OrbWanderer.Wildlife;
using OrbWanderer.Equipment;

namespace OrbWanderer.Player
{
    /// <summary>
    /// Handles player movement, interaction input, and mount control.
    /// Designed for mobile with virtual joystick support and tap interactions.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Movement")]
        [SerializeField] private float baseMoveSpeed = 5f;
        [SerializeField] private float sprintMultiplier = 1.5f;

        [Header("Interaction")]
        [SerializeField] private float interactionRange = 2f;
        [SerializeField] private LayerMask interactableLayer;

        [Header("References")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;

        private Rigidbody2D rb;
        private Vector2 moveInput;
        private bool isSprinting;
        private bool inputEnabled = true;

        // Animation hash IDs
        private static readonly int AnimSpeed = Animator.StringToHash("Speed");
        private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");
        private static readonly int AnimIsMounted = Animator.StringToHash("IsMounted");

        public float CurrentSpeed { get; private set; }
        public bool IsMoving => moveInput.magnitude > 0.1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;
        }

        private void Update()
        {
            if (!inputEnabled) return;

            HandleInput();
            UpdateAnimations();
        }

        private void FixedUpdate()
        {
            if (!inputEnabled) return;

            ApplyMovement();
        }

        private void HandleInput()
        {
            // Keyboard input (for editor testing)
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            moveInput = new Vector2(h, v).normalized;

            isSprinting = Input.GetKey(KeyCode.LeftShift);

            // Tap/click interaction
            if (Input.GetMouseButtonDown(0))
            {
                TryInteract();
            }

            // Mount/Dismount
            if (Input.GetKeyDown(KeyCode.E))
            {
                ToggleMount();
            }
        }

        /// <summary>
        /// Called by the mobile virtual joystick UI.
        /// </summary>
        public void SetMoveInput(Vector2 input)
        {
            if (!inputEnabled) return;
            moveInput = input.normalized;
        }

        public void SetSprinting(bool sprinting)
        {
            isSprinting = sprinting;
        }

        private void ApplyMovement()
        {
            float speed = CalculateSpeed();
            CurrentSpeed = speed;

            Vector2 velocity = moveInput * speed;
            rb.linearVelocity = velocity;

            // Flip sprite based on direction
            if (moveInput.x != 0 && spriteRenderer != null)
            {
                spriteRenderer.flipX = moveInput.x < 0;
            }
        }

        private float CalculateSpeed()
        {
            float speed = baseMoveSpeed;

            // Apply sprint
            if (isSprinting) speed *= sprintMultiplier;

            // Apply mount bonus
            var companion = CompanionManager.Instance;
            if (companion != null && companion.IsMounted)
            {
                var mountProps = companion.GetMountProperties();
                speed = mountProps.speedMultiplier;
            }

            // Apply equipment bonus
            var equip = EquipmentManager.Instance;
            if (equip != null)
            {
                speed *= equip.GetSpeedMultiplier();
            }

            return speed;
        }

        private void UpdateAnimations()
        {
            if (animator == null) return;

            animator.SetFloat(AnimSpeed, CurrentSpeed);
            animator.SetBool(AnimIsMoving, IsMoving);

            var companion = CompanionManager.Instance;
            animator.SetBool(AnimIsMounted, companion != null && companion.IsMounted);
        }

        private void TryInteract()
        {
            Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D hit = Physics2D.OverlapCircle(worldPos, 0.5f, interactableLayer);

            if (hit == null) return;

            float dist = Vector2.Distance(transform.position, hit.transform.position);
            if (dist > interactionRange) return;

            // Try wildlife interaction
            var wildlife = hit.GetComponent<WildlifeController>();
            if (wildlife != null)
            {
                HandleWildlifeInteraction(wildlife);
                return;
            }
        }

        private void HandleWildlifeInteraction(WildlifeController wildlife)
        {
            if (wildlife.IsBefriended)
            {
                // Already befriended - show companion info or set as active
                CompanionManager.Instance?.SetActiveCompanion(wildlife.WildlifeData);
            }
            else
            {
                // Try to befriend
                var satchel = Inventory.SatchelManager.Instance;
                if (satchel != null)
                {
                    var result = wildlife.TryBefriend(satchel);
                    HandleBefriendResult(result, wildlife);
                }
            }
        }

        private void HandleBefriendResult(BefriendResult result, WildlifeController wildlife)
        {
            switch (result)
            {
                case BefriendResult.Success:
                    CompanionManager.Instance?.AddCompanion(wildlife.WildlifeData, wildlife);
                    break;
                case BefriendResult.FriendshipIncreased:
                    // Show friendship progress UI
                    break;
                case BefriendResult.InsufficientOrbs:
                    // Show "need more orbs" feedback
                    break;
                case BefriendResult.TooFar:
                    // Show "get closer" feedback
                    break;
            }
        }

        private void ToggleMount()
        {
            var companion = CompanionManager.Instance;
            if (companion == null) return;

            if (companion.IsMounted)
            {
                companion.Dismount();
            }
            else
            {
                var result = companion.TryMount();
                HandleMountResult(result);
            }
        }

        private void HandleMountResult(MountResult result)
        {
            switch (result)
            {
                case MountResult.Success:
                    // Play mount animation, update visuals
                    break;
                case MountResult.NeedBetterSaddle:
                    // Show "need better saddle" message
                    break;
                case MountResult.MissingEquipment:
                    // Show "missing equipment" message
                    break;
                case MountResult.NotRideable:
                    // Show "can't ride this companion" message
                    break;
            }
        }

        /// <summary>
        /// Check if player can traverse the current terrain.
        /// Used for water, steep cliffs, etc.
        /// </summary>
        public bool CanTraverse(TerrainType terrain)
        {
            var companion = CompanionManager.Instance;
            if (companion == null || !companion.IsMounted)
            {
                return terrain == TerrainType.Normal;
            }

            var mountProps = companion.GetMountProperties();
            return terrain switch
            {
                TerrainType.Normal => true,
                TerrainType.Water => mountProps.canSwim,
                TerrainType.Cliff => mountProps.canClimb,
                TerrainType.Air => mountProps.canFly,
                _ => false
            };
        }

        public void EnableInput(bool enabled)
        {
            inputEnabled = enabled;
            if (!enabled) rb.linearVelocity = Vector2.zero;
        }
    }

    public enum TerrainType
    {
        Normal,
        Water,
        Cliff,
        Air
    }
}
