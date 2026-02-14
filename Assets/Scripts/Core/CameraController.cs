using UnityEngine;

namespace OrbWanderer.Core
{
    /// <summary>
    /// Third-person camera that follows the player from behind.
    /// Supports zoom in/out via scroll wheel and pinch gesture.
    /// Player is seen from behind as they walk around.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Follow Settings")]
        [SerializeField] private Transform target;
        [SerializeField] private float smoothSpeed = 8f;

        [Header("Third-Person Offset")]
        [SerializeField] private float defaultDistance = 10f;
        [SerializeField] private float heightOffset = 6f;
        [SerializeField] private float lookAheadDistance = 2f;
        [SerializeField] private float cameraAngle = 35f;

        [Header("Zoom")]
        [SerializeField] private float minDistance = 4f;
        [SerializeField] private float maxDistance = 25f;
        [SerializeField] private float zoomSpeed = 4f;

        [Header("Bounds")]
        [SerializeField] private bool useBounds;
        [SerializeField] private Vector3 boundsMin;
        [SerializeField] private Vector3 boundsMax;

        private Camera cam;
        private float currentDistance;
        private float targetDistance;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.orthographic = false;
                cam.fieldOfView = 50f;
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = 500f;
            }
            currentDistance = defaultDistance;
            targetDistance = defaultDistance;
        }

        private void Start()
        {
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            HandleZoom();
            FollowTarget();
        }

        private void FollowTarget()
        {
            // Smooth zoom
            currentDistance = Mathf.Lerp(currentDistance, targetDistance, smoothSpeed * Time.deltaTime);

            // Calculate camera position behind and above player
            float angleRad = cameraAngle * Mathf.Deg2Rad;
            float horizontalDist = currentDistance * Mathf.Cos(angleRad);
            float verticalDist = currentDistance * Mathf.Sin(angleRad) + heightOffset;

            // Position behind the player's forward direction
            Vector3 playerForward = target.forward;
            Vector3 desiredPos = target.position
                - playerForward * horizontalDist
                + Vector3.up * verticalDist;

            if (useBounds)
            {
                desiredPos.x = Mathf.Clamp(desiredPos.x, boundsMin.x, boundsMax.x);
                desiredPos.y = Mathf.Clamp(desiredPos.y, boundsMin.y, boundsMax.y);
                desiredPos.z = Mathf.Clamp(desiredPos.z, boundsMin.z, boundsMax.z);
            }

            transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);

            // Look at player with slight look-ahead
            Vector3 lookTarget = target.position + Vector3.up * 1.5f + playerForward * lookAheadDistance;
            Quaternion targetRot = Quaternion.LookRotation(lookTarget - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, smoothSpeed * Time.deltaTime);
        }

        private void HandleZoom()
        {
            // Pinch zoom on mobile
            if (Input.touchCount == 2)
            {
                Touch t0 = Input.GetTouch(0);
                Touch t1 = Input.GetTouch(1);

                float prevDist = ((t0.position - t0.deltaPosition) - (t1.position - t1.deltaPosition)).magnitude;
                float currDist = (t0.position - t1.position).magnitude;

                float diff = prevDist - currDist;
                targetDistance += diff * zoomSpeed * 0.01f;
            }

            // Scroll wheel on desktop
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                targetDistance -= scroll * zoomSpeed * 3f;
            }

            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void SetBounds(Vector3 min, Vector3 max)
        {
            useBounds = true;
            boundsMin = min;
            boundsMax = max;
        }

        public void ClearBounds()
        {
            useBounds = false;
        }

        public void SetZoom(float distance)
        {
            targetDistance = Mathf.Clamp(distance, minDistance, maxDistance);
        }
    }
}
