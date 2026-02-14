using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using OrbWanderer.Player;

namespace OrbWanderer.UI
{
    /// <summary>
    /// Mobile virtual joystick and action buttons.
    /// Provides touch-based movement and interaction controls.
    /// </summary>
    public class MobileInputUI : MonoBehaviour
    {
        [Header("Joystick")]
        [SerializeField] private RectTransform joystickBackground;
        [SerializeField] private RectTransform joystickHandle;
        [SerializeField] private float joystickRange = 50f;

        [Header("Buttons")]
        [SerializeField] private Button interactButton;
        [SerializeField] private Button mountButton;
        [SerializeField] private Button mapButton;
        [SerializeField] private Button satchelButton;

        [Header("References")]
        [SerializeField] private WorldMapUI worldMapUI;
        [SerializeField] private SatchelUI satchelUI;

        private bool isDraggingJoystick;
        private int joystickTouchId = -1;
        private Vector2 joystickStartPos;

        private void Start()
        {
            if (joystickBackground != null)
            {
                joystickStartPos = joystickBackground.anchoredPosition;
            }

            SetupButtons();
        }

        private void SetupButtons()
        {
            if (interactButton != null)
                interactButton.onClick.AddListener(OnInteractPressed);

            if (mountButton != null)
                mountButton.onClick.AddListener(OnMountPressed);

            if (mapButton != null)
                mapButton.onClick.AddListener(OnMapPressed);

            if (satchelButton != null)
                satchelButton.onClick.AddListener(OnSatchelPressed);
        }

        private void Update()
        {
            HandleTouchInput();
            HandleKeyboardShortcuts();
        }

        private void HandleTouchInput()
        {
            if (Input.touchCount == 0)
            {
                ResetJoystick();
                return;
            }

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                // Check if touch is on the joystick side of the screen (left half)
                bool isLeftSide = touch.position.x < Screen.width * 0.4f;

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        if (isLeftSide && !isDraggingJoystick && !IsPointerOverUI(touch.position))
                        {
                            StartJoystickDrag(touch);
                        }
                        break;

                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        if (isDraggingJoystick && touch.fingerId == joystickTouchId)
                        {
                            UpdateJoystickDrag(touch);
                        }
                        break;

                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        if (touch.fingerId == joystickTouchId)
                        {
                            ResetJoystick();
                        }
                        break;
                }
            }
        }

        private void StartJoystickDrag(Touch touch)
        {
            isDraggingJoystick = true;
            joystickTouchId = touch.fingerId;

            // Move joystick base to touch position
            if (joystickBackground != null)
            {
                joystickBackground.position = touch.position;
            }
        }

        private void UpdateJoystickDrag(Touch touch)
        {
            if (joystickBackground == null || joystickHandle == null) return;

            Vector2 offset = touch.position - (Vector2)joystickBackground.position;
            Vector2 clampedOffset = Vector2.ClampMagnitude(offset, joystickRange);

            joystickHandle.anchoredPosition = clampedOffset;

            // Send input to player
            Vector2 input = clampedOffset / joystickRange;
            var player = PlayerController.Instance;
            if (player != null)
            {
                player.SetMoveInput(input);
            }
        }

        private void ResetJoystick()
        {
            isDraggingJoystick = false;
            joystickTouchId = -1;

            if (joystickHandle != null)
            {
                joystickHandle.anchoredPosition = Vector2.zero;
            }

            if (joystickBackground != null)
            {
                joystickBackground.anchoredPosition = joystickStartPos;
            }

            var player = PlayerController.Instance;
            if (player != null)
            {
                player.SetMoveInput(Vector2.zero);
            }
        }

        private void HandleKeyboardShortcuts()
        {
            // For testing in editor
            if (Input.GetKeyDown(KeyCode.M))
            {
                OnMapPressed();
            }

            if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Tab))
            {
                OnSatchelPressed();
            }
        }

        private void OnInteractPressed()
        {
            // Trigger interaction through the player controller
            // Mobile: the interact button serves as the tap-to-interact equivalent
        }

        private void OnMountPressed()
        {
            var player = PlayerController.Instance;
            if (player == null) return;

            var companion = Wildlife.CompanionManager.Instance;
            if (companion == null) return;

            if (companion.IsMounted)
                companion.Dismount();
            else
                companion.TryMount();
        }

        private void OnMapPressed()
        {
            if (worldMapUI != null)
            {
                worldMapUI.ToggleMap();
            }
        }

        private void OnSatchelPressed()
        {
            if (satchelUI != null)
            {
                satchelUI.ToggleSatchel();
            }
        }

        private bool IsPointerOverUI(Vector2 screenPos)
        {
            var eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPos
            };
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            return results.Count > 0;
        }
    }
}
