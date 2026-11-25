using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Animación cinemática del Gran Final al completar el Nivel 3:
/// Celebra el triunfo de Finn devolviendo la paz a las Fiestas Patronales de Dolores Hidalgo
/// y Guanajuato con 4 actos animados en tiempo real:
/// - Acto I: Regreso nocturno al Jardín Principal con la Parroquia y papel picado.
/// - Acto II: Los 3 Jefes (Vendedor Viral, Munícipe Carismático e Influencer del Callejón) rendidos.
/// - Acto III: Encendido del Castillo de Fuegos Artificiales, ruedas de pólvora y globos de cantoya.
/// - Acto IV: Mensaje final de Fiestas Patronales Salvadas con botón para continuar a la Pantalla de Victoria.
/// </summary>
public class GrandFinaleCutscene : MonoBehaviour
{
    private class SkyFirework
    {
        public float cx, cy, age, maxAge;
        public Color colA, colB;
    }

    private class SkyLantern
    {
        public float x, y, vy, phase, size;
    }

    private class ConfettiSpec
    {
        public float x, y, vx, vy, size;
        public Color col;
    }

    public static bool IsPlaying { get; private set; } = false;

    private Action onFinishedCallback;
    private float elapsed = 0f;
    private bool isFinished = false;
    private bool canSkipOrContinue = false;
    private float nextBurstTime = 0f;
    private float fadeOutAlpha = 0f;
    private float fadeInAlpha = 1f;
    private bool finaleMusicStarted = false;

    // Texturas y sprites precargados una sola vez en Awake (cero asignaciones en OnGUI)
    private Texture2D nightBgTex;
    private Texture2D fiestaPazTex;
    private Texture2D papelPicadoTex;
    private Texture2D groundTileTex;
    private Texture2D plankNormalTex;
    private Texture2D plankHoverTex;
    private Texture2D plankPressedTex;
    private Texture2D whiteTex;
    private Texture2D darkTex;
    private Texture2D goldTex;
    private Texture2D woodDarkTex;
    private Texture2D woodMidTex;
    private Texture2D greenMexTex;
    private Texture2D redMexTex;
    private Texture2D speechBgTex;

    private Sprite finnSprite;
    private Sprite boss1Defeat;
    private Sprite boss2Defeat;
    private Sprite boss3Defeat;

    private GUIStyle plankBtnStyle;

    private readonly List<SkyFirework> fireworks = new List<SkyFirework>();
    private readonly List<SkyLantern> lanterns = new List<SkyLantern>();
    private readonly List<ConfettiSpec> confetti = new List<ConfettiSpec>();

    public static void Play(string finalBossTitle, string finalVictoryMessage, Action onFinished)
    {
        GameObject go = new GameObject("_GrandFinaleCutscene");
        GrandFinaleCutscene cutscene = go.AddComponent<GrandFinaleCutscene>();
        cutscene.onFinishedCallback = onFinished;
        cutscene.StartCoroutine(cutscene.RunFinaleSequence());
    }

