using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Animación automática de fin de nivel estilo Sonic / Mario al derrotar a cada Jefe:
/// 1) Breve cámara lenta dramática + desbloqueo inmediato del siguiente nivel.
/// 2) Fuegos artificiales de Fiesta Patronal, lluvia de confeti, saltos de festejo de Finn y listón triunfal.
/// 3) Se abre la Puerta / Arco Colonial al fondo de la arena y Finn CAMINA/CORRE AUTOMÁTICAMENTE
///    hacia la derecha hasta cruzar la puerta (o salir del mapa), hace fundido cinematográfico y
///    CARGA AUTOMÁTICAMENTE el siguiente nivel (Nivel 1 -> Nivel 2 -> Nivel 3).
///    Al completar el Nivel 3 (Gran Final), tras cruzar el Arco Triunfal abre la Pantalla de Victoria Final.
/// </summary>
public class LevelCompletionCelebration : MonoBehaviour
{
    private class ConfettiPiece
    {
        public float x, y, vx, vy, size;
        public Color color;
    }

    private bool isCelebrating = false;
    private string bannerHeadline = "";
    private string bannerSubline = "";
    private float bannerAnimProgress = 0f;
    private float fadeAlpha = 0f;
    private readonly List<ConfettiPiece> confetti = new List<ConfettiPiece>();

    private Texture2D whiteTex;
    private Texture2D blackTex;

    public static void PlayForLevel(
        int completedLevel,
        string bossTitle,
        string victoryMessage,
        Vector3 bossDefeatPos,
        Transform playerTransform)
    {
        GameObject go = new GameObject($"_Level{completedLevel}_Celebration");
        LevelCompletionCelebration celeb = go.AddComponent<LevelCompletionCelebration>();
        celeb.StartCoroutine(celeb.CelebrationSequence(completedLevel, bossTitle, victoryMessage, bossDefeatPos, playerTransform));
    }

    private void Awake()
    {
        whiteTex = MakeTex(Color.white);
        blackTex = MakeTex(new Color(0.03f, 0.02f, 0.06f, 1f));
    }

