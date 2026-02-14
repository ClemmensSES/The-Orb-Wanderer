using UnityEngine;

namespace OrbWanderer.World
{
    /// <summary>
    /// Generates procedural 3D meshes at runtime for the player,
    /// creatures, orbs, and other game entities.
    /// Replaces the old 2D sprite generator with real 3D geometry.
    /// </summary>
    public static class AlienSpriteGenerator
    {
        // --- Player ---
        public static void ApplyAlienPlayerModel(GameObject player)
        {
            // Body capsule
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(player.transform, false);
            body.transform.localPosition = new Vector3(0, 0.75f, 0);
            body.transform.localScale = new Vector3(0.6f, 0.75f, 0.4f);
            var bodyMat = CreateMaterial(new Color(0.3f, 0.85f, 0.6f));
            body.GetComponent<Renderer>().material = bodyMat;
            Object.Destroy(body.GetComponent<Collider>());

            // Head sphere
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(player.transform, false);
            head.transform.localPosition = new Vector3(0, 1.8f, 0);
            head.transform.localScale = new Vector3(0.55f, 0.65f, 0.5f);
            head.GetComponent<Renderer>().material = bodyMat;
            Object.Destroy(head.GetComponent<Collider>());

            // Eyes (emissive yellow)
            var eyeMat = CreateEmissiveMaterial(new Color(0.95f, 0.95f, 0.3f), 2f);
            CreateEye(player.transform, new Vector3(-0.12f, 1.85f, 0.2f), eyeMat);
            CreateEye(player.transform, new Vector3(0.12f, 1.85f, 0.2f), eyeMat);

            // Antennae
            var antennaMat = CreateMaterial(new Color(0.25f, 0.7f, 0.5f));
            CreateAntenna(player.transform, new Vector3(-0.15f, 2.1f, 0), antennaMat);
            CreateAntenna(player.transform, new Vector3(0.15f, 2.1f, 0), antennaMat);

            // Cloak (wider capsule at bottom)
            var cloak = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            cloak.name = "Cloak";
            cloak.transform.SetParent(player.transform, false);
            cloak.transform.localPosition = new Vector3(0, 0.3f, 0);
            cloak.transform.localScale = new Vector3(0.8f, 0.35f, 0.6f);
            var cloakMat = CreateMaterial(new Color(0.2f, 0.15f, 0.4f));
            cloak.GetComponent<Renderer>().material = cloakMat;
            Object.Destroy(cloak.GetComponent<Collider>());
        }

        private static void CreateEye(Transform parent, Vector3 pos, Material mat)
        {
            var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = "Eye";
            eye.transform.SetParent(parent, false);
            eye.transform.localPosition = pos;
            eye.transform.localScale = Vector3.one * 0.12f;
            eye.GetComponent<Renderer>().material = mat;
            Object.Destroy(eye.GetComponent<Collider>());
        }

        private static void CreateAntenna(Transform parent, Vector3 basePos, Material mat)
        {
            var stalk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stalk.name = "Antenna";
            stalk.transform.SetParent(parent, false);
            stalk.transform.localPosition = basePos + Vector3.up * 0.15f;
            stalk.transform.localScale = new Vector3(0.03f, 0.15f, 0.03f);
            stalk.GetComponent<Renderer>().material = mat;
            Object.Destroy(stalk.GetComponent<Collider>());

            var tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tip.name = "AntennaTip";
            tip.transform.SetParent(parent, false);
            tip.transform.localPosition = basePos + Vector3.up * 0.35f;
            tip.transform.localScale = Vector3.one * 0.06f;
            var tipMat = CreateEmissiveMaterial(new Color(1f, 1f, 0.5f), 3f);
            tip.GetComponent<Renderer>().material = tipMat;
            Object.Destroy(tip.GetComponent<Collider>());
        }

        // --- Creatures ---

        public static void ApplyCreatureModel(GameObject obj, CreatureType type, int level)
        {
            switch (type)
            {
                case CreatureType.Jellyfish: BuildJellyfish(obj, level); break;
                case CreatureType.Whale: BuildWhale(obj, level); break;
                case CreatureType.Spider: BuildSpider(obj, level); break;
                case CreatureType.Firebird: BuildFirebird(obj, level); break;
                case CreatureType.IceGolem: BuildIceGolem(obj, level); break;
            }
        }

