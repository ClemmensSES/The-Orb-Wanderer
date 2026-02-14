using UnityEngine;

namespace OrbWanderer.World
{
    /// <summary>
    /// Generates pixel art sprites at runtime for the alien player,
    /// creatures, orbs, and other game entities.
    /// </summary>
    public static class AlienSpriteGenerator
    {
        // --- Player Alien ---
        public static Sprite CreateAlienPlayer()
        {
            int size = 32;
            var tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Point;
            ClearTexture(tex);

            Color body = new Color(0.3f, 0.85f, 0.6f);
            Color bodyDark = new Color(0.2f, 0.65f, 0.45f);
            Color eyes = new Color(0.95f, 0.95f, 0.3f);
            Color eyeGlow = new Color(1f, 1f, 0.5f);
            Color cloak = new Color(0.2f, 0.15f, 0.4f);
            Color cloakLight = new Color(0.35f, 0.25f, 0.55f);

            // Cloak / body (robe-like shape)
            for (int y = 2; y < 18; y++)
            {
                int halfWidth = y < 8 ? 5 + (8 - y) / 2 : 5;
                for (int x = 16 - halfWidth; x <= 16 + halfWidth; x++)
                {
                    Color c = (x + y) % 3 == 0 ? cloakLight : cloak;
                    tex.SetPixel(x, y, c);
                }
            }

            // Head (oval)
            for (int y = 18; y < 30; y++)
            {
                int headWidth = y < 20 ? (y - 17) * 2 : y > 27 ? (30 - y) * 2 : 6;
                for (int x = 16 - headWidth; x <= 16 + headWidth; x++)
                {
                    if (x < 0 || x >= size) continue;
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(16, 24));
                    Color c = dist > 4 ? bodyDark : body;
                    tex.SetPixel(x, y, c);
                }
            }

            // Big alien eyes
            DrawCircle(tex, 13, 24, 2, eyes);
            DrawCircle(tex, 19, 24, 2, eyes);
            tex.SetPixel(13, 24, eyeGlow);
            tex.SetPixel(19, 24, eyeGlow);

            // Antennae
            tex.SetPixel(12, 29, body);
            tex.SetPixel(11, 30, body);
            tex.SetPixel(11, 31, eyeGlow);
            tex.SetPixel(20, 29, body);
            tex.SetPixel(21, 30, body);
            tex.SetPixel(21, 31, eyeGlow);