    private void Awake()
    {
        whiteTex    = MakeTex(Color.white);
        darkTex     = MakeTex(new Color(0.03f, 0.02f, 0.07f, 1f));
        goldTex     = MakeTex(new Color(0.955f, 0.785f, 0.340f, 1f));
        woodDarkTex = MakeTex(new Color(0.28f, 0.16f, 0.09f, 1f));
        woodMidTex  = MakeTex(new Color(0.45f, 0.27f, 0.15f, 1f));
        greenMexTex = MakeTex(new Color(0.08f, 0.56f, 0.26f, 1f));
        redMexTex   = MakeTex(new Color(0.84f, 0.16f, 0.18f, 1f));
        speechBgTex = MakeTex(new Color(0.97f, 0.95f, 0.88f, 0.96f));

        nightBgTex      = LoadPng("Images/Backgrounds/Bg_Nivel1_DoloresNoche.png", new Color(0.07f, 0.05f, 0.15f, 1f));
        fiestaPazTex    = LoadPng("Images/Cutscenes/Cutscene_01_FiestaPaz.png", new Color(0.10f, 0.07f, 0.18f, 1f));
        papelPicadoTex  = LoadPng("Images/UI/UI_PapelPicado_Garland.png", Color.clear);
        groundTileTex   = LoadPng("Images/GroundTiles/Tile_N1_Cantera_Top.png", new Color(0.28f, 0.22f, 0.25f, 1f));
        plankNormalTex  = LoadPng("Images/UI/UI_PlankButton_Normal.png", new Color(0.72f, 0.52f, 0.32f, 1f));
        plankHoverTex   = LoadPng("Images/UI/UI_PlankButton_Hover.png", new Color(0.84f, 0.62f, 0.38f, 1f));
        plankPressedTex = LoadPng("Images/UI/UI_PlankButton_Pressed.png", new Color(0.58f, 0.40f, 0.24f, 1f));

        PlayerMovement pm = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
        if (pm != null)
        {
            pm.SetControlLocked(true);
            SpriteRenderer psr = pm.GetComponent<SpriteRenderer>();
            if (psr != null && psr.sprite != null)
                finnSprite = psr.sprite;
        }
        if (finnSprite == null)
        {
            Texture2D fTex = LoadPng("Images/Nivel1/FinnSprite.png", Color.clear);
            if (fTex != null && fTex.width > 16)
                finnSprite = Sprite.Create(fTex, new Rect(0, 0, Mathf.Min(32, fTex.width), Mathf.Min(32, fTex.height)), new Vector2(0.5f, 0f), 32f);
        }

        boss1Defeat = ProceduralPixelArt.GetVendorBossSprite(6);
        boss2Defeat = ProceduralPixelArt.GetMayorBossSprite(4);
        boss3Defeat = ProceduralPixelArt.GetInfluencerBossSprite(4);

        // Globos de cantoya iniciales
        for (int i = 0; i < 14; i++)
        {
            lanterns.Add(new SkyLantern
            {
                x = UnityEngine.Random.Range(0.06f, 0.94f),
                y = UnityEngine.Random.Range(0.25f, 0.95f),
                vy = UnityEngine.Random.Range(0.035f, 0.068f),
                phase = UnityEngine.Random.Range(0f, 6.28f),
                size = UnityEngine.Random.Range(14f, 26f)
            });
        }

        // Lluvia de confeti / papel picado
        Color[] fiestaCols =
        {
            new Color(0.12f, 0.72f, 0.32f, 1f),
            new Color(0.98f, 0.96f, 0.92f, 1f),
            new Color(0.88f, 0.22f, 0.22f, 1f),
            new Color(0.98f, 0.80f, 0.22f, 1f),
            new Color(0.95f, 0.30f, 0.68f, 1f)
        };
        for (int i = 0; i < 65; i++)
        {
            confetti.Add(new ConfettiSpec
            {
                x = UnityEngine.Random.Range(0f, Screen.width),
                y = UnityEngine.Random.Range(-Screen.height * 0.4f, Screen.height),
                vx = UnityEngine.Random.Range(-40f, 40f),
                vy = UnityEngine.Random.Range(90f, 210f),
                size = UnityEngine.Random.Range(7f, 13f),
                col = fiestaCols[i % fiestaCols.Length]
            });
        }
    }

