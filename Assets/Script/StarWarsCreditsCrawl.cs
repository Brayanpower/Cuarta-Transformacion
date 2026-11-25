using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Secuencia final de créditos estilo Star Wars con texto dorado desplazándose en perspectiva 3D
/// sobre un cielo estrellado tras el Gran Final del Nivel 3. Al terminar, regresa automáticamente al MainMenu.
/// </summary>
public class StarWarsCreditsCrawl : MonoBehaviour
{
    private enum EntryKind
    {
        EpisodeHeader,
        SectionTitle,
        RoleLabel,
        PersonName,
        StoryLine,
        FinaleMark
    }

    private struct CrawlEntry
    {
        public string text;
        public EntryKind kind;
        public float trackOffset;
    }

    private struct StarSpec
    {
        public float xNorm;
        public float yNorm;
        public float size;
        public float phase;
        public float speed;
        public Color tint;
    }

    private readonly List<CrawlEntry> entries = new List<CrawlEntry>();
    private readonly List<StarSpec> stars = new List<StarSpec>();

    private Action onFinishedCallback;
    private float elapsed = 0f;
    private float crawlProgress = 0f;
    private float maxTrackOffset = 0f;
    private float postCrawlTimer = 0f;
    private float fadeOutAlpha = 0f;
    private bool isExiting = false;

    private Texture2D blackTex;
    private Texture2D whiteTex;

    private GUIStyle logoSubStyle;
    private GUIStyle sectionStyle;
    private GUIStyle roleStyle;
    private GUIStyle nameStyle;
    private GUIStyle bodyStyle;
    private GUIStyle endStyle;
    private GUIStyle hintStyle;

    public static void Play(Action onFinished)
    {
        GameObject go = new GameObject("_StarWarsCreditsCrawl");
        StarWarsCreditsCrawl crawl = go.AddComponent<StarWarsCreditsCrawl>();
        crawl.onFinishedCallback = onFinished;
        crawl.StartCoroutine(crawl.RunCrawlRoutine());
    }

    private void Awake()
    {
        blackTex = MakeTex(new Color(0.016f, 0.012f, 0.039f, 1f));
        whiteTex = MakeTex(Color.white);

        BuildStarfield();
        BuildCreditsEntries();
    }

    private void BuildStarfield()
    {
        Color[] starTints =
        {
            new Color(1.00f, 1.00f, 1.00f, 1f),
            new Color(0.84f, 0.92f, 1.00f, 1f),
            new Color(1.00f, 0.94f, 0.72f, 1f)
        };

        for (int i = 0; i < 145; i++)
        {
            stars.Add(new StarSpec
            {
                xNorm = UnityEngine.Random.value,
                yNorm = UnityEngine.Random.value,
                size  = UnityEngine.Random.Range(1.5f, 3.6f),
                phase = UnityEngine.Random.Range(0f, 6.283f),
                speed = UnityEngine.Random.Range(1.4f, 3.8f),
                tint  = starTints[i % starTints.Length]
            });
        }
    }