            // Small mouth
            tex.SetPixel(15, 21, bodyDark);
            tex.SetPixel(16, 21, bodyDark);
            tex.SetPixel(17, 21, bodyDark);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.1f), 16);
        }

        // --- Creatures ---

        public static Sprite CreateJellyfishSprite(int level)
        {
            int size = 24;
            var tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Point;
            ClearTexture(tex);

            float glow = 0.5f + level * 0.1f;
            Color bodyColor = new Color(0.4f * glow, 0.6f * glow, 1f * glow, 0.85f);
            Color innerGlow = new Color(0.6f, 0.8f, 1f, 0.9f);

            // Bell (dome)
            for (int y = 10; y < 22; y++)
            {
                int w = y < 14 ? (y - 9) * 2 : y > 19 ? (22 - y) * 3 : 8;
                for (int x = 12 - w; x <= 12 + w; x++)
                {
                    if (x < 0 || x >= size) continue;
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(12, 16));
                    Color c = dist < 3 ? innerGlow : bodyColor;
                    tex.SetPixel(x, y, c);
                }
            }

            // Tentacles
            Color tentacle = bodyColor * 0.7f;
            tentacle.a = 0.6f;
            for (int t = 0; t < 5 + level; t++)
            {
                int tx = 6 + t * 2;
                if (tx >= size) break;
                for (int y = 2; y < 10; y++)
                {
                    int wobble = (int)(Mathf.Sin(y * 0.8f + t) * 1.5f);
                    int px = Mathf.Clamp(tx + wobble, 0, size - 1);
                    tex.SetPixel(px, y, tentacle);
                }
            }

            // Level indicator dots
            for (int i = 0; i < Mathf.Min(level, 5); i++)
            {
                tex.SetPixel(10 + i * 2, 16, Color.white);
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 12);
        }

        public static Sprite CreateWhaleSprite(int level)
        {
            int size = 32;
            var tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Point;
            ClearTexture(tex);

            Color body = new Color(0.25f, 0.35f, 0.6f);
            Color belly = new Color(0.5f, 0.6f, 0.75f);
            Color eye = Color.white;

            // Body (large oval)
            for (int y = 6; y < 26; y++)
            {
                int halfW = (int)(Mathf.Sqrt(1f - Mathf.Pow((y - 16f) / 10f, 2)) * 14);
                for (int x = 16 - halfW; x <= 16 + halfW; x++)
                {
                    if (x < 0 || x >= size) continue;
                    Color c = y < 14 ? belly : body;
                    tex.SetPixel(x, y, c);
                }
            }

            // Tail
            for (int x = 28; x < 32; x++)
            {
                int spread = (x - 27) * 2;
                for (int y = 16 - spread; y <= 16 + spread; y++)
                {
                    if (y >= 0 && y < size)
                        tex.SetPixel(x, y, body);
                }
            }

            // Eye
            tex.SetPixel(7, 18, eye);
            tex.SetPixel(8, 18, eye);
            tex.SetPixel(7, 19, new Color(0.1f, 0.1f, 0.3f));

            // Fins grow with level
            if (level >= 2)
            {
                for (int i = 0; i < 3 + level; i++)
                {
                    int fy = 6 - i;
                    if (fy >= 0) tex.SetPixel(14 + i, fy, body * 0.8f);
                    if (fy >= 0) tex.SetPixel(15 + i, fy, body * 0.8f);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 12);
        }

        public static Sprite CreateSpiderSprite(int level)
        {
            int size = 20;
            var tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Point;
            ClearTexture(tex);

            Color body = new Color(0.5f, 0.2f, 0.6f);
            Color legs = new Color(0.4f, 0.15f, 0.5f);
            Color eyes = new Color(1f, 0.3f, 0.3f);

            // Body (two circles)
            DrawCircle(tex, 10, 8, 4, body);   // Abdomen
            DrawCircle(tex, 10, 13, 3, body * 1.1f); // Head

            // Eyes (multiple, spider-like)
            tex.SetPixel(8, 14, eyes);
            tex.SetPixel(12, 14, eyes);
            if (level >= 2)
            {
                tex.SetPixel(9, 15, eyes * 0.7f);
                tex.SetPixel(11, 15, eyes * 0.7f);
            }

            // Legs (4 pairs)
            int legCount = 4 + Mathf.Min(level, 4);
            for (int i = 0; i < legCount; i++)
            {
                int ly = 8 + i - legCount / 2;
                for (int lx = 0; lx < 4 + level / 2; lx++)
                {
                    int leftX = 6 - lx;
                    int rightX = 14 + lx;
                    int legY = ly + (lx > 2 ? -1 : 0);
                    if (leftX >= 0 && legY >= 0 && legY < size) tex.SetPixel(leftX, legY, legs);
                    if (rightX < size && legY >= 0 && legY < size) tex.SetPixel(rightX, legY, legs);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 10);
        }

        public static Sprite CreateFirebirdSprite(int level)
        {
            int size = 24;
            var tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Point;
            ClearTexture(tex);

            Color body = new Color(0.9f, 0.4f, 0.1f);
            Color flame = new Color(1f, 0.7f, 0.2f);
            Color wing = new Color(0.8f, 0.3f, 0.05f);

            // Body
            DrawCircle(tex, 12, 12, 4, body);

            // Head
            DrawCircle(tex, 12, 18, 3, body * 1.1f);
            tex.SetPixel(10, 19, Color.white); // Eye
            tex.SetPixel(9, 18, flame); // Beak

            // Wings
            int wingSpan = 4 + level;
            for (int i = 0; i < wingSpan; i++)
            {
                int wy = 12 + i / 2;
                tex.SetPixel(6 - i, wy, wing);
                tex.SetPixel(18 + i, wy, wing);
                if (wy + 1 < size)
                {
                    tex.SetPixel(6 - i, wy + 1, wing * 0.8f);
                    tex.SetPixel(18 + i, wy + 1, wing * 0.8f);
                }
            }

            // Tail flame
            for (int i = 0; i < 3 + level; i++)
            {
                int fy = 7 - i;
                if (fy >= 0)
                {
                    tex.SetPixel(11 + (i % 2), fy, flame);
                    tex.SetPixel(13 - (i % 2), fy, flame * 0.8f);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 12);
        }

        public static Sprite CreateIceGolemSprite(int level)
        {
            int size = 28;
            var tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Point;
            ClearTexture(tex);

            Color ice = new Color(0.6f, 0.8f, 1f);
            Color iceLight = new Color(0.8f, 0.9f, 1f);
            Color iceDark = new Color(0.3f, 0.5f, 0.7f);
            Color eyes = new Color(0.2f, 0.6f, 1f);

            // Body (chunky)
            for (int y = 2; y < 18; y++)
            {
                int halfW = y < 5 ? 3 + y : y > 14 ? 3 + (18 - y) : 8;
                for (int x = 14 - halfW; x <= 14 + halfW; x++)
                {
                    if (x < 0 || x >= size) continue;
                    Color c = ((x + y) % 4 == 0) ? iceLight : ice;
                    tex.SetPixel(x, y, c);
                }
            }

            // Head
            for (int y = 18; y < 26; y++)
            {
                int halfW = y < 20 ? (y - 17) * 2 : y > 24 ? (26 - y) * 2 : 5;
                for (int x = 14 - halfW; x <= 14 + halfW; x++)
                {
                    if (x < 0 || x >= size) continue;
                    tex.SetPixel(x, y, iceLight);
                }
            }

            // Crystal crown (grows with level)
            for (int i = 0; i < 3 + level; i++)
            {
                int cx = 11 + i * 2;
                if (cx < size)
                {
                    for (int h = 0; h < 2 + level / 2; h++)
                    {
                        int cy = 26 + h;
                        if (cy < size) tex.SetPixel(cx, cy, iceLight);
                    }
                }
            }

            // Eyes
            tex.SetPixel(12, 22, eyes);
            tex.SetPixel(16, 22, eyes);

            // Arms
            for (int i = 0; i < 4 + level / 2; i++)
            {
                tex.SetPixel(6 - i, 12, iceDark);
                tex.SetPixel(22 + i, 12, iceDark);
                if (i > 0)
                {
                    tex.SetPixel(6 - i, 13, iceDark);
                    tex.SetPixel(22 + i, 13, iceDark);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.1f), 14);
        }

        // --- Orb Sprites ---

        public static Sprite CreateOrbSprite(Color glowColor, int size = 16)
        {
            var tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Point;
            ClearTexture(tex);

            float center = size / 2f;
            float radius = center - 2;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist <= radius)
                    {
                        float t = dist / radius;
                        Color c = Color.Lerp(Color.white * 0.9f, glowColor, t);
                        c.a = 1f;

                        // Highlight
                        float highlight = Vector2.Distance(new Vector2(x, y),
                            new Vector2(center - 2, center + 2));
                        if (highlight < radius * 0.3f)
                            c = Color.Lerp(c, Color.white, 0.4f);

                        tex.SetPixel(x, y, c);
                    }
                    else if (dist <= radius + 1.5f)
                    {
                        // Glow halo
                        Color glow = glowColor;
                        glow.a = 0.3f * (1f - (dist - radius) / 1.5f);
                        tex.SetPixel(x, y, glow);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        // --- Utility ---

        private static void ClearTexture(Texture2D tex)
        {
            var clear = new Color[tex.width * tex.height];
            for (int i = 0; i < clear.Length; i++)
                clear[i] = Color.clear;
            tex.SetPixels(clear);
        }

        private static void DrawCircle(Texture2D tex, int cx, int cy, int radius, Color color)
        {
            for (int y = cy - radius; y <= cy + radius; y++)
            {
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    if (x < 0 || x >= tex.width || y < 0 || y >= tex.height) continue;
                    if (Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) <= radius)
                        tex.SetPixel(x, y, color);
                }
            }
        }
    }
}