    private IEnumerator RunFinaleSequence()
    {
        IsPlaying = true;
        PauseMenu.BlockPauseDuringTransition = true;
        PlayerMovement pm = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
        if (pm != null)
            pm.SetControlLocked(true);

        Time.timeScale = 0f;
        ProceduralSoundManager.Instance?.PlayVictoryFanfare();

        float prevElapsed = 0f;
        nextBurstTime = 9.0f;

        while (!isFinished)
        {
            float dt = Time.unscaledDeltaTime;
            elapsed += dt;

            if (fadeInAlpha > 0f)
                fadeInAlpha = Mathf.Max(0f, fadeInAlpha - dt * 1.4f);

            if (!finaleMusicStarted && elapsed >= 1.55f)
            {
                finaleMusicStarted = true;
                ProceduralSoundManager.Instance?.PlayFinaleMusic();
            }

            // Actualizar globos de cantoya y confeti
            for (int i = 0; i < lanterns.Count; i++)
            {
                lanterns[i].y -= lanterns[i].vy * dt;
                if (lanterns[i].y < -0.10f)
                {
                    lanterns[i].y = 0.85f;
                    lanterns[i].x = UnityEngine.Random.Range(0.06f, 0.94f);
                }
            }

            for (int i = 0; i < confetti.Count; i++)
            {
                confetti[i].x += (confetti[i].vx + Mathf.Sin(elapsed * 3.5f + i) * 30f) * dt;
                confetti[i].y += confetti[i].vy * dt;
                if (confetti[i].y > Screen.height + 20f)
                {
                    confetti[i].y = -15f;
                    confetti[i].x = UnityEngine.Random.Range(0f, Screen.width);
                }
            }

            // Explosiones de fuegos artificiales en Acto III y Acto IV
            if (elapsed >= 8.8f && elapsed >= nextBurstTime)
            {
                SpawnFinaleFirework();
                ProceduralSoundManager.Instance?.PlayFireworkBurst();
                nextBurstTime = elapsed + (elapsed < 13.5f ? 0.52f : 0.72f);
            }

            // Saltos alegres sonoros en Acto III
            if (prevElapsed < 9.4f && elapsed >= 9.4f) ProceduralSoundManager.Instance?.PlayJump();
            if (prevElapsed < 11.2f && elapsed >= 11.2f) ProceduralSoundManager.Instance?.PlayJump();

            for (int i = fireworks.Count - 1; i >= 0; i--)
            {
                fireworks[i].age += dt;
                if (fireworks[i].age >= fireworks[i].maxAge)
                    fireworks.RemoveAt(i);
            }

            if (elapsed >= 0.9f)
                canSkipOrContinue = true;

            bool escPressed = WasKeyPressed(Key.Escape, KeyCode.Escape);
            bool spacePressed = WasKeyPressed(Key.Space, KeyCode.Space);
            bool enterPressed = WasKeyPressed(Key.Enter, KeyCode.Return) || WasKeyPressed(Key.NumpadEnter, KeyCode.KeypadEnter);

            if (canSkipOrContinue && (escPressed ||
                (elapsed >= 13.6f && (spacePressed || enterPressed))))
            {
                TriggerFinish();
                break;
            }

            // Permitir saltar los primeros actos rápidamente si el jugador presiona Espacio antes del Acto IV
            if (canSkipOrContinue && elapsed < 13.5f && spacePressed)
            {
                elapsed = 13.6f;
                SpawnFinaleFirework();
                ProceduralSoundManager.Instance?.PlayButtonClick();
            }

            prevElapsed = elapsed;
            yield return null;
        }

        // Fundido suave hacia la Pantalla de Victoria
        float f = 0f;
        while (f < 0.45f)
        {
            f += Time.unscaledDeltaTime;
            fadeOutAlpha = Mathf.Clamp01(f / 0.45f);
            yield return null;
        }

        IsPlaying = false;
        Time.timeScale = 1f;
        PauseMenu.BlockPauseDuringTransition = false;
        onFinishedCallback?.Invoke();
        Destroy(gameObject);
    }

