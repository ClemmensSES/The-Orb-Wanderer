using UnityEngine;
using OrbWanderer.Data;
using OrbWanderer.Inventory;

namespace OrbWanderer.World
{
    /// <summary>
    /// Attached to orb instances in the world. Handles collection by the player.
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

            // Gentle bobbing animation
            float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);

            // Slow rotation
            transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
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
            // Play collection effect / sound here
            // For now, just destroy after a brief moment
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;

            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            Destroy(gameObject, 0.1f);
        }
    }
}
