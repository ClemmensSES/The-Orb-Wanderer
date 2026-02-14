using UnityEngine;

namespace OrbWanderer.Core
{
    /// <summary>
    /// Smooth camera follow for the player. Handles zoom and region bounds clamping.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Follow Settings")]
        [SerializeField] private Transform target;
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);

        [Header("Zoom")]
        [SerializeField] private float defaultZoom = 5f;
        [SerializeField] private float minZoom = 3f;
        [SerializeField] private float maxZoom = 10f;
        [SerializeField] private float zoomSpeed = 2f;

        [Header("Bounds")]
        [SerializeField] private bool useBounds;
        [SerializeField] private Vector2 boundsMin;
        [SerializeField] private Vector2 boundsMax;

        private Camera cam;
        private float targetZoom;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.orthographicSize = defaultZoom;
                targetZoom = defaultZoom;
            }
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

            FollowTarget();
            HandleZoom();
        }

        private void FollowTarget()
        {
            Vector3 desiredPos = target.position + offset;

            if (useBounds)
            {
                desiredPos.x = Mathf.Clamp(desiredPos.x, boundsMin.x, boundsMax.x);
                desiredPos.y = Mathf.Clamp(desiredPos.y, boundsMin.y, boundsMax.y);
            }

            transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
        }

        private void HandleZoom()
        {
            if (cam == null) return;

            // Pinch zoom on mobile
            if (Input.touchCount == 2)
            {
                Touch t0 = Input.GetTouch(0);
                Touch t1 = Input.GetTouch(1);

                float prevDist = ((t0.position - t0.deltaPosition) - (t1.position - t1.deltaPosition)).magnitude;
                float currDist = (t0.position - t1.position).magnitude;

                float diff = prevDist - currDist;
                targetZoom += diff * zoomSpeed * 0.01f;
            }

            // Scroll wheel in editor
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                targetZoom -= scroll * zoomSpeed;
            }

            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, smoothSpeed * Time.deltaTime);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void SetBounds(Vector2 min, Vector2 max)
        {
            useBounds = true;
            boundsMin = min;
            boundsMax = max;
        }

        public void ClearBounds()
        {
            useBounds = false;
        }

        public void SetZoom(float zoom)
        {
            targetZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
        }
    }
}