        private static void BuildJellyfish(GameObject obj, int level)
        {
            float glow = 0.5f + level * 0.1f;
            var bodyMat = CreateEmissiveMaterial(new Color(0.4f * glow, 0.6f * glow, 1f * glow), 1.5f);

            // Bell dome
            var bell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bell.name = "Bell";
            bell.transform.SetParent(obj.transform, false);
            bell.transform.localPosition = new Vector3(0, 0.8f, 0);
            bell.transform.localScale = new Vector3(1f, 0.6f, 1f);
            bell.GetComponent<Renderer>().material = bodyMat;
            Object.Destroy(bell.GetComponent<Collider>());

            // Tentacles
            var tentMat = CreateEmissiveMaterial(new Color(0.3f * glow, 0.5f * glow, 0.9f * glow), 0.8f);
            int tentCount = 5 + level;
            for (int i = 0; i < tentCount; i++)
            {
                float angle = (i / (float)tentCount) * Mathf.PI * 2f;
                float radius = 0.3f;
                var tent = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tent.name = "Tentacle";
                tent.transform.SetParent(obj.transform, false);
                tent.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * radius, 0.1f, Mathf.Sin(angle) * radius);
                tent.transform.localScale = new Vector3(0.05f, 0.35f, 0.05f);
                tent.GetComponent<Renderer>().material = tentMat;
                Object.Destroy(tent.GetComponent<Collider>());
            }
        }

