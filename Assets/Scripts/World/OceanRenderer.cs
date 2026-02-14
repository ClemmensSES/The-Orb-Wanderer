using UnityEngine;

namespace OrbWanderer.World
{
    /// <summary>
    /// Renders a large 3D ocean water plane between islands.
    /// Uses a semi-transparent animated material with sparkle effects.
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
        [SerializeField] private float sparkleInterval = 0.3f;

        private MeshRenderer oceanRenderer;
        private Transform[] sparkles;

        private void Start()
        {
            CreateOceanPlane();
            CreateSparkles();
        }

        private void Update()
        {
            AnimateSparkles();
            AnimateOcean();
        }

        private void CreateOceanPlane()
        {
            // Create a flat plane for the ocean
            var oceanObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
            oceanObj.name = "OceanPlane";
            oceanObj.transform.SetParent(transform, false);
            oceanObj.transform.localPosition = new Vector3(0, -0.3f, 0);
            oceanObj.transform.localScale = new Vector3(oceanSize / 10f, 1, oceanSize / 10f);

            // Semi-transparent water material
            var waterMat = new Material(Shader.Find("Standard"));
            Color waterColor = Color.Lerp(deepColor, shallowColor, 0.5f);
            waterMat.color = new Color(waterColor.r, waterColor.g, waterColor.b, 0.85f);
            waterMat.SetFloat("_Mode", 3); // Transparent
            waterMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            waterMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            waterMat.SetInt("_ZWrite", 0);
            waterMat.DisableKeyword("_ALPHATEST_ON");
            waterMat.EnableKeyword("_ALPHABLEND_ON");
            waterMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            waterMat.renderQueue = 3000;
            waterMat.SetFloat("_Metallic", 0.3f);
            waterMat.SetFloat("_Glossiness", 0.9f);
            waterMat.EnableKeyword("_EMISSION");
            waterMat.SetColor("_EmissionColor", deepColor * 0.2f);

            oceanRenderer = oceanObj.GetComponent<MeshRenderer>();
            oceanRenderer.material = waterMat;

            // Remove the default collider (ocean doesn't need physical collision)
            Object.Destroy(oceanObj.GetComponent<Collider>());
        }

        private void CreateSparkles()
        {
            int count = 30;
            sparkles = new Transform[count];
            var sparkleParent = new GameObject("Sparkles");
            sparkleParent.transform.SetParent(transform, false);

            var sparkleMat = AlienSpriteGenerator.CreateEmissiveMaterial(
                new Color(0.8f, 0.9f, 1f), 2f);

            for (int i = 0; i < count; i++)
            {
                var sparkle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sparkle.name = "Sparkle";
                sparkle.transform.SetParent(sparkleParent.transform, false);
                sparkle.transform.localPosition = new Vector3(
                    Random.Range(-oceanSize / 2f, oceanSize / 2f),
                    -0.15f,
                    Random.Range(-oceanSize / 2f, oceanSize / 2f));
                sparkle.transform.localScale = Vector3.one * Random.Range(0.1f, 0.3f);
                sparkle.GetComponent<Renderer>().material = sparkleMat;
                Object.Destroy(sparkle.GetComponent<Collider>());

                sparkles[i] = sparkle.transform;
            }
        }

        private void AnimateSparkles()
        {
            if (sparkles == null) return;

            for (int i = 0; i < sparkles.Length; i++)
            {
                if (sparkles[i] == null) continue;

                // Gentle bobbing on XZ plane
                var pos = sparkles[i].localPosition;
                pos.x += Mathf.Sin(Time.time * waveSpeed + i * 0.5f) * Time.deltaTime * 0.2f;
                pos.z += Mathf.Cos(Time.time * waveSpeed * 0.7f + i * 0.3f) * Time.deltaTime * 0.15f;
                sparkles[i].localPosition = pos;

                // Twinkle by scaling
                float alpha = Mathf.Sin(Time.time * 2f + i * 1.7f) * 0.5f + 0.5f;
                sparkles[i].localScale = Vector3.one * (0.1f + alpha * 0.2f);
            }
        }

        private void AnimateOcean()
        {
            if (oceanRenderer == null) return;

            // Subtle emission color shift for wave illusion
            float t = Mathf.Sin(Time.time * waveSpeed) * 0.5f + 0.5f;
            Color emission = Color.Lerp(deepColor * 0.15f, shallowColor * 0.25f, t);
            oceanRenderer.material.SetColor("_EmissionColor", emission);
        }

        public void SetOceanSize(float size)
        {
            oceanSize = size;
        }
    }
}