    private void SpawnFinaleFirework()
    {
        Color[] palette =
        {
            new Color(0.14f, 0.86f, 0.36f, 1f), // Verde fiesta
            new Color(1.00f, 0.96f, 0.82f, 1f), // Blanco brillante
            new Color(0.95f, 0.24f, 0.24f, 1f), // Rojo fiesta
            new Color(1.00f, 0.82f, 0.22f, 1f), // Dorado
            new Color(0.96f, 0.28f, 0.72f, 1f), // Rosa mexicano
            new Color(0.24f, 0.92f, 1.00f, 1f)  // Azul talavera neón
        };
        fireworks.Add(new SkyFirework
        {
            cx = UnityEngine.Random.Range(0.14f, 0.86f) * Screen.width,
            cy = UnityEngine.Random.Range(0.14f, 0.48f) * Screen.height,
            age = 0f,
            maxAge = UnityEngine.Random.Range(0.95f, 1.35f),
            colA = palette[UnityEngine.Random.Range(0, palette.Length)],
            colB = palette[UnityEngine.Random.Range(0, palette.Length)]
        });
    }

    private void TriggerFinish()
    {
        if (isFinished)
            return;
        isFinished = true;
        ProceduralSoundManager.Instance?.PlayButtonClick();
    }

    private void OnDestroy()
    {
        IsPlaying = false;
        if (!isFinished)
        {
            Time.timeScale = 1f;
            PauseMenu.BlockPauseDuringTransition = false;
        }
    }