    private IEnumerator CelebrationSequence(
        int completedLevel,
        string bossTitle,
        string victoryMessage,
        Vector3 bossPos,
        Transform playerTransform)
    {
        isCelebrating = true;
        bannerAnimProgress = 0f;
        fadeAlpha = 0f;

        // BLOQUEO INMEDIATO DE CONTROLES estilo Sonic desde el instante en que cae el jefe
        PauseMenu.BlockPauseDuringTransition = true;
        PlayerMovement pm = playerTransform != null
            ? playerTransform.GetComponent<PlayerMovement>()
            : UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
        if (pm != null)
            pm.SetControlLocked(true);

        // Limpiar proyectiles enemigos que hayan quedado en el aire al derrotar al jefe
        foreach (GarbanzoProjectile gp in UnityEngine.Object.FindObjectsByType<GarbanzoProjectile>())
            if (gp != null) Destroy(gp.gameObject);
        foreach (FallingPropagandaProjectile fp in UnityEngine.Object.FindObjectsByType<FallingPropagandaProjectile>())
            if (fp != null) Destroy(fp.gameObject);

        // Desbloquear siguiente nivel en PlayerPrefs de inmediato
        LevelProgressionManager.UnlockNextLevel(completedLevel);

        switch (completedLevel)
        {
            case 1:
                bannerHeadline = "JARDÍN PRINCIPAL LIBERADO";
                bannerSubline = "Avanzando hacia los Talleres de Talavera (Nivel 2)";
                break;
            case 2:
                bannerHeadline = "TALLERES DE TALAVERA LIBERADOS";
                bannerSubline = "Avanzando hacia los Callejones de Guanajuato (Nivel 3)";
                break;
            default:
                bannerHeadline = "FIESTAS PATRONALES SALVADAS";
                bannerSubline = "Has derrotado a los 3 Jefes de Dolores Hidalgo y Guanajuato";
                break;
        }

        // 1. Breve cámara lenta dramática al derrotar al jefe
        Time.timeScale = 0.35f;
        yield return new WaitForSecondsRealtime(0.42f);
        Time.timeScale = 1f;

        // 2. Activar portal/puerta de salida y abrir muro trasero de la arena para salida estilo Sonic/Mario
        LevelPortalTrigger exitDoor = UnityEngine.Object.FindAnyObjectByType<LevelPortalTrigger>();
        if (exitDoor != null)
        {
            exitDoor.ActivatePortal();
            // Evitar que el trigger manual interrumpa antes de terminar nuestra animación automática
            Collider2D doorCol = exitDoor.GetComponent<Collider2D>();
            if (doorCol != null) doorCol.enabled = false;
        }

        GameObject backWall = GameObject.Find("Arena_Back_Wall");
        if (backWall != null)
            backWall.SetActive(false);

        // Desactivar colisionador del jefe derrotado para que Finn pueda pasar corriendo sin chocar
        SimpleEnemy[] allEnemies = UnityEngine.Object.FindObjectsByType<SimpleEnemy>();
        foreach (SimpleEnemy en in allEnemies)
        {
            if (en != null && en.currentHealth <= 0)
            {
                Collider2D ecol = en.GetComponent<Collider2D>();
                if (ecol != null) ecol.enabled = false;
            }
        }

        // 3. Lluvia festiva de confeti / papel picado y fuegos artificiales
        SpawnScreenConfetti(completedLevel >= 3 ? 120 : 80);

        if (pm != null)
            pm.UnlockFromArena();

        // Festejo inicial breve de Finn (2 saltos alegres con fuegos artificiales)
        StartCoroutine(QuickVictoryHopRoutine(playerTransform));

        Vector3 anchorCenter = playerTransform != null ? playerTransform.position : bossPos;
        int initialWaves = completedLevel >= 3 ? 4 : 3;
        for (int w = 0; w < initialWaves; w++)
        {
            Vector3 burstOrigin = anchorCenter + new Vector3(
                Random.Range(-4.5f, 5.5f),
                Random.Range(2.8f, 5.2f),
                0f
            );
            SpawnWorldFireworkBurst(burstOrigin);
            ProceduralSoundManager.Instance?.PlayFireworkBurst();

            float wait = 0.45f;
            float elapsed = 0f;
            while (elapsed < wait)
            {
                bannerAnimProgress = Mathf.Min(1f, bannerAnimProgress + Time.deltaTime * 1.8f);
                UpdateConfetti(Time.deltaTime);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        // 4. CARRERA AUTOMÁTICA ESTILO SONIC / MARIO HACIA LA PUERTA DE SALIDA
        float exitTargetX = exitDoor != null
            ? exitDoor.transform.position.x
            : (playerTransform != null ? playerTransform.position.x + 12f : bossPos.x + 10f);

        if (pm != null && playerTransform != null)
        {
            // Asegurar que la dirección apunte hacia la puerta (normalmente hacia la derecha)
            float dirToDoor = exitTargetX >= playerTransform.position.x ? 1f : -1f;
            pm.SetAutoWalkExit(true, dirToDoor);

            float runTimer = 0f;
            float maxRunTime = 3.2f;
            while (runTimer < maxRunTime && playerTransform != null)
            {
                float distToDoor = Mathf.Abs(exitTargetX - playerTransform.position.x);
                if (distToDoor <= 0.55f)
                    break;

                // Fuego artificial extra mientras corre hacia la puerta
                if (Mathf.Approximately(Mathf.Floor(runTimer * 2f), Mathf.Floor((runTimer + Time.deltaTime) * 2f) - 1f))
                {
                    SpawnWorldFireworkBurst(playerTransform.position + new Vector3(2f, 3.8f, 0f));
                    ProceduralSoundManager.Instance?.PlayFireworkBurst();
                }

                UpdateConfetti(Time.deltaTime);
                runTimer += Time.deltaTime;
                yield return null;
            }

            // Al llegar a la puerta: sonido de entrada al portal + desvanecimiento de Finn entrando
            ProceduralSoundManager.Instance?.PlayPortalEnter();
            pm.SetAutoWalkExit(false, 1f);
            SpriteRenderer psr = playerTransform.GetComponent<SpriteRenderer>();
            if (psr != null)
            {
                float shrink = 0f;
                while (shrink < 0.30f)
                {
                    psr.color = new Color(1f, 0.95f, 0.7f, Mathf.Lerp(1f, 0f, shrink / 0.30f));
                    UpdateConfetti(Time.deltaTime);
                    shrink += Time.deltaTime;
                    yield return null;
                }
            }
        }

        // 5. Fundido cinematográfico a negro
        float fadeTime = 0.55f;
        float f = 0f;
        while (f < fadeTime)
        {
            fadeAlpha = Mathf.Clamp01(f / fadeTime);
            UpdateConfetti(Time.deltaTime);
            f += Time.deltaTime;
            yield return null;
        }
        fadeAlpha = 1f;

        // 6. TRANSICIÓN AUTOMÁTICA AL SIGUIENTE NIVEL (Nivel 1 -> Nivel 2 -> Nivel 3)
        if (completedLevel == 1)
        {
            if (LevelProgressionManager.TryLoadScene("Nivel2"))
                yield break;
        }
        else if (completedLevel == 2)
        {
            if (LevelProgressionManager.TryLoadScene("Nivel3"))
                yield break;
        }

        // Si fue el Nivel 3 (Gran Final): lanzar animación temática de Fiestas Patronales y regresar al Menú Principal
        isCelebrating = false;
        if (completedLevel >= 3)
        {
            GrandFinaleCutscene.Play(bossTitle, victoryMessage, () =>
            {
                PauseMenu.BlockPauseDuringTransition = false;
                Time.timeScale = 1f;
                if (!LevelProgressionManager.TryLoadScene("MainMenu"))
                    SceneManager.LoadScene(0);
            });
            Destroy(gameObject);
            yield break;
        }

        // Fallback si no existe la siguiente escena en Build Settings (Niveles 1 o 2)
        PauseMenu pauseMenu = PauseMenu.Instance != null
            ? PauseMenu.Instance
            : UnityEngine.Object.FindAnyObjectByType<PauseMenu>();
        PauseMenu.BlockPauseDuringTransition = false;
        if (pauseMenu != null)
        {
            pauseMenu.ShowVictoryScreen(completedLevel, bossTitle, victoryMessage);
        }

        Destroy(gameObject, 0.5f);
    }

    private IEnumerator QuickVictoryHopRoutine(Transform playerTransform)
    {
        if (playerTransform == null)
            yield break;

        Rigidbody2D rb = playerTransform.GetComponent<Rigidbody2D>();
        SpriteRenderer sr = playerTransform.GetComponent<SpriteRenderer>();
        Animator anim = playerTransform.GetComponent<Animator>();
        Color origColor = sr != null ? sr.color : Color.white;

        for (int hop = 0; hop < 2; hop++)
        {
            if (playerTransform == null) yield break;
            if (rb != null) rb.linearVelocity = new Vector2(0f, 6.4f);
            if (anim != null) anim.SetTrigger("Jump");
            ProceduralSoundManager.Instance?.PlayJump();

            float t = 0f;
            while (t < 0.55f)
            {
                if (sr != null)
                {
                    float glow = 0.5f + 0.5f * Mathf.Sin(t * 20f);
                    sr.color = Color.Lerp(origColor, new Color(1f, 0.92f, 0.45f, 1f), glow * 0.6f);
                }
                t += Time.deltaTime;
                yield return null;
            }
        }

        if (sr != null)
            sr.color = origColor;
    }

    private void SpawnWorldFireworkBurst(Vector3 center)
    {
        Color[] fiestaColors = new Color[]
        {
            new Color(1.00f, 0.80f, 0.22f, 1f),
            new Color(0.95f, 0.32f, 0.24f, 1f),
            new Color(0.22f, 0.88f, 0.98f, 1f),
            new Color(1.00f, 0.25f, 0.68f, 1f),
            new Color(0.32f, 0.96f, 0.48f, 1f)
        };

        Color primary = fiestaColors[Random.Range(0, fiestaColors.Length)];
        Color secondary = fiestaColors[Random.Range(0, fiestaColors.Length)];
        Sprite sparkSprite = ProceduralPixelArt.GetCollectibleSprite(false);

        int sparkCount = 16;
        for (int i = 0; i < sparkCount; i++)
        {
            float angle = (i / (float)sparkCount) * Mathf.PI * 2f + Random.Range(-0.12f, 0.12f);
            float speed = Random.Range(3.2f, 6.8f);

            GameObject spark = new GameObject("Firework_Spark");
            spark.transform.position = center;
            spark.transform.localScale = Vector3.one * Random.Range(0.32f, 0.58f);

            SpriteRenderer sr = spark.AddComponent<SpriteRenderer>();
            sr.sprite = sparkSprite;
            sr.color = (i % 2 == 0) ? primary : secondary;
            sr.sortingOrder = 25;

            Rigidbody2D rb = spark.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0.65f;
            rb.linearVelocity = new Vector2(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed);
            rb.linearDamping = 0.8f;

            Destroy(spark, Random.Range(0.75f, 1.15f));
        }
    }

    private void SpawnScreenConfetti(int count)
    {
        confetti.Clear();
        Color[] colors =
        {
            new Color(0.98f, 0.78f, 0.24f, 1f),
            new Color(0.86f, 0.30f, 0.22f, 1f),
            new Color(0.24f, 0.72f, 0.95f, 1f),
            new Color(0.95f, 0.28f, 0.64f, 1f),
            new Color(0.30f, 0.88f, 0.44f, 1f)
        };

        for (int i = 0; i < count; i++)
        {
            confetti.Add(new ConfettiPiece
            {
                x = Random.Range(0f, Screen.width),
                y = Random.Range(-Screen.height * 0.5f, 0f),
                vx = Random.Range(-45f, 45f),
                vy = Random.Range(140f, 290f),
                size = Random.Range(8f, 15f),
                color = colors[i % colors.Length]
            });
        }
    }

    private void UpdateConfetti(float dt)
    {
        for (int i = 0; i < confetti.Count; i++)
        {
            ConfettiPiece p = confetti[i];
            p.x += (p.vx + Mathf.Sin(Time.time * 4f + i) * 35f) * dt;
            p.y += p.vy * dt;
            if (p.y > Screen.height + 20f)
            {
                p.y = -15f;
                p.x = Random.Range(0f, Screen.width);
            }
        }
    }

    private void OnGUI()
    {
        if (!isCelebrating || whiteTex == null)
            return;

        // 1. Confeti / papel picado cayendo
        Color prevColor = GUI.color;
        for (int i = 0; i < confetti.Count; i++)
        {
            ConfettiPiece p = confetti[i];
            GUI.color = p.color;
            GUI.DrawTexture(new Rect(p.x, p.y, p.size, p.size * 0.65f), whiteTex);
        }
        GUI.color = prevColor;

        // 2. Texto triunfal limpio con contorno oscuro (sin tarjeta ni caja detrás)
        float eased = 1f - Mathf.Pow(1f - bannerAnimProgress, 3f);
        float w = Mathf.Min(880f, Screen.width * 0.92f);
        float x = (Screen.width - w) * 0.5f;
        float y = Mathf.Lerp(-120f, Screen.height * 0.16f, eased);
        Color outlineCol = new Color(0.03f, 0.02f, 0.06f, 0.95f);

        GUIStyle headStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 38,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        DrawOutlinedLabel(new Rect(x, y, w, 52f), bannerHeadline, headStyle, new Color(0.955f, 0.785f, 0.340f, 1f), outlineCol, 3f);

        GUIStyle subStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 19,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        DrawOutlinedLabel(new Rect(x + 20f, y + 54f, w - 40f, 46f), bannerSubline, subStyle, new Color(0.965f, 0.940f, 0.875f, 1f), outlineCol, 2f);

        // 3. Fundido cinematográfico a negro al cruzar la puerta
        if (fadeAlpha > 0.001f && blackTex != null)
        {
            GUI.color = new Color(1f, 1f, 1f, fadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), blackTex);
            GUI.color = prevColor;
        }
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
