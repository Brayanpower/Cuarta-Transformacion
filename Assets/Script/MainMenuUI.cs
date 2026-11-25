using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Controlador principal de UI para el Menú Temático de "Cuarta Transformación".
/// Gestiona la navegación entre Pantalla de Inicio, Selección de Nivel (Mapa del Municipio)
/// y Ajustes del Pueblo, sincronizando el estado de bloqueo con LevelProgressionManager.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Paneles del Canvas")]
    public GameObject panelMain;
    public GameObject panelLevelSelect;
    public GameObject panelControls;
    public GameObject panelSettings;
    public GameObject panelCredits;

    [Header("Botones del Menú Principal")]
    public Button btnPlayDirect;
    public Button btnWatchStory;
    public Button btnOpenLevelSelect;
    public Button btnOpenControls;
    public Button btnOpenSettings;
    public Button btnOpenCredits;
    public Button btnQuitGame;

    [Header("Botón Volver desde Créditos y Controles")]
    public Button btnBackFromCredits;
    public Button btnBackFromControls;

    [Header("Cinemática de Historia")]
    public StoryCutsceneController storyCutsceneController;
    public bool playIntroOnFirstPlay = true;
    private const string INTRO_SEEN_KEY = "CuartaTransformacion_IntroSeen";

    [Header("Tarjetas / Botones de Selección de Nivel")]
    public Button btnLevel1;
    public Button btnLevel2;
    public Button btnLevel3;
    public Button btnDebugToggleProgress;
    public Button btnBackFromLevels;

    // Propiedades de compatibilidad con el generador de menú (CuartaTransformacionMenuBuilder)
    public Button btnLevel1Card
    {
        get => btnLevel1;
        set => btnLevel1 = value;
    }

    public Button btnLevel2Card
    {
        get => btnLevel2;
        set => btnLevel2 = value;
    }

    public Button btnLevel3Card
    {
        get => btnLevel3;
        set => btnLevel3 = value;
    }

    [Header("Textos de Estado de las Tarjetas")]
    public Text statusTextLevel1;
    public Text statusTextLevel2;
    public Text statusTextLevel3;

    [Header("Overlays de Candado (Bloqueado)")]
    public GameObject lockOverlayLevel2;
    public GameObject lockOverlayLevel3;

    [Header("Botón Volver desde Ajustes")]
    public Button btnSaveAndBackFromSettings;

    [Header("Nombres de Escenas (Build Settings)")]
    public string sceneLevel1 = "Nivel1";
    public string sceneLevel2 = "Nivel2";
    public string sceneLevel3 = "Nivel3";

    private void Start()
    {
        Time.timeScale = 1f;
        if (GetComponent<MainMenuBackgroundAnimator>() == null)
            gameObject.AddComponent<MainMenuBackgroundAnimator>();

        EnsureControlsScreenExists();
        EnsureButtonListenersConnected();
        if (Application.platform == RuntimePlatform.WebGLPlayer && btnQuitGame != null)
            btnQuitGame.gameObject.SetActive(false);
        ShowMainPanel();
        RefreshLevelSelectionUI();
    }

    private void EnsureControlsScreenExists()
    {
        if (panelMain != null && btnOpenControls == null && btnOpenCredits != null)
        {
            // Reacomodar limpiamente la columna del menú principal para albergar 6 tablones formales
            SetChildLayout(panelMain.transform, "Btn_Jugar",            new Vector2(0f,  230f), new Vector2(540f, 92f));
            SetChildLayout(panelMain.transform, "Btn_SeleccionarNivel", new Vector2(0f,  128f), new Vector2(540f, 92f));
            SetChildLayout(panelMain.transform, "Btn_Ajustes",          new Vector2(0f,  -76f), new Vector2(540f, 92f));
            SetChildLayout(panelMain.transform, "Btn_Creditos",         new Vector2(0f, -178f), new Vector2(540f, 92f));
            SetChildLayout(panelMain.transform, "Btn_Salir",            new Vector2(0f, -280f), new Vector2(540f, 92f));

            GameObject ctrlBtnObj = Object.Instantiate(btnOpenCredits.gameObject, panelMain.transform);
            ctrlBtnObj.name = "Btn_ComoJugar";
            SetChildLayout(panelMain.transform, "Btn_ComoJugar", new Vector2(0f, 26f), new Vector2(540f, 92f));
            Text lbl = ctrlBtnObj.GetComponentInChildren<Text>();
            if (lbl != null)
            {
                lbl.text = "CÓMO JUGAR";
                lbl.fontSize = 30;
            }
            Button createdBtn = ctrlBtnObj.GetComponent<Button>();
            if (createdBtn != null)
            {
                createdBtn.onClick = new Button.ButtonClickedEvent();
                btnOpenControls = createdBtn;
            }
        }

        if (panelControls == null && panelCredits != null)
        {
            GameObject ctrlPanelObj = Object.Instantiate(panelCredits, panelCredits.transform.parent);
            ctrlPanelObj.name = "Panel_Controls";
            panelControls = ctrlPanelObj;

            Transform frame = ctrlPanelObj.transform.Find("Frame_Creditos");
            if (frame != null)
            {
                frame.name = "Frame_Controles";
                RectTransform frt = frame as RectTransform;
                if (frt != null)
                    frt.sizeDelta = new Vector2(980f, 700f);

                Transform titleTf = frame.Find("Title_Creditos");
                if (titleTf != null)
                {
                    titleTf.name = "Title_Controles";
                    Text t = titleTf.GetComponent<Text>();
                    if (t != null) t.text = "CÓMO JUGAR Y CONTROLES";
                    SetChildLayout(frame, "Title_Controles", new Vector2(0f, 265f), new Vector2(760f, 56f));
                }

                Transform bodyTf = frame.Find("Body_Creditos");
                if (bodyTf != null)
                {
                    bodyTf.name = "Body_Controles";
                    Text b = bodyTf.GetComponent<Text>();
                    if (b != null)
                    {
                        b.fontSize = 23;
                        b.text =
                            "MOVERSE:  Teclas A / D o Flechas Izquierda / Derecha\n\n" +
                            "SALTAR / PISOTÓN:  Espacio (salta sobre enemigos para rebotar)\n\n" +
                            "DASH (ESQUIVE):  Left Shift (atraviesa golpes y proyectiles)\n\n" +
                            "ESPADAZO:  Clic Izquierdo o Enter\n\n" +
                            "PAUSA:  Tecla ESC (ajustes de música, efectos, zoom y nivel)";
                    }
                    SetChildLayout(frame, "Body_Controles", new Vector2(0f, 15f), new Vector2(840f, 380f));
                }

                Transform backTf = frame.Find("Btn_VolverCreditos");
                if (backTf != null)
                {
                    backTf.name = "Btn_VolverControles";
                    SetChildLayout(frame, "Btn_VolverControles", new Vector2(0f, -245f), new Vector2(400f, 84f));
                    Button backBtn = backTf.GetComponent<Button>();
                    if (backBtn != null)
                    {
                        backBtn.onClick = new Button.ButtonClickedEvent();
                        btnBackFromControls = backBtn;
                    }
                }
            }
        }
    }

    private static void SetChildLayout(Transform parent, string name, Vector2 anchoredPos, Vector2 size)
    {
        Transform tf = parent.Find(name);
        if (tf == null)
            return;

        RectTransform rt = tf as RectTransform;
        if (rt != null)
        {
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }
    }

    /// <summary>
    /// Garantiza que todos los botones respondan tanto por eventos persistentes de Inspector
    /// como en tiempo de ejecución.
    /// </summary>
    private void EnsureButtonListenersConnected()
    {
        if (btnPlayDirect != null)
        {
            btnPlayDirect.onClick.RemoveListener(PlayLevel1Direct);
            btnPlayDirect.onClick.AddListener(PlayLevel1Direct);
        }

        if (btnOpenLevelSelect != null)
        {
            btnOpenLevelSelect.onClick.RemoveListener(ShowLevelSelectPanel);
            btnOpenLevelSelect.onClick.AddListener(ShowLevelSelectPanel);
        }

        if (btnOpenControls != null)
        {
            btnOpenControls.onClick.RemoveListener(ShowControlsPanel);
            btnOpenControls.onClick.AddListener(ShowControlsPanel);
        }

        if (btnBackFromControls != null)
        {
            btnBackFromControls.onClick.RemoveListener(ShowMainPanel);
            btnBackFromControls.onClick.AddListener(ShowMainPanel);
        }

        if (btnOpenSettings != null)
        {
            btnOpenSettings.onClick.RemoveListener(ShowSettingsPanel);
            btnOpenSettings.onClick.AddListener(ShowSettingsPanel);
        }

        if (btnOpenCredits != null)
        {
            btnOpenCredits.onClick.RemoveListener(ShowCreditsPanel);
            btnOpenCredits.onClick.AddListener(ShowCreditsPanel);
        }

        if (btnBackFromCredits != null)
        {
            btnBackFromCredits.onClick.RemoveListener(ShowMainPanel);
            btnBackFromCredits.onClick.AddListener(ShowMainPanel);
        }

        if (btnQuitGame != null)
        {
            btnQuitGame.onClick.RemoveListener(QuitGame);
            btnQuitGame.onClick.AddListener(QuitGame);
        }

        if (btnLevel1 != null)
        {
            btnLevel1.onClick.RemoveListener(LoadLevel1);
            btnLevel1.onClick.AddListener(LoadLevel1);
        }

        if (btnLevel2 != null)
        {
            btnLevel2.onClick.RemoveListener(LoadLevel2);
            btnLevel2.onClick.AddListener(LoadLevel2);
        }

        if (btnLevel3 != null)
        {
            btnLevel3.onClick.RemoveListener(LoadLevel3);
            btnLevel3.onClick.AddListener(LoadLevel3);
        }

        if (btnDebugToggleProgress != null)
        {
            btnDebugToggleProgress.onClick.RemoveListener(ToggleOrResetProgressDebug);
            btnDebugToggleProgress.onClick.AddListener(ToggleOrResetProgressDebug);
        }

        if (btnBackFromLevels != null)
        {
            btnBackFromLevels.onClick.RemoveListener(ShowMainPanel);
            btnBackFromLevels.onClick.AddListener(ShowMainPanel);
        }

        if (btnSaveAndBackFromSettings != null)
        {
            btnSaveAndBackFromSettings.onClick.RemoveListener(ShowMainPanel);
            btnSaveAndBackFromSettings.onClick.AddListener(ShowMainPanel);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // NAVEGACIÓN ENTRE PANTALLAS
    // ═══════════════════════════════════════════════════════════

    public void ShowMainPanel()
    {
        ProceduralSoundManager.Instance?.PlayButtonClick();
        SetAllPanelsActive(false);
        if (panelMain != null) panelMain.SetActive(true);
    }

    public void ShowLevelSelectPanel()
    {
        ProceduralSoundManager.Instance?.PlayButtonClick();
        SetAllPanelsActive(false);
        if (panelLevelSelect != null)
        {
            panelLevelSelect.SetActive(true);
            RefreshLevelSelectionUI();
        }
    }

    public void ShowSettingsPanel()
    {
        ProceduralSoundManager.Instance?.PlayButtonClick();
        SetAllPanelsActive(false);
        if (panelSettings != null) panelSettings.SetActive(true);
    }

    public void ShowCreditsPanel()
    {
        ProceduralSoundManager.Instance?.PlayButtonClick();
        SetAllPanelsActive(false);
        if (panelCredits != null) panelCredits.SetActive(true);
    }

    public void ShowControlsPanel()
    {
        ProceduralSoundManager.Instance?.PlayButtonClick();
        SetAllPanelsActive(false);
        if (panelControls != null) panelControls.SetActive(true);
    }

    private void SetAllPanelsActive(bool state)
    {
        if (panelMain != null) panelMain.SetActive(state);
        if (panelLevelSelect != null) panelLevelSelect.SetActive(state);
        if (panelControls != null) panelControls.SetActive(state);
        if (panelSettings != null) panelSettings.SetActive(state);
        if (panelCredits != null) panelCredits.SetActive(state);
    }

    // ═══════════════════════════════════════════════════════════
    // ESTADO DE TARJETAS DE NIVEL (PlayerPrefs)
    // ═══════════════════════════════════════════════════════════

    public void RefreshLevelSelectionUI()
    {
        int unlockedLevel = LevelProgressionManager.GetUnlockedLevel();

        // Nivel 1: Jardín Principal (Siempre disponible)
        ConfigureLevelCard(btnLevel1, null, statusTextLevel1, true);

        // Nivel 2: Talleres de Talavera
        bool lvl2Unlocked = unlockedLevel >= 2;
        ConfigureLevelCard(btnLevel2, lockOverlayLevel2, statusTextLevel2, lvl2Unlocked);

        // Nivel 3: Callejones de Guanajuato
        bool lvl3Unlocked = unlockedLevel >= 3;
        ConfigureLevelCard(btnLevel3, lockOverlayLevel3, statusTextLevel3, lvl3Unlocked);
    }

    private void ConfigureLevelCard(Button btn, GameObject lockOverlay, Text statusLabel, bool isUnlocked)
    {
        if (btn != null)
        {
            btn.interactable = isUnlocked;

            CanvasGroup cg = btn.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = isUnlocked ? 1f : 0.75f;
            }
        }

        if (lockOverlay != null)
        {
            lockOverlay.SetActive(!isUnlocked);
        }

        if (statusLabel != null)
        {
            statusLabel.text = isUnlocked ? "DISPONIBLE" : "BLOQUEADO";

            if (statusLabel.transform.parent != null)
            {
                Image footerBg = statusLabel.transform.parent.GetComponent<Image>();
                if (footerBg != null)
                {
                    footerBg.color = isUnlocked
                        ? Color.white
                        : new Color(0.45f, 0.36f, 0.36f, 1f);
                }
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // CARGA DE NIVELES Y DEBUG
    // ═══════════════════════════════════════════════════════════

    public void PlayLevel1Direct()
    {
        LoadLevel1();
    }

    public void SelectLevel(int levelNumber)
    {
        switch (levelNumber)
        {
            case 1:
                LoadLevel1();
                break;
            case 2:
                LoadLevel2();
                break;
            case 3:
                LoadLevel3();
                break;
            default:
                LoadLevel1();
                break;
        }
    }

    public void LoadLevel1()
    {
        ProceduralSoundManager.Instance?.PlayButtonClick();
        Level1IntroCutsceneTrigger.ResetIntroForNewPlay();
        LevelProgressionManager.TryLoadScene(sceneLevel1);
    }

    public void LoadLevel2()
    {
        ProceduralSoundManager.Instance?.PlayButtonClick();
        if (LevelProgressionManager.IsLevelUnlocked(2))
        {
            LevelProgressionManager.TryLoadScene(sceneLevel2);
        }
        else
        {
            Debug.Log("[MainMenuUI] Nivel 2 bloqueado. Derrota primero a El Vendedor Viral (Don Chava).");
        }
    }

    public void LoadLevel3()
    {
        ProceduralSoundManager.Instance?.PlayButtonClick();
        if (LevelProgressionManager.IsLevelUnlocked(3))
        {
            LevelProgressionManager.TryLoadScene(sceneLevel3);
        }
        else
        {
            Debug.Log("[MainMenuUI] Nivel 3 bloqueado. Completa primero el Nivel 2.");
        }
    }

    public void DebugToggleUnlockOrResetProgress()
    {
        ToggleOrResetProgressDebug();
    }

    public void ToggleOrResetProgressDebug()
    {
        ProceduralSoundManager.Instance?.PlayButtonClick();
        LevelProgressionManager.ToggleDebugProgression();
        RefreshLevelSelectionUI();
    }

    public void ResetProgressButton()
    {
        LevelProgressionManager.ResetProgression();
        RefreshLevelSelectionUI();
    }

    public void QuitGame()
    {
        Debug.Log("[MainMenuUI] Saliendo del juego...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