        private static void BuildWhale(GameObject obj, int level)
        {
            var bodyMat = CreateMaterial(new Color(0.25f, 0.35f, 0.6f));
            var bellyMat = CreateMaterial(new Color(0.5f, 0.6f, 0.75f));

            // Main body
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(obj.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(1f, 0.6f, 2f);
            body.transform.localRotation = Quaternion.Euler(0, 0, 90);
            body.GetComponent<Renderer>().material = bodyMat;
            Object.Destroy(body.GetComponent<Collider>());

            // Belly
            var belly = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            belly.name = "Belly";
            belly.transform.SetParent(obj.transform, false);
            belly.transform.localPosition = new Vector3(0, -0.15f, 0);
            belly.transform.localScale = new Vector3(0.7f, 0.4f, 1.6f);
            belly.transform.localRotation = Quaternion.Euler(0, 0, 90);
            belly.GetComponent<Renderer>().material = bellyMat;
            Object.Destroy(belly.GetComponent<Collider>());

            // Tail fins
            var tail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tail.name = "Tail";
            tail.transform.SetParent(obj.transform, false);
            tail.transform.localPosition = new Vector3(0, 0, -1.3f);
            tail.transform.localScale = new Vector3(1.2f, 0.08f, 0.4f);
            tail.GetComponent<Renderer>().material = bodyMat;
            Object.Destroy(tail.GetComponent<Collider>());

            // Eye
            var eyeMat = CreateMaterial(Color.white);
            var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = "Eye";
            eye.transform.SetParent(obj.transform, false);
            eye.transform.localPosition = new Vector3(0.4f, 0.1f, 0.7f);
            eye.transform.localScale = Vector3.one * 0.12f;
            eye.GetComponent<Renderer>().material = eyeMat;
            Object.Destroy(eye.GetComponent<Collider>());
        }

        private static void BuildSpider(GameObject obj, int level)
        {
            var bodyMat = CreateMaterial(new Color(0.5f, 0.2f, 0.6f));
            var legMat = CreateMaterial(new Color(0.4f, 0.15f, 0.5f));
            var eyeMat = CreateEmissiveMaterial(new Color(1f, 0.3f, 0.3f), 2f);

            // Abdomen
            var abdomen = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            abdomen.name = "Abdomen";
            abdomen.transform.SetParent(obj.transform, false);
            abdomen.transform.localPosition = new Vector3(0, 0.3f, -0.3f);
            abdomen.transform.localScale = new Vector3(0.6f, 0.5f, 0.7f);
            abdomen.GetComponent<Renderer>().material = bodyMat;
            Object.Destroy(abdomen.GetComponent<Collider>());

            // Head
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(obj.transform, false);
            head.transform.localPosition = new Vector3(0, 0.35f, 0.3f);
            head.transform.localScale = Vector3.one * 0.4f;
            head.GetComponent<Renderer>().material = bodyMat;
            Object.Destroy(head.GetComponent<Collider>());

            // Eyes
            CreateEye(obj.transform, new Vector3(-0.1f, 0.42f, 0.45f), eyeMat);
            CreateEye(obj.transform, new Vector3(0.1f, 0.42f, 0.45f), eyeMat);

            // Legs
            int legPairs = 4 + Mathf.Min(level, 2);
            for (int i = 0; i < legPairs; i++)
            {
                float z = -0.3f + i * 0.15f;
                CreateSpiderLeg(obj.transform, new Vector3(-0.4f, 0.15f, z), true, legMat);
                CreateSpiderLeg(obj.transform, new Vector3(0.4f, 0.15f, z), false, legMat);
            }
        }

        private static void CreateSpiderLeg(Transform parent, Vector3 pos, bool left, Material mat)
        {
            var leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leg.name = "Leg";
            leg.transform.SetParent(parent, false);
            leg.transform.localPosition = pos;
            leg.transform.localScale = new Vector3(0.04f, 0.2f, 0.04f);
            leg.transform.localRotation = Quaternion.Euler(0, 0, left ? 45 : -45);
            leg.GetComponent<Renderer>().material = mat;
            Object.Destroy(leg.GetComponent<Collider>());
        }

        private static void BuildFirebird(GameObject obj, int level)
        {
            var bodyMat = CreateEmissiveMaterial(new Color(0.9f, 0.4f, 0.1f), 1f);
            var wingMat = CreateEmissiveMaterial(new Color(0.8f, 0.3f, 0.05f), 0.8f);
            var flameMat = CreateEmissiveMaterial(new Color(1f, 0.7f, 0.2f), 3f);

            // Body
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Body";
            body.transform.SetParent(obj.transform, false);
            body.transform.localPosition = new Vector3(0, 0.4f, 0);
            body.transform.localScale = new Vector3(0.5f, 0.4f, 0.6f);
            body.GetComponent<Renderer>().material = bodyMat;
            Object.Destroy(body.GetComponent<Collider>());

            // Head
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(obj.transform, false);
            head.transform.localPosition = new Vector3(0, 0.65f, 0.25f);
            head.transform.localScale = Vector3.one * 0.3f;
            head.GetComponent<Renderer>().material = bodyMat;
            Object.Destroy(head.GetComponent<Collider>());

            // Beak
            var beak = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beak.name = "Beak";
            beak.transform.SetParent(obj.transform, false);
            beak.transform.localPosition = new Vector3(0, 0.6f, 0.45f);
            beak.transform.localScale = new Vector3(0.06f, 0.04f, 0.12f);
            beak.GetComponent<Renderer>().material = flameMat;
            Object.Destroy(beak.GetComponent<Collider>());

            // Wings
            float wingSpan = 0.5f + level * 0.1f;
            CreateWing(obj.transform, new Vector3(-wingSpan, 0.5f, 0), wingMat, true);
            CreateWing(obj.transform, new Vector3(wingSpan, 0.5f, 0), wingMat, false);

            // Tail flame
            for (int i = 0; i < 2 + level; i++)
            {
                var flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flame.name = "TailFlame";
                flame.transform.SetParent(obj.transform, false);
                flame.transform.localPosition = new Vector3(
                    Random.Range(-0.08f, 0.08f), 0.35f - i * 0.1f, -0.35f - i * 0.1f);
                flame.transform.localScale = Vector3.one * (0.12f - i * 0.02f);
                flame.GetComponent<Renderer>().material = flameMat;
                Object.Destroy(flame.GetComponent<Collider>());
            }
        }

        private static void CreateWing(Transform parent, Vector3 pos, Material mat, bool left)
        {
            var wing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wing.name = "Wing";
            wing.transform.SetParent(parent, false);
            wing.transform.localPosition = pos;
            wing.transform.localScale = new Vector3(0.5f, 0.04f, 0.3f);
            wing.transform.localRotation = Quaternion.Euler(0, 0, left ? 15 : -15);
            wing.GetComponent<Renderer>().material = mat;
            Object.Destroy(wing.GetComponent<Collider>());
        }

        private static void BuildIceGolem(GameObject obj, int level)
        {
            var iceMat = CreateEmissiveMaterial(new Color(0.6f, 0.8f, 1f), 0.5f);
            var iceLightMat = CreateEmissiveMaterial(new Color(0.8f, 0.9f, 1f), 1f);
            var eyeMat = CreateEmissiveMaterial(new Color(0.2f, 0.6f, 1f), 3f);

            // Body
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(obj.transform, false);
            body.transform.localPosition = new Vector3(0, 0.6f, 0);
            body.transform.localScale = new Vector3(0.8f, 1f, 0.6f);
            body.GetComponent<Renderer>().material = iceMat;
            Object.Destroy(body.GetComponent<Collider>());

            // Head
            var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "Head";
            head.transform.SetParent(obj.transform, false);
            head.transform.localPosition = new Vector3(0, 1.4f, 0);
            head.transform.localScale = new Vector3(0.55f, 0.5f, 0.5f);
            head.GetComponent<Renderer>().material = iceLightMat;
            Object.Destroy(head.GetComponent<Collider>());

            // Eyes
            CreateEye(obj.transform, new Vector3(-0.15f, 1.45f, 0.26f), eyeMat);
            CreateEye(obj.transform, new Vector3(0.15f, 1.45f, 0.26f), eyeMat);

            // Crystal crown
            for (int i = 0; i < 3 + level; i++)
            {
                var crystal = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crystal.name = "Crown";
                crystal.transform.SetParent(obj.transform, false);
                float xOff = -0.2f + i * 0.15f;
                crystal.transform.localPosition = new Vector3(xOff, 1.8f + Random.Range(0f, 0.1f), 0);
                crystal.transform.localScale = new Vector3(0.08f, 0.2f + level * 0.05f, 0.08f);
                crystal.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-10f, 10f));
                crystal.GetComponent<Renderer>().material = iceLightMat;
                Object.Destroy(crystal.GetComponent<Collider>());
            }

