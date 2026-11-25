using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generador y caché de sprites pixel-art procedurales (Texture2D + Sprite.Create)
/// para asegurar que ningún enemigo, jefe, plataforma, trampa, proyectil ni portal
/// de los Niveles 1, 2 y 3 aparezca jamás vacío o invisible.
/// </summary>
public static class ProceduralPixelArt
{
    private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    public static Sprite GetOrCreate(string key, int width, int height, float ppu, Vector2 pivot, System.Func<int, int, Color> pixelFunc)
    {
        if (spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                pixels[y * width + x] = pixelFunc(x, y);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, width, height), pivot, ppu);
        sprite.name = key;
        spriteCache[key] = sprite;
        return sprite;
    }

    // ─── PLATAFORMA DE TALAVERA QUEBRADIZA (NIVEL 2) ───────────────────────────
    public static Sprite GetTalaveraPlatformSprite(bool cracked = false)
    {
        string key = cracked ? "TalaveraPlat_Cracked" : "TalaveraPlat_Intact";
        Color colWhite = new Color(0.96f, 0.96f, 0.98f, 1f);
        Color colBlue  = new Color(0.11f, 0.30f, 0.62f, 1f);
        Color colGold  = new Color(0.95f, 0.72f, 0.22f, 1f);
        Color colDark  = new Color(0.08f, 0.14f, 0.28f, 1f);
        Color colCrack = new Color(0.12f, 0.06f, 0.08f, 1f);

        return GetOrCreate(key, 48, 16, 16f, new Vector2(0.5f, 0.5f), (x, y) =>
        {
            if (x == 0 || x == 47 || y == 0 || y == 15) return colDark;
            if (y == 14 || y == 1) return colBlue;
            if (y == 13 || y == 2) return colGold;

            // Grietas visibles al vibrar
            if (cracked)
            {
                if (Mathf.Abs(x - 14 - (y / 2)) <= 0 && y > 2 && y < 14) return colCrack;
                if (Mathf.Abs(x - 32 + (y / 2)) <= 0 && y > 1 && y < 14) return colCrack;
                if (y == 8 && x > 18 && x < 29) return colCrack;
            }

            // Patrón geométrico floral estilo Talavera de Dolores Hidalgo
            int mx = x % 8;
            int my = y % 8;
            if ((mx == 4 && my == 4) || (Mathf.Abs(mx - 4) + Mathf.Abs(my - 4) == 2))
                return colBlue;
            if (mx == 0 || my == 0)
                return new Color(0.82f, 0.88f, 0.96f, 1f);

            return colWhite;
        });
    }

    // ─── JARRÓN EXPLOSIVO DE CERÁMICA (NIVEL 2) ────────────────────────────────
    public static Sprite GetExplosivePotSprite(bool primed = false)
    {
        string key = primed ? "ExplosivePot_Primed" : "ExplosivePot_Idle";
        Color clay     = primed ? new Color(0.98f, 0.32f, 0.15f, 1f) : new Color(0.76f, 0.34f, 0.18f, 1f);
        Color pattern  = primed ? new Color(1.00f, 0.92f, 0.35f, 1f) : new Color(0.14f, 0.35f, 0.68f, 1f);
        Color outline  = new Color(0.16f, 0.08f, 0.06f, 1f);
        Color fuse     = primed ? new Color(1f, 0.95f, 0.2f, 1f) : new Color(0.85f, 0.65f, 0.30f, 1f);

        return GetOrCreate(key, 24, 28, 24f, new Vector2(0.5f, 0f), (x, y) =>
        {
            float cx = x - 11.5f;

            // Mecha superior
            if (y >= 22 && y <= 26 && Mathf.Abs(cx - (y - 22) * 0.4f) < 1.1f)
                return fuse;

            // Boca del jarrón
            if (y >= 19 && y <= 21)
            {
                if (Mathf.Abs(cx) <= 5.2f) return Mathf.Abs(cx) > 4.1f ? outline : pattern;
                return Color.clear;
            }

            // Cuello
            if (y >= 16 && y < 19)
            {
                if (Mathf.Abs(cx) <= 3.6f) return Mathf.Abs(cx) > 2.6f ? outline : clay;
                return Color.clear;
            }

            // Cuerpo redondeado
            if (y >= 1 && y < 16)
            {
                float ny = (y - 8.5f) / 7.5f;
                float halfW = Mathf.Sqrt(Mathf.Max(0f, 1f - ny * ny * 0.72f)) * 9.5f;
                if (Mathf.Abs(cx) <= halfW)
                {
                    if (Mathf.Abs(cx) > halfW - 1.2f || y == 1) return outline;
                    if (y == 8 || y == 9 || y == 10)
                        return ((x + y) % 2 == 0) ? pattern : Color.white;
                    return clay;
                }
            }
            return Color.clear;
        });
    }

    // ─── PROYECTILES TEMÁTICOS (NIVEL 2 Y NIVEL 3) ─────────────────────────────
    public static Sprite GetProjectileSprite(string variant)
    {
        return GetOrCreate("Proj_" + variant, 16, 16, 20f, new Vector2(0.5f, 0.5f), (x, y) =>
        {
            float dx = x - 7.5f;
            float dy = y - 7.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);

            switch (variant)
            {
                case "TalaveraShard":
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) <= 6.5f)
                    {
                        if (Mathf.Abs(dx) + Mathf.Abs(dy) >= 5.3f) return new Color(0.08f, 0.15f, 0.35f, 1f);
                        return (x + y) % 3 == 0 ? new Color(0.15f, 0.38f, 0.78f, 1f) : Color.white;
                    }
                    return Color.clear;

                case "MayorPamphlet":
                    if (x >= 2 && x <= 13 && y >= 3 && y <= 12)
                    {
                        if (x == 2 || x == 13 || y == 3 || y == 12) return new Color(0.15f, 0.12f, 0.18f, 1f);
                        if (y == 9 || y == 6) return new Color(0.82f, 0.18f, 0.22f, 1f);
                        return new Color(0.98f, 0.95f, 0.85f, 1f);
                    }
                    return Color.clear;

                case "GoldenEnvelope":
                    if (x >= 1 && x <= 14 && y >= 3 && y <= 12)
                    {
                        if (x == 1 || x == 14 || y == 3 || y == 12) return new Color(0.45f, 0.28f, 0.05f, 1f);
                        if (y >= 8 && Mathf.Abs(Mathf.Abs(dx) - (12 - y)) < 1.1f) return new Color(0.70f, 0.45f, 0.08f, 1f);
                        return new Color(0.98f, 0.80f, 0.22f, 1f);
                    }
                    return Color.clear;

                case "MegaphoneWave":
                    if (r >= 4.2f && r <= 7.2f && dx >= -1f)
                        return new Color(1f, 0.85f, 0.20f, 0.95f);
                    if (r >= 1.5f && r <= 3.2f && dx >= 0f)
                        return new Color(1f, 0.40f, 0.15f, 0.95f);
                    return Color.clear;

                case "ViralEmojiLike":
                    // Corazón / Like viral neón magenta
                    float nx = dx / 6.2f;
                    float ny = (dy - 0.5f) / 5.8f;
                    float eq = Mathf.Pow(nx * nx + ny * ny - 1f, 3f) - nx * nx * ny * ny * ny;
                    if (eq <= 0.05f)
                        return eq > -0.25f ? new Color(1f, 0.85f, 0.95f, 1f) : new Color(1f, 0.14f, 0.45f, 1f);
                    return Color.clear;

                case "FlashBeam":
                    if (y >= 3 && y <= 12)
                    {
                        if (y >= 6 && y <= 9) return new Color(1f, 1f, 0.95f, 1f);
                        return new Color(0.25f, 0.92f, 1f, 0.90f);
                    }
                    return Color.clear;

                default: // DroneShot
                    if (r <= 6.2f)
                    {
                        if (r > 4.8f) return new Color(0.08f, 0.90f, 1f, 1f);
                        return new Color(0.95f, 0.22f, 0.88f, 1f);
                    }
                    return Color.clear;
            }
        });
    }

    // ─── LIENZO ORGÁNICO CURVO PARA PIXEL-ART CARTÓN NO CUADRADO ──────────────
    private sealed class PixelCanvas
    {
        public readonly int width;
        public readonly int height;
        private readonly Color[] pixels;

        public PixelCanvas(int width, int height)
        {
            this.width = width;
            this.height = height;
            pixels = new Color[width * height];
        }

        public void SetPixel(int x, int y, Color col)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
                pixels[y * width + x] = col;
        }

        public Color GetPixel(int x, int y)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
                return pixels[y * width + x];
            return Color.clear;
        }

        public void BlendPixel(int x, int y, Color col)
        {
            if (x < 0 || x >= width || y < 0 || y >= height || col.a <= 0.001f)
                return;

            int idx = y * width + x;
            Color dst = pixels[idx];
            if (dst.a <= 0.001f || col.a >= 0.995f)
            {
                pixels[idx] = col;
                return;
            }

            float outA = col.a + dst.a * (1f - col.a);
            if (outA <= 0.001f)
                return;

            float r = (col.r * col.a + dst.r * dst.a * (1f - col.a)) / outA;
            float g = (col.g * col.a + dst.g * dst.a * (1f - col.a)) / outA;
            float b = (col.b * col.a + dst.b * dst.a * (1f - col.a)) / outA;
            pixels[idx] = new Color(r, g, b, outA);
        }

        public void FillEllipse(float cx, float cy, float rx, float ry, Color col)
        {
            if (rx <= 0.05f || ry <= 0.05f) return;
            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - rx - 1f));
            int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(cx + rx + 1f));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - ry - 1f));
            int maxY = Mathf.Min(height - 1, Mathf.CeilToInt(cy + ry + 1f));

            for (int y = minY; y <= maxY; y++)
            {
                float dy = (y - cy) / ry;
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = (x - cx) / rx;
                    if (dx * dx + dy * dy <= 1f)
                        BlendPixel(x, y, col);
                }
            }
        }

        public void FillShadedEllipse(float cx, float cy, float rx, float ry, Color baseCol, Color lightCol, Color darkCol)
        {
            if (rx <= 0.05f || ry <= 0.05f) return;
            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - rx - 1f));
            int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(cx + rx + 1f));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - ry - 1f));
            int maxY = Mathf.Min(height - 1, Mathf.CeilToInt(cy + ry + 1f));

            for (int y = minY; y <= maxY; y++)
            {
                float dy = (y - cy) / ry;
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = (x - cx) / rx;
                    float d2 = dx * dx + dy * dy;
                    if (d2 <= 1f)
                    {
                        float keyLight = (-dx * 0.55f + dy * 0.72f);
                        Color chosen = baseCol;
                        if (keyLight > 0.36f && d2 < 0.84f)
                            chosen = lightCol;
                        else if (keyLight < -0.30f || (d2 > 0.78f && dy < 0.25f))
                            chosen = darkCol;
                        BlendPixel(x, y, chosen);
                    }
                }
            }
        }

        public void FillRing(float cx, float cy, float rInner, float rOuter, Color col)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - rOuter - 1f));
            int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(cx + rOuter + 1f));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - rOuter - 1f));
            int maxY = Mathf.Min(height - 1, Mathf.CeilToInt(cy + rOuter + 1f));
            float rIn2 = rInner * rInner;
            float rOut2 = rOuter * rOuter;

            for (int y = minY; y <= maxY; y++)
            {
                float dy = y - cy;
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - cx;
                    float d2 = dx * dx + dy * dy;
                    if (d2 >= rIn2 && d2 <= rOut2)
                        BlendPixel(x, y, col);
                }
            }
        }

        public void FillCapsule(float x0, float y0, float x1, float y1, float radius, Color col)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(x0, x1) - radius - 1f));
            int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(Mathf.Max(x0, x1) + radius + 1f));
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(y0, y1) - radius - 1f));
            int maxY = Mathf.Min(height - 1, Mathf.CeilToInt(Mathf.Max(y0, y1) + radius + 1f));

            float vx = x1 - x0;
            float vy = y1 - y0;
            float lenSq = vx * vx + vy * vy;
            float r2 = radius * radius;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float t = lenSq > 0.0001f ? Mathf.Clamp01(((x - x0) * vx + (y - y0) * vy) / lenSq) : 0f;
                    float px = x0 + vx * t;
                    float py = y0 + vy * t;
                    float dx = x - px;
                    float dy = y - py;
                    if (dx * dx + dy * dy <= r2)
                        BlendPixel(x, y, col);
                }
            }
        }

        public void FillQuadCurve(float x0, float y0, float cx, float cy, float x1, float y1, float radius, Color col)
        {
            const int segments = 16;
            float prevX = x0;
            float prevY = y0;
            for (int s = 1; s <= segments; s++)
            {
                float t = s / (float)segments;
                float u = 1f - t;
                float nx = u * u * x0 + 2f * u * t * cx + t * t * x1;
                float ny = u * u * y0 + 2f * u * t * cy + t * t * y1;
                FillCapsule(prevX, prevY, nx, ny, radius, col);
                prevX = nx;
                prevY = ny;
            }
        }

        public void FillRoundedRect(float x0, float y0, float x1, float y1, float radius, Color col)
        {
            float left = Mathf.Min(x0, x1);
            float right = Mathf.Max(x0, x1);
            float bottom = Mathf.Min(y0, y1);
            float top = Mathf.Max(y0, y1);
            float r = Mathf.Clamp(radius, 0f, Mathf.Min(right - left, top - bottom) * 0.5f);

            int minX = Mathf.Max(0, Mathf.FloorToInt(left));
            int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(right));
            int minY = Mathf.Max(0, Mathf.FloorToInt(bottom));
            int maxY = Mathf.Min(height - 1, Mathf.CeilToInt(top));

            float innerL = left + r;
            float innerR = right - r;
            float innerB = bottom + r;
            float innerT = top - r;
            float r2 = r * r;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float cx = Mathf.Clamp(x, innerL, innerR);
                    float cy = Mathf.Clamp(y, innerB, innerT);
                    float dx = x - cx;
                    float dy = y - cy;
                    if (dx * dx + dy * dy <= r2)
                        BlendPixel(x, y, col);
                }
            }
        }

        public void ApplyOutline(Color outlineCol)
        {
            bool[] solid = new bool[width * height];
            for (int i = 0; i < pixels.Length; i++)
                solid[i] = pixels[i].a > 0.45f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = y * width + x;
                    if (solid[idx])
                        continue;

                    bool touchesSolid =
                        (x > 0 && solid[idx - 1]) ||
                        (x < width - 1 && solid[idx + 1]) ||
                        (y > 0 && solid[idx - width]) ||
                        (y < height - 1 && solid[idx + width]);

                    if (touchesSolid)
                        pixels[idx] = outlineCol;
                }
            }
        }

        public Sprite ToSprite(string key, float ppu, Vector2 pivot)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            tex.SetPixels(pixels);
            tex.Apply();

            Sprite sp = Sprite.Create(tex, new Rect(0, 0, width, height), pivot, ppu);
            sp.name = key;
            spriteCache[key] = sp;
            return sp;
        }
    }

    // ─── JEFE 2: EL MUNÍCIPE CARISMÁTICO (PIXEL ART ORGÁNICO 64x80) ─────────────
    public static Sprite GetMayorBossSprite(int frame)
    {
        // frame: 0=Idle/WalkA, 1=WalkB, 2=Megaphone/Attack, 3=PodiumShield, 4=Defeat
        int f = Mathf.Clamp(frame, 0, 4);
        string key = $"MayorBoss_HD_F{f}";
        if (spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        PixelCanvas c = new PixelCanvas(64, 80);
        Color outline  = new Color(0.09f, 0.07f, 0.13f, 1f);
        Color suit     = new Color(0.14f, 0.23f, 0.46f, 1f);
        Color suitLit  = new Color(0.22f, 0.34f, 0.62f, 1f);
        Color suitDark = new Color(0.09f, 0.14f, 0.30f, 1f);
        Color shirt    = new Color(0.97f, 0.97f, 0.99f, 1f);
        Color tie      = new Color(0.86f, 0.14f, 0.20f, 1f);
        Color tieLit   = new Color(1.00f, 0.32f, 0.36f, 1f);
        Color sashG    = new Color(0.10f, 0.58f, 0.26f, 1f);
        Color sashR    = new Color(0.86f, 0.18f, 0.22f, 1f);
        Color skin     = new Color(0.94f, 0.76f, 0.60f, 1f);
        Color skinLit  = new Color(0.99f, 0.85f, 0.70f, 1f);
        Color skinDark = new Color(0.80f, 0.60f, 0.46f, 1f);
        Color blush    = new Color(0.96f, 0.56f, 0.52f, 0.85f);
        Color hair     = new Color(0.20f, 0.16f, 0.19f, 1f);
        Color hairLit  = new Color(0.42f, 0.38f, 0.44f, 1f);
        Color hairSilv = new Color(0.76f, 0.80f, 0.88f, 1f);
        Color gold     = new Color(0.96f, 0.78f, 0.24f, 1f);
        Color goldLit  = new Color(1.00f, 0.94f, 0.54f, 1f);
        Color goldDark = new Color(0.70f, 0.50f, 0.12f, 1f);
        Color shoeDark = new Color(0.14f, 0.10f, 0.10f, 1f);
        Color woodBase = new Color(0.48f, 0.28f, 0.14f, 1f);
        Color woodLit  = new Color(0.66f, 0.41f, 0.22f, 1f);
        Color woodDark = new Color(0.32f, 0.18f, 0.09f, 1f);

        if (f == 4)
        {
            // Derrotado sentado en el suelo, despeinado y aturdido
            c.FillCapsule(34f, 6f, 45f, 6f, 3.2f, suitDark);
            c.FillCapsule(19f, 6f, 29f, 6f, 3.2f, suit);
            c.FillEllipse(46f, 6.5f, 3.8f, 2.8f, shoeDark);
            c.FillEllipse(18f, 6.5f, 3.8f, 2.8f, shoeDark);

            c.FillShadedEllipse(31f, 14f, 13f, 9.5f, suit, suitLit, suitDark);
            c.FillQuadCurve(22f, 19f, 31f, 14f, 39f, 9f, 1.5f, sashG);
            c.FillEllipse(31f, 19f, 2.4f, 2.4f, shirt);
            c.FillCapsule(19f, 17f, 17f, 9f, 2.8f, suitDark);
            c.FillCapsule(42f, 17f, 44f, 9f, 2.8f, suit);
            c.FillEllipse(17f, 8f, 2.4f, 2.2f, skin);
            c.FillEllipse(44f, 8f, 2.4f, 2.2f, skin);

            // Cabeza inclinada + copete desordenado + ojos X
            c.FillShadedEllipse(32f, 29f, 9.5f, 8.8f, skin, skinLit, skinDark);
            c.FillQuadCurve(23f, 32f, 30f, 40f, 42f, 34f, 3.2f, hair);
            c.FillQuadCurve(25f, 35f, 33f, 41f, 40f, 35f, 1.4f, hairSilv);
            // Ojos de mareo
            c.FillCapsule(27f, 28f, 30f, 31f, 0.7f, outline);
            c.FillCapsule(27f, 31f, 30f, 28f, 0.7f, outline);
            c.FillCapsule(34f, 28f, 37f, 31f, 0.7f, outline);
            c.FillCapsule(34f, 31f, 37f, 28f, 0.7f, outline);
            c.FillQuadCurve(29f, 24f, 32f, 25.5f, 36f, 23.5f, 0.8f, outline);

            c.ApplyOutline(outline);
            return c.ToSprite(key, 36f, new Vector2(0.5f, 0f));
        }

        float walkBob = (f == 1) ? 1.8f : 0f;
        float lean = (f == 2) ? 2.5f : 0f;

        // Piernas orgánicas en paso y zapatos lustrados ovalados
        float lx = (f == 1) ? 22f : 25.5f;
        float rx = (f == 1) ? 39f : 35.5f;
        c.FillCapsule(26f, 19f + walkBob, lx, 5f, 3.6f, suitDark);
        c.FillCapsule(35f, 19f + walkBob, rx, 5f, 3.6f, suit);
        c.FillEllipse(lx + 1.5f, 3.5f, 5.2f, 3.0f, shoeDark);
        c.FillEllipse(rx + 2.0f, 3.5f, 5.2f, 3.0f, shoeDark);
        c.FillEllipse(rx + 3.2f, 4.4f, 2.0f, 1.0f, hairLit); // Brillo zapato

        // Faldón trasero redondeado del saco
        c.FillQuadCurve(19f, 25f + walkBob, 16f, 17f + walkBob, 21f, 13f + walkBob, 2.5f, suitDark);

        // Torso redondo carismático (barriga simpática + hombros curvos)
        float torsoCx = 30.5f + lean * 0.4f;
        float torsoCy = 32f + walkBob;
        c.FillShadedEllipse(torsoCx, torsoCy, 14.5f, 15.5f, suit, suitLit, suitDark);
        c.FillEllipse(torsoCx, torsoCy + 11.5f, 13.5f, 5.0f, suitLit);

        // Camisa blanca en V + corbata roja curva
        c.FillQuadCurve(torsoCx - 1f, torsoCy + 13f, torsoCx + 2f, torsoCy + 5f, torsoCx + 3.5f, torsoCy + 12.5f, 3.8f, shirt);
        c.FillCapsule(torsoCx + 1.5f, torsoCy + 11f, torsoCx + 1.8f, torsoCy - 1f, 1.6f, tie);
        c.FillEllipse(torsoCx + 1.8f, torsoCy - 2f, 2.1f, 2.5f, tieLit);

        // Banda municipal diagonal tricolor con medallón dorado
        c.FillQuadCurve(torsoCx - 10f, torsoCy + 10f, torsoCx + 1f, torsoCy + 1f, torsoCx + 11f, torsoCy - 8f, 1.8f, sashG);
        c.FillQuadCurve(torsoCx - 9f, torsoCy + 11.5f, torsoCx + 2f, torsoCy + 2.5f, torsoCx + 12f, torsoCy - 6.5f, 1.1f, shirt);
        c.FillQuadCurve(torsoCx - 8f, torsoCy + 12.8f, torsoCx + 3f, torsoCy + 3.8f, torsoCx + 12.5f, torsoCy - 5.2f, 1.1f, sashR);
        c.FillShadedEllipse(torsoCx - 7f, torsoCy + 7.5f, 2.4f, 2.4f, gold, goldLit, goldDark);

        // Brazo trasero / izquierdo
        c.FillQuadCurve(torsoCx - 12f, torsoCy + 9f, torsoCx - 17f, torsoCy + 1f, torsoCx - 13f, torsoCy - 6f, 3.2f, suitDark);
        c.FillEllipse(torsoCx - 13f, torsoCy - 7f, 2.8f, 2.5f, skinDark);

        // Brazo derecho y accesorio de ataque/escudo
        if (f == 2)
        {
            // Brazo extendido sosteniendo megáfono dorado abocinado
            c.FillQuadCurve(torsoCx + 10f, torsoCy + 8f, torsoCx + 15f, torsoCy + 8f, torsoCx + 17f, torsoCy + 12f, 3.0f, suitLit);
            c.FillEllipse(torsoCx + 17f, torsoCy + 12f, 2.6f, 2.4f, skin);

            float mx = torsoCx + 18f;
            float my = torsoCy + 13f;
            for (int i = 0; i <= 11; i++)
            {
                float t = i / 11f;
                c.FillEllipse(mx + i * 1.1f, my, 1.8f, Mathf.Lerp(2.2f, 7.2f, t), t > 0.7f ? goldLit : gold);
            }
            c.FillEllipse(mx + 12f, my, 2.1f, 7.0f, goldDark);
        }
        else if (f == 3)
        {
            c.FillQuadCurve(torsoCx + 10f, torsoCy + 8f, torsoCx + 16f, torsoCy + 4f, torsoCx + 18f, torsoCy + 6f, 3.0f, suitLit);
            c.FillEllipse(torsoCx + 18f, torsoCy + 6f, 2.6f, 2.4f, skin);

            // Podio curvo elegante frente al alcalde
            c.FillRoundedRect(42f, 2f, 59f, 35f, 4.5f, woodBase);
            c.FillRoundedRect(44f, 6f, 57f, 31f, 3f, woodDark);
            c.FillRoundedRect(40f, 33f, 61f, 39f, 2.5f, woodLit);
            c.FillShadedEllipse(50.5f, 21f, 4.8f, 4.8f, gold, goldLit, goldDark);
            // Micrófono cuello de ganso
            c.FillQuadCurve(48f, 39f, 46f, 45f, 42f, 46f, 0.9f, outline);
            c.FillEllipse(41.5f, 46f, 1.8f, 1.5f, goldDark);
        }
        else
        {
            c.FillQuadCurve(torsoCx + 11f, torsoCy + 9f, torsoCx + 16f, torsoCy + 1f, torsoCx + 14f, torsoCy - 5f, 3.1f, suitLit);
            c.FillEllipse(torsoCx + 14f, torsoCy - 6f, 2.8f, 2.5f, skin);
        }

        // Cabeza redonda caricaturesca + papada simpática + oreja curva
        float headCx = 32f + lean;
        float headCy = 55.5f + walkBob;
        c.FillShadedEllipse(headCx, headCy - 5f, 7.5f, 5.2f, skin, skin, skinDark); // Papada
        c.FillShadedEllipse(headCx, headCy, 11.5f, 11.2f, skin, skinLit, skinDark);
        c.FillShadedEllipse(headCx - 11.5f, headCy - 1f, 2.6f, 3.6f, skin, skinLit, skinDark); // Oreja

        // Mejillas rosadas de político sonriente
        c.FillEllipse(headCx - 3f, headCy - 2.5f, 2.6f, 1.6f, blush);
        c.FillEllipse(headCx + 8f, headCy - 2.5f, 2.4f, 1.5f, blush);

        // Copete voluminoso curvado con mechón plateado
        c.FillQuadCurve(headCx - 11f, headCy + 4f, headCx - 6f, headCy + 14f, headCx + 8f, headCy + 11f, 4.5f, hair);
        c.FillQuadCurve(headCx + 2f, headCy + 10f, headCx + 10f, headCy + 14f, headCx + 13f, headCy + 6f, 3.2f, hair);
        c.FillQuadCurve(headCx - 6f, headCy + 10f, headCx + 3f, headCy + 14f, headCx + 10f, headCy + 9f, 1.5f, hairSilv);
        c.FillCapsule(headCx - 10.5f, headCy + 5f, headCx - 9.5f, headCy - 1f, 1.6f, hair); // Patilla

        // Ojos expresivos con brillo blanco + cejas arqueadas
        c.FillEllipse(headCx + 1.5f, headCy + 2.5f, 2.1f, 2.6f, Color.white);
        c.FillEllipse(headCx + 7.5f, headCy + 2.5f, 2.0f, 2.6f, Color.white);
        c.FillEllipse(headCx + 2.0f, headCy + 2.5f, 1.1f, 1.4f, outline);
        c.FillEllipse(headCx + 8.0f, headCy + 2.5f, 1.1f, 1.4f, outline);
        c.SetPixel(Mathf.RoundToInt(headCx + 1.4f), Mathf.RoundToInt(headCy + 3.4f), Color.white);
        c.SetPixel(Mathf.RoundToInt(headCx + 7.4f), Mathf.RoundToInt(headCy + 3.4f), Color.white);

        c.FillQuadCurve(headCx - 1f, headCy + 6.2f, headCx + 1.5f, headCy + 7.5f, headCx + 4f, headCy + 5.8f, 0.9f, hair);
        c.FillQuadCurve(headCx + 5.5f, headCy + 5.8f, headCx + 8f, headCy + 7.5f, headCx + 10.5f, headCy + 6.0f, 0.9f, hair);

        // Nariz redonda y gran sonrisa colmilluda de campaña
        c.FillEllipse(headCx + 5.2f, headCy - 0.5f, 2.4f, 1.8f, skinDark);
        c.FillEllipse(headCx + 5.0f, headCy, 1.8f, 1.3f, skinLit);
        if (f == 2)
            c.FillEllipse(headCx + 4.5f, headCy - 5.2f, 3.8f, 2.8f, outline); // Boca gritando al megáfono
        else
            c.FillQuadCurve(headCx - 0.5f, headCy - 4.2f, headCx + 4.5f, headCy - 7.5f, headCx + 9.2f, headCy - 4.0f, 1.6f, Color.white);

        c.ApplyOutline(outline);

        // Ondas sónicas brillantes al frente del megáfono (sin contorno grueso)
        if (f == 2)
        {
            c.FillQuadCurve(56f, 52f, 60f, 45f, 56f, 38f, 1.0f, goldLit);
            c.FillQuadCurve(60f, 55f, 63f, 45f, 60f, 35f, 0.9f, gold);
        }

        return c.ToSprite(key, 36f, new Vector2(0.5f, 0f));
    }

    // ─── JEFE 3: EL INFLUENCER DEL CALLEJÓN (PIXEL ART ORGÁNICO 64x80) ──────────
    public static Sprite GetInfluencerBossSprite(int frame)
    {
        // frame: 0=Idle/WalkA, 1=WalkB, 2=RingLightFlash, 3=SelfieDash, 4=Defeat
        int f = Mathf.Clamp(frame, 0, 4);
        string key = $"InfluencerBoss_HD_F{f}";
        if (spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        PixelCanvas c = new PixelCanvas(64, 80);
        Color outline    = new Color(0.08f, 0.05f, 0.14f, 1f);
        Color hoodie     = new Color(0.56f, 0.14f, 0.86f, 1f);
        Color hoodieLit  = new Color(0.76f, 0.34f, 0.98f, 1f);
        Color hoodieDark = new Color(0.36f, 0.08f, 0.58f, 1f);
        Color cyan       = new Color(0.14f, 0.94f, 0.98f, 1f);
        Color cyanLit    = new Color(0.72f, 1.00f, 1.00f, 1f);
        Color magenta    = new Color(1.00f, 0.20f, 0.64f, 1f);
        Color magentaLit = new Color(1.00f, 0.62f, 0.84f, 1f);
        Color jeans      = new Color(0.15f, 0.17f, 0.27f, 1f);
        Color jeansDark  = new Color(0.09f, 0.10f, 0.17f, 1f);
        Color skin       = new Color(0.95f, 0.78f, 0.64f, 1f);
        Color skinLit    = new Color(1.00f, 0.88f, 0.76f, 1f);
        Color skinDark   = new Color(0.78f, 0.58f, 0.45f, 1f);
        Color visorDark  = new Color(0.07f, 0.06f, 0.14f, 1f);
        Color redAlert   = new Color(1.00f, 0.16f, 0.24f, 1f);

        if (f == 4)
        {
            // Derrotado sin señal sentado en el callejón
            c.FillCapsule(18f, 7f, 28f, 6f, 3.0f, jeansDark);
            c.FillCapsule(34f, 7f, 44f, 6f, 3.0f, jeans);
            c.FillEllipse(17f, 6f, 4.5f, 2.8f, cyan);
            c.FillEllipse(45f, 6f, 4.5f, 2.8f, cyan);

            // Selfie stick doblado tirado a la izquierda
            c.FillQuadCurve(4f, 4f, 10f, 10f, 16f, 4f, 1.2f, cyan);
            c.FillRoundedRect(2f, 2f, 7f, 7f, 1.5f, magenta);

            c.FillShadedEllipse(31f, 15f, 12f, 9.5f, hoodie, hoodieLit, hoodieDark);
            c.FillQuadCurve(24f, 19f, 31f, 13f, 39f, 18f, 1.8f, magenta);

            // Cabeza caída con copete neón y visor agrietado
            c.FillShadedEllipse(32f, 29f, 9.5f, 9.0f, skin, skinLit, skinDark);
            c.FillQuadCurve(22f, 32f, 31f, 41f, 42f, 32f, 3.4f, cyan);
            c.FillQuadCurve(24f, 35f, 33f, 42f, 40f, 35f, 1.6f, magenta);
            c.FillRoundedRect(27f, 27f, 39f, 32f, 2.0f, visorDark);
            c.FillCapsule(29f, 28f, 33f, 31f, 0.6f, redAlert);
            c.FillCapsule(34f, 31f, 38f, 28f, 0.6f, redAlert);

            c.ApplyOutline(outline);
            return c.ToSprite(key, 36f, new Vector2(0.5f, 0f));
        }

        float bob = (f == 1) ? 2.0f : 0f;
        float dashLean = (f == 3) ? 4.0f : 0f;

        // Tenis urbanos grandes y joggers con postura dinámica
        float lx = (f == 1) ? 19f : (f == 3 ? 16f : 23f);
        float rx = (f == 1) ? 41f : (f == 3 ? 42f : 36f);
        c.FillCapsule(25f, 20f + bob, lx, 6f, 3.2f, jeansDark);
        c.FillCapsule(34f, 20f + bob, rx, 6f, 3.2f, jeans);
        // Suela y cuerpo grueso de tenis neón
        c.FillEllipse(lx + 1.5f, 4.0f, 5.8f, 3.4f, cyan);
        c.FillEllipse(lx + 1.5f, 2.4f, 6.0f, 1.6f, Color.white);
        c.FillEllipse(rx + 2.5f, 4.0f, 5.8f, 3.4f, magenta);
        c.FillEllipse(rx + 2.5f, 2.4f, 6.0f, 1.6f, Color.white);

        // Bufanda / cinta urbana ondeando atrás en el viento
        c.FillQuadCurve(22f + dashLean, 42f + bob, 11f, 40f + bob, 7f, 29f + bob, 2.2f, magenta);
        c.FillQuadCurve(21f + dashLean, 40f + bob, 13f, 34f + bob, 10f, 24f + bob, 1.6f, cyan);

        // Torso hoodie streetwear abombado y con pliegues curvos
        float tCx = 30f + dashLean * 0.6f;
        float tCy = 32f + bob;
        c.FillShadedEllipse(tCx, tCy, 12.0f, 14.5f, hoodie, hoodieLit, hoodieDark);
        // Cuello / capucha caída atrás
        c.FillShadedEllipse(tCx - 4f, tCy + 12f, 9.5f, 5.5f, hoodieDark, hoodie, hoodieDark);
        // Franja de pecho neón + cordones del hoodie
        c.FillQuadCurve(tCx - 9f, tCy - 1f, tCx + 1f, tCy + 2f, tCx + 10f, tCy - 1f, 2.0f, magenta);
        c.FillCapsule(tCx + 1f, tCy + 11f, tCx + 0.5f, tCy + 3f, 0.9f, cyan);
        c.FillCapsule(tCx + 4f, tCy + 11f, tCx + 4.5f, tCy + 3f, 0.9f, cyan);

        // Brazo izquierdo
        c.FillQuadCurve(tCx - 9f, tCy + 8f, tCx - 14f, tCy + 2f, tCx - 11f, tCy - 4f, 2.9f, hoodieDark);
        c.FillEllipse(tCx - 11f, tCy - 5f, 2.6f, 2.3f, skinDark);

        // Brazo derecho + Ring Light o Selfie Stick Lance
        if (f == 2)
        {
            c.FillQuadCurve(tCx + 9f, tCy + 8f, tCx + 16f, tCy + 11f, tCx + 18f, tCy + 17f, 2.9f, hoodieLit);
            c.FillEllipse(tCx + 18f, tCy + 17f, 2.6f, 2.3f, skin);
            // Trípode mano + Ring Light redondo brillante
            c.FillCapsule(tCx + 18f, tCy + 17f, 52f, 52f, 1.3f, jeans);
            c.FillRing(52f, 54f, 4.8f, 8.2f, cyan);
            c.FillRing(52f, 54f, 5.6f, 7.4f, Color.white);
        }
        else if (f == 3)
        {
            c.FillQuadCurve(tCx + 8f, tCy + 9f, tCx + 16f, tCy + 6f, tCx + 20f, tCy + 7f, 2.9f, hoodieLit);
            c.FillEllipse(tCx + 20f, tCy + 7f, 2.6f, 2.3f, skin);
            // Lanza selfie-stick inclinada hacia adelante
            c.FillCapsule(tCx + 16f, tCy + 4f, 60f, tCy + 14f, 1.4f, cyan);
            c.FillRoundedRect(54f, tCy + 10f, 61f, tCy + 18f, 2.0f, magenta);
            c.FillEllipse(58f, tCy + 14f, 1.8f, 1.8f, Color.white);
        }
        else
        {
            c.FillQuadCurve(tCx + 9f, tCy + 8f, tCx + 14f, tCy + 1f, tCx + 12f, tCy - 5f, 2.9f, hoodieLit);
            c.FillEllipse(tCx + 12f, tCy - 6f, 2.6f, 2.3f, skin);
            // Smartphone en mano en pose idle/walk
            c.FillRoundedRect(tCx + 10f, tCy - 6f, tCx + 16f, tCy + 1f, 1.8f, visorDark);
            c.FillRoundedRect(tCx + 11f, tCy - 5f, tCx + 15f, tCy, 1.0f, cyanLit);
        }

        // Cabeza estilizada + copete puntiagudo curvo neón + visor futurista curvo
        float hCx = 31.5f + dashLean;
        float hCy = 55.5f + bob;
        c.FillShadedEllipse(hCx, hCy, 10.5f, 10.8f, skin, skinLit, skinDark);
        c.FillShadedEllipse(hCx - 10.5f, hCy - 1f, 2.4f, 3.2f, skin, skinLit, skinDark);

        // Cabello neón cian con mechones curvos + punta magenta
        c.FillQuadCurve(hCx - 10f, hCy + 3f, hCx - 4f, hCy + 15f, hCx + 9f, hCy + 11f, 4.0f, cyan);
        c.FillQuadCurve(hCx + 2f, hCy + 11f, hCx + 10f, hCy + 17f, hCx + 15f, hCy + 10f, 3.0f, cyanLit);
        c.FillQuadCurve(hCx + 6f, hCy + 13f, hCx + 13f, hCy + 16f, hCx + 16f, hCy + 11f, 1.8f, magenta);
        c.FillQuadCurve(hCx - 10f, hCy + 5f, hCx - 13f, hCy + 11f, hCx - 6f, hCy + 13f, 2.4f, hoodieLit);

        // Visor oscuro redondeado con brillo magenta-cian
        c.FillQuadCurve(hCx - 2f, hCy + 2.5f, hCx + 5f, hCy + 3.5f, hCx + 11f, hCy + 2.0f, 3.2f, visorDark);
        c.FillQuadCurve(hCx + 1f, hCy + 3.2f, hCx + 5f, hCy + 3.8f, hCx + 9f, hCy + 2.8f, 1.1f, magentaLit);
        c.SetPixel(Mathf.RoundToInt(hCx + 3f), Mathf.RoundToInt(hCy + 2.5f), cyanLit);
        c.SetPixel(Mathf.RoundToInt(hCx + 8f), Mathf.RoundToInt(hCy + 2.5f), cyanLit);

        // Sonrisa confiada de streamer
        c.FillQuadCurve(hCx + 2f, hCy - 4.5f, hCx + 5.5f, headSmirkY(hCy), hCx + 8.5f, hCy - 3.6f, 0.9f, Color.white);

        c.ApplyOutline(outline);

        // Efectos luminosos sin contorno pesado (destello Ring Light o arco del selfie stick)
        if (f == 2)
        {
            c.FillRing(52f, 54f, 8.6f, 10.6f, new Color(0.25f, 0.95f, 1f, 0.42f));
        }
        else if (f == 3)
        {
            c.FillQuadCurve(53f, tCy + 22f, 62f, tCy + 14f, 53f, tCy + 4f, 1.2f, new Color(1f, 0.3f, 0.7f, 0.72f));
        }

        return c.ToSprite(key, 36f, new Vector2(0.5f, 0f));
    }

    private static float headSmirkY(float hCy) => hCy - 6.2f;

    // ─── ESBIRRO NIVEL 2: GUERRERO VASIJA DE TALAVERA (ORGÁNICO 56x64) ────────
    public static Sprite GetTalaveraEnemySprite(int frame)
    {
        // frame: 0 = paso A, 1 = paso B, 2 = cerámica rota (derrota)
        int f = Mathf.Clamp(frame, 0, 2);
        string key = $"TalaveraEnemy_HD_F{f}";
        if (spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        PixelCanvas c = new PixelCanvas(56, 64);
        Color outline    = new Color(0.08f, 0.12f, 0.28f, 1f);
        Color glaze      = new Color(0.95f, 0.96f, 0.99f, 1f);
        Color glazeLit   = new Color(1.00f, 1.00f, 1.00f, 1f);
        Color glazeShd   = new Color(0.76f, 0.84f, 0.94f, 1f);
        Color cobalt     = new Color(0.10f, 0.34f, 0.80f, 1f);
        Color cobaltDark = new Color(0.07f, 0.22f, 0.54f, 1f);
        Color gold       = new Color(0.96f, 0.74f, 0.22f, 1f);
        Color goldLit    = new Color(1.00f, 0.92f, 0.54f, 1f);
        Color clay       = new Color(0.76f, 0.34f, 0.20f, 1f);
        Color clayDark   = new Color(0.52f, 0.20f, 0.11f, 1f);
        Color eyeGlow    = new Color(1.00f, 0.34f, 0.10f, 1f);

        if (f == 2)
        {
            // Fragmentos curvos de porcelana y asas rotas en el suelo
            c.FillShadedEllipse(16f, 8f, 8.5f, 6.5f, glaze, glazeLit, glazeShd);
            c.FillEllipse(16f, 8f, 4.0f, 2.2f, cobalt);
            c.FillShadedEllipse(36f, 9f, 9.5f, 7.2f, glaze, glazeLit, glazeShd);
            c.FillQuadCurve(31f, 10f, 36f, 14f, 41f, 9f, 1.8f, cobalt);
            c.FillQuadCurve(24f, 16f, 28f, 19f, 32f, 15f, 2.0f, gold);
            c.FillEllipse(45f, 5f, 4.2f, 3.2f, clay);
            c.ApplyOutline(outline);
            return c.ToSprite(key, 44f, new Vector2(0.5f, 0f));
        }

        float stepShift = (f == 1) ? 3.5f : 0f;
        float bob = (f == 1) ? 1.8f : 0f;

        // Patitas curvas de barro cocido con botitas redondeadas
        c.FillQuadCurve(21f, 17f + bob, 18f - stepShift * 0.4f, 11f, 17f + stepShift, 5f, 2.6f, clayDark);
        c.FillQuadCurve(34f, 17f + bob, 37f + stepShift * 0.4f, 11f, 38f - stepShift, 5f, 2.6f, clay);
        c.FillEllipse(17f + stepShift + 0.8f, 4f, 4.5f, 2.8f, clayDark);
        c.FillEllipse(38f - stepShift + 0.8f, 4f, 4.5f, 2.8f, clay);

        // Asas curvas de oro estilo ánfora elegante (en C suave a cada lado)
        c.FillQuadCurve(12f, 42f + bob, 3.5f, 35f + bob, 10f, 25f + bob, 2.3f, gold);
        c.FillQuadCurve(43f, 42f + bob, 51.5f, 35f + bob, 45f, 25f + bob, 2.3f, gold);
        c.FillQuadCurve(11f, 42f + bob, 5f, 35f + bob, 10f, 26f + bob, 0.9f, goldLit);

        // Pie / base torneada curva de la vasija + vientre panzón de ánfora
        c.FillShadedEllipse(27.5f, 16.5f + bob, 10.5f, 4.2f, cobalt, cobalt, cobaltDark);
        c.FillShadedEllipse(27.5f, 31.5f + bob, 18.2f, 15.8f, glaze, glazeLit, glazeShd);
        // Cuello estrecho + boca superior acampanada
        c.FillShadedEllipse(27.5f, 48.0f + bob, 11.2f, 6.2f, glaze, glazeLit, glazeShd);
        c.FillShadedEllipse(27.5f, 54.5f + bob, 14.5f, 4.5f, cobalt, cobalt, cobaltDark);
        c.FillQuadCurve(15f, 53f + bob, 27.5f, 51f + bob, 40f, 53f + bob, 1.3f, goldLit);

        // Franja floral de Talavera siguiendo la curva de la panza
        for (int i = -3; i <= 3; i++)
        {
            float px = 27.5f + i * 4.5f;
            float arcY = 23.5f + bob - (i * i) * 0.25f;
            c.FillEllipse(px, arcY, 2.2f, 2.4f, cobalt);
            c.FillEllipse(px, arcY, 0.9f, 0.9f, goldLit);
        }
        c.FillQuadCurve(11f, 29f + bob, 27.5f, 26.5f + bob, 44f, 29f + bob, 1.1f, gold);
        c.FillQuadCurve(12f, 19.5f + bob, 27.5f, 17.5f + bob, 43f, 19.5f + bob, 1.0f, gold);

        // Reflejo blanco de porcelana vidriada arriba-izquierda
        c.FillQuadCurve(14f, 42f + bob, 12f, 36f + bob, 14f, 29f + bob, 1.5f, glazeLit);

        // Rostro fiero expresivo pintado en la cerámica (ojos de brasa + cejas curvas)
        c.FillEllipse(20.5f, 38.5f + bob, 3.6f, 4.0f, outline);
        c.FillEllipse(34.5f, 38.5f + bob, 3.6f, 4.0f, outline);
        c.FillEllipse(20.5f, 38.2f + bob, 2.4f, 2.8f, eyeGlow);
        c.FillEllipse(34.5f, 38.2f + bob, 2.4f, 2.8f, eyeGlow);
        c.FillEllipse(20.0f, 39.2f + bob, 1.1f, 1.3f, Color.yellow);
        c.FillEllipse(34.0f, 39.2f + bob, 1.1f, 1.3f, Color.yellow);
        // Cejas arqueadas furiosas
        c.FillQuadCurve(15f, 43.5f + bob, 20f, 42.5f + bob, 25f, 39.5f + bob, 1.3f, cobaltDark);
        c.FillQuadCurve(40f, 43.5f + bob, 35f, 42.5f + bob, 30f, 39.5f + bob, 1.3f, cobaltDark);

        c.ApplyOutline(outline);
        return c.ToSprite(key, 44f, new Vector2(0.5f, 0f));
    }

    // ─── ESBIRRO NIVEL 3A: MINI-DRON PAPARAZZI (ORGÁNICO 64x48) ───────────────
    public static Sprite GetDroneEnemySprite(int frame)
    {
        // frame: 0 = hélice A, 1 = hélice B, 2 = dron averiado (derrota)
        int f = Mathf.Clamp(frame, 0, 2);
        string key = $"DroneEnemy_HD_F{f}";
        if (spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        PixelCanvas c = new PixelCanvas(64, 48);
        Color outline    = new Color(0.06f, 0.05f, 0.13f, 1f);
        Color dome       = new Color(0.24f, 0.26f, 0.42f, 1f);
        Color domeLit    = new Color(0.44f, 0.48f, 0.68f, 1f);
        Color domeDark   = new Color(0.13f, 0.14f, 0.25f, 1f);
        Color silver     = new Color(0.74f, 0.80f, 0.90f, 1f);
        Color cyan       = new Color(0.14f, 0.94f, 1.00f, 1f);
        Color magenta    = new Color(1.00f, 0.20f, 0.64f, 1f);
        Color recRed     = new Color(1.00f, 0.14f, 0.22f, 1f);

        if (f == 2)
        {
            // Cápsula estrellada ladeada y chispas neón
            c.FillShadedEllipse(31.5f, 20f, 15f, 10f, domeDark, dome, domeDark);
            c.FillEllipse(33f, 19f, 5.5f, 5.5f, outline);
            c.FillEllipse(33f, 19f, 3.0f, 3.0f, recRed);
            c.FillQuadCurve(18f, 23f, 10f, 28f, 7f, 22f, 1.8f, silver);
            c.FillQuadCurve(45f, 23f, 53f, 27f, 56f, 19f, 1.8f, silver);
            c.ApplyOutline(outline);
            c.FillEllipse(24f, 32f, 2.2f, 2.2f, Color.yellow);
            c.FillEllipse(42f, 30f, 2.0f, 2.0f, magenta);
            c.FillEllipse(32f, 34f, 1.8f, 1.8f, cyan);
            return c.ToSprite(key, 48f, new Vector2(0.5f, 0.5f));
        }

        // Brazos curvos tipo ala de gaviota hacia los rotores laterales
        c.FillQuadCurve(20f, 26f, 13f, 25f, 9f, 32f, 2.4f, silver);
        c.FillQuadCurve(43f, 26f, 50f, 25f, 54f, 32f, 2.4f, silver);
        c.FillEllipse(9f, 33f, 3.2f, 2.8f, dome);
        c.FillEllipse(54f, 33f, 3.2f, 2.8f, dome);

        // Gimbal / cámara colgante redondeada inferior
        c.FillShadedEllipse(31.5f, 10.5f, 7.5f, 5.5f, silver, Color.white, domeDark);
        c.FillEllipse(31.5f, 9.5f, 3.4f, 3.4f, recRed);

        // Cuerpo ovalado aerodinámico tipo cápsula futurista
        c.FillShadedEllipse(31.5f, 24.5f, 17.5f, 11.5f, dome, domeLit, domeDark);
        // Franja curva neón superior
        c.FillQuadCurve(16f, 29f, 31.5f, 35f, 47f, 29f, 1.4f, magenta);

        // Ojo cámara gigante con aro LED cian y pupila REC
        c.FillEllipse(31.5f, 24.0f, 8.2f, 8.2f, outline);
        c.FillRing(31.5f, 24.0f, 5.4f, 7.6f, cyan);
        c.FillEllipse(31.5f, 24.0f, 4.4f, 4.4f, f == 0 ? recRed : new Color(1f, 0.45f, 0.52f, 1f));
        c.FillEllipse(29.8f, 25.8f, 1.5f, 1.5f, Color.white);

        c.ApplyOutline(outline);

        // Discos translúcidos de hélices girando suavemente (alternan entre frame 0 y 1)
        float rotorW = (f == 0) ? 9.5f : 6.8f;
        c.FillEllipse(9f, 37.5f, rotorW, 2.2f, new Color(0.18f, 0.96f, 1.0f, 0.72f));
        c.FillEllipse(54f, 37.5f, rotorW, 2.2f, new Color(0.18f, 0.96f, 1.0f, 0.72f));
        c.FillEllipse(9f, 37.5f, rotorW * 0.45f, 1.2f, new Color(1f, 1f, 1f, 0.90f));
        c.FillEllipse(54f, 37.5f, rotorW * 0.45f, 1.2f, new Color(1f, 1f, 1f, 0.90f));

        return c.ToSprite(key, 48f, new Vector2(0.5f, 0.5f));
    }

    // ─── ESBIRRO NIVEL 3B: FAN DE SELFIES DEL CALLEJÓN (ORGÁNICO 56x68) ────────
    public static Sprite GetSelfieFanEnemySprite(int frame)
    {
        // frame: 0 = caminando A, 1 = caminando B, 2 = derrotado
        int f = Mathf.Clamp(frame, 0, 2);
        string key = $"SelfieFan_HD_F{f}";
        if (spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        PixelCanvas c = new PixelCanvas(56, 68);
        Color outline    = new Color(0.08f, 0.05f, 0.14f, 1f);
        Color jacket     = new Color(0.62f, 0.18f, 0.84f, 1f);
        Color jacketLit  = new Color(0.80f, 0.36f, 0.98f, 1f);
        Color jacketDark = new Color(0.40f, 0.10f, 0.58f, 1f);
        Color cyan       = new Color(0.16f, 0.92f, 0.98f, 1f);
        Color pants      = new Color(0.16f, 0.19f, 0.30f, 1f);
        Color pantsDark  = new Color(0.10f, 0.11f, 0.19f, 1f);
        Color skin       = new Color(0.94f, 0.76f, 0.62f, 1f);
        Color skinLit    = new Color(1.00f, 0.87f, 0.74f, 1f);
        Color skinDark   = new Color(0.78f, 0.58f, 0.44f, 1f);
        Color magenta    = new Color(1.00f, 0.22f, 0.62f, 1f);
        Color magLit     = new Color(1.00f, 0.56f, 0.82f, 1f);
        Color phoneBody  = new Color(0.14f, 0.14f, 0.22f, 1f);
        Color phoneGlow  = f == 0 ? new Color(0.95f, 0.99f, 1f, 1f) : cyan;

        if (f == 2)
        {
            // Caído en el callejón con gorra ladeada y celular tirado
            c.FillCapsule(15f, 6f, 24f, 6f, 2.8f, pantsDark);
            c.FillCapsule(30f, 6f, 38f, 6f, 2.8f, pants);
            c.FillEllipse(14f, 6f, 4.0f, 2.5f, cyan);
            c.FillEllipse(39f, 6f, 4.0f, 2.5f, cyan);

            c.FillShadedEllipse(27f, 14f, 10.5f, 8.2f, jacket, jacketLit, jacketDark);
            c.FillShadedEllipse(26f, 26f, 8.5f, 8.0f, skin, skinLit, skinDark);
            c.FillQuadCurve(17f, 28f, 25f, 36f, 34f, 30f, 3.2f, magenta);
            c.FillRoundedRect(40f, 2f, 47f, 9f, 1.5f, phoneBody);
            c.ApplyOutline(outline);
            return c.ToSprite(key, 44f, new Vector2(0.5f, 0f));
        }

        float bob = (f == 1) ? 1.8f : 0f;
        float lx = (f == 1) ? 16f : 20f;
        float rx = (f == 1) ? 34f : 29f;

        // Piernas con tenis neón chunky redondeados
        c.FillCapsule(22f, 19f + bob, lx, 5f, 2.8f, pantsDark);
        c.FillCapsule(29f, 19f + bob, rx, 5f, 2.8f, pants);
        c.FillEllipse(lx + 1.5f, 3.5f, 4.8f, 2.8f, cyan);
        c.FillEllipse(rx + 2.0f, 3.5f, 4.8f, 2.8f, cyan);
        c.FillEllipse(lx + 1.5f, 2.2f, 5.0f, 1.3f, Color.white);
        c.FillEllipse(rx + 2.0f, 2.2f, 5.0f, 1.3f, Color.white);

        // Chamarra urbana acolchada redondeada + capucha
        c.FillShadedEllipse(25.5f, 30f + bob, 10.5f, 12.2f, jacket, jacketLit, jacketDark);
        c.FillQuadCurve(16f, 28f + bob, 25.5f, 26f + bob, 35f, 28f + bob, 1.8f, magenta);
        c.FillShadedEllipse(22f, 40f + bob, 7.5f, 4.2f, jacketDark, jacket, jacketDark);

        // Brazo izquierdo relajado
        c.FillQuadCurve(17f, 36f + bob, 13f, 30f + bob, 16f, 24f + bob, 2.6f, jacketDark);
        c.FillEllipse(16f, 23f + bob, 2.4f, 2.2f, skinDark);

        // Brazo derecho extendido hacia adelante sosteniendo smartphone brillante
        c.FillQuadCurve(32f, 36f + bob, 38f, 34f + bob, 43f, 37f + bob, 2.6f, jacketLit);
        c.FillEllipse(43f, 37f + bob, 2.4f, 2.2f, skin);
        c.FillRoundedRect(42f, 34f + bob, 50f, 47f + bob, 2.0f, phoneBody);
        c.FillRoundedRect(43.5f, 35.5f + bob, 48.5f, 45.5f + bob, 1.2f, phoneGlow);

        // Cabeza redondeada con gorra snapback hacia atrás, mechón de pelo y lentes oscuros
        float hCx = 26f;
        float hCy = 49.5f + bob;
        c.FillShadedEllipse(hCx, hCy, 9.2f, 9.2f, skin, skinLit, skinDark);
        // Gorra hacia atrás
        c.FillShadedEllipse(hCx - 0.5f, hCy + 5.8f, 9.2f, 5.2f, magenta, magLit, jacketDark);
        c.FillCapsule(hCx - 7f, hCy + 4.5f, hCx - 14.5f, hCy + 3.2f, 2.0f, magenta); // Visera hacia atrás
        // Mechón asomando al frente
        c.FillQuadCurve(hCx + 3f, hCy + 5f, hCx + 8f, hCy + 7f, hCx + 10f, hCy + 3f, 1.6f, outline);
        // Lentes oscuros curvados
        c.FillRoundedRect(hCx + 0.5f, hCy - 0.5f, hCx + 10f, hCy + 3.5f, 1.6f, phoneBody);
        c.SetPixel(Mathf.RoundToInt(hCx + 3f), Mathf.RoundToInt(hCy + 2f), cyan);
        c.SetPixel(Mathf.RoundToInt(hCx + 7f), Mathf.RoundToInt(hCy + 2f), cyan);
        c.FillQuadCurve(hCx + 2f, hCy - 4f, hCx + 5f, hCy - 5.2f, hCx + 7.5f, hCy - 3.6f, 0.8f, Color.white);

        c.ApplyOutline(outline);

        // Destello suave de pantalla/selfie sin borde negro
        c.FillEllipse(47f, 42f + bob, 6.5f, 6.5f, new Color(cyan.r, cyan.g, cyan.b, f == 0 ? 0.24f : 0.36f));

        return c.ToSprite(key, 44f, new Vector2(0.5f, 0f));
    }

    // ─── PORTAL COLONIAL DE FIN DE NIVEL ───────────────────────────────────────
    public static Sprite GetColonialPortalSprite(bool active)
    {
        string key = active ? "Portal_Active" : "Portal_Locked";
        Color stone = new Color(0.76f, 0.52f, 0.38f, 1f);
        Color dark  = new Color(0.24f, 0.14f, 0.16f, 1f);
        Color gold  = new Color(0.96f, 0.76f, 0.24f, 1f);
        Color glow  = active ? new Color(0.28f, 0.92f, 0.98f, 0.85f) : new Color(0.12f, 0.10f, 0.18f, 0.75f);

        return GetOrCreate(key, 32, 44, 16f, new Vector2(0.5f, 0f), (x, y) =>
        {
            float dx = x - 15.5f;
            bool inArchTop = y >= 28 && Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 28f)) <= 14f;
            bool inArchInner = y >= 28 && Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 28f)) <= 9.5f;
            bool inPillar = y < 28 && Mathf.Abs(dx) <= 14f;
            bool inDoorInner = y < 28 && y >= 2 && Mathf.Abs(dx) <= 9.5f;

            if (inArchInner || inDoorInner)
            {
                if (active && ((x + y) % 5 == 0)) return Color.white;
                return glow;
            }

            if (inArchTop || inPillar)
            {
                if (y <= 2 || Mathf.Abs(dx) >= 13f) return dark;
                if (y == 27 || y == 28 || (y > 38 && Mathf.Abs(dx) < 3f)) return gold;
                return stone;
            }

            return Color.clear;
        });
    }

    // ─── REFLECTOR DE CALLEJÓN (NIVEL 3) ───────────────────────────────────────
    public static Sprite GetSpotlightBeamSprite()
    {
        return GetOrCreate("Spotlight_Beam", 24, 48, 16f, new Vector2(0.5f, 1f), (x, y) =>
        {
            float normY = 1f - (y / 47f); // 0 arriba (lámpara), 1 abajo (suelo)
            float halfW = Mathf.Lerp(2.5f, 11.5f, normY);
            float dx = Mathf.Abs(x - 11.5f);

            if (y >= 44 && dx <= 4f)
                return new Color(0.18f, 0.18f, 0.24f, 1f); // Lámpara superior

            if (dx <= halfW)
            {
                float edge = 1f - (dx / Mathf.Max(0.1f, halfW));
                float alpha = Mathf.Clamp01(edge * 0.65f * (1f - normY * 0.35f));
                return new Color(1.0f, 0.92f, 0.45f, alpha);
            }
            return Color.clear;
        });
    }

    // ─── TRAMPOLÍN Y MONEDA / CORAZÓN PROCEDURALES ─────────────────────────────
    public static Sprite GetSpringSprite()
    {
        return GetOrCreate("Spring_Procedural", 20, 14, 20f, new Vector2(0.5f, 0f), (x, y) =>
        {
            if (y >= 10 && x >= 1 && x <= 18) return y == 13 ? new Color(1f, 0.85f, 0.2f, 1f) : new Color(0.88f, 0.20f, 0.18f, 1f);
            if (y <= 2 && x >= 3 && x <= 16) return new Color(0.25f, 0.26f, 0.32f, 1f);
            if (y > 2 && y < 10 && x >= 5 && x <= 14 && (y % 2 == 0)) return new Color(0.82f, 0.84f, 0.88f, 1f);
            return Color.clear;
        });
    }

    public static Sprite GetCollectibleSprite(bool isHeart)
    {
        return GetOrCreate(isHeart ? "Collectible_Heart" : "Collectible_Coin", 16, 16, 24f, new Vector2(0.5f, 0.5f), (x, y) =>
        {
            float dx = x - 7.5f;
            float dy = y - 7.5f;
            if (isHeart)
            {
                float nx = dx / 6f;
                float ny = (dy - 0.5f) / 5.5f;
                float eq = Mathf.Pow(nx * nx + ny * ny - 1f, 3f) - nx * nx * ny * ny * ny;
                if (eq <= 0.04f)
                    return eq > -0.25f ? new Color(0.45f, 0.05f, 0.10f, 1f) : new Color(0.96f, 0.18f, 0.30f, 1f);
                return Color.clear;
            }
            else
            {
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r <= 6.2f)
                {
                    if (r > 5.0f) return new Color(0.55f, 0.36f, 0.05f, 1f);
                    if (Mathf.Abs(dx) < 1.2f && Mathf.Abs(dy) < 3.2f) return new Color(1f, 0.96f, 0.70f, 1f);
                    return new Color(0.98f, 0.80f, 0.18f, 1f);
                }
                return Color.clear;
            }
        });
    }

    public static Sprite GetSolidTileSprite(Color topColor, Color bodyColor, Color accentColor, string key)
    {
        return GetOrCreate("TileBlock_" + key, 32, 16, 16f, new Vector2(0.5f, 0.5f), (x, y) =>
        {
            Color border = bodyColor * 0.45f;
            border.a = 1f;
            if (x == 0 || x == 31 || y == 0 || y == 15) return border;
            if (y >= 12) return topColor;
            if (y == 11) return accentColor;
            if ((x % 8 == 0 && y < 11) || (y == 5)) return border;
            return bodyColor;
        });
    }

    // ─── LAVA PIXEL-ART ANIMADA (4 FRAMES CONTINUOS SIN COSTURAS) ─────────────
    public static Sprite[] GetLavaTileFrames()
    {
        Sprite[] frames = new Sprite[4];
        Color colCrestWhite = new Color(1.00f, 0.957f, 0.580f, 1f); // #FFF494
        Color colCrestGold  = new Color(1.00f, 0.792f, 0.157f, 1f); // #FFCA28
        Color colVeinGold   = new Color(1.00f, 0.702f, 0.102f, 1f); // #FFB31A
        Color colOrange     = new Color(1.00f, 0.420f, 0.039f, 1f); // #FF6B0A
        Color colMidBright  = new Color(0.957f, 0.345f, 0.078f, 1f);// #F45814
        Color colCrimson    = new Color(0.847f, 0.180f, 0.047f, 1f); // #D82E0C
        Color colCrustDark  = new Color(0.392f, 0.063f, 0.031f, 1f); // #641008
        Color colDeepA      = new Color(0.533f, 0.071f, 0.031f, 1f); // #881208
        Color colDeepB      = new Color(0.659f, 0.106f, 0.039f, 1f); // #A81B0A

        for (int f = 0; f < 4; f++)
        {
            int frameIdx = f;
            frames[f] = GetOrCreate($"LavaTile_Frame_{frameIdx}", 32, 24, 32f, new Vector2(0.5f, 0f), (x, y) =>
            {
                float phase = frameIdx * (Mathf.PI * 0.5f);
                float angle = (2f * Mathf.PI * x / 32f) + phase;
                float angle2 = (4f * Mathf.PI * x / 32f) - phase;

                float wave = Mathf.Sin(angle) * 1.45f + Mathf.Cos(angle2) * 0.65f;
                int crestY = Mathf.Clamp(Mathf.RoundToInt(20.5f + wave), 17, 23);

                // Burbujas pixeladas que suben y revientan en la cresta según el frame
                int shiftX = (x + frameIdx * 8) & 31;
                bool surfaceBubble =
                    (shiftX >= 6 && shiftX <= 8 && y == crestY + 1) ||
                    (shiftX == 22 && y == crestY + 1) ||
                    (shiftX == 15 && frameIdx % 2 == 1 && y == crestY + 1);

                if (y > crestY)
                    return surfaceBubble ? colCrestGold : Color.clear;

                // Borde incandescente superior
                if (y >= crestY - 1)
                {
                    bool brightCap = (shiftX % 5 != 0) || (y == crestY && Mathf.Sin(angle) > 0f);
                    return brightCap ? colCrestWhite : colCrestGold;
                }

                // Magma superior con vetas doradas ondulantes
                if (y >= 12)
                {
                    float vein = Mathf.Sin(angle + y * 0.45f) + Mathf.Cos(angle2 - phase);
                    if (vein > 1.15f) return colVeinGold;
                    if (y == 12 && ((x + frameIdx * 3) % 7 == 0)) return colMidBright;
                    return colOrange;
                }

                // Magma medio con bolsas incandescentes y motas de costra volcánica
                if (y >= 5)
                {
                    int pocket = (x + y * 3 + frameIdx * 8) & 31;
                    if ((pocket == 4 || pocket == 5 || pocket == 19) && (y >= 7 && y <= 10))
                        return colMidBright;
                    if ((x + y * 5 + frameIdx * 4) % 19 == 0)
                        return colCrustDark;
                    if (Mathf.Sin(angle - y * 0.3f) > 0.68f)
                        return colMidBright;
                    return colCrimson;
                }

                // Base profunda ardiente apoyada sobre el foso del horno
                return ((x + y + frameIdx * 2) % 6 == 0) ? colDeepB : colDeepA;
            });
        }

        return frames;
    }

    public static Sprite GetLavaGlowSprite()
    {
        const string key = "LavaAmbientGlow";
        if (spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        const int w = 16;
        const int h = 16;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float t = y / (float)(h - 1);
            float alpha = Mathf.Pow(1f - t, 1.8f);
            Color c = Color.Lerp(new Color(1.0f, 0.72f, 0.22f, alpha), new Color(1.0f, 0.34f, 0.06f, alpha), t);
            for (int x = 0; x < w; x++)
                pixels[y * w + x] = c;
        }

        tex.SetPixels(pixels);
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16f);
        sprite.name = key;
        spriteCache[key] = sprite;
        return sprite;
    }

    public static Sprite GetLavaEmberSprite()
    {
        return GetOrCreate("LavaEmberDot", 4, 4, 16f, new Vector2(0.5f, 0.5f), (x, y) =>
        {
            bool core = (x == 1 || x == 2) && (y == 1 || y == 2);
            if (core)
                return new Color(1.00f, 0.96f, 0.62f, 1f);
            bool corner = (x == 0 || x == 3) && (y == 0 || y == 3);
            return corner ? Color.clear : new Color(1.00f, 0.50f, 0.08f, 0.90f);
        });
    }

    // ─── JEFE 1: EL VENDEDOR VIRAL — DON CHAVA (PIXEL ART 40x44, 7 FRAMES) ────
    public static Sprite GetVendorBossSprite(int frame)
    {
        // frame: 0=Idle, 1=WalkA, 2=WalkB, 3=Cucharazo Windup, 4=Cucharazo Swing, 5=Olla/Lluvia, 6=Derrota
        int f = Mathf.Clamp(frame, 0, 6);
        return GetOrCreate($"VendorBoss_F{f}", 40, 44, 18f, new Vector2(0.5f, 0f), (x, y) =>
        {
            Color outline   = new Color(0.08f, 0.06f, 0.10f, 1f);
            Color shirt     = new Color(0.20f, 0.44f, 0.22f, 1f); // Camisa verde vendedor
            Color shirtLit  = new Color(0.28f, 0.56f, 0.30f, 1f);
            Color apron     = new Color(0.84f, 0.74f, 0.55f, 1f); // Mandil beige
            Color apronShd  = new Color(0.68f, 0.58f, 0.42f, 1f);
            Color pants     = new Color(0.14f, 0.18f, 0.30f, 1f);
            Color shoes     = new Color(0.22f, 0.13f, 0.09f, 1f);
            Color skin      = new Color(0.92f, 0.72f, 0.54f, 1f);
            Color hair      = new Color(0.16f, 0.11f, 0.09f, 1f);
            Color capGreen  = new Color(0.16f, 0.36f, 0.18f, 1f);
            Color capBrim   = new Color(0.26f, 0.52f, 0.24f, 1f);
            Color steelPot  = new Color(0.62f, 0.68f, 0.76f, 1f);
            Color steelDark = new Color(0.36f, 0.42f, 0.50f, 1f);
            Color steelLit  = new Color(0.88f, 0.94f, 1.00f, 1f);
            Color garbanzo  = new Color(0.38f, 0.86f, 0.22f, 1f);
            Color garbanLit = new Color(0.76f, 0.98f, 0.40f, 1f);
            Color steam     = new Color(0.92f, 0.96f, 1.00f, 0.85f);
            Color slashGold = new Color(1.00f, 0.90f, 0.42f, 1f);

            // Frame 6: Sentado derrotado en el empedrado junto a su olla volcada y cucharón
            if (f == 6)
            {
                // Olla tirada a la izquierda (x: 3..14, y: 1..12)
                if (x >= 3 && x <= 14 && y >= 1 && y <= 11)
                {
                    if (x == 3 || x == 14 || y == 1 || y == 11) return outline;
                    if (x >= 12 && y >= 4 && y <= 8) return garbanzo;
                    return (y == 9) ? steelLit : steelPot;
                }
                // Cucharón caído al frente (x: 16..34, y: 1..4)
                if (y >= 1 && y <= 4)
                {
                    if (y == 2 && x >= 15 && x <= 30) return steelLit;
                    if (x >= 30 && x <= 35 && y >= 1 && y <= 4)
                        return (x == 30 || x == 35 || y == 1 || y == 4) ? outline : steelPot;
                }
                // Cuerpo sentado de Don Chava (x: 15..28, y: 2..24)
                if (x >= 15 && x <= 28 && y >= 2 && y <= 14)
                {
                    if (x == 15 || x == 28 || y == 2 || y == 14) return outline;
                    if (y <= 5 && x >= 23) return shoes;
                    if (x >= 19 && x <= 25 && y <= 8) return pants;
                    if (x >= 18 && x <= 23 && y >= 6 && y <= 13) return apron;
                    return shirt;
                }
                // Cabeza agachada + gorra ladeada (x: 16..26, y: 15..25)
                if (x >= 16 && x <= 26 && y >= 15 && y <= 24)
                {
                    if (x == 16 || x == 26 || y == 15 || y == 24) return outline;
                    if (y >= 21) return capGreen;
                    if (y == 20 && x >= 21) return capBrim;
                    if (y == 17 && x >= 21 && x <= 24) return hair; // Bigote
                    if (y == 19 && (x == 21 || x == 24)) return outline; // Ojos cerrados
                    return skin;
                }
                return Color.clear;
            }

            int bobY = (f == 1) ? 1 : (f == 2 ? -1 : 0);
            int ly = y - bobY;

            // 1. Cucharón gigante de plata según postura
            if (f == 0 || f == 1 || f == 2) // Descansando al hombro / cintura apuntando atrás-arriba
            {
                float diag = (x - 10) * 0.65f + 16f;
                if (x >= 4 && x <= 24 && Mathf.Abs(ly - diag) <= 1.1f)
                    return steelLit;
                // Cazo redondo atrás-izquierda (x: 2..8, y: 15..21)
                float scoopD = Vector2.Distance(new Vector2(x, ly), new Vector2(5.5f, 17.5f));
                if (scoopD <= 3.6f)
                    return scoopD > 2.5f ? outline : steelPot;
            }
            else if (f == 3) // Wind-up: cucharón alzado bien alto sobre la cabeza
            {
                if (x >= 18 && x <= 21 && ly >= 26 && ly <= 42)
                {
                    if (ly >= 36)
                        return (x == 18 || x == 21 || ly == 42) ? outline : slashGold;
                    return (x == 18 || x == 21) ? outline : steelLit;
                }
                // Destello dorado en la punta del cucharón
                if (Vector2.Distance(new Vector2(x, ly), new Vector2(20f, 39f)) <= 4.5f && ly >= 36)
                    return (ly == 42 || x <= 16 || x >= 24) ? slashGold : steelLit;
            }
            else if (f == 4) // Swing: cucharón barrido hacia la derecha + estela dorada
            {
                if (x >= 20 && x <= 37 && ly >= 18 && ly <= 22)
                {
                    if (x >= 32)
                        return (ly == 18 || ly == 22 || x == 37) ? outline : steelLit;
                    return (ly == 19 || ly == 21) ? outline : steelPot;
                }
                // Estela de ataque en arco al frente
                float arcD = Vector2.Distance(new Vector2(x, ly), new Vector2(26f, 20f));
                if (x >= 29 && arcD >= 9.5f && arcD <= 13.5f && ly >= 10 && ly <= 32)
                    return (arcD > 11.5f) ? slashGold : Color.white;
            }

            // 2. Piernas y botas (animadas en Walk A / B)
            int leftFootX = f == 1 ? 11 : (f == 2 ? 15 : 13);
            int rightFootX = f == 1 ? 23 : (f == 2 ? 19 : 21);
            if (y >= 0 && y <= 11)
            {
                if (Mathf.Abs(x - leftFootX) <= 2 || Mathf.Abs(x - rightFootX) <= 2)
                {
                    if (y <= 3) return (y == 0) ? outline : shoes;
                    return pants;
                }
            }

            // 3. Olla de garbanzos hirviendo (en cadera izquierda, o al frente en frame 5)
            if (f == 5)
            {
                // Sosteniendo la olla gigante delante/arriba para arrojar garbanzos (x: 20..35, y: 18..31)
                if (x >= 20 && x <= 35 && ly >= 18 && ly <= 30)
                {
                    if (x == 20 || x == 35 || ly == 18 || ly == 30) return outline;
                    if (ly >= 27) return ((x + ly) % 2 == 0) ? garbanLit : garbanzo;
                    if (x >= 31) return steelDark;
                    return ly == 25 ? steelLit : steelPot;
                }
                // Garbanzos saliendo disparados
                if (ly >= 31 && ly <= 36 && ((x == 25 && ly == 33) || (x == 29 && ly == 35) || (x == 33 && ly == 32)))
                    return garbanLit;
            }
            else
            {
                // Olla colgada a la izquierda/cadera (x: 5..16, y: 11..22)
                if (x >= 5 && x <= 16 && ly >= 11 && ly <= 22)
                {
                    if (x == 5 || x == 16 || ly == 11 || ly == 22) return outline;
                    if (ly >= 19) return ((x + ly) % 2 == 0) ? garbanLit : garbanzo;
                    if (x >= 13) return steelDark;
                    return (ly == 17) ? steelLit : steelPot;
                }
                // Nubecitas de vapor subiendo de la olla
                if ((x == 8 && (ly == 24 || ly == 26)) || (x == 11 && (ly == 25 || ly == 27)))
                    return steam;
            }

            // 4. Torso robusto con camisa verde y mandil manchado
            if (ly >= 11 && ly <= 26 && x >= 12 && x <= 25)
            {
                if (x == 12 || x == 25 || ly == 11 || ly == 26) return outline;
                // Mandil central
                if (x >= 15 && x <= 24 && ly <= 22)
                {
                    if (x == 15 || x == 24) return outline;
                    if (ly == 14 && (x == 18 || x == 21)) return garbanzo; // Mancha de caldo
                    return (x >= 21) ? apronShd : apron;
                }
                return (x <= 15) ? shirtLit : shirt;
            }

            // 5. Brazo extendido según acción
            if (ly >= 18 && ly <= 23 && x >= 22 && x <= 28)
            {
                if (f == 4 || f == 5)
                    return (ly == 18 || ly == 23 || x == 28) ? outline : skin;
            }

            // 6. Cabeza expresiva, bigote y gorra de Don Chava
            if (ly >= 26 && ly <= 38 && x >= 13 && x <= 25)
            {
                if (x == 13 || x == 25 || ly == 26 || ly == 38) return outline;
                // Gorra verde + visera hacia la derecha
                if (ly >= 34)
                {
                    if (ly == 34 && x >= 21) return capBrim;
                    return capGreen;
                }
                // Visera extendida al frente (x: 25..28, y: 33..34)
                // (se maneja abajo)
                // Ojos y ceño decidido
                if (ly == 31 && (x == 19 || x == 23)) return outline;
                if (ly == 32 && x >= 18 && x <= 24) return hair;
                // Bigote clásico
                if (ly == 28 && x >= 19 && x <= 24) return hair;
                // Patilla
                if (x <= 15 && ly >= 29 && ly <= 33) return hair;
                return skin;
            }

            // Visera de la gorra sobresaliendo hacia la derecha
            if ((ly == 33 || ly == 34) && x >= 25 && x <= 28)
                return (ly == 33 || x == 28) ? outline : capBrim;

            return Color.clear;
        });
    }

    public static Sprite GetGarbanzoProjectileSprite()
    {
        return GetOrCreate("Boss1_GarbanzoProj", 14, 14, 28f, new Vector2(0.5f, 0.5f), (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(6.5f, 6.5f));
            if (d <= 6.2f)
            {
                if (d > 5.0f) return new Color(0.10f, 0.24f, 0.06f, 1f);
                if (x >= 4 && x <= 6 && y >= 7 && y <= 9) return new Color(0.84f, 1.00f, 0.52f, 1f);
                if (y <= 4 && x >= 7) return new Color(0.95f, 0.45f, 0.12f, 1f); // Toque picoso caliente
                return new Color(0.32f, 0.88f, 0.20f, 1f);
            }
            return Color.clear;
        });
    }

    public static Sprite GetBrothShockwaveSprite()
    {
        return GetOrCreate("Boss1_BrothWave", 24, 16, 20f, new Vector2(0.5f, 0f), (x, y) =>
        {
            // Ola creciente de caldo hirviendo verde-dorado hacia la derecha (flipX cuando va a la izquierda)
            float crest = Mathf.Sin((x / 23f) * Mathf.PI) * 10f + (x / 23f) * 5f;
            if (y <= crest && y >= 0)
            {
                if (y >= crest - 1.5f || y == 0 || x == 23)
                    return new Color(1.00f, 0.94f, 0.55f, 1f); // Cresta luminosa
                if (y >= crest - 4f)
                    return new Color(0.45f, 0.92f, 0.26f, 0.95f); // Caldo de garbanzo
                return new Color(0.22f, 0.62f, 0.16f, 0.88f);
            }
            return Color.clear;
        });
    }
}
