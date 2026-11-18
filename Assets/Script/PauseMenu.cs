using System.IO;
using UnityEngine;
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

    private enum MenuSubView
    {
        PauseMain,
        LevelSelect,
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
    private float currentVolume = 0.85f;
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
        currentVolume = PlayerPrefs.GetFloat(LevelProgressionManager.VOLUME_KEY, 0.85f);
        isFullscreen = PlayerPrefs.GetInt(LevelProgressionManager.FULLSCREEN_KEY, 1) == 1;
        currentCameraZoom = LevelProgressionManager.GetCameraZoom();
        AudioListener.volume = currentVolume;
        Screen.fullScreen = isFullscreen;
        CameraFollow.ApplyZoomToActiveCamera(currentCameraZoom, true);
    }

    void Update()
    {
        if (notificationTimer > 0f)
            notificationTimer -= Time.unscaledDeltaTime;

        // No abrir el menú de pausa en MainMenu ni durante transiciones/tarjetas estilo Sonic
        if (SceneManager.GetActiveScene().name == mainMenuSceneName || BlockPauseDuringTransition)
            return;

        if (Input.GetKeyDown(pauseKey) && !isVictoryActive)
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

    // ═══════════════════════════════════════════════════════════
    // API PÚBLICA DE PAUSA Y VICTORIA
    // ═══════════════════════════════════════════════════════════

    public void PauseGame()
    {
        isPaused = true;
        currentView = MenuSubView.PauseMain;
        statusNotification = "";
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        isVictoryActive = false;
        currentView = MenuSubView.PauseMain;
        Time.timeScale = 1f;
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
        Time.timeScale = 0f;
    }

    public void OpenLevelSelection()
    {
        isPaused = true;
        currentView = MenuSubView.LevelSelect;
        statusNotification = "";
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
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
            Time.timeScale = 1f;
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
        Time.timeScale = 1f;
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
        AudioListener.volume = audioMuted ? 0f : (currentVolume > 0.05f ? currentVolume : 0.85f);
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
        float panelH = 420f;
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
            Time.timeScale = 1f;
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
                if (LevelProgressionManager.TryLoadScene(level2SceneName))
                {
                    isPaused = false;
                    isVictoryActive = false;
                }
                else
                {
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
                if (LevelProgressionManager.TryLoadScene(level3SceneName))
                {
                    isPaused = false;
                    isVictoryActive = false;
                }
                else
                {
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
        float panelH = 475f;
        float panelX = (screenW - panelW) / 2f;
        float panelY = (screenH - panelH) / 2f;

        DrawPanelFrame(panelX, panelY, panelW, panelH, "AJUSTES");

        float contentX = panelX + 40f;
        float contentW = panelW - 80f;
        float currentY = panelY + 80f;

        int volPercent = Mathf.RoundToInt((audioMuted ? 0f : currentVolume) * 100f);
        GUI.Label(new Rect(contentX, currentY, contentW, 24), $"VOLUMEN GENERAL: {volPercent}%", labelStyle);
        currentY += 28f;

        float newVol = GUI.HorizontalSlider(new Rect(contentX, currentY, contentW, 22), currentVolume, 0f, 1f);
        if (Mathf.Abs(newVol - currentVolume) > 0.005f)
        {
            currentVolume = newVol;
            audioMuted = false;
            AudioListener.volume = currentVolume;
            PlayerPrefs.SetFloat(LevelProgressionManager.VOLUME_KEY, currentVolume);
            PlayerPrefs.Save();
        }
        currentY += 34f;

        GUI.Label(
            new Rect(contentX, currentY, contentW, 24),
            $"FOV / ZOOM DE CÁMARA: {LevelProgressionManager.FormatCameraZoomLabel(currentCameraZoom)}",
            labelStyle
        );
        currentY += 28f;

        float newZoom = GUI.HorizontalSlider(
            new Rect(contentX, currentY, contentW, 22),
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
        currentY += 30f;

        if (DrawPlankButton(new Rect(contentX, currentY, contentW, 40f), "RESTABLECER ZOOM (100%)"))
        {
            ProceduralSoundManager.Instance?.PlayButtonClick();
            currentCameraZoom = LevelProgressionManager.DEFAULT_CAMERA_ZOOM;
            LevelProgressionManager.SetCameraZoom(currentCameraZoom);
            CameraFollow.ApplyZoomToActiveCamera(currentCameraZoom, true);
        }
        currentY += 52f;

        string muteText = audioMuted ? "SONIDO: SILENCIADO" : "SONIDO: ACTIVO";
        if (DrawPlankButton(new Rect(contentX, currentY, contentW, 46f), muteText))
        {
            ToggleAudio();
            ProceduralSoundManager.Instance?.PlayButtonClick();
        }
        currentY += 56f;

        string screenText = Screen.fullScreen ? "PANTALLA: COMPLETA" : "PANTALLA: VENTANA";
        if (DrawPlankButton(new Rect(contentX, currentY, contentW, 46f), screenText))
        {
            ProceduralSoundManager.Instance?.PlayButtonClick();
            isFullscreen = !Screen.fullScreen;
            Screen.fullScreen = isFullscreen;
            PlayerPrefs.SetInt(LevelProgressionManager.FULLSCREEN_KEY, isFullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }
        currentY += 62f;

        if (DrawPlankButton(new Rect(contentX, currentY, contentW, 48f), "GUARDAR Y VOLVER"))
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
