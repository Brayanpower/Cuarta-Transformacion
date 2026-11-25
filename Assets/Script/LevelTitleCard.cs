using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Título de zona con puro texto estilizado estilo Sonic ("NIVEL 2 — TALLERES DE TALAVERA",
/// "NIVEL 3 — CALLEJONES DE GUANAJUATO") al iniciar cada nuevo nivel.
/// Bloquea completamente todo movimiento, física y pausa mientras aparece el texto
/// y libera el control suavemente al retirarse.
/// </summary>
public class LevelTitleCard : MonoBehaviour
{
    private static LevelTitleCard activeCard;
    public static bool IsActive => activeCard != null && activeCard.isShowing;

    private string levelTag = "";
    private string zoneTitle = "";
    private string bossSubtitle = "";

    private bool isShowing = false;
    private float slideProgress = 0f;   // 0 = fuera, 1 = centrado
    private float bgFadeAlpha = 0.88f;

    private Texture2D darkOverlayTex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void HookSceneLoad()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryShowForScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryShowForScene(scene);
    }

    private static void TryShowForScene(Scene scene)
    {
        if (scene.name == "Nivel2")
        {
            Show("NIVEL 2", "TALLERES DE TALAVERA", "Jefe: El Munícipe Carismático");
        }
        else if (scene.name == "Nivel3")
        {
            Show("NIVEL 3", "CALLEJONES DE GUANAJUATO", "Jefe Final: El Influencer del Callejón");
        }
        else if (scene.name == "Nivel1")
        {
            if (!Level1IntroCutsceneTrigger.IsPlaying)
                PauseMenu.BlockPauseDuringTransition = false;
        }
        else
        {
            // Asegurar que PauseMenu y tiempo no queden bloqueados al volver al Menú Principal
            Time.timeScale = 1f;
            PauseMenu.BlockPauseDuringTransition = false;
        }
    }

    public static void Show(string tag, string zone, string subtitle)
    {
        if (activeCard != null)
            Destroy(activeCard.gameObject);

        GameObject go = new GameObject("_SonicLevelTitleCard");
        LevelTitleCard card = go.AddComponent<LevelTitleCard>();
        activeCard = card;
        card.levelTag = tag;
        card.zoneTitle = zone;
        card.bossSubtitle = subtitle;
        card.StartCoroutine(card.PlayTitleCardRoutine());
    }

    private void Awake()
    {
        darkOverlayTex = MakeTex(new Color(0.03f, 0.02f, 0.07f, 1f));
    }

    private IEnumerator PlayTitleCardRoutine()
    {
        isShowing = true;
        slideProgress = 0f;
        bgFadeAlpha = 0.92f;

        // Congelar juego, pausa y controles del jugador mientras se muestra "NIVEL 2 / NIVEL 3"
        PauseMenu.BlockPauseDuringTransition = true;
        Time.timeScale = 0f;

        PlayerMovement pm = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
        if (pm != null)
            pm.SetControlLocked(true);

        // 1. Entrada deslizada rápida estilo Sonic (0.45s)
        float t = 0f;
        while (t < 0.45f)
        {
            if (pm == null)
            {
                pm = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
                if (pm != null) pm.SetControlLocked(true);
            }

            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / 0.45f);
            slideProgress = 1f - Mathf.Pow(1f - n, 3f);
            yield return null;
        }
        slideProgress = 1f;

        // 2. Mantener legible en pantalla mientras el fondo se revela suavemente (1.75s)
        float hold = 0f;
        while (hold < 1.75f)
        {
            hold += Time.unscaledDeltaTime;
            bgFadeAlpha = Mathf.Lerp(0.92f, 0.35f, Mathf.Clamp01(hold / 0.8f));
            yield return null;
        }

        // 3. Salida deslizada hacia la derecha (0.40s)
        float exit = 0f;
        while (exit < 0.40f)
        {
            exit += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(exit / 0.40f);
            slideProgress = 1f + (n * n * 1.4f);
            bgFadeAlpha = Mathf.Lerp(0.35f, 0f, n);
            yield return null;
        }

        // 4. Liberar controles y arrancar el nivel
        isShowing = false;
        Time.timeScale = 1f;
        PauseMenu.BlockPauseDuringTransition = false;

        if (pm == null)
            pm = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
        if (pm != null)
            pm.SetControlLocked(false);

        if (activeCard == this)
            activeCard = null;
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (activeCard == this)
            activeCard = null;

        if (isShowing)
        {
            isShowing = false;
            Time.timeScale = 1f;
            PauseMenu.BlockPauseDuringTransition = false;
            PlayerMovement pm = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
            if (pm != null)
                pm.SetControlLocked(false);
        }
    }

    private void OnGUI()
    {
        if (!isShowing)
            return;

        Color prev = GUI.color;

        // Fondo oscuro que se va revelando detrás del texto
        if (bgFadeAlpha > 0.01f && darkOverlayTex != null)
        {
            GUI.color = new Color(1f, 1f, 1f, bgFadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), darkOverlayTex);
            GUI.color = prev;
        }

        // Puro texto deslizando al centro (sin caja ni tarjeta detrás)
        float w = Mathf.Min(820f, Screen.width * 0.92f);
        float h = 180f;
        float centerX = (Screen.width - w) * 0.5f;
        float x = slideProgress <= 1f
            ? Mathf.Lerp(-w - 60f, centerX, slideProgress)
            : Mathf.Lerp(centerX, Screen.width + 90f, slideProgress - 1f);
        float y = (Screen.height - h) * 0.5f;

        Color outlineCol = new Color(0.03f, 0.02f, 0.06f, 0.95f);

        GUIStyle tagStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 54,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        DrawOutlinedLabel(new Rect(x, y + 4f, w, 68f), levelTag, tagStyle, new Color(0.955f, 0.785f, 0.340f, 1f), outlineCol, 3f);

        GUIStyle zoneStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 30,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        DrawOutlinedLabel(new Rect(x, y + 76f, w, 44f), zoneTitle, zoneStyle, new Color(0.965f, 0.940f, 0.875f, 1f), outlineCol, 2f);

        GUIStyle subStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        DrawOutlinedLabel(new Rect(x, y + 124f, w, 34f), bossSubtitle, subStyle, new Color(0.86f, 0.82f, 0.74f, 1f), outlineCol, 2f);
    }

    private static void DrawOutlinedLabel(Rect rect, string text, GUIStyle style, Color textColor, Color outlineColor, float px)
    {
        style.normal.textColor = outlineColor;
        GUI.Label(new Rect(rect.x - px, rect.y, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x + px, rect.y, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x, rect.y - px, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x, rect.y + px, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x + px, rect.y + px * 1.5f, rect.width, rect.height), text, style);

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
