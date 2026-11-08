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

    // ─── JEFE 2: EL MUNÍCIPE CARISMÁTICO (PIXEL ART 32x40) ─────────────────────
    public static Sprite GetMayorBossSprite(int frame)
    {
        // frame: 0=Idle/WalkA, 1=WalkB, 2=Megaphone/Attack, 3=PodiumShield, 4=Defeat
        return GetOrCreate($"MayorBoss_F{frame}", 32, 40, 18f, new Vector2(0.5f, 0f), (x, y) =>
        {
            Color outline = new Color(0.10f, 0.08f, 0.14f, 1f);
            Color suit    = new Color(0.14f, 0.22f, 0.44f, 1f); // Traje azul marino político
            Color shirt   = new Color(0.97f, 0.97f, 0.99f, 1f);
            Color tie     = new Color(0.85f, 0.15f, 0.20f, 1f); // Corbata roja llamativa
            Color sash    = new Color(0.12f, 0.58f, 0.26f, 1f); // Banda municipal verde-blanco-rojo
            Color skin    = new Color(0.94f, 0.76f, 0.60f, 1f);
            Color hair    = new Color(0.22f, 0.18f, 0.20f, 1f);
            Color gold    = new Color(0.96f, 0.78f, 0.24f, 1f);

            if (frame == 4) // Derrotado sentado
            {
                if (y >= 1 && y <= 14 && x >= 7 && x <= 25)
                {
                    if (x == 7 || x == 25 || y == 1 || y == 14) return outline;
                    if (y >= 8 && x >= 11 && x <= 20) return skin;
                    if (y >= 11 && x >= 11 && x <= 20) return hair;
                    return suit;
                }
                return Color.clear;
            }

            // Podio de burocracia frente al jefe en frame 3
            if (frame == 3 && x >= 21 && x <= 30 && y >= 0 && y <= 20)
            {
                if (x == 21 || x == 30 || y == 0 || y == 20) return outline;
                if (y == 18 || y == 19) return gold;
                return new Color(0.48f, 0.28f, 0.14f, 1f);
            }

            // Piernas (animación de caminata entre frame 0 y 1)
            int leftLegX = frame == 1 ? 10 : 12;
            int rightLegX = frame == 1 ? 20 : 18;
            if (y >= 0 && y <= 10)
            {
                if (Mathf.Abs(x - leftLegX) <= 2 || Mathf.Abs(x - rightLegX) <= 2)
                {
                    if (y <= 2) return outline; // Zapatos lustrados
                    return suit;
                }
            }

            // Torso de traje + banda municipal + corbata
            if (y >= 10 && y <= 25 && x >= 9 && x <= 22)
            {
                if (x == 9 || x == 22 || y == 10 || y == 25) return outline;
                if (x >= 14 && x <= 17 && y >= 15) return (x == 15 || x == 16) ? tie : shirt;
                if (Mathf.Abs((x - 10) - (24 - y)) <= 1) return sash;
                if (y == 22 && x == 12) return gold; // Pin de campaña
                return suit;
            }

            // Megáfono dorado al atacar (frame 2)
            if (frame == 2 && x >= 22 && x <= 31 && y >= 18 && y <= 27)
            {
                int spread = (x - 21) / 2;
                if (Mathf.Abs(y - 22) <= spread)
                    return (x >= 29 || Mathf.Abs(y - 22) == spread) ? outline : gold;
            }

            // Cabeza + sonrisa exagerada + copete político
            if (y >= 26 && y <= 37 && x >= 10 && x <= 21)
            {
                if (x == 10 || x == 21 || y == 26 || y == 37) return outline;
                if (y >= 34 || (y == 33 && x <= 19)) return hair;
                if (y == 31 && (x == 14 || x == 18)) return outline; // Ojos
                if (y == 28 && x >= 14 && x <= 19) return Color.white; // Sonrisa blanca de campaña
                return skin;
            }

            return Color.clear;
        });
    }

    // ─── JEFE 3: EL INFLUENCER DEL CALLEJÓN (PIXEL ART 32x40) ──────────────────
    public static Sprite GetInfluencerBossSprite(int frame)
    {
        // frame: 0=Idle/WalkA, 1=WalkB, 2=RingLightFlash, 3=SelfieDash, 4=Defeat
        return GetOrCreate($"InfluencerBoss_F{frame}", 32, 40, 18f, new Vector2(0.5f, 0f), (x, y) =>
        {
            Color outline = new Color(0.08f, 0.06f, 0.14f, 1f);
            Color hoodie  = new Color(0.58f, 0.14f, 0.86f, 1f); // Sudadera neón morada
            Color cyan    = new Color(0.14f, 0.92f, 0.96f, 1f); // Acentos cian neón
            Color magenta = new Color(1.00f, 0.20f, 0.62f, 1f);
            Color jeans   = new Color(0.14f, 0.16f, 0.24f, 1f);
            Color skin    = new Color(0.95f, 0.78f, 0.64f, 1f);
            Color glasses = new Color(0.06f, 0.06f, 0.10f, 1f);

            if (frame == 4) // Derrotado sin señal
            {
                if (y >= 1 && y <= 14 && x >= 7 && x <= 25)
                {
                    if (x == 7 || x == 25 || y == 1 || y == 14) return outline;
                    if (y >= 9 && x >= 12 && x <= 20) return skin;
                    return hoodie;
                }
                return Color.clear;
            }

            // Ring light o Selfie Stick extendido
            if ((frame == 2 || frame == 3) && x >= 22 && x <= 31)
            {
                if (frame == 2)
                {
                    float r = Vector2.Distance(new Vector2(x, y), new Vector2(27f, 24f));
                    if (r >= 2.5f && r <= 4.3f) return Color.white;
                    if (r < 2.5f) return cyan;
                }
                else if (y >= 19 && y <= 22)
                {
                    return x >= 29 ? magenta : cyan; // Selfie stick gigante
                }
            }

            // Piernas con tenis neón
            int leftX = frame == 1 ? 10 : 12;
            int rightX = frame == 1 ? 20 : 18;
            if (y >= 0 && y <= 10)
            {
                if (Mathf.Abs(x - leftX) <= 2 || Mathf.Abs(x - rightX) <= 2)
                    return y <= 2 ? cyan : jeans;
            }

            // Sudadera streetwear
            if (y >= 10 && y <= 25 && x >= 9 && x <= 22)
            {
                if (x == 9 || x == 22 || y == 10 || y == 25) return outline;
                if (y == 17 || y == 18) return magenta;
                if (x == 15 || x == 16) return cyan;
                return hoodie;
            }

            // Cabeza con lentes oscuros y gorra/cabello pintado
            if (y >= 26 && y <= 37 && x >= 10 && x <= 21)
            {
                if (x == 10 || x == 21 || y == 26 || y == 37) return outline;
                if (y >= 34) return cyan; // Cabello teñido viral
                if (y >= 30 && y <= 32 && x >= 12 && x <= 20)
                    return (x == 14 || x == 18) && y == 31 ? magenta : glasses;
                return skin;
            }

            return Color.clear;
        });
    }

    // ─── ESBIRRO NIVEL 2: GUERRERO VASIJA DE TALAVERA (28x32) ──────────────────
    public static Sprite GetTalaveraEnemySprite(int frame)
    {
        // frame: 0 = paso A, 1 = paso B, 2 = cerámica rota (derrota)
        return GetOrCreate($"TalaveraEnemyV2_F{frame}", 28, 32, 22f, new Vector2(0.5f, 0f), (x, y) =>
        {
            Color outline = new Color(0.08f, 0.12f, 0.26f, 1f);
            Color glaze   = new Color(0.95f, 0.96f, 0.99f, 1f);
            Color cobalt  = new Color(0.10f, 0.34f, 0.78f, 1f);
            Color gold    = new Color(0.96f, 0.74f, 0.22f, 1f);
            Color clay    = new Color(0.76f, 0.34f, 0.20f, 1f);
            Color eye     = new Color(1.00f, 0.30f, 0.12f, 1f);

            if (frame == 2) // Vasija quebrada en pedazos
            {
                if (y <= 10 && x >= 3 && x <= 24 && ((x + y) % 3 != 0))
                {
                    if (y == 0 || y == 10 || x == 3 || x == 24) return outline;
                    return ((x / 3 + y / 3) % 2 == 0) ? cobalt : glaze;
                }
                return Color.clear;
            }

            // Patas de barro caminando
            int step = frame == 0 ? 0 : 2;
            if (y <= 6)
            {
                if ((x >= 7 + step && x <= 11 + step) || (x >= 16 - step && x <= 20 - step))
                    return y <= 1 ? outline : clay;
                return Color.clear;
            }

            // Asas laterales doradas de la vasija
            if (y >= 13 && y <= 22)
            {
                if ((x >= 2 && x <= 5) || (x >= 22 && x <= 25))
                {
                    bool ring = (y == 13 || y == 22 || x == 2 || x == 25);
                    if (ring) return gold;
                }
            }

            // Cuerpo abombado de cerámica azul y blanca de Talavera
            if (y >= 7 && y <= 26)
            {
                float ny = (y - 16.5f) / 10.5f;
                float halfW = Mathf.Sqrt(Mathf.Max(0.15f, 1f - ny * ny * 0.55f)) * 9.5f;
                float dx = Mathf.Abs(x - 13.5f);
                if (dx <= halfW)
                {
                    if (dx > halfW - 1.3f || y == 7 || y == 26) return outline;
                    // Ojos encendidos de horno
                    if (y >= 19 && y <= 21 && ((x >= 9 && x <= 11) || (x >= 16 && x <= 18)))
                        return (y == 20 && (x == 10 || x == 17)) ? Color.yellow : eye;
                    // Ceño fruncido
                    if (y == 22 && ((x >= 9 && x <= 12) || (x >= 15 && x <= 18)))
                        return outline;
                    // Friso floral de Talavera en el vientre
                    if (y >= 11 && y <= 15)
                        return ((x + y) % 2 == 0) ? cobalt : ((x % 4 == 0) ? gold : glaze);
                    if (y == 10 || y == 16) return gold;
                    return glaze;
                }
            }

            // Borde/corona superior de la vasija
            if (y >= 27 && y <= 30 && x >= 7 && x <= 20)
            {
                if (x == 7 || x == 20 || y == 30) return outline;
                return y == 28 ? gold : cobalt;
            }

            return Color.clear;
        });
    }

    // ─── ESBIRRO NIVEL 3A: MINI-DRON PAPARAZZI (32x24) ─────────────────────────
    public static Sprite GetDroneEnemySprite(int frame)
    {
        // frame: 0 = hélice A, 1 = hélice B, 2 = dron averiado (derrota)
        return GetOrCreate($"DroneEnemyV2_F{frame}", 32, 24, 24f, new Vector2(0.5f, 0.5f), (x, y) =>
        {
            Color outline = new Color(0.06f, 0.06f, 0.12f, 1f);
            Color chassis = new Color(0.24f, 0.26f, 0.38f, 1f);
            Color silver  = new Color(0.68f, 0.74f, 0.84f, 1f);
            Color cyan    = new Color(0.14f, 0.94f, 1.00f, 1f);
            Color magenta = new Color(1.00f, 0.18f, 0.62f, 1f);
            Color recRed  = new Color(1.00f, 0.14f, 0.20f, 1f);

            if (frame == 2) // Chispas y carcasa rota
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 11.5f));
                if (d <= 8.5f && ((x + y) % 2 == 0))
                    return d < 4f ? Color.yellow : (d < 6.5f ? magenta : chassis);
                return Color.clear;
            }

            // Hélices giratorias neón izquierda y derecha
            int rotorMin = frame == 0 ? 1 : 3;
            int rotorMax = frame == 0 ? 11 : 9;
            if (y == 20 || y == 21)
            {
                if ((x >= rotorMin && x <= rotorMax) || (x >= 31 - rotorMax && x <= 31 - rotorMin))
                    return y == 21 ? cyan : Color.white;
            }
            // Soportes de hélices
            if (y >= 15 && y <= 19 && ((x >= 5 && x <= 6) || (x >= 25 && x <= 26)))
                return silver;

            // Brazos laterales del dron
            if (y >= 13 && y <= 15 && x >= 4 && x <= 27)
                return y == 14 ? silver : outline;

            // Cuerpo central blindado con lente de cámara rojo REC
            if (y >= 7 && y <= 16 && x >= 8 && x <= 23)
            {
                if (x == 8 || x == 23 || y == 7 || y == 16) return outline;
                // Aro de luz alrededor del lente
                float lr = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 11.5f));
                if (lr <= 2.3f) return frame == 0 ? recRed : Color.white;
                if (lr <= 3.6f) return cyan;
                if (y == 14) return magenta;
                return chassis;
            }

            // Cámara colgante inferior
            if (y >= 3 && y <= 6 && x >= 12 && x <= 19)
            {
                if (x == 12 || x == 19 || y == 3) return outline;
                return (x >= 14 && x <= 17) ? recRed : silver;
            }

            return Color.clear;
        });
    }

    // ─── ESBIRRO NIVEL 3B: FAN DE SELFIES DEL CALLEJÓN (28x34) ─────────────────
    public static Sprite GetSelfieFanEnemySprite(int frame)
    {
        // frame: 0 = caminando A, 1 = caminando B, 2 = derrotado
        return GetOrCreate($"SelfieFanEnemy_F{frame}", 28, 34, 22f, new Vector2(0.5f, 0f), (x, y) =>
        {
            Color outline = new Color(0.08f, 0.06f, 0.14f, 1f);
            Color jacket  = new Color(0.62f, 0.18f, 0.84f, 1f); // Chamarra neón morada
            Color cyan    = new Color(0.16f, 0.92f, 0.98f, 1f);
            Color pants   = new Color(0.15f, 0.18f, 0.28f, 1f);
            Color skin    = new Color(0.94f, 0.76f, 0.62f, 1f);
            Color phone   = new Color(0.95f, 0.98f, 1.00f, 1f);
            Color magenta = new Color(1.00f, 0.22f, 0.60f, 1f);

            if (frame == 2)
            {
                if (y <= 10 && x >= 5 && x <= 22)
                    return (x == 5 || x == 22 || y == 0 || y == 10) ? outline : jacket;
                return Color.clear;
            }

            int step = frame == 0 ? 0 : 2;
            // Piernas con tenis
            if (y <= 9)
            {
                if ((x >= 8 + step && x <= 11 + step) || (x >= 16 - step && x <= 19 - step))
                    return y <= 2 ? cyan : pants;
            }

            // Torso chamarra urbana
            if (y >= 10 && y <= 21 && x >= 7 && x <= 19)
            {
                if (x == 7 || x == 19 || y == 10 || y == 21) return outline;
                if (y == 15 || y == 16) return magenta;
                return jacket;
            }

            // Brazo extendido sosteniendo celular brillante (hacia la derecha, se voltea con flipX)
            if (y >= 15 && y <= 18 && x >= 20 && x <= 23)
                return jacket;
            if (y >= 16 && y <= 24 && x >= 22 && x <= 26)
            {
                if (x == 22 || x == 26 || y == 16 || y == 24) return outline;
                return frame == 0 ? phone : cyan; // Pantalla/flash del celular
            }

            // Cabeza con gorra y lentes oscuros
            if (y >= 22 && y <= 31 && x >= 8 && x <= 18)
            {
                if (x == 8 || x == 18 || y == 22 || y == 31) return outline;
                if (y >= 29) return magenta; // Gorra llamativa
                if (y == 26 || y == 27) return x >= 11 ? outline : skin; // Lentes oscuros
                return skin;
            }

            return Color.clear;
        });
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
}
