#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;

/// <summary>
/// Herramienta de Editor para "Cuarta Transformación":
/// 1) Tools > Cuarta Transformacion > Construir Menu Tematico
///    Construye MainMenu.unity con diseño formal, limpio y pixel-art clásico:
///    - Fondo nocturno pixel-art de la Parroquia de Dolores Hidalgo limpio y sin marcos invasivos.
///    - Franja de suelo de cantera colonial pixel-art en la base + señalización de madera y farol.
///    - Columna central de 5 botones tallados en tablón de madera pixel-art con transición SpriteSwap:
///      JUGAR, SELECCIONAR NIVEL, AJUSTES, CRÉDITOS y SALIR.
///    - Subpantallas formales y coherentes (Selección de Nivel con miniaturas reales, Ajustes y Créditos).
/// 2) Tools > Cuarta Transformacion > Corregir Objetos y Mapa Nivel 1
/// 3) Tools > Cuarta Transformacion > Mejorar Mapa Nivel 1
/// </summary>
public static class CuartaTransformacionMenuBuilder
{
    // Paleta formal colonial nocturna
    private static readonly Color ColNocheFondo    = new Color(0.065f, 0.048f, 0.115f, 1.00f);
    private static readonly Color ColDoradoFormal  = new Color(0.955f, 0.785f, 0.340f, 1.00f);
    private static readonly Color ColCremaFormal   = new Color(0.965f, 0.940f, 0.875f, 1.00f);
    private static readonly Color ColTextoTablon   = new Color(0.140f, 0.085f, 0.055f, 1.00f);
    private static readonly Color ColSombraTablon  = new Color(0.960f, 0.800f, 0.560f, 0.65f);
    private static readonly Color ColTerracota     = new Color(0.760f, 0.310f, 0.210f, 1.00f);
    private static readonly Color ColAzulTalavera  = new Color(0.180f, 0.360f, 0.500f, 1.00f);

    // Rutas de sprites pixel-art formales
    private const string PathPlankNormal   = "Assets/Images/UI/UI_PlankButton_Normal.png";
    private const string PathPlankHover    = "Assets/Images/UI/UI_PlankButton_Hover.png";
    private const string PathPlankPressed  = "Assets/Images/UI/UI_PlankButton_Pressed.png";
    private const string PathFormalPanel   = "Assets/Images/UI/UI_PlankPanel_Formal.png";
    private const string PathSignpost      = "Assets/Images/UI/UI_Signpost_Colonial.png";
    private const string PathLockShield    = "Assets/Images/UI/UI_Badge_LockShield.png";
    private const string PathLanternPost   = "Assets/Images/Medieval/Medieval_LanternPost.png";

    private const string PathBgN1          = "Assets/Images/Backgrounds/Bg_Nivel1_DoloresNoche.png";
    private const string PathBgN2          = "Assets/Images/Backgrounds/Bg_Nivel2_TalaveraNoche.png";
    private const string PathBgN3          = "Assets/Images/Backgrounds/Bg_Nivel3_CallejonesNoche.png";
    private const string PathGroundTop     = "Assets/Images/GroundTiles/Tile_N1_Cantera_Top.png";
    private const string PathGroundFill    = "Assets/Images/GroundTiles/Tile_N1_Cantera_Fill.png";

    private struct UIAssets
    {
        public Sprite plankNormal;
        public Sprite plankHover;
        public Sprite plankPressed;
        public Sprite formalPanel;
        public Sprite signpost;
        public Sprite lockShield;
        public Sprite lanternPost;
        public Sprite bgN1;
        public Sprite bgN2;
        public Sprite bgN3;
        public Sprite groundTop;
        public Sprite groundFill;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // OPCIÓN 1: CONSTRUIR MENÚ PRINCIPAL FORMAL PIXEL-ART (ESTILO REFERENCIA)
    // ═══════════════════════════════════════════════════════════════════════════

    [MenuItem("Tools/Cuarta Transformacion/Construir Menu Tematico", false, 1)]
    public static void BuildThematicMainMenu()
    {
        BuildThematicMainMenuInternal(true);
    }

    public static void BuildThematicMainMenuSilent()
    {
        BuildThematicMainMenuInternal(false);
    }