    private void BuildCreditsEntries()
    {
        entries.Clear();
        float cursor = 0.02f;

        void Add(string text, EntryKind kind, float extraGapBefore = 0f)
        {
            cursor += extraGapBefore;
            entries.Add(new CrawlEntry
            {
                text = text,
                kind = kind,
                trackOffset = cursor
            });

            switch (kind)
            {
                case EntryKind.EpisodeHeader:
                case EntryKind.SectionTitle:
                case EntryKind.FinaleMark:
                    cursor += 0.044f;
                    break;
                case EntryKind.PersonName:
                    cursor += 0.034f;
                    break;
                case EntryKind.RoleLabel:
                case EntryKind.StoryLine:
                default:
                    cursor += 0.028f;
                    break;
            }
        }

        Add("EPISODIO IV", EntryKind.EpisodeHeader);
        Add("LA PAZ DE LAS FIESTAS PATRONALES", EntryKind.SectionTitle);

        Add("Tras una noche épica desde la Parroquia de Dolores", EntryKind.StoryLine, 0.018f);
        Add("hasta los oscuros Callejones de Guanajuato,", EntryKind.StoryLine);
        Add("Finn derrotó a quienes sembraban el caos y el miedo.", EntryKind.StoryLine);
        Add("Con el gran castillo tricolor brillando en lo alto,", EntryKind.StoryLine, 0.012f);
        Add("la música, la tradición y la alegría regresaron al pueblo.", EntryKind.StoryLine);
        Add("¡La Cuarta Transformación de las Fiestas ha triunfado!", EntryKind.StoryLine);

        Add("PROTAGONISTAS Y JEFES", EntryKind.SectionTitle, 0.042f);
        Add("HÉROE DE LA JORNADA", EntryKind.RoleLabel, 0.010f);
        Add("Finn", EntryKind.PersonName);
        Add("GUARDIÁN DEL MERCADO — JEFE I", EntryKind.RoleLabel, 0.014f);
        Add("Don Chava — El Vendedor Viral", EntryKind.PersonName);
        Add("ALCALDE DE TALAVERA — JEFE II", EntryKind.RoleLabel, 0.014f);
        Add("El Munícipe Carismático", EntryKind.PersonName);
        Add("REY DEL CALLEJÓN — JEFE III", EntryKind.RoleLabel, 0.014f);
        Add("El Influencer del Callejón", EntryKind.PersonName);

        Add("ESCENARIOS Y MUNDOS", EntryKind.SectionTitle, 0.042f);
        Add("NIVEL 1", EntryKind.RoleLabel, 0.010f);
        Add("Jardín Principal y Parroquia de Dolores Hidalgo", EntryKind.PersonName);
        Add("NIVEL 2", EntryKind.RoleLabel, 0.014f);
        Add("Talleres de Talavera y Grandes Hornos", EntryKind.PersonName);
        Add("NIVEL 3", EntryKind.RoleLabel, 0.014f);
        Add("Callejones y Azoteas de Guanajuato", EntryKind.PersonName);

        Add("DIRECCIÓN Y DESARROLLO", EntryKind.SectionTitle, 0.042f);
        Add("CREADOR Y PROGRAMADOR PRINCIPAL", EntryKind.RoleLabel, 0.010f);
        Add("Brayanpower", EntryKind.PersonName);
        Add("DISEÑO DE NIVELES, JEFES Y CINEMÁTICAS", EntryKind.RoleLabel, 0.014f);
        Add("Equipo Cuarta Transformación", EntryKind.PersonName);

        Add("ARTE, MOTOR Y SONIDO", EntryKind.SectionTitle, 0.042f);
        Add("MOTOR GRÁFICO", EntryKind.RoleLabel, 0.010f);
        Add("Unity 6 (2D URP)", EntryKind.PersonName);
        Add("ARTE PIXEL-ART COLONIAL NOCTURNO", EntryKind.RoleLabel, 0.014f);
        Add("Ilustración Temática y Pixel Procedural en C#", EntryKind.PersonName);
        Add("MÚSICA Y EFECTOS DE SONIDO", EntryKind.RoleLabel, 0.014f);
        Add("Sintetizador Procedural Chiptune en C#", EntryKind.PersonName);

        Add("AGRADECIMIENTOS ESPECIALES", EntryKind.SectionTitle, 0.042f);
        Add("A la cultura y tradiciones de Dolores Hidalgo y Guanajuato", EntryKind.StoryLine, 0.010f);
        Add("A quienes mantuvieron viva la magia de las Fiestas Patronales", EntryKind.StoryLine);
        Add("Y ESPECIALMENTE...", EntryKind.RoleLabel, 0.022f);
        Add("¡A ti por jugar Cuarta Transformación!", EntryKind.FinaleMark);

        maxTrackOffset = cursor;
    }

