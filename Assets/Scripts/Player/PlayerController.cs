using UnityEngine;
using OrbWanderer.Wildlife;
using OrbWanderer.Equipment;

namespace OrbWanderer.Player
{
    /// <summary>
    /// 3D third-person player controller.
    /// Moves on XZ plane, rotates to face movement direction.
    /// Supports mobile virtual joystick and keyboard (WASD/arrows).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Movement")]
        [SerializeField] private float baseMoveSpeed = 5f;
        [SerializeField] private float sprintMultiplier = 1.5f;
        [SerializeField] private float rotationSpeed = 10f;

        [Header("Interaction")]
        [SerializeField] private float interactionRange = 2f;
        [SerializeField] private LayerMask interactableLayer;

        [Header("References")]
        [SerializeField] private Animator animator;

        private Rigidbody rb;
        private Vector2 moveInput;
        private bool isSprinting;
        private bool inputEnabled = true;

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

            rb = GetComponent<Rigidbody>();
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
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
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            moveInput = new Vector2(h, v).normalized;

            isSprinting = Input.GetKey(KeyCode.LeftShift);

            if (Input.GetMouseButtonDown(0))
                TryInteract();

            if (Input.GetKeyDown(KeyCode.E))
                ToggleMount();
        }

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

            // Map 2D input to 3D XZ plane
            Vector3 moveDir = new Vector3(moveInput.x, 0f, moveInput.y);

            // Preserve Y velocity for gravity
            Vector3 velocity = moveDir * speed;
            velocity.y = rb.linearVelocity.y;
            rb.linearVelocity = velocity;

            // Rotate to face movement direction
            if (moveDir.magnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
            }
        }

        private float CalculateSpeed()
        {
            float speed = baseMoveSpeed;
            if (isSprinting) speed *= sprintMultiplier;

            var companion = CompanionManager.Instance;
            if (companion != null && companion.IsMounted)
            {
                var mountProps = companion.GetMountProperties();
                speed = mountProps.speedMultiplier;
            }

            var equip = EquipmentManager.Instance;
            if (equip != null)
                speed *= equip.GetSpeedMultiplier();

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
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 50f, interactableLayer))
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist > interactionRange) return;

                var wildlife = hit.collider.GetComponent<WildlifeController>();
                if (wildlife != null)
                {
                    HandleWildlifeInteraction(wildlife);
                }
            }
        }

        private void HandleWildlifeInteraction(WildlifeController wildlife)
        {
            if (wildlife.IsBefriended)
            {
                CompanionManager.Instance?.SetActiveCompanion(wildlife.WildlifeData);
            }
            else
            {
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
                case BefriendResult.InsufficientOrbs:
                case BefriendResult.TooFar:
                    break;
            }
        }

        private void ToggleMount()
        {
            var companion = CompanionManager.Instance;
            if (companion == null) return;

            if (companion.IsMounted)
                companion.Dismount();
            else
                companion.TryMount();
        }

        public bool CanTraverse(TerrainType terrain)
        {
            var companion = CompanionManager.Instance;
            if (companion == null || !companion.IsMounted)
                return terrain == TerrainType.Normal;

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
            if (!enabled) rb.linearVelocity = Vector3.zero;
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