    private static void BuildThematicMainMenuInternal(bool showDialog)
    {
        AssetDatabase.Refresh();
        UIAssets ui = PrepareAndLoadUIAssets();

        string scenePath = "Assets/Scenes/MainMenu.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Cámara Principal
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = ColNocheFondo;
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        camObj.transform.position = new Vector3(0f, 0f, -10f);
        camObj.AddComponent<AudioListener>();

        // 2. EventSystem compatible con Input System nuevo y clásico
        CreateEventSystem();

        // 3. Canvas Principal (1920x1080, Scale With Screen Size)
        GameObject canvasObj = new GameObject("Canvas_MenuDoloresHidalgo");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        MainMenuUI mainMenuController = canvasObj.AddComponent<MainMenuUI>();
        SettingsMenuUI settingsController = canvasObj.AddComponent<SettingsMenuUI>();

        // 4. Paisaje nocturno limpio de Dolores Hidalgo + franja de suelo empedrado + señal colonial
        BuildScenicDoloresBackground(canvasObj.transform, ui);

        // 5. Pantallas formales: Inicio, Selección de Nivel, Ajustes y Créditos
        GameObject panelMain     = BuildMainScreen(canvasObj.transform, mainMenuController, ui);
        GameObject panelLevels   = BuildLevelSelectScreen(canvasObj.transform, mainMenuController, ui);
        GameObject panelSettings = BuildSettingsScreen(canvasObj.transform, mainMenuController, settingsController, ui);
        GameObject panelCredits  = BuildCreditsScreen(canvasObj.transform, mainMenuController, ui);

        mainMenuController.panelMain = panelMain;
        mainMenuController.panelLevelSelect = panelLevels;
        mainMenuController.panelSettings = panelSettings;
        mainMenuController.panelCredits = panelCredits;

        panelMain.SetActive(true);
        panelLevels.SetActive(false);
        panelSettings.SetActive(false);
        panelCredits.SetActive(false);

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, scenePath);
        RegisterScenesInBuildSettings();

        EditorUtility.SetDirty(mainMenuController);
        EditorUtility.SetDirty(settingsController);
        AssetDatabase.SaveAssets();