    private void OnGUI()
    {
        if (whiteTex == null)
            return;

        GUI.depth = -100;
        float sw = Screen.width;
        float sh = Screen.height;
        float floorY = sh * 0.76f;

        Color prev = GUI.color;

        // 1. Fondo nocturno de Dolores Hidalgo con ligero paneo cinematográfico
        Texture2D bg = elapsed >= 8.8f && fiestaPazTex != null ? fiestaPazTex : nightBgTex;
        float panOffset = Mathf.Sin(elapsed * 0.18f) * 28f;
        GUI.DrawTexture(new Rect(-35f + panOffset, -15f, sw + 70f, floorY + 25f), bg, ScaleMode.ScaleAndCrop);
        GUI.color = new Color(0.04f, 0.02f, 0.09f, elapsed >= 8.8f ? 0.28f : 0.42f);
        GUI.DrawTexture(new Rect(0f, 0f, sw, floorY), whiteTex);
        GUI.color = prev;

        // 2. Globos de cantoya flotando hacia el cielo desde el Acto I en adelante
        if (elapsed >= 1.5f)
        {
            float lanternAlpha = Mathf.Clamp01((elapsed - 1.5f) / 2.0f) * 0.90f;
            for (int i = 0; i < lanterns.Count; i++)
            {
                SkyLantern l = lanterns[i];
                float lx = l.x * sw + Mathf.Sin(elapsed * 1.4f + l.phase) * 14f;
                float ly = l.y * floorY;
                GUI.color = new Color(1.0f, 0.56f, 0.14f, lanternAlpha * 0.35f);
                GUI.DrawTexture(new Rect(lx - l.size * 0.5f, ly - l.size * 0.6f, l.size * 1.8f, l.size * 2.0f), whiteTex);
                GUI.color = new Color(1.0f, 0.82f, 0.30f, lanternAlpha);
                GUI.DrawTexture(new Rect(lx, ly, l.size * 0.85f, l.size), whiteTex);
                GUI.color = new Color(1.0f, 0.98f, 0.75f, lanternAlpha);
                GUI.DrawTexture(new Rect(lx + l.size * 0.2f, ly + l.size * 0.55f, l.size * 0.45f, l.size * 0.35f), whiteTex);
            }
            GUI.color = prev;
        }

        // 3. Explosiones de fuegos artificiales en el cielo nocturno
        for (int i = 0; i < fireworks.Count; i++)
        {
            SkyFirework fw = fireworks[i];
            float p = Mathf.Clamp01(fw.age / fw.maxAge);
            float radius = Mathf.Lerp(10f, 130f, 1f - Mathf.Pow(1f - p, 2f));
            float alpha = 1f - Mathf.Pow(p, 1.8f);
            for (int s = 0; s < 14; s++)
            {
                float ang = (s / 14f) * Mathf.PI * 2f + p * 0.5f;
                float dist = radius * (s % 2 == 0 ? 1f : 0.62f);
                float sx = fw.cx + Mathf.Cos(ang) * dist;
                float sy = fw.cy + Mathf.Sin(ang) * dist + (p * p * 35f);
                Color sc = (s % 2 == 0) ? fw.colA : fw.colB;
                GUI.color = new Color(sc.r, sc.g, sc.b, alpha);
                GUI.DrawTexture(new Rect(sx - 4f, sy - 4f, 8f, 8f), whiteTex);
            }
        }
        GUI.color = prev;

        // 4. Guirnaldas de papel picado en lo alto moviéndose con la brisa
        if (papelPicadoTex != null && papelPicadoTex.width > 8)
        {
            float sway = Mathf.Sin(elapsed * 2.2f) * 8f;
            float tileW = 360f;
            for (float gx = -tileW * 0.5f; gx < sw + tileW; gx += tileW)
                GUI.DrawTexture(new Rect(gx + sway, 46f, tileW, 74f), papelPicadoTex);
        }

        // 5. Franja de cantera colonial de la plaza en la parte inferior
        float tilePx = 64f;
        for (float gx = 0f; gx < sw + tilePx; gx += tilePx)
            GUI.DrawTexture(new Rect(gx, floorY, tilePx, tilePx), groundTileTex);
        GUI.color = new Color(0.11f, 0.08f, 0.14f, 1f);
        GUI.DrawTexture(new Rect(0f, floorY + tilePx, sw, sh - (floorY + tilePx)), whiteTex);
        GUI.color = prev;

        // 6. Castillo de Fuegos Artificiales (Acto III & IV)
        if (elapsed >= 8.8f)
        {
            float rise = Mathf.Clamp01((elapsed - 8.8f) / 1.0f);
            float castleX = sw * 0.72f;
            float castleTopY = floorY - 310f * rise;
            float castleH = floorY - castleTopY;

            // Estructura de madera del Castillo
            GUI.DrawTexture(new Rect(castleX - 28f, castleTopY + 30f, 8f, Mathf.Max(0f, castleH - 30f)), woodDarkTex);
            GUI.DrawTexture(new Rect(castleX + 20f, castleTopY + 30f, 8f, Mathf.Max(0f, castleH - 30f)), woodDarkTex);
            for (float barY = castleTopY + 50f; barY < floorY - 20f; barY += 58f)
                GUI.DrawTexture(new Rect(castleX - 38f, barY, 76f, 6f), woodMidTex);

            // Remate tricolor en la cima
            if (rise > 0.8f)
            {
                GUI.DrawTexture(new Rect(castleX - 30f, castleTopY + 6f, 20f, 22f), greenMexTex);
                GUI.DrawTexture(new Rect(castleX - 10f, castleTopY + 6f, 20f, 22f), whiteTex);
                GUI.DrawTexture(new Rect(castleX + 10f, castleTopY + 6f, 20f, 22f), redMexTex);
            }

            // 3 Ruedas giratorias de pólvora multicolor
            if (rise > 0.65f)
            {
                DrawCastilloWheel(castleX, castleTopY + 86f, 44f, elapsed * 5.2f, goldTex, redMexTex);
                DrawCastilloWheel(castleX, castleTopY + 178f, 52f, -elapsed * 4.4f, greenMexTex, goldTex);
                DrawCastilloWheel(castleX, castleTopY + 258f, 38f, elapsed * 6.0f, goldTex, whiteTex);
            }
        }

        // 7. Personajes en escena: Finn + Los 3 Jefes derrotados
        DrawCharactersStage(sw, floorY);

        // 8. Lluvia festiva de papel picado / confeti desde Acto III
        if (elapsed >= 8.8f)
        {
            for (int i = 0; i < confetti.Count; i++)
            {
                ConfettiSpec c = confetti[i];
                GUI.color = c.col;
                GUI.DrawTexture(new Rect(c.x, c.y, c.size, c.size * 0.65f), whiteTex);
            }
            GUI.color = prev;
        }

        // 9. Subtítulos / Títulos de Puro Texto con contorno oscuro (sin cajas cuadradas)
        DrawActCaptions(sw, sh, floorY);

        // 10. Hint discreto arriba a la derecha + Fundidos de entrada/salida
        GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight
        };
        DrawOutlinedLabel(new Rect(sw - 280f, 16f, 256f, 26f), "SALTAR [ESC / ESPACIO]", hintStyle,
            new Color(0.95f, 0.88f, 0.72f, 0.90f), new Color(0.03f, 0.02f, 0.06f, 0.95f), 1.5f);