    private IEnumerator RunCrawlRoutine()
    {
        PauseMenu.BlockPauseDuringTransition = true;
        PlayerMovement pm = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
        if (pm != null)
            pm.SetControlLocked(true);

        Time.timeScale = 0f;
        ProceduralSoundManager.Instance?.PlayCreditsMusic();

        const float crawlStart = 0.4f;
        const float crawlSpeed = 0.040f; // Ritmo pausado cinematográfico (~54–58s de créditos + cierre)
        float crawlEndOffset = maxTrackOffset + 0.96f;

        while (!isExiting)
        {
            float dt = Time.unscaledDeltaTime;
            elapsed += dt;

            if (elapsed >= crawlStart)
            {
                float mult = IsSpeedUpHeld() ? 3.2f : 1.0f;
                if (crawlProgress < crawlEndOffset)
                {
                    crawlProgress = Mathf.Min(crawlEndOffset, crawlProgress + dt * crawlSpeed * mult);
                }
                else
                {
                    postCrawlTimer += dt;
                    if (postCrawlTimer >= 4.4f)
                        isExiting = true;
                }
            }

            if (elapsed >= 1.2f && WasEscapePressed())
            {
                ProceduralSoundManager.Instance?.PlayButtonClick();
                isExiting = true;
            }

            yield return null;
        }

        float fadeDur = 1.6f;
        float f = 0f;
        while (f < fadeDur)
        {
            f += Time.unscaledDeltaTime;
            fadeOutAlpha = Mathf.Clamp01(f / fadeDur);
            yield return null;
        }
        fadeOutAlpha = 1f;

        Time.timeScale = 1f;
        PauseMenu.BlockPauseDuringTransition = false;
        onFinishedCallback?.Invoke();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (!isExiting)
        {
            Time.timeScale = 1f;
            PauseMenu.BlockPauseDuringTransition = false;
        }
    }

    private void EnsureStyles()
    {
        if (sectionStyle != null)
            return;

        logoSubStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        sectionStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 42,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = false
        };

        roleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = false
        };

        nameStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 36,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = false
        };

        bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 31,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = false
        };

        endStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 56,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight
        };
    }

    private void OnGUI()
    {
        if (blackTex == null || whiteTex == null)
            return;

        GUI.depth = -100;
        EnsureStyles();

        float sw = Screen.width;
        float sh = Screen.height;
        Color prevColor = GUI.color;

        // 1. Fondo espacial negro profundo con estrellas parpadeantes
        GUI.DrawTexture(new Rect(0f, 0f, sw, sh), blackTex);

        float starFieldFade = Mathf.Clamp01(elapsed / 0.8f);
        for (int i = 0; i < stars.Count; i++)
        {
            StarSpec st = stars[i];
            float twinkle = 0.42f + 0.58f * Mathf.Sin(elapsed * st.speed + st.phase);
            GUI.color = new Color(st.tint.r, st.tint.g, st.tint.b, starFieldFade * twinkle * 0.92f);
            GUI.DrawTexture(new Rect(st.xNorm * sw, st.yNorm * sh, st.size, st.size), whiteTex);
        }
        GUI.color = prevColor;

        // 2. Fase III: Créditos deslizándose en perspectiva inclinada estilo Star Wars
        if (elapsed >= 0.4f)
        {
            const float L = 2.6f;
            float pEnd = 1f / (1f + L);
            float yBottom = sh * 1.05f;
            float yHorizon = sh * 0.11f;

            for (int i = 0; i < entries.Count; i++)
            {
                CrawlEntry entry = entries[i];
                float u = crawlProgress - entry.trackOffset;
                if (u < -0.02f || u > 0.98f)
                    continue;

                float clampedU = Mathf.Max(0f, u);
                float p = 1f / (1f + L * clampedU);
                float normH = (p - pEnd) / (1f - pEnd);
                float screenY = yHorizon + (yBottom - yHorizon) * normH;

                // Escorzo vertical (scaleY < scaleX) para efecto real de plano inclinado alejándose
                float scaleX = p * 1.28f;
                float scaleY = p * 0.92f;

                float alphaIn = u < 0.05f ? Mathf.Clamp01((u + 0.02f) / 0.07f) : 1f;
                float alphaOut = u > 0.68f ? Mathf.Clamp01((0.96f - u) / 0.28f) : 1f;
                float alpha = Mathf.Min(alphaIn, alphaOut);
                if (alpha <= 0.01f)
                    continue;

                GUIStyle st;
                Color col;
                float rectW = 1280f;
                float rectH = 66f;
                switch (entry.kind)
                {
                    case EntryKind.EpisodeHeader:
                        st = sectionStyle;
                        col = new Color(1.00f, 0.91f, 0.12f, alpha);
                        break;
                    case EntryKind.SectionTitle:
                        st = sectionStyle;
                        col = new Color(1.00f, 0.91f, 0.12f, alpha);
                        break;
                    case EntryKind.FinaleMark:
                        st = sectionStyle;
                        col = new Color(1.00f, 0.95f, 0.42f, alpha);
                        break;
                    case EntryKind.PersonName:
                        st = nameStyle;
                        col = new Color(1.00f, 0.96f, 0.80f, alpha);
                        break;
                    case EntryKind.RoleLabel:
                        st = roleStyle;
                        col = new Color(0.88f, 0.72f, 0.25f, alpha);
                        break;
                    case EntryKind.StoryLine:
                    default:
                        st = bodyStyle;
                        col = new Color(1.00f, 0.91f, 0.12f, alpha);
                        break;
                }

                Matrix4x4 prevMatrix = GUI.matrix;
                GUIUtility.ScaleAroundPivot(new Vector2(scaleX, scaleY), new Vector2(sw * 0.5f, screenY));
                Color outline = new Color(0.02f, 0.01f, 0.04f, alpha * 0.95f);
                DrawOutlinedLabel(new Rect(sw * 0.5f - rectW * 0.5f, screenY - rectH * 0.5f, rectW, rectH), entry.text, st, col, outline, 2.2f);
                GUI.matrix = prevMatrix;
            }
        }

        // 5. Fase IV: Cierre estelar tras terminar todo el desplazamiento
        if (postCrawlTimer > 0.1f)
        {
            float endAlpha = Mathf.Clamp01((postCrawlTimer - 0.1f) / 1.2f);
            Color starGold = new Color(1.00f, 0.91f, 0.12f, endAlpha);
            Color subCream = new Color(0.96f, 0.94f, 0.86f, endAlpha);
            Color outline  = new Color(0.02f, 0.01f, 0.04f, endAlpha * 0.95f);

            DrawOutlinedLabel(new Rect(40f, sh * 0.39f, sw - 80f, 70f), "FIN", endStyle, starGold, outline, 3f);
            DrawOutlinedLabel(new Rect(40f, sh * 0.49f, sw - 80f, 44f), "¡GRACIAS POR JUGAR CUARTA TRANSFORMACIÓN!", logoSubStyle, subCream, outline, 2f);
        }

        // 6. Indicación discreta de controles en la esquina
        if (elapsed >= 2.0f)
        {
            DrawOutlinedLabel(
                new Rect(sw - 500f, sh - 34f, 480f, 24f),
                "MANTÉN [ESPACIO] ADELANTAR  |  [ESC] MENÚ PRINCIPAL",
                hintStyle,
                new Color(0.95f, 0.88f, 0.65f, 0.45f),
                new Color(0f, 0f, 0f, 0.70f),
                1.2f);
        }

        // 7. Fundido final suave hacia el Menú Principal
        if (fadeOutAlpha > 0.005f)
        {
            GUI.color = new Color(0f, 0f, 0f, fadeOutAlpha);
            GUI.DrawTexture(new Rect(0f, 0f, sw, sh), whiteTex);
            GUI.color = prevColor;
        }
    }

    private static bool IsSpeedUpHeld()
    {
        if (Keyboard.current != null &&
            (Keyboard.current.spaceKey.isPressed || Keyboard.current.downArrowKey.isPressed))
        {
            return true;
        }

        try
        {
            return Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.DownArrow);
        }
        catch
        {
            return false;
        }
    }

    private static bool WasEscapePressed()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            return true;

        try
        {
            return Input.GetKeyDown(KeyCode.Escape);
        }
        catch
        {
            return false;
        }
    }

    private static void DrawOutlinedLabel(Rect rect, string text, GUIStyle style, Color textColor, Color outlineColor, float px)
    {
        style.normal.textColor = outlineColor;
        GUI.Label(new Rect(rect.x - px, rect.y, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x + px, rect.y, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x, rect.y - px, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x, rect.y + px, rect.width, rect.height), text, style);

        style.normal.textColor = textColor;
        GUI.Label(rect, text, style);
    }

    private static Texture2D MakeTex(Color col)
    {
        Texture2D tex = new Texture2D(2, 2);
        tex.SetPixels(new[] { col, col, col, col });
        tex.Apply();
        return tex;
    }
}
