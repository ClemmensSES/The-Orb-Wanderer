using UnityEngine;
using OrbWanderer.Data;
using OrbWanderer.Inventory;

namespace OrbWanderer.World
{
    /// <summary>
    /// Attached to 3D orb instances in the world. Handles collection by the player.
    /// </summary>
    public class CollectibleOrb : MonoBehaviour
    {
        private OrbData orbData;
        private bool isCollected;

        [Header("Animation")]
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobHeight = 0.2f;
        [SerializeField] private float rotateSpeed = 30f;

        private Vector3 startPosition;

        public OrbData OrbData => orbData;

        public void Initialize(OrbData data)
        {
            orbData = data;
            startPosition = transform.position;
        }

        private void Update()
        {
            if (isCollected) return;

            // Gentle bobbing animation (Y axis)
            float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);

            // Spin on Y axis for 3D look
            transform.Rotate(0, rotateSpeed * Time.deltaTime, 0);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCollected) return;

            if (other.CompareTag("Player"))
            {
                TryCollect();
            }
        }

        private void TryCollect()
        {
            var satchel = SatchelManager.Instance;
            if (satchel == null) return;

            if (satchel.TryAddOrb(orbData))
            {
                isCollected = true;
                OnCollected();
            }
        }

        private void OnCollected()
        {
            // Disable all renderers
            foreach (var r in GetComponentsInChildren<Renderer>())
                r.enabled = false;

            // Disable all colliders
            foreach (var c in GetComponentsInChildren<Collider>())
                c.enabled = false;

            // Disable lights
            foreach (var l in GetComponentsInChildren<Light>())
                l.enabled = false;

            Destroy(gameObject, 0.1f);
        }
    }
}