        Debug.Log("✅ [Cuarta Transformación] Menú Principal formal pixel-art construido en " + scenePath);
        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Menú Principal Construido",
                "Se ha actualizado MainMenu.unity con el diseño pixel-art formal:\n\n" +
                "• Fondo nocturno limpio de la Parroquia de Dolores Hidalgo con franja de cantera\n" +
                "• Columna central de 5 botones tallados en madera pixel-art (JUGAR, SELECCIONAR NIVEL, AJUSTES, CRÉDITOS, SALIR)\n" +
                "• Subpantallas limpias y formales sin elementos recargados.",
                "Aceptar"
            );
        }
    }

    private static UIAssets PrepareAndLoadUIAssets()
    {
        UIAssets ui = new UIAssets();
        ui.plankNormal  = ImportAndLoadCrispSprite(PathPlankNormal,  new Vector4(14f, 8f, 14f, 8f), 100f);
        ui.plankHover   = ImportAndLoadCrispSprite(PathPlankHover,   new Vector4(14f, 8f, 14f, 8f), 100f);
        ui.plankPressed = ImportAndLoadCrispSprite(PathPlankPressed, new Vector4(14f, 8f, 14f, 8f), 100f);
        ui.formalPanel  = ImportAndLoadCrispSprite(PathFormalPanel,  new Vector4(18f, 18f, 18f, 18f), 100f);
        ui.signpost     = ImportAndLoadCrispSprite(PathSignpost,     null, 100f);
        ui.lockShield   = ImportAndLoadCrispSprite(PathLockShield,   null, 100f);
        ui.lanternPost  = ImportAndLoadCrispSprite(PathLanternPost,  null, 100f);

        ui.bgN1       = ImportAndLoadCrispSprite(PathBgN1, null, 100f);
        ui.bgN2       = ImportAndLoadCrispSprite(PathBgN2, null, 100f);
        ui.bgN3       = ImportAndLoadCrispSprite(PathBgN3, null, 100f);
        ui.groundTop  = ImportAndLoadCrispSprite(PathGroundTop, null, 32f);
        ui.groundFill = ImportAndLoadCrispSprite(PathGroundFill, null, 32f);
        return ui;
    }

    public static Sprite ImportAndLoadCrispSprite(string assetPath, Vector4? sliceBorder, float ppu, Vector2? customPivot = null)
    {
        if (!System.IO.File.Exists(assetPath))
            return null;

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
            if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; changed = true; }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
            if (Mathf.Abs(importer.spritePixelsPerUnit - ppu) > 0.01f) { importer.spritePixelsPerUnit = ppu; changed = true; }
            if (sliceBorder.HasValue && importer.spriteBorder != sliceBorder.Value) { importer.spriteBorder = sliceBorder.Value; changed = true; }
            if (customPivot.HasValue)
            {
                TextureImporterSettings tis = new TextureImporterSettings();
                importer.ReadTextureSettings(tis);
                if (tis.spriteAlignment != (int)SpriteAlignment.Custom || Vector2.Distance(tis.spritePivot, customPivot.Value) > 0.001f)
                {
                    tis.spriteAlignment = (int)SpriteAlignment.Custom;
                    tis.spritePivot = customPivot.Value;
                    importer.SetTextureSettings(tis);
                    changed = true;
                }
            }
            if (changed)
                importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    // ───────────────────────────────────────────────────────────────────────────
    // PAISAJE DE FONDO LIMPIO (PARROQUIA NOCTURNA + SUELO DE CANTERA + SEÑAL)
    // ───────────────────────────────────────────────────────────────────────────
    private static void BuildScenicDoloresBackground(Transform canvasTransform, UIAssets ui)
    {
        // 1. Fondo nocturno pixel-art de Dolores Hidalgo a pantalla completa (sin cajas encima)
        GameObject bgObj = CreateUIObject("Background_ParroquiaDoloresNoche", canvasTransform);
        StretchFull(bgObj.GetComponent<RectTransform>());
        Image bgImg = bgObj.AddComponent<Image>();
        if (ui.bgN1 != null)
        {
            bgImg.sprite = ui.bgN1;
            bgImg.preserveAspect = false;
            bgImg.color = new Color(0.92f, 0.90f, 0.96f, 1f);
        }
        else
        {
            bgImg.color = ColNocheFondo;
        }

        // Oscurecimiento muy sutil solo para dar contraste formal al texto del título y botones
        GameObject subtleShade = CreateUIObject("Overlay_AtmosferaNocturna", canvasTransform);
        StretchFull(subtleShade.GetComponent<RectTransform>());
        Image shImg = subtleShade.AddComponent<Image>();
        shImg.color = new Color(0.04f, 0.03f, 0.09f, 0.24f);
        shImg.raycastTarget = false;

        // 2. Franja de suelo de cantera colonial pixel-art en el borde inferior (como la referencia)
        GameObject groundBar = CreateUIObject("GroundStrip_CanteraTop", canvasTransform);
        RectTransform gRt = groundBar.GetComponent<RectTransform>();
        gRt.anchorMin = new Vector2(0f, 0f);
        gRt.anchorMax = new Vector2(1f, 0f);
        gRt.pivot = new Vector2(0.5f, 0f);
        gRt.anchoredPosition = new Vector2(0f, 64f);
        gRt.sizeDelta = new Vector2(0f, 96f);
        Image gImg = groundBar.AddComponent<Image>();
        if (ui.groundTop != null)
        {
            gImg.sprite = ui.groundTop;
            gImg.type = Image.Type.Tiled;
            gImg.pixelsPerUnitMultiplier = 0.34f;
            gImg.color = Color.white;
        }
        else gImg.color = new Color(0.28f, 0.22f, 0.26f, 1f);
        gImg.raycastTarget = false;

        GameObject subGround = CreateUIObject("GroundStrip_CanteraFill", canvasTransform);
        RectTransform sgRt = subGround.GetComponent<RectTransform>();
        sgRt.anchorMin = new Vector2(0f, 0f);
        sgRt.anchorMax = new Vector2(1f, 0f);
        sgRt.pivot = new Vector2(0.5f, 0f);
        sgRt.anchoredPosition = Vector2.zero;
        sgRt.sizeDelta = new Vector2(0f, 66f);
        Image sgImg = subGround.AddComponent<Image>();
        if (ui.groundFill != null)
        {
            sgImg.sprite = ui.groundFill;
            sgImg.type = Image.Type.Tiled;
            sgImg.pixelsPerUnitMultiplier = 0.34f;
            sgImg.color = new Color(0.85f, 0.82f, 0.88f, 1f);
        }
        else sgImg.color = new Color(0.14f, 0.11f, 0.18f, 1f);
        sgImg.raycastTarget = false;

        // 3. Señalización colonial de madera a la derecha y farol nocturno a la izquierda (composición de la referencia)
        if (ui.signpost != null)
        {
            GameObject signObj = CreateUIObject("Prop_SenalColonial", canvasTransform);
            RectTransform sRt = signObj.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(1f, 0f);
            sRt.anchorMax = new Vector2(1f, 0f);
            sRt.pivot = new Vector2(0.5f, 0f);
            sRt.anchoredPosition = new Vector2(-175f, 156f);
            sRt.sizeDelta = new Vector2(112f, 112f);
            Image sImg = signObj.AddComponent<Image>();
            sImg.sprite = ui.signpost;
            sImg.raycastTarget = false;
        }

        if (ui.lanternPost != null)
        {
            GameObject lanObj = CreateUIObject("Prop_FarolColonial", canvasTransform);
            RectTransform lRt = lanObj.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0f, 0f);
            lRt.anchorMax = new Vector2(0f, 0f);
            lRt.pivot = new Vector2(0.5f, 0f);
            lRt.anchoredPosition = new Vector2(190f, 154f);
            lRt.sizeDelta = new Vector2(116f, 260f);
            Image lImg = lanObj.AddComponent<Image>();
            lImg.sprite = ui.lanternPost;
            lImg.raycastTarget = false;
        }
    }

    // ───────────────────────────────────────────────────────────────────────────
    // PANTALLA 1: MENÚ PRINCIPAL CENTRADO Y FORMAL (5 TABLONES PIXEL-ART)
    // ───────────────────────────────────────────────────────────────────────────
    private static GameObject BuildMainScreen(Transform canvasTransform, MainMenuUI controller, UIAssets ui)
    {
        GameObject panel = CreateUIObject("Panel_MainMenu", canvasTransform);
        StretchFull(panel.GetComponent<RectTransform>());

        // Título limpio y formal en la parte superior del cielo nocturno (sin cajas detrás)
        Text titleTxt = CreateStyledText(
            "Title_CuartaTransformacion",
            panel.transform,
            "CUARTA TRANSFORMACIÓN",
            72,
            FontStyle.Bold,
            ColCremaFormal,
            new Vector2(0f, 405f),
            new Vector2(1400f, 92f)
        );
        AddPixelOutlineAndShadow(titleTxt.gameObject, new Color(0.08f, 0.05f, 0.12f, 1f), 4f);

        Text subTxt = CreateStyledText(
            "Subtitle_DoloresHidalgo",
            panel.transform,
            "DOLORES HIDALGO  •  GUANAJUATO",
            22,
            FontStyle.Bold,
            ColDoradoFormal,
            new Vector2(0f, 342f),
            new Vector2(900f, 36f)
        );
        AddPixelOutlineAndShadow(subTxt.gameObject, new Color(0.06f, 0.04f, 0.10f, 1f), 2f);

        // Columna central limpia de 5 botones de tablón pixel-art (exactamente como la referencia)
        Vector2 btnSize = new Vector2(540f, 108f);
        float startY = 205f;
        float stepY  = 118f;

        Button btnPlay = CreateWoodPlankButton(
            "Btn_Jugar",
            panel.transform,
            "JUGAR",
            new Vector2(0f, startY),
            btnSize,
            34,
            ui
        );

        Button btnLevels = CreateWoodPlankButton(
            "Btn_SeleccionarNivel",
            panel.transform,
            "SELECCIONAR NIVEL",
            new Vector2(0f, startY - stepY * 1f),
            btnSize,
            30,
            ui
        );

        Button btnSettings = CreateWoodPlankButton(
            "Btn_Ajustes",
            panel.transform,
            "AJUSTES",
            new Vector2(0f, startY - stepY * 2f),
            btnSize,
            32,
            ui
        );

        Button btnCredits = CreateWoodPlankButton(
            "Btn_Creditos",
            panel.transform,
            "CRÉDITOS",
            new Vector2(0f, startY - stepY * 3f),
            btnSize,
            32,
            ui
        );

        Button btnQuit = CreateWoodPlankButton(
            "Btn_Salir",
            panel.transform,
            "SALIR",
            new Vector2(0f, startY - stepY * 4f),
            btnSize,
            32,
            ui
        );

        // Conectar referencias y eventos persistentes
        controller.btnPlayDirect = btnPlay;
        controller.btnWatchStory = null;
        controller.btnOpenLevelSelect = btnLevels;
        controller.btnOpenSettings = btnSettings;
        controller.btnOpenCredits = btnCredits;
        controller.btnQuitGame = btnQuit;

        UnityEventTools.AddPersistentListener(btnPlay.onClick, controller.PlayLevel1Direct);
        UnityEventTools.AddPersistentListener(btnLevels.onClick, controller.ShowLevelSelectPanel);
        UnityEventTools.AddPersistentListener(btnSettings.onClick, controller.ShowSettingsPanel);
        UnityEventTools.AddPersistentListener(btnCredits.onClick, controller.ShowCreditsPanel);
        UnityEventTools.AddPersistentListener(btnQuit.onClick, controller.QuitGame);

        return panel;
    }

    // ───────────────────────────────────────────────────────────────────────────
    // PANTALLA 2: SELECCIÓN DE NIVEL FORMAL
    // ───────────────────────────────────────────────────────────────────────────
    private static GameObject BuildLevelSelectScreen(Transform canvasTransform, MainMenuUI controller, UIAssets ui)
    {
        GameObject panel = CreateUIObject("Panel_LevelSelect", canvasTransform);
        StretchFull(panel.GetComponent<RectTransform>());

        GameObject dim = CreateUIObject("Dim_LevelSelect", panel.transform);
        StretchFull(dim.GetComponent<RectTransform>());
        dim.AddComponent<Image>().color = new Color(0.04f, 0.03f, 0.08f, 0.65f);

        GameObject container = CreateFormalPanel("Frame_SeleccionNivel", panel.transform, new Vector2(1620f, 820f), new Vector2(0f, 25f), ui.formalPanel);

        Text header = CreateStyledText(
            "Title_SeleccionNivel",
            container.transform,
            "SELECCIONAR NIVEL",
            44,
            FontStyle.Bold,
            ColDoradoFormal,
            new Vector2(0f, 330f),
            new Vector2(1000f, 58f)
        );
        AddPixelOutlineAndShadow(header.gameObject, Color.black, 3f);

        CreateStyledText(
            "Subtitle_SeleccionNivel",
            container.transform,
            "Completa cada sector para desbloquear el siguiente",
            21,
            FontStyle.Normal,
            ColCremaFormal,
            new Vector2(0f, 280f),
            new Vector2(1100f, 34f)
        );

        Button btnLvl1 = BuildFormalLevelCard(
            container.transform,
            1,
            new Vector2(-490f, 5f),
            "NIVEL 1",
            "Jardín Principal",
            "Jefe: El Vendedor Viral",
            ui.bgN1,
            ui,
            false,
            out GameObject lock1,
            out Text status1
        );

        Button btnLvl2 = BuildFormalLevelCard(
            container.transform,
            2,
            new Vector2(0f, 5f),
            "NIVEL 2",
            "Talleres de Talavera",
            "Jefe: El Munícipe Carismático",
            ui.bgN2,
            ui,
            true,
            out GameObject lock2,
            out Text status2
        );

        Button btnLvl3 = BuildFormalLevelCard(
            container.transform,
            3,
            new Vector2(490f, 5f),
            "NIVEL 3",
            "Callejones de Guanajuato",
            "Jefe: El Influencer del Callejón",
            ui.bgN3,
            ui,
            true,
            out GameObject lock3,
            out Text status3
        );

        Button btnBack = CreateWoodPlankButton(
            "Btn_VolverInicio",
            container.transform,
            "VOLVER",
            new Vector2(-280f, -320f),
            new Vector2(400f, 84f),
            26,
            ui
        );

        Button btnDebug = CreateWoodPlankButton(
            "Btn_DebugProgreso",
            container.transform,
            "DESBLOQUEAR / REINICIAR",
            new Vector2(260f, -320f),
            new Vector2(520f, 84f),
            22,
            ui
        );

        controller.btnLevel1Card = btnLvl1;
        controller.btnLevel2Card = btnLvl2;
        controller.btnLevel3Card = btnLvl3;
        controller.lockOverlayLevel2 = lock2;
        controller.lockOverlayLevel3 = lock3;
        controller.statusTextLevel1 = status1;
        controller.statusTextLevel2 = status2;
        controller.statusTextLevel3 = status3;
        controller.btnBackFromLevels = btnBack;
        controller.btnDebugToggleProgress = btnDebug;

        UnityEventTools.AddIntPersistentListener(btnLvl1.onClick, controller.SelectLevel, 1);
        UnityEventTools.AddIntPersistentListener(btnLvl2.onClick, controller.SelectLevel, 2);
        UnityEventTools.AddIntPersistentListener(btnLvl3.onClick, controller.SelectLevel, 3);
        UnityEventTools.AddPersistentListener(btnBack.onClick, controller.ShowMainPanel);
        UnityEventTools.AddPersistentListener(btnDebug.onClick, controller.DebugToggleUnlockOrResetProgress);

        return panel;
    }

    private static Button BuildFormalLevelCard(
        Transform parent,
        int levelNumber,
        Vector2 anchoredPos,
        string levelCode,
        string zoneName,
        string bossName,
        Sprite thumb,
        UIAssets ui,
        bool startLocked,
        out GameObject lockOverlayObj,
        out Text statusLabel)
    {
        GameObject cardObj = CreateFormalPanel($"Card_Nivel_{levelNumber}", parent, new Vector2(445f, 470f), anchoredPos, ui.formalPanel);

        Button cardBtn = cardObj.AddComponent<Button>();
        ColorBlock cb = cardBtn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 0.94f, 0.80f, 1f);
        cb.pressedColor = new Color(0.84f, 0.76f, 0.62f, 1f);
        cb.disabledColor = new Color(0.60f, 0.60f, 0.64f, 0.90f);
        cardBtn.colors = cb;
        cardBtn.interactable = !startLocked;

        CreateStyledText("Txt_NivelNum", cardObj.transform, levelCode, 22, FontStyle.Bold, ColDoradoFormal, new Vector2(0f, 188f), new Vector2(360f, 32f));

        if (thumb != null)
        {
            GameObject thumbObj = CreateUIObject("Thumb_Nivel", cardObj.transform);
            RectTransform tRt = thumbObj.GetComponent<RectTransform>();
            tRt.anchoredPosition = new Vector2(0f, 70f);
            tRt.sizeDelta = new Vector2(370f, 185f);
            Image tImg = thumbObj.AddComponent<Image>();
            tImg.sprite = thumb;
            tImg.preserveAspect = false;
            Outline tOut = thumbObj.AddComponent<Outline>();
            tOut.effectColor = new Color(0.08f, 0.05f, 0.06f, 1f);
            tOut.effectDistance = new Vector2(3f, -3f);
        }

        CreateStyledText("Txt_Zona", cardObj.transform, zoneName, 26, FontStyle.Bold, ColCremaFormal, new Vector2(0f, -62f), new Vector2(390f, 38f));
        CreateStyledText("Txt_Jefe", cardObj.transform, bossName, 19, FontStyle.Normal, new Color(0.86f, 0.82f, 0.75f, 1f), new Vector2(0f, -104f), new Vector2(390f, 34f));

        GameObject statusBar = CreateUIObject("Status_Footer", cardObj.transform);
        RectTransform stRt = statusBar.GetComponent<RectTransform>();
        stRt.anchoredPosition = new Vector2(0f, -172f);
        stRt.sizeDelta = new Vector2(360f, 58f);
        Image stImg = statusBar.AddComponent<Image>();
        if (ui.plankNormal != null)
        {
            stImg.sprite = ui.plankNormal;
            stImg.type = Image.Type.Sliced;
        }
        stImg.color = startLocked ? new Color(0.45f, 0.36f, 0.36f, 1f) : Color.white;

        statusLabel = CreateStyledText(
            "Txt_Status",
            statusBar.transform,
            startLocked ? "BLOQUEADO" : "DISPONIBLE",
            22,
            FontStyle.Bold,
            ColTextoTablon,
            new Vector2(0f, 2f),
            new Vector2(340f, 40f)
        );

        lockOverlayObj = CreateUIObject("LockOverlay", cardObj.transform);
        RectTransform loRt = lockOverlayObj.GetComponent<RectTransform>();
        loRt.anchoredPosition = Vector2.zero;
        loRt.sizeDelta = new Vector2(416f, 442f);
        lockOverlayObj.AddComponent<Image>().color = new Color(0.05f, 0.04f, 0.09f, 0.80f);

        if (ui.lockShield != null)
        {
            GameObject shObj = CreateUIObject("Icon_Lock", lockOverlayObj.transform);
            RectTransform shRt = shObj.GetComponent<RectTransform>();
            shRt.anchoredPosition = new Vector2(0f, 40f);
            shRt.sizeDelta = new Vector2(88f, 96f);
            shObj.AddComponent<Image>().sprite = ui.lockShield;
        }

        CreateStyledText("Txt_Lock", lockOverlayObj.transform, "BLOQUEADO\nCompleta el nivel anterior", 21, FontStyle.Bold, ColDoradoFormal, new Vector2(0f, -52f), new Vector2(350f, 70f));
        lockOverlayObj.SetActive(startLocked);

        return cardBtn;
    }

    // ───────────────────────────────────────────────────────────────────────────
    // PANTALLA 3: AJUSTES FORMAL
    // ───────────────────────────────────────────────────────────────────────────
    private static GameObject BuildSettingsScreen(
        Transform canvasTransform,
        MainMenuUI mainController,
        SettingsMenuUI settingsController,
        UIAssets ui)
    {
        GameObject panel = CreateUIObject("Panel_Settings", canvasTransform);
        StretchFull(panel.GetComponent<RectTransform>());

        GameObject dim = CreateUIObject("Dim_Settings", panel.transform);
        StretchFull(dim.GetComponent<RectTransform>());
        dim.AddComponent<Image>().color = new Color(0.04f, 0.03f, 0.08f, 0.65f);

        GameObject frame = CreateFormalPanel("Frame_Ajustes", panel.transform, new Vector2(960f, 760f), new Vector2(0f, 25f), ui.formalPanel);

        Text title = CreateStyledText("Title_Ajustes", frame.transform, "AJUSTES", 44, FontStyle.Bold, ColDoradoFormal, new Vector2(0f, 295f), new Vector2(600f, 56f));
        AddPixelOutlineAndShadow(title.gameObject, Color.black, 3f);

        CreateStyledText("Label_Volumen", frame.transform, "VOLUMEN GENERAL", 24, FontStyle.Bold, ColCremaFormal, new Vector2(-130f, 190f), new Vector2(460f, 40f));
        Text volPctText = CreateStyledText("Value_VolumenPct", frame.transform, "85%", 26, FontStyle.Bold, ColDoradoFormal, new Vector2(260f, 190f), new Vector2(140f, 40f));
        Slider volSlider = CreateThematicSlider("Slider_VolumenGeneral", frame.transform, new Vector2(0f, 130f), new Vector2(700f, 44f));

        CreateStyledText("Label_CameraZoom", frame.transform, "FOV / ZOOM DE CÁMARA", 24, FontStyle.Bold, ColCremaFormal, new Vector2(-110f, 42f), new Vector2(500f, 40f));
        Text zoomValText = CreateStyledText("Value_CameraZoom", frame.transform, "100% (Normal)", 24, FontStyle.Bold, ColDoradoFormal, new Vector2(255f, 42f), new Vector2(220f, 40f));
        Slider zoomSlider = CreateThematicSlider("Slider_CameraZoom", frame.transform, new Vector2(0f, -18f), new Vector2(700f, 44f));
        zoomSlider.minValue = LevelProgressionManager.MIN_CAMERA_ZOOM;
        zoomSlider.maxValue = LevelProgressionManager.MAX_CAMERA_ZOOM;
        zoomSlider.value = LevelProgressionManager.DEFAULT_CAMERA_ZOOM;

        Toggle fsToggle = CreateThematicToggle("Toggle_PantallaCompleta", frame.transform, "PANTALLA COMPLETA", new Vector2(0f, -115f), new Vector2(480f, 56f));

        Button btnSaveBack = CreateWoodPlankButton("Btn_GuardarYVolver", frame.transform, "GUARDAR Y VOLVER", new Vector2(0f, -255f), new Vector2(460f, 88f), 26, ui);

        settingsController.volumeSlider = volSlider;
        settingsController.volumeValueLabel = volPctText;
        settingsController.cameraZoomSlider = zoomSlider;
        settingsController.cameraZoomValueLabel = zoomValText;
        settingsController.fullscreenToggle = fsToggle;
        mainController.btnSaveAndBackFromSettings = btnSaveBack;

        UnityEventTools.AddPersistentListener(volSlider.onValueChanged, settingsController.SetVolume);
        UnityEventTools.AddPersistentListener(zoomSlider.onValueChanged, settingsController.SetCameraZoom);
        UnityEventTools.AddPersistentListener(fsToggle.onValueChanged, settingsController.SetFullscreen);
        UnityEventTools.AddPersistentListener(btnSaveBack.onClick, mainController.ShowMainPanel);

        return panel;
    }

    // ───────────────────────────────────────────────────────────────────────────
    // PANTALLA 4: CRÉDITOS FORMAL
    // ───────────────────────────────────────────────────────────────────────────
    private static GameObject BuildCreditsScreen(Transform canvasTransform, MainMenuUI mainController, UIAssets ui)
    {
        GameObject panel = CreateUIObject("Panel_Credits", canvasTransform);
        StretchFull(panel.GetComponent<RectTransform>());

        GameObject dim = CreateUIObject("Dim_Credits", panel.transform);
        StretchFull(dim.GetComponent<RectTransform>());
        dim.AddComponent<Image>().color = new Color(0.04f, 0.03f, 0.08f, 0.65f);

        GameObject frame = CreateFormalPanel("Frame_Creditos", panel.transform, new Vector2(960f, 640f), new Vector2(0f, 25f), ui.formalPanel);

        Text title = CreateStyledText("Title_Creditos", frame.transform, "CRÉDITOS", 44, FontStyle.Bold, ColDoradoFormal, new Vector2(0f, 235f), new Vector2(600f, 56f));
        AddPixelOutlineAndShadow(title.gameObject, Color.black, 3f);

        string creditsBody =
            "CUARTA TRANSFORMACIÓN\n\n" +
            "Inspirado en Dolores Hidalgo y Guanajuato, México\n\n" +
            "Diseño y Desarrollo:\n" +
            "Brayanpower — Equipo Cuarta Transformación\n\n" +
            "Motor Gráfico: Unity 6 (2D URP)\n" +
            "Audio: Sintetizador Procedural Chiptune en C#";

        CreateStyledText("Body_Creditos", frame.transform, creditsBody, 24, FontStyle.Normal, ColCremaFormal, new Vector2(0f, 20f), new Vector2(820f, 340f));

        Button btnBack = CreateWoodPlankButton("Btn_VolverCreditos", frame.transform, "VOLVER", new Vector2(0f, -210f), new Vector2(400f, 84f), 26, ui);
        mainController.btnBackFromCredits = btnBack;
        UnityEventTools.AddPersistentListener(btnBack.onClick, mainController.ShowMainPanel);

        return panel;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // OPCIÓN 2 Y 3: UTILIDADES DE NIVEL 1 (COMPATIBILIDAD)
    // ═══════════════════════════════════════════════════════════════════════════

    [MenuItem("Tools/Cuarta Transformacion/Corregir Objetos y Mapa Nivel 1", false, 2)]
    public static void FixLevel1ObjectsAndMap()
    {
        CuartaTransformacionWorldBuilder.UpgradeLevel1MedievalAndNight(true);
    }

    [MenuItem("Tools/Cuarta Transformacion/Mejorar Mapa Nivel 1", false, 3)]
    public static void EnhanceLevel1Map()
    {
        CuartaTransformacionWorldBuilder.UpgradeLevel1MedievalAndNight(true);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UTILIDADES DE CONSTRUCCIÓN UI FORMAL PIXEL-ART Y BUILD SETTINGS
    // ═══════════════════════════════════════════════════════════════════════════

    public static void RegisterScenesInBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
        string[] paths = new string[]
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Nivel1.unity",
            "Assets/Scenes/Nivel2.unity",
            "Assets/Scenes/Nivel3.unity"
        };

        foreach (string p in paths)
        {
            if (System.IO.File.Exists(p))
                scenes.Add(new EditorBuildSettingsScene(p, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void CreateEventSystem()
    {
        GameObject esObj = new GameObject("EventSystem");
        esObj.AddComponent<EventSystem>();
        Type inputSystemModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputSystemModuleType != null)
            esObj.AddComponent(inputSystemModuleType);
        else
            esObj.AddComponent<StandaloneInputModule>();
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static GameObject CreateFormalPanel(string name, Transform parent, Vector2 size, Vector2 pos, Sprite panelSprite)
    {
        GameObject frame = CreateUIObject(name, parent);
        RectTransform rt = frame.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = frame.AddComponent<Image>();
        if (panelSprite != null)
        {
            img.sprite = panelSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        else
        {
            img.color = new Color(0.11f, 0.09f, 0.16f, 0.94f);
            Outline outline = frame.AddComponent<Outline>();
            outline.effectColor = ColDoradoFormal;
            outline.effectDistance = new Vector2(3f, -3f);
        }

        return frame;
    }

    /// <summary>
    /// Botón pixel-art en tablón de madera tallada (inspirado directamente en la referencia):
    /// bordes oscuros pixelados, veta de madera, bisel inferior 3D y SpriteSwap al pasar el cursor.
    /// </summary>
    private static Button CreateWoodPlankButton(
        string name,
        Transform parent,
        string label,
        Vector2 pos,
        Vector2 size,
        int fontSize,
        UIAssets ui)
    {
        GameObject btnObj = CreateUIObject(name, parent);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = btnObj.AddComponent<Image>();
        if (ui.plankNormal != null)
        {
            img.sprite = ui.plankNormal;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 0.35f;
            img.color = Color.white;
        }
        else
        {
            img.color = new Color(0.72f, 0.52f, 0.32f, 1f);
        }

        Button btn = btnObj.AddComponent<Button>();
        if (ui.plankHover != null && ui.plankPressed != null)
        {
            btn.transition = Selectable.Transition.SpriteSwap;
            SpriteState ss = new SpriteState
            {
                highlightedSprite = ui.plankHover,
                selectedSprite = ui.plankHover,
                pressedSprite = ui.plankPressed
            };
            btn.spriteState = ss;
        }

        // Tipografía formal oscura tallada sobre la madera (con ligero bisel claro debajo)
        Text txt = CreateStyledText(
            "Label",
            btnObj.transform,
            label,
            fontSize,
            FontStyle.Bold,
            ColTextoTablon,
            new Vector2(0f, 4f),
            new Vector2(size.x - 40f, size.y - 12f)
        );
        Shadow bevel = txt.gameObject.AddComponent<Shadow>();
        bevel.effectColor = ColSombraTablon;
        bevel.effectDistance = new Vector2(0f, -2f);

        return btn;
    }

    private static Text CreateStyledText(
        string name,
        Transform parent,
        string content,
        int fontSize,
        FontStyle style,
        Color color,
        Vector2 pos,
        Vector2 size)
    {
        GameObject txtObj = CreateUIObject(name, parent);
        RectTransform rt = txtObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Text txt = txtObj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (txt.font == null)
            txt.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);

        txt.text = content;
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.color = color;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;

        return txt;
    }

    private static void AddPixelOutlineAndShadow(GameObject target, Color outlineCol, float dist)
    {
        Outline o = target.AddComponent<Outline>();
        o.effectColor = outlineCol;
        o.effectDistance = new Vector2(dist, -dist);
        Shadow sh = target.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.85f);
        sh.effectDistance = new Vector2(0f, -dist * 1.3f);
    }

    private static Slider CreateThematicSlider(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        GameObject sliderObj = CreateUIObject(name, parent);
        RectTransform rt = sliderObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        GameObject bg = CreateUIObject("Background", sliderObj.transform);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.25f);
        bgRt.anchorMax = new Vector2(1f, 0.75f);
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.10f, 0.08f, 0.14f, 1f);
        Outline bgOut = bg.AddComponent<Outline>();
        bgOut.effectColor = ColDoradoFormal;
        bgOut.effectDistance = new Vector2(2f, -2f);

        GameObject fillArea = CreateUIObject("Fill Area", sliderObj.transform);
        RectTransform faRt = fillArea.GetComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0f, 0.25f);
        faRt.anchorMax = new Vector2(1f, 0.75f);
        faRt.offsetMin = new Vector2(5f, 4f);
        faRt.offsetMax = new Vector2(-5f, -4f);

        GameObject fill = CreateUIObject("Fill", fillArea.transform);
        RectTransform fillRt = fill.GetComponent<RectTransform>();
        StretchFull(fillRt);
        fill.AddComponent<Image>().color = ColTerracota;

        GameObject handleArea = CreateUIObject("Handle Slide Area", sliderObj.transform);
        RectTransform haRt = handleArea.GetComponent<RectTransform>();
        StretchFull(haRt);
        haRt.offsetMin = new Vector2(14f, 0f);
        haRt.offsetMax = new Vector2(-14f, 0f);

        GameObject handle = CreateUIObject("Handle", handleArea.transform);
        RectTransform hRt = handle.GetComponent<RectTransform>();
        hRt.sizeDelta = new Vector2(28f, 42f);
        Image hImg = handle.AddComponent<Image>();
        hImg.color = ColDoradoFormal;

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.fillRect = fillRt;
        slider.handleRect = hRt;
        slider.targetGraphic = hImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.85f;
        return slider;
    }

    private static Toggle CreateThematicToggle(string name, Transform parent, string labelText, Vector2 pos, Vector2 size)
    {
        GameObject toggleObj = CreateUIObject(name, parent);
        RectTransform rt = toggleObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        GameObject box = CreateUIObject("Background", toggleObj.transform);
        RectTransform boxRt = box.GetComponent<RectTransform>();
        boxRt.anchoredPosition = new Vector2(-175f, 0f);
        boxRt.sizeDelta = new Vector2(44f, 44f);
        Image boxImg = box.AddComponent<Image>();
        boxImg.color = ColAzulTalavera;
        Outline boxOut = box.AddComponent<Outline>();
        boxOut.effectColor = ColDoradoFormal;
        boxOut.effectDistance = new Vector2(2f, -2f);

        GameObject check = CreateUIObject("Checkmark", box.transform);
        RectTransform checkRt = check.GetComponent<RectTransform>();
        checkRt.anchoredPosition = Vector2.zero;
        checkRt.sizeDelta = new Vector2(26f, 26f);
        Image checkImg = check.AddComponent<Image>();
        checkImg.color = ColDoradoFormal;

        CreateStyledText("Label", toggleObj.transform, labelText, 24, FontStyle.Bold, ColCremaFormal, new Vector2(40f, 0f), new Vector2(360f, 44f));

        Toggle toggle = toggleObj.AddComponent<Toggle>();
        toggle.targetGraphic = boxImg;
        toggle.graphic = checkImg;
        toggle.isOn = true;
        return toggle;
    }
}
#endif