        float blackAlpha = Mathf.Max(fadeInAlpha, fadeOutAlpha);
        if (blackAlpha > 0.005f)
        {
            GUI.color = new Color(1f, 1f, 1f, blackAlpha);
            GUI.DrawTexture(new Rect(0f, 0f, sw, sh), darkTex);
            GUI.color = prev;
        }
    }

    private void DrawCharactersStage(float sw, float floorY)
    {
        // Posición de Finn: camina desde la izquierda al centro en Acto I y luego celebra
        float finnNormX = elapsed < 3.6f
            ? Mathf.Lerp(0.08f, 0.30f, Mathf.Clamp01(elapsed / 3.4f))
            : (elapsed >= 8.8f ? 0.26f : 0.30f);

        float walkBob = elapsed < 3.4f ? Mathf.Abs(Mathf.Sin(elapsed * 12f)) * 8f : 0f;
        float hopOffset = 0f;
        if (elapsed >= 8.8f)
        {
            float cycle = Mathf.Repeat(elapsed - 8.8f, 1.8f);
            if (cycle < 0.55f)
                hopOffset = Mathf.Sin((cycle / 0.55f) * Mathf.PI) * 42f;
        }

        float finnW = 86f;
        float finnH = 86f;
        float finnX = finnNormX * sw - finnW * 0.5f;
        float finnY = floorY - finnH - walkBob - hopOffset;
        if (finnSprite != null)
            DrawSprite(new Rect(finnX, finnY, finnW, finnH), finnSprite, false);

        // En Acto II, III y IV aparecen los 3 Jefes derrotados en la plaza
        if (elapsed >= 4.0f)
        {
            float slideIn = Mathf.Clamp01((elapsed - 4.0f) / 0.75f);
            float baseShift = Mathf.Lerp(sw * 0.45f, 0f, 1f - Mathf.Pow(1f - slideIn, 3f));

            float b1X = sw * 0.48f + baseShift;
            float b2X = sw * 0.61f + baseShift;
            float b3X = sw * 0.74f + baseShift;

            // En Acto III y IV movemos un poco a los jefes hacia el centro-izquierda para dejar lucir el Castillo
            if (elapsed >= 8.8f)
            {
                float moveT = Mathf.Clamp01((elapsed - 8.8f) / 0.9f);
                b1X = Mathf.Lerp(sw * 0.48f, sw * 0.40f, moveT);
                b2X = Mathf.Lerp(sw * 0.61f, sw * 0.50f, moveT);
                b3X = Mathf.Lerp(sw * 0.74f, sw * 0.60f, moveT);
            }

            DrawDefeatedBoss(boss1Defeat, b1X, floorY, 108f, 118f, elapsed * 4.5f);
            DrawDefeatedBoss(boss2Defeat, b2X, floorY, 96f, 116f, elapsed * 4.5f + 2.0f);
            DrawDefeatedBoss(boss3Defeat, b3X, floorY, 96f, 116f, elapsed * 4.5f + 4.0f);

            // Globos de diálogo escalonados en Acto II
            if (elapsed >= 4.8f && elapsed < 8.7f)
            {
                if (elapsed < 6.4f)
                    DrawSpeechBubble(b1X, floorY - 120f, "¡Ya pues marchante, les regalo los garbanzos!");
                else if (elapsed < 7.6f)
                    DrawSpeechBubble(b2X, floorY - 120f, "¡Prometo... ahora sí portarme bien!");
                else
                    DrawSpeechBubble(b3X, floorY - 120f, "¡Sin señal... me desconecto del caos!");
            }
        }
    }

    private void DrawDefeatedBoss(Sprite sp, float centerX, float floorY, float w, float h, float starPhase)
    {
        if (sp != null)
            DrawSprite(new Rect(centerX - w * 0.5f, floorY - h, w, h), sp, true);

        // Estrellitas doradas de mareo girando sobre su cabeza
        Color prev = GUI.color;
        GUI.color = new Color(1f, 0.90f, 0.25f, 0.95f);
        for (int s = 0; s < 3; s++)
        {
            float a = starPhase + s * (Mathf.PI * 2f / 3f);
            float sx = centerX + Mathf.Cos(a) * 26f - 4f;
            float sy = (floorY - h * 0.78f) + Mathf.Sin(a) * 7f - 4f;
            GUI.DrawTexture(new Rect(sx, sy, 8f, 8f), whiteTex);
        }
        GUI.color = prev;
    }

    private void DrawCastilloWheel(float cx, float cy, float radius, float angle, Texture2D colA, Texture2D colB)
    {
        for (int i = 0; i < 8; i++)
        {
            float a = angle + i * (Mathf.PI * 0.25f);
            float px = cx + Mathf.Cos(a) * radius;
            float py = cy + Mathf.Sin(a) * radius;
            Texture2D tex = (i % 2 == 0) ? colA : colB;
            GUI.DrawTexture(new Rect(px - 5f, py - 5f, 10f, 10f), tex);

            float mx = cx + Mathf.Cos(a) * (radius * 0.55f);
            float my = cy + Mathf.Sin(a) * (radius * 0.55f);
            GUI.DrawTexture(new Rect(mx - 3f, my - 3f, 6f, 6f), whiteTex);
        }
        GUI.DrawTexture(new Rect(cx - 7f, cy - 7f, 14f, 14f), goldTex);
    }

    private void DrawSpeechBubble(float centerX, float topY, string msg)
    {
        GUIStyle st = new GUIStyle(GUI.skin.box)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            padding = new RectOffset(8, 8, 5, 5)
        };
        st.normal.textColor = new Color(0.14f, 0.09f, 0.06f, 1f);
        st.normal.background = speechBgTex;
        GUI.Box(new Rect(centerX - 135f, topY - 48f, 270f, 44f), msg, st);
    }

    private void DrawActCaptions(float sw, float sh, float floorY)
    {
        Color gold = new Color(0.955f, 0.785f, 0.340f, 1f);
        Color cream = new Color(0.965f, 0.940f, 0.875f, 1f);
        Color outline = new Color(0.03f, 0.02f, 0.06f, 0.96f);

        if (elapsed >= 13.5f)
        {
            // Acto IV: Gran Cierre de Fiestas Patronales Salvadas (puro texto + botón tablón)
            GUIStyle headStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 46,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            DrawOutlinedLabel(new Rect(40f, sh * 0.16f, sw - 80f, 62f), "¡FIESTAS PATRONALES SALVADAS!", headStyle, gold, outline, 3f);

            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            DrawOutlinedLabel(new Rect(60f, sh * 0.25f, sw - 120f, 64f),
                "Dolores Hidalgo y Guanajuato celebran en paz.\nGracias por jugar CUARTA TRANSFORMACIÓN",
                subStyle, cream, outline, 2f);

            if (plankBtnStyle == null)
            {
                plankBtnStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    border = new RectOffset(14, 14, 8, 8),
                    padding = new RectOffset(12, 12, 4, 8)
                };
                plankBtnStyle.normal.background  = plankNormalTex;
                plankBtnStyle.hover.background   = plankHoverTex;
                plankBtnStyle.active.background  = plankPressedTex;
                Color txtDark = new Color(0.14f, 0.085f, 0.055f, 1f);
                plankBtnStyle.normal.textColor = txtDark;
                plankBtnStyle.hover.textColor  = txtDark;
                plankBtnStyle.active.textColor = txtDark;
            }

            float btnW = 280f;
            float btnH = 62f;
            if (GUI.Button(new Rect((sw - btnW) * 0.5f, floorY + 72f, btnW, btnH), "MENÚ PRINCIPAL", plankBtnStyle))
            {
                TriggerFinish();
            }
            return;
        }

        string titleText;
        string bodyText;
        if (elapsed < 4.2f)
        {
            titleText = "EPÍLOGO — EL REGRESO AL JARDÍN PRINCIPAL";
            bodyText = "Tras vencer en los Callejones de Guanajuato, Finn regresa victorioso a la plaza nocturna de Dolores Hidalgo.";
        }
        else if (elapsed < 8.8f)
        {
            titleText = "LOS TRES JEFES SE RINDEN";
            bodyText = "El Vendedor Viral, El Munícipe Carismático y El Influencer del Callejón deponen su caos ante el pueblo.";
        }
        else
        {
            titleText = "¡GRAN CASTILLO DE FIESTAS PATRONALES!";
            bodyText = "Las luces, la pólvora tricolor y los globos de cantoya iluminan la noche de Dolores Hidalgo.";
        }

        GUIStyle capTitle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 26,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        DrawOutlinedLabel(new Rect(40f, floorY + 66f, sw - 80f, 36f), titleText, capTitle, gold, outline, 2.5f);

        GUIStyle capBody = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        DrawOutlinedLabel(new Rect(60f, floorY + 104f, sw - 120f, 54f), bodyText, capBody, cream, outline, 2f);
    }

    private static void DrawSprite(Rect destRect, Sprite sprite, bool flipX)
    {
        if (sprite == null || sprite.texture == null)
            return;

        Texture2D tex = sprite.texture;
        Rect r = sprite.textureRect;
        Rect uv = new Rect(r.x / tex.width, r.y / tex.height, r.width / tex.width, r.height / tex.height);
        if (flipX)
        {
            uv.x += uv.width;
            uv.width = -uv.width;
        }
        GUI.DrawTextureWithTexCoords(destRect, tex, uv, true);
    }

    private static void DrawOutlinedLabel(Rect rect, string text, GUIStyle style, Color textColor, Color outlineColor, float px)
    {
        style.normal.textColor = outlineColor;
        GUI.Label(new Rect(rect.x - px, rect.y, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x + px, rect.y, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x, rect.y - px, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x, rect.y + px, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x + px, rect.y + px * 1.4f, rect.width, rect.height), text, style);

        style.normal.textColor = textColor;
        GUI.Label(rect, text, style);
    }

    private static Texture2D LoadPng(string relativePath, Color fallback)
    {
        string fullPath = Path.Combine(Application.dataPath, relativePath);
        if (File.Exists(fullPath))
        {
            Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            if (t.LoadImage(File.ReadAllBytes(fullPath)))
                return t;
        }

        string baseName = Path.GetFileNameWithoutExtension(relativePath);
        Texture2D resTex = Resources.Load<Texture2D>("UI/" + baseName);
        if (resTex == null)
            resTex = Resources.Load<Texture2D>("Finale/" + baseName);
        if (resTex != null)
        {
            resTex.filterMode = FilterMode.Point;
            return resTex;
        }

        return MakeTex(fallback);
    }

    private static bool WasKeyPressed(Key newKey, KeyCode legacyKey)
    {
        if (Keyboard.current != null && Keyboard.current[newKey].wasPressedThisFrame)
            return true;

        try
        {
            return Input.GetKeyDown(legacyKey);
        }
        catch
        {
            return false;
        }
    }

    private static Texture2D MakeTex(Color col)
    {
        Texture2D tex = new Texture2D(2, 2);
        tex.SetPixels(new[] { col, col, col, col });
        tex.Apply();
        return tex;
    }
}
