using UnityEngine;

namespace OrbWanderer.World
{
    /// <summary>
    /// Renders animated ocean water between islands.
    /// Creates a large water plane with animated wave effect.
    /// </summary>
    public class OceanRenderer : MonoBehaviour
    {
        [Header("Ocean Settings")]
        [SerializeField] private float oceanSize = 200f;
        [SerializeField] private Color deepColor = new Color(0.05f, 0.1f, 0.25f);
        [SerializeField] private Color shallowColor = new Color(0.1f, 0.25f, 0.45f);
        [SerializeField] private Color foamColor = new Color(0.4f, 0.55f, 0.7f, 0.6f);

        [Header("Animation")]
        [SerializeField] private float waveSpeed = 0.5f;
        [SerializeField] private float waveScale = 0.05f;
        [SerializeField] private float sparkleInterval = 0.3f;

        private SpriteRenderer oceanSprite;
        private Transform[] sparkles;
        private float sparkleTimer;

        private void Start()
        {
            CreateOceanBase();
            CreateSparkles();
        }

        private void Update()
        {
            AnimateSparkles();
        }

        private void CreateOceanBase()
        {
            int texSize = 128;
            var tex = new Texture2D(texSize, texSize);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Repeat;

            for (int y = 0; y < texSize; y++)
            {
                for (int x = 0; x < texSize; x++)
                {
                    float noise1 = Mathf.PerlinNoise(x * waveScale, y * waveScale);
                    float noise2 = Mathf.PerlinNoise(x * waveScale * 2f + 100, y * waveScale * 2f + 100);
                    float combined = noise1 * 0.7f + noise2 * 0.3f;

                    Color c = Color.Lerp(deepColor, shallowColor, combined);
                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();

            oceanSprite = gameObject.GetComponent<SpriteRenderer>();
            if (oceanSprite == null)
                oceanSprite = gameObject.AddComponent<SpriteRenderer>();

            oceanSprite.sprite = Sprite.Create(tex, new Rect(0, 0, texSize, texSize),
                new Vector2(0.5f, 0.5f), texSize / oceanSize);
            oceanSprite.sortingOrder = -100;
            oceanSprite.drawMode = SpriteDrawMode.Tiled;
            oceanSprite.size = new Vector2(oceanSize, oceanSize);
        }

        private void CreateSparkles()
        {
            int count = 40;
            sparkles = new Transform[count];
            var sparkleParent = new GameObject("Sparkles");
            sparkleParent.transform.SetParent(transform, false);

            for (int i = 0; i < count; i++)
            {
                var sparkle = new GameObject("Sparkle");
                sparkle.transform.SetParent(sparkleParent.transform, false);
                sparkle.transform.localPosition = new Vector3(
                    Random.Range(-oceanSize / 2f, oceanSize / 2f),
                    Random.Range(-oceanSize / 2f, oceanSize / 2f), 0);

                var sr = sparkle.AddComponent<SpriteRenderer>();
                int size = 4;
                var tex = new Texture2D(size, size);
                tex.filterMode = FilterMode.Point;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                        tex.SetPixel(x, y, x > 0 && x < size - 1 && y > 0 && y < size - 1
                            ? foamColor : Color.clear);
                tex.Apply();
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, size, size),
                    new Vector2(0.5f, 0.5f), size);
                sr.sortingOrder = -99;
                sr.color = new Color(1, 1, 1, 0);

                sparkles[i] = sparkle.transform;
            }
        }

        private void AnimateSparkles()
        {
            if (sparkles == null) return;

            sparkleTimer += Time.deltaTime;

            for (int i = 0; i < sparkles.Length; i++)
            {
                if (sparkles[i] == null) continue;

                // Gentle bobbing
                var pos = sparkles[i].localPosition;
                pos.x += Mathf.Sin(Time.time * waveSpeed + i * 0.5f) * Time.deltaTime * 0.2f;
                pos.y += Mathf.Cos(Time.time * waveSpeed * 0.7f + i * 0.3f) * Time.deltaTime * 0.15f;
                sparkles[i].localPosition = pos;

                // Twinkle effect
                var sr = sparkles[i].GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    float alpha = Mathf.Sin(Time.time * 2f + i * 1.7f) * 0.5f + 0.5f;
                    sr.color = new Color(foamColor.r, foamColor.g, foamColor.b, alpha * 0.4f);
                }
            }
        }

        public void SetOceanSize(float size)
        {
            oceanSize = size;
        }
    }
}
