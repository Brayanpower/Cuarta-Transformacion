using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Sistema unificado de Pausa (ESC), Pantalla de Victoria, Selección de Niveles y Ajustes.
/// Comparte exactamente la misma temática pixel-art formal de tablones de madera tallada
/// y marco colonial oscuro que el Menú Principal (sin cajas moradas ni emojis).
/// </summary>
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }
    public static bool BlockPauseDuringTransition { get; set; } = false;
    public static bool IsGamePaused => Instance != null && (Instance.isPaused || Instance.isVictoryActive);

    private enum MenuSubView
    {
        PauseMain,
        LevelSelect,
        Controls,
        Settings,
        Victory
    }

    [Header("Configuración")]
    public KeyCode pauseKey = KeyCode.Escape;
    public string mainMenuSceneName = "MainMenu";
    public string level1SceneName = "Nivel1";
    public string level2SceneName = "Nivel2";
    public string level3SceneName = "Nivel3";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PauseMenu>() == null)
        {
            GameObject go = new GameObject("_PauseMenu");
            Instance = go.AddComponent<PauseMenu>();
            DontDestroyOnLoad(go);
        }
    }

    private bool isPaused = false;
    private bool isVictoryActive = false;
    private MenuSubView currentView = MenuSubView.PauseMain;

    private bool audioMuted = false;
    private float currentMusicVolume = LevelProgressionManager.DEFAULT_VOLUME;
    private float currentSfxVolume = LevelProgressionManager.DEFAULT_VOLUME;
    private float nextSfxPreviewTime = 0f;
    private bool isFullscreen = true;
    private float currentCameraZoom = LevelProgressionManager.DEFAULT_CAMERA_ZOOM;

    // Datos de pantalla de victoria
    private int lastCompletedLevel = 1;
    private string defeatedBossTitle = "EL VENDEDOR VIRAL";
    private string victoryMessage = "El Jardín Principal de Dolores Hidalgo ha recuperado la paz.";
    private string statusNotification = "";
    private float notificationTimer = 0f;

    // Texturas y estilos pixel-art formales (tablones de madera + marco oscuro)
    private GUIStyle panelBoxStyle;
    private GUIStyle titleStyle;
    private GUIStyle shadowStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle labelStyle;
    private GUIStyle plankButtonStyle;
    private GUIStyle lockedPlankStyle;

    private Texture2D darkBgTexture;
    private Texture2D formalPanelTex;
    private Texture2D plankNormalTex;
    private Texture2D plankHoverTex;
    private Texture2D plankPressedTex;
    private Texture2D plankLockedTex;
    private Texture2D goldLineTex;
    private bool stylesInitialized = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        currentMusicVolume = LevelProgressionManager.GetMusicVolume();
        currentSfxVolume = LevelProgressionManager.GetSfxVolume();
        isFullscreen = PlayerPrefs.GetInt(LevelProgressionManager.FULLSCREEN_KEY, 1) == 1;
        currentCameraZoom = LevelProgressionManager.GetCameraZoom();
        AudioListener.volume = audioMuted ? 0f : 1f;
        ProceduralSoundManager.Instance?.RefreshVolumesFromPrefs();
        Screen.fullScreen = isFullscreen;
        CameraFollow.ApplyZoomToActiveCamera(currentCameraZoom, true);
    }

    void Update()
    {
        if (notificationTimer > 0f)
            notificationTimer -= Time.unscaledDeltaTime;

        if ((isPaused || isVictoryActive) && Time.timeScale != 0f)
            SetWorldSimulationPaused(true);

        // No abrir el menú de pausa en MainMenu ni durante transiciones/tarjetas estilo Sonic
        if (SceneManager.GetActiveScene().name == mainMenuSceneName || BlockPauseDuringTransition)
            return;

        if (WasPauseKeyPressed() && !isVictoryActive)
        {
            if (isPaused)
            {
                if (currentView != MenuSubView.PauseMain)
                    currentView = MenuSubView.PauseMain;
                else
                    ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    private bool WasPauseKeyPressed()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            return true;

        try
        {
            return Input.GetKeyDown(pauseKey);
        }
        catch
        {
            return false;
        }
    }

    private static void SetWorldSimulationPaused(bool paused)
    {
        Time.timeScale = paused ? 0f : 1f;
        Physics2D.simulationMode = paused ? SimulationMode2D.Script : SimulationMode2D.FixedUpdate;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SetWorldSimulationPaused(false);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // API PÚBLICA DE PAUSA Y VICTORIA
    // ═══════════════════════════════════════════════════════════

    public void PauseGame()
    {
        isPaused = true;
        currentView = MenuSubView.PauseMain;
        statusNotification = "";
        currentMusicVolume = LevelProgressionManager.GetMusicVolume();
        currentSfxVolume = LevelProgressionManager.GetSfxVolume();
        currentCameraZoom = LevelProgressionManager.GetCameraZoom();
        SetWorldSimulationPaused(true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        isVictoryActive = false;
        currentView = MenuSubView.PauseMain;
        SetWorldSimulationPaused(false);
    }

    public void ShowVictoryScreen(int completedLevel, string bossName, string customMessage = "")
    {
        LevelProgressionManager.UnlockNextLevel(completedLevel);

        lastCompletedLevel = completedLevel;
        defeatedBossTitle = bossName;
        if (!string.IsNullOrEmpty(customMessage))
            victoryMessage = customMessage;

        isVictoryActive = true;
        isPaused = true;
        currentView = MenuSubView.Victory;
        statusNotification = "";
        SetWorldSimulationPaused(true);
    }

    public void OpenLevelSelection()
    {
        isPaused = true;
        currentView = MenuSubView.LevelSelect;
        statusNotification = "";
        SetWorldSimulationPaused(true);
    }

    public void RestartGame()
    {
        SetWorldSimulationPaused(false);
        isPaused = false;
        isVictoryActive = false;
        Level1IntroCutsceneTrigger.SkipOnNextLoad();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenuOrLevelSelect()
    {
        if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName) &&
            SceneManager.GetActiveScene().name != mainMenuSceneName)
        {
            SetWorldSimulationPaused(false);
            isPaused = false;
            isVictoryActive = false;
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            currentView = MenuSubView.LevelSelect;
            statusNotification = "";
        }
    }

    public void QuitGame()
    {
        SetWorldSimulationPaused(false);
        Debug.Log("[PauseMenu] Saliendo del juego...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ToggleAudio()
    {
        audioMuted = !audioMuted;
        AudioListener.volume = audioMuted ? 0f : 1f;
    }

    private void Notify(string message)
    {
        statusNotification = message;
        notificationTimer = 3.5f;
    }

    // ═══════════════════════════════════════════════════════════
    // CARGA DE TEXTURAS FORMALES PIXEL-ART (TABLONES DE MADERA)
    // ═══════════════════════════════════════════════════════════

    private Texture2D LoadFormalUITexture(string resourceBaseName, Color fallbackColor)
    {
        // 1. Intentar desde Assets/Images/UI (siempre nítido en Editor)
        string diskPath = Path.Combine(Application.dataPath, "Images/UI/" + resourceBaseName + ".png");
        if (File.Exists(diskPath))
        {
            byte[] bytes = File.ReadAllBytes(diskPath);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            if (tex.LoadImage(bytes))
                return tex;
        }

        // 2. Cargar desde Resources/UI (compatible con builds exportados)
        Texture2D resTex = Resources.Load<Texture2D>("UI/" + resourceBaseName);
        if (resTex != null)
        {
            resTex.filterMode = FilterMode.Point;
            return resTex;
        }

        return MakeSolidTexture(2, 2, fallbackColor);
    }

    private Texture2D MakeTintedCopy(Texture2D source, Color tint)
    {
        if (source == null || !source.isReadable)
            return MakeSolidTexture(2, 2, new Color(0.28f, 0.24f, 0.26f, 0.92f));

        Color[] pixels = source.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color(
                pixels[i].r * tint.r,
                pixels[i].g * tint.g,
                pixels[i].b * tint.b,
                pixels[i].a * tint.a
            );
        }

        Texture2D copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        copy.SetPixels(pixels);
        copy.Apply();
        return copy;
    }

    private void InitializeStyles()
    {
        if (stylesInitialized && darkBgTexture != null && plankNormalTex != null)
            return;

        darkBgTexture   = MakeSolidTexture(2, 2, new Color(0.03f, 0.02f, 0.06f, 0.72f));
        goldLineTex     = MakeSolidTexture(2, 2, new Color(0.88f, 0.69f, 0.33f, 0.75f));
        formalPanelTex  = LoadFormalUITexture("UI_PlankPanel_Formal",   new Color(0.11f, 0.09f, 0.15f, 0.95f));
        plankNormalTex  = LoadFormalUITexture("UI_PlankButton_Normal",  new Color(0.72f, 0.52f, 0.32f, 1f));
        plankHoverTex   = LoadFormalUITexture("UI_PlankButton_Hover",   new Color(0.84f, 0.62f, 0.38f, 1f));
        plankPressedTex = LoadFormalUITexture("UI_PlankButton_Pressed", new Color(0.58f, 0.40f, 0.24f, 1f));
        plankLockedTex  = MakeTintedCopy(plankNormalTex, new Color(0.46f, 0.42f, 0.44f, 0.92f));

        Color colGold  = new Color(0.955f, 0.785f, 0.340f, 1f);
        Color colCream = new Color(0.965f, 0.940f, 0.875f, 1f);
        Color colPlankText = new Color(0.14f, 0.085f, 0.055f, 1f);

        panelBoxStyle = new GUIStyle(GUI.skin.box)
        {
            border = new RectOffset(18, 18, 18, 18),
            padding = new RectOffset(0, 0, 0, 0)
        };
        panelBoxStyle.normal.background = formalPanelTex;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = colGold;

        shadowStyle = new GUIStyle(titleStyle);
        shadowStyle.normal.textColor = new Color(0.04f, 0.02f, 0.06f, 0.90f);

        subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        subtitleStyle.normal.textColor = colCream;

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        labelStyle.normal.textColor = colCream;

        plankButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            border = new RectOffset(14, 14, 8, 8),
            padding = new RectOffset(12, 12, 4, 8)
        };
        plankButtonStyle.normal.background = plankNormalTex;
        plankButtonStyle.hover.background  = plankHoverTex;
        plankButtonStyle.active.background = plankPressedTex;
        plankButtonStyle.normal.textColor  = colPlankText;
        plankButtonStyle.hover.textColor   = new Color(0.09f, 0.05f, 0.03f, 1f);
        plankButtonStyle.active.textColor  = colPlankText;

        lockedPlankStyle = new GUIStyle(plankButtonStyle);
        lockedPlankStyle.normal.background = plankLockedTex;
        lockedPlankStyle.hover.background  = plankLockedTex;
        lockedPlankStyle.active.background = plankLockedTex;
        lockedPlankStyle.normal.textColor  = new Color(0.22f, 0.19f, 0.20f, 1f);
        lockedPlankStyle.hover.textColor   = new Color(0.22f, 0.19f, 0.20f, 1f);
        lockedPlankStyle.active.textColor  = new Color(0.22f, 0.19f, 0.20f, 1f);

        stylesInitialized = true;
    }

    void OnGUI()
    {
        if (!isPaused && !isVictoryActive)
            return;

        GUI.depth = -50;
        InitializeStyles();

        float screenW = Screen.width;
        float screenH = Screen.height;

        GUI.DrawTexture(new Rect(0, 0, screenW, screenH), darkBgTexture);

        switch (currentView)
        {
            case MenuSubView.PauseMain:
                DrawPauseMainPanel(screenW, screenH);
                break;
            case MenuSubView.LevelSelect:
                DrawLevelSelectPanel(screenW, screenH);
                break;
            case MenuSubView.Controls:
                DrawControlsPanel(screenW, screenH);
                break;
            case MenuSubView.Settings:
                DrawSettingsPanel(screenW, screenH);
                break;
            case MenuSubView.Victory:
                DrawVictoryPanel(screenW, screenH);
                break;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 1. PANEL DE PAUSA (MISMA ESTÉTICA TABLÓN FORMAL)
    // ═══════════════════════════════════════════════════════════

    private void DrawPauseMainPanel(float screenW, float screenH)
    {
        float panelW = Mathf.Min(430f, screenW * 0.88f);
        float panelH = 480f;
        float panelX = (screenW - panelW) / 2f;
        float panelY = (screenH - panelH) / 2f;

        DrawPanelFrame(panelX, panelY, panelW, panelH, "PAUSA");

        float btnW = panelW - 76f;
        float btnX = panelX + 38f;
        float currentY = panelY + 84f;

        if (DrawPlankButton(new Rect(btnX, currentY, btnW, 50f), "REANUDAR"))
        {
            ProceduralSoundManager.Instance?.PlayButtonClick();
            ResumeGame();
        }
        currentY += 60f;

        if (DrawPlankButton(new Rect(btnX, currentY, btnW, 50f), "SELECCIONAR NIVEL"))
        {
            ProceduralSoundManager.Instance?.PlayButtonClick();
            currentView = MenuSubView.LevelSelect;
            statusNotification = "";
        }
        currentY += 60f;

        if (DrawPlankButton(new Rect(btnX, currentY, btnW, 50f), "CÓMO JUGAR / CONTROLES"))
        {
            ProceduralSoundManager.Instance?.PlayButtonClick();
            currentView = MenuSubView.Controls;
            statusNotification = "";
        }
        currentY += 60f;

        if (DrawPlankButton(new Rect(btnX, currentY, btnW, 50f), "AJUSTES"))
        {
            ProceduralSoundManager.Instance?.PlayButtonClick();
            currentCameraZoom = LevelProgressionManager.GetCameraZoom();
            currentView = MenuSubView.Settings;
            statusNotification = "";
        }
        currentY += 60f;

        if (DrawPlankButton(new Rect(btnX, currentY, btnW, 50f), "REINICIAR NIVEL"))
        {
            ProceduralSoundManager.Instance?.PlayButtonClick();
            RestartGame();
        }
        currentY += 60f;

        if (DrawPlankButton(new Rect(btnX, currentY, btnW, 50f), "MENÚ PRINCIPAL"))
        {
            ProceduralSoundManager.Instance?.PlayButtonClick();
            GoToMainMenuOrLevelSelect();
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 1.5. PANEL DE CÓMO JUGAR Y CONTROLES
    // ═══════════════════════════════════════════════════════════

    private void DrawControlsPanel(float screenW, float screenH)
    {
        float panelW = Mathf.Min(540f, screenW * 0.92f);
        float panelH = 460f;
        float panelX = (screenW - panelW) / 2f;
        float panelY = (screenH - panelH) / 2f;

        DrawPanelFrame(panelX, panelY, panelW, panelH, "CÓMO JUGAR Y CONTROLES");

        float contentX = panelX + 38f;
        float contentW = panelW - 76f;
        float currentY = panelY + 84f;

        GUIStyle keyTitleStyle = new GUIStyle(labelStyle);
        keyTitleStyle.normal.textColor = new Color(0.955f, 0.785f, 0.340f);
        keyTitleStyle.fontSize = 15;

        GUIStyle tipStyle = new GUIStyle(subtitleStyle);
        tipStyle.alignment = TextAnchor.MiddleLeft;
        tipStyle.fontSize = 14;

        string[,] rows =
        {
            { "MOVERSE:", "Teclas A / D o Flechas Izquierda / Derecha" },
            { "SALTAR / PISOTÓN:", "Espacio (salta sobre enemigos para rebotar y dañarlos)" },
            { "DASH (ESQUIVE):", "Left Shift (atraviesa ataques y enemigos siendo invulnerable)" },
            { "ESPADAZO:", "Clic Izquierdo o Enter (corta a enemigos y activa jarrones)" },
            { "PAUSA:", "ESC (pausa la partida, ajusta sonido o cambia de nivel)" }
        };

        for (int i = 0; i < rows.GetLength(0); i++)
        {
            GUI.Label(new Rect(contentX, currentY, contentW, 22f), rows[i, 0], keyTitleStyle);
            GUI.Label(new Rect(contentX, currentY + 20f, contentW, 24f), rows[i, 1], tipStyle);
            currentY += 54f;
        }

        GUI.Label(
            new Rect(contentX, currentY + 4f, contentW, 38f),
            "Objetivo: Cruza cada sector, esquiva la lava y derrota al jefe final de cada nivel.",
            subtitleStyle
        );

        if (DrawPlankButton(new Rect(contentX + 40f, panelY + panelH - 68f, contentW - 80f, 48f), "VOLVER"))
        {
            ProceduralSoundManager.Instance?.PlayButtonClick();
            currentView = MenuSubView.PauseMain;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 2. PANEL DE SELECCIÓN DE NIVELES IN-GAME
    // ═══════════════════════════════════════════════════════════

    private void DrawLevelSelectPanel(float screenW, float screenH)
    {
        float panelW = Mathf.Min(500f, screenW * 0.92f);
        float panelH = 445f;
        float panelX = (screenW - panelW) / 2f;
        float panelY = (screenH - panelH) / 2f;

        DrawPanelFrame(panelX, panelY, panelW, panelH, "SELECCIONAR NIVEL");

        int unlockedLevel = LevelProgressionManager.GetUnlockedLevel();
        GUI.Label(new Rect(panelX + 24, panelY + 62, panelW - 48, 24),
            $"Progreso actual: {unlockedLevel} de 3 sectores disponibles", subtitleStyle);

        float btnW = panelW - 76f;
        float btnX = panelX + 38f;
        float currentY = panelY + 98f;

        // NIVEL 1
        if (DrawPlankButton(new Rect(btnX, currentY, btnW, 52f), "NIVEL 1 — JARDÍN PRINCIPAL"))
        {
            ProceduralSoundManager.Instance?.PlayButtonClick();
            SetWorldSimulationPaused(false);
            isPaused = false;
            isVictoryActive = false;
            Level1IntroCutsceneTrigger.SkipOnNextLoad();
            LevelProgressionManager.TryLoadScene(level1SceneName);
        }
        currentY += 62f;

        // NIVEL 2
        bool lvl2Unlocked = LevelProgressionManager.IsLevelUnlocked(2);
        string lvl2Label = lvl2Unlocked ? "NIVEL 2 — TALLERES DE TALAVERA" : "NIVEL 2 — TALLERES DE TALAVERA [BLOQUEADO]";
        if (DrawPlankButton(new Rect(btnX, currentY, btnW, 52f), lvl2Label, !lvl2Unlocked))
        {
            ProceduralSoundManager.Instance?.PlayButtonClick();
            if (lvl2Unlocked)
            {
                SetWorldSimulationPaused(false);
                if (LevelProgressionManager.TryLoadScene(level2SceneName))
                {
                    isPaused = false;
                    isVictoryActive = false;
                }
                else
                {
                    SetWorldSimulationPaused(true);
                    Notify("Escena Nivel2 no registrada en Build Settings.");
                }
            }
            else
            {
                Notify("Nivel 2 bloqueado: derrota primero a El Vendedor Viral.");
            }
        }
        currentY += 62f;

        // NIVEL 3
        bool lvl3Unlocked = LevelProgressionManager.IsLevelUnlocked(3);
        string lvl3Label = lvl3Unlocked ? "NIVEL 3 — CALLEJONES DE GUANAJUATO" : "NIVEL 3 — CALLEJONES DE GUANAJUATO [BLOQUEADO]";
        if (DrawPlankButton(new Rect(btnX, currentY, btnW, 52f), lvl3Label, !lvl3Unlocked))
        {
            ProceduralSoundManager.Instance?.PlayButtonClick();
            if (lvl3Unlocked)
            {
                SetWorldSimulationPaused(false);
                if (LevelProgressionManager.TryLoadScene(level3SceneName))
                {
                    isPaused = false;
                    isVictoryActive = false;
                }
                else
                {
                    SetWorldSimulationPaused(true);
                    Notify("Escena Nivel3 no registrada en Build Settings.");
                }
            }
            else
            {
                Notify("Nivel 3 bloqueado: completa primero el Nivel 2.");
            }
        }
        currentY += 66f;

        float halfW = (btnW - 14f) / 2f;
        if (DrawPlankButton(new Rect(btnX, currentY, halfW, 46f), "MENÚ PRINCIPAL"))
        {
            ProceduralSoundManager.Instance?.PlayButtonClick();
            GoToMainMenuOrLevelSelect();
        }

        if (DrawPlankButton(new Rect(btnX + halfW + 14f, currentY, halfW, 46f), "VOLVER"))
        {
            ProceduralSoundManager.Instance?.PlayButtonClick();
            currentView = isVictoryActive ? MenuSubView.Victory : MenuSubView.PauseMain;
        }

        DrawNotificationFooter(panelX, panelY + panelH - 36f, panelW);
    }

    // ═══════════════════════════════════════════════════════════
    // 3. PANEL DE AJUSTES IN-GAME
    // ═══════════════════════════════════════════════════════════

    private void DrawSettingsPanel(float screenW, float screenH)
    {
        float panelW = Mathf.Min(480f, screenW * 0.90f);
        float panelH = Mathf.Min(515f, screenH * 0.94f);
        float panelX = (screenW - panelW) / 2f;
        float panelY = (screenH - panelH) / 2f;

        DrawPanelFrame(panelX, panelY, panelW, panelH, "AJUSTES");

        float contentX = panelX + 38f;
        float contentW = panelW - 76f;
        float currentY = panelY + 72f;

        int musicPct = Mathf.RoundToInt((audioMuted ? 0f : currentMusicVolume) * 100f);
        GUI.Label(new Rect(contentX, currentY, contentW, 22), $"VOLUMEN DE MÚSICA: {musicPct}%", labelStyle);
        currentY += 23f;

        float newMusicVol = GUI.HorizontalSlider(new Rect(contentX, currentY, contentW, 20), currentMusicVolume, 0f, 1f);
        if (Mathf.Abs(newMusicVol - currentMusicVolume) > 0.005f)
        {
            currentMusicVolume = newMusicVol;
            audioMuted = false;
            AudioListener.volume = 1f;
            LevelProgressionManager.SetMusicVolume(currentMusicVolume);
            ProceduralSoundManager.Instance?.SetMusicVolume(currentMusicVolume);
        }
        currentY += 27f;

        int sfxPct = Mathf.RoundToInt((audioMuted ? 0f : currentSfxVolume) * 100f);
        GUI.Label(new Rect(contentX, currentY, contentW, 22), $"VOLUMEN DE EFECTOS: {sfxPct}%", labelStyle);
        currentY += 23f;

        float newSfxVol = GUI.HorizontalSlider(new Rect(contentX, currentY, contentW, 20), currentSfxVolume, 0f, 1f);
        if (Mathf.Abs(newSfxVol - currentSfxVolume) > 0.005f)
        {
            currentSfxVolume = newSfxVol;
            audioMuted = false;
            AudioListener.volume = 1f;
            LevelProgressionManager.SetSfxVolume(currentSfxVolume);
            ProceduralSoundManager.Instance?.SetSfxVolume(currentSfxVolume);
            if (Time.unscaledTime >= nextSfxPreviewTime)
            {
                nextSfxPreviewTime = Time.unscaledTime + 0.22f;
                ProceduralSoundManager.Instance?.PlayButtonClick();
            }
        }
        currentY += 27f;

        GUI.Label(
            new Rect(contentX, currentY, contentW, 22),
            $"FOV / ZOOM DE CÁMARA: {LevelProgressionManager.FormatCameraZoomLabel(currentCameraZoom)}",
            labelStyle
        );
        currentY += 24f;

        float newZoom = GUI.HorizontalSlider(
            new Rect(contentX, currentY, contentW, 20),
            currentCameraZoom,
            LevelProgressionManager.MIN_CAMERA_ZOOM,
            LevelProgressionManager.MAX_CAMERA_ZOOM
        );
        if (Mathf.Abs(newZoom - currentCameraZoom) > 0.005f)
        {
            currentCameraZoom = newZoom;
            LevelProgressionManager.SetCameraZoom(currentCameraZoom);
            CameraFollow.ApplyZoomToActiveCamera(currentCameraZoom, true);
        }
        currentY += 25f;

        if (DrawPlankButton(new Rect(contentX, currentY, contentW, 38f), "RESTABLECER ZOOM (100%)"))
        {
            ProceduralSoundManager.Instance?.PlayButtonClick();
            currentCameraZoom = LevelProgressionManager.DEFAULT_CAMERA_ZOOM;
            LevelProgressionManager.SetCameraZoom(currentCameraZoom);
            CameraFollow.ApplyZoomToActiveCamera(currentCameraZoom, true);
        }
        currentY += 44f;

        string muteText = audioMuted ? "SONIDO: SILENCIADO" : "SONIDO: ACTIVO";
        if (DrawPlankButton(new Rect(contentX, currentY, contentW, 42f), muteText))
        {
            ToggleAudio();
            ProceduralSoundManager.Instance?.PlayButtonClick();
        }
        currentY += 48f;

        string screenText = Screen.fullScreen ? "PANTALLA: COMPLETA" : "PANTALLA: VENTANA";
        if (DrawPlankButton(new Rect(contentX, currentY, contentW, 42f), screenText))
        {
            ProceduralSoundManager.Instance?.PlayButtonClick();
            isFullscreen = !Screen.fullScreen;
            Screen.fullScreen = isFullscreen;
            PlayerPrefs.SetInt(LevelProgressionManager.FULLSCREEN_KEY, isFullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }
        currentY += 48f;

        int touchMode = LevelProgressionManager.GetTouchControlsMode();
        string touchText = $"CONTROLES TÁCTILES: {LevelProgressionManager.FormatTouchControlsModeLabel(touchMode)}";
        if (DrawPlankButton(new Rect(contentX, currentY, contentW, 42f), touchText))
        {
            ProceduralSoundManager.Instance?.PlayButtonClick();
            int nextMode = (touchMode + 1) % 3;
            LevelProgressionManager.SetTouchControlsMode(nextMode);
            MobileTouchControls.Instance?.RefreshVisibility();
        }
        currentY += 52f;

        if (DrawPlankButton(new Rect(contentX, currentY, contentW, 44f), "GUARDAR Y VOLVER"))
        {
            ProceduralSoundManager.Instance?.PlayButtonClick();
            PlayerPrefs.Save();
            currentView = isVictoryActive ? MenuSubView.Victory : MenuSubView.PauseMain;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 4. PANTALLA DE VICTORIA (FIN DE NIVEL / GRAN FINAL)
    // ═══════════════════════════════════════════════════════════

    private void DrawVictoryPanel(float screenW, float screenH)
    {
        float panelW = Mathf.Min(520f, screenW * 0.92f);
        float panelH = 415f;
        float panelX = (screenW - panelW) / 2f;
        float panelY = (screenH - panelH) / 2f;

        bool isFinalVictory = lastCompletedLevel >= 3;
        string header = isFinalVictory ? "VICTORIA FINAL" : $"NIVEL {lastCompletedLevel} COMPLETADO";

        DrawPanelFrame(panelX, panelY, panelW, panelH, header);

        GUI.Label(new Rect(panelX + 30, panelY + 66, panelW - 60, 26),
            $"Jefe derrotado: {defeatedBossTitle}", subtitleStyle);

        GUIStyle highlightSub = new GUIStyle(subtitleStyle);
        highlightSub.normal.textColor = new Color(0.955f, 0.785f, 0.340f);
        int nextLvl = Mathf.Clamp(lastCompletedLevel + 1, 2, 3);
        string statusLine = isFinalVictory
            ? $"{victoryMessage}\nLas Fiestas Patronales de Dolores Hidalgo están a salvo."
            : $"{victoryMessage}\nNivel {nextLvl} desbloqueado.";

        GUI.Label(new Rect(panelX + 30, panelY + 96, panelW - 60, 48), statusLine, highlightSub);

        float btnW = panelW - 84f;
        float btnX = panelX + 42f;
        float currentY = panelY + 162f;

        if (!isFinalVictory)
        {
            if (DrawPlankButton(new Rect(btnX, currentY, btnW, 52f), $"CONTINUAR AL NIVEL {nextLvl}"))
            {
                ProceduralSoundManager.Instance?.PlayButtonClick();
                string targetScene = nextLvl == 2 ? level2SceneName : level3SceneName;
                if (!LevelProgressionManager.TryLoadScene(targetScene))
                {
                    currentView = MenuSubView.LevelSelect;
                    Notify($"Nivel {nextLvl} desbloqueado. Registra la escena en Build Settings.");
                }
                else
                {
                    isPaused = false;
                    isVictoryActive = false;
                }
            }
        }
        else
        {
            if (DrawPlankButton(new Rect(btnX, currentY, btnW, 52f), "JUGAR DE NUEVO DESDE NIVEL 1"))
            {
                ProceduralSoundManager.Instance?.PlayButtonClick();
                isPaused = false;
                isVictoryActive = false;
                Level1IntroCutsceneTrigger.ResetIntroForNewPlay();
                LevelProgressionManager.TryLoadScene(level1SceneName);
            }
        }
        currentY += 62f;

        if (DrawPlankButton(new Rect(btnX, currentY, btnW, 52f), "MENÚ PRINCIPAL / SELECCIONAR NIVEL"))
        {
            ProceduralSoundManager.Instance?.PlayButtonClick();
            GoToMainMenuOrLevelSelect();
        }
        currentY += 62f;

        if (DrawPlankButton(new Rect(btnX, currentY, btnW, 48f), $"REPETIR NIVEL {lastCompletedLevel}"))
        {
            ProceduralSoundManager.Instance?.PlayButtonClick();
            RestartGame();
        }

        DrawNotificationFooter(panelX, panelY + panelH - 36f, panelW);
    }

    // ═══════════════════════════════════════════════════════════
    // UTILIDADES DE DIBUJO FORMAL PIXEL-ART
    // ═══════════════════════════════════════════════════════════

    private bool DrawPlankButton(Rect rect, string label, bool locked = false)
    {
        return GUI.Button(rect, label, locked ? lockedPlankStyle : plankButtonStyle);
    }

    private void DrawPanelFrame(float panelX, float panelY, float panelW, float panelH, string headerTitle)
    {
        // Marco pixel-art formal idéntico al del Menú Principal (9-sliced)
        GUI.Box(new Rect(panelX, panelY, panelW, panelH), GUIContent.none, panelBoxStyle);

        // Título formal con sombra oscura limpia + línea separadora dorada sutil
        GUI.Label(new Rect(panelX + 2, panelY + 20, panelW, 34), headerTitle, shadowStyle);
        GUI.Label(new Rect(panelX, panelY + 18, panelW, 34), headerTitle, titleStyle);
        GUI.DrawTexture(new Rect(panelX + 38, panelY + 56, panelW - 76, 2), goldLineTex);
    }

    private void DrawNotificationFooter(float panelX, float yPos, float panelW)
    {
        if (notificationTimer > 0f && !string.IsNullOrEmpty(statusNotification))
        {
            GUIStyle noteStyle = new GUIStyle(subtitleStyle) { fontSize = 12 };
            noteStyle.normal.textColor = new Color(0.955f, 0.785f, 0.340f);
            GUI.Label(new Rect(panelX + 20, yPos, panelW - 40, 26), statusNotification, noteStyle);
        }
    }

    private Texture2D MakeSolidTexture(int width, int height, Color col)
    {
        Texture2D tex = new Texture2D(width, height);
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        tex.SetPixels(pix);
        tex.Apply();
        return tex;
    }
}
