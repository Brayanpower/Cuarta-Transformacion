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
    public GameObject panelSettings;
    public GameObject panelCredits;

    [Header("Botones del Menú Principal")]
    public Button btnPlayDirect;
    public Button btnWatchStory;
    public Button btnOpenLevelSelect;
    public Button btnOpenSettings;
    public Button btnOpenCredits;
    public Button btnQuitGame;

    [Header("Botón Volver desde Créditos")]
    public Button btnBackFromCredits;

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
        EnsureButtonListenersConnected();
        ShowMainPanel();
        RefreshLevelSelectionUI();
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

    private void SetAllPanelsActive(bool state)
    {
        if (panelMain != null) panelMain.SetActive(state);
        if (panelLevelSelect != null) panelLevelSelect.SetActive(state);
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