            // Arms
            float armLen = 0.4f + level * 0.05f;
            var leftArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftArm.name = "LeftArm";
            leftArm.transform.SetParent(obj.transform, false);
            leftArm.transform.localPosition = new Vector3(-0.6f, 0.8f, 0);
            leftArm.transform.localScale = new Vector3(armLen, 0.15f, 0.15f);
            leftArm.GetComponent<Renderer>().material = iceMat;
            Object.Destroy(leftArm.GetComponent<Collider>());

            var rightArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightArm.name = "RightArm";
            rightArm.transform.SetParent(obj.transform, false);
            rightArm.transform.localPosition = new Vector3(0.6f, 0.8f, 0);
            rightArm.transform.localScale = new Vector3(armLen, 0.15f, 0.15f);
            rightArm.GetComponent<Renderer>().material = iceMat;
            Object.Destroy(rightArm.GetComponent<Collider>());
        }

        // --- Orb (shiny 3D sphere with glow) ---

        public static void ApplyOrbModel(GameObject obj, Color glowColor)
        {
            // Main orb sphere
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "OrbMesh";
            sphere.transform.SetParent(obj.transform, false);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale = Vector3.one * 0.5f;
            var orbMat = CreateShinyOrbMaterial(glowColor);
            sphere.GetComponent<Renderer>().material = orbMat;
            Object.Destroy(sphere.GetComponent<Collider>());

            // Inner glow sphere (slightly smaller, strong emission)
            var innerGlow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            innerGlow.name = "InnerGlow";
            innerGlow.transform.SetParent(obj.transform, false);
            innerGlow.transform.localPosition = Vector3.zero;
            innerGlow.transform.localScale = Vector3.one * 0.35f;
            var innerMat = CreateEmissiveMaterial(Color.Lerp(glowColor, Color.white, 0.5f), 4f);
            innerGlow.GetComponent<Renderer>().material = innerMat;
            Object.Destroy(innerGlow.GetComponent<Collider>());

            // Point light for real glow effect
            var lightObj = new GameObject("OrbLight");
            lightObj.transform.SetParent(obj.transform, false);
            lightObj.transform.localPosition = Vector3.zero;
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = glowColor;
            light.intensity = 1.5f;
            light.range = 3f;
        }

        // --- Material Helpers ---

        public static Material CreateMaterial(Color color)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            return mat;
        }

        public static Material CreateEmissiveMaterial(Color color, float intensity)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * intensity);
            return mat;
        }

        public static Material CreateShinyOrbMaterial(Color color)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            mat.SetFloat("_Metallic", 0.8f);
            mat.SetFloat("_Glossiness", 0.95f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 2f);
            return mat;
        }

        // Keep legacy method signatures that return Sprite for any code
        // that hasn't been updated yet — they now return null.
        public static Sprite CreateAlienPlayer() => null;
        public static Sprite CreateOrbSprite(Color glowColor, int size = 16) => null;
        public static Sprite CreateJellyfishSprite(int level) => null;
        public static Sprite CreateWhaleSprite(int level) => null;
        public static Sprite CreateSpiderSprite(int level) => null;
        public static Sprite CreateFirebirdSprite(int level) => null;
        public static Sprite CreateIceGolemSprite(int level) => null;
    }

    public enum CreatureType
    {
        Jellyfish,
        Whale,
        Spider,
        Firebird,
        IceGolem
    }
}
