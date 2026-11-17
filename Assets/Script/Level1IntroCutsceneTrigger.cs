using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Dispara automáticamente la Cinemática de Historia (4 actos de Dolores Hidalgo)
/// ÚNICAMENTE al entrar al Nivel 1 (NO aparece como opción en el Menú Principal).
/// Se auto-construye con marco ornamental colonial-medieval, pausa el gameplay durante
/// la narración y entrega el control al jugador al finalizar o presionar Saltar.
/// </summary>
public class Level1IntroCutsceneTrigger : MonoBehaviour
{
    private static bool skipNextNivel1Load = false;
    public static bool IsPlaying { get; private set; } = false;

    [Header("Ilustraciones Nocturnas de los 4 Actos (auto-cargadas si faltan)")]
    public Sprite slide1Art;
    public Sprite slide2Art;
    public Sprite slide3Art;
    public Sprite slide4Art;

    [Header("Sprites UI Formales de Tablón (auto-cargados si faltan)")]
    public Sprite ornateFrameSprite;
    public Sprite titleRibbonSprite;
    public Sprite bannerButtonSprite;
    public Sprite bannerButtonHoverSprite;
    public Sprite bannerButtonPressedSprite;

    private GameObject cutsceneCanvasObj;

    public static void ResetIntroForNewPlay()
    {
        skipNextNivel1Load = false;
    }

    public static void SkipOnNextLoad()
    {
        skipNextNivel1Load = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoHookNivel1()
    {
        SceneManager.sceneLoaded -= OnAnySceneLoaded;
        SceneManager.sceneLoaded += OnAnySceneLoaded;
        TryTriggerInScene(SceneManager.GetActiveScene());
    }

    private static void OnAnySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryTriggerInScene(scene);
    }

    private static void TryTriggerInScene(Scene scene)
    {
        if (scene.name != "Nivel1")
            return;

        if (skipNextNivel1Load)
        {
            skipNextNivel1Load = false;
            return;
        }

        Level1IntroCutsceneTrigger existing = UnityEngine.Object.FindAnyObjectByType<Level1IntroCutsceneTrigger>();
        if (existing == null)
        {
            GameObject go = new GameObject("_Level1IntroCutscene");
            existing = go.AddComponent<Level1IntroCutsceneTrigger>();
        }

        existing.BeginLevel1Intro();
    }

    private bool hasStartedThisInstance = false;

    public void BeginLevel1Intro()
    {
        if (hasStartedThisInstance)
            return;

        if (SceneManager.GetActiveScene().name != "Nivel1")
            return;

        if (skipNextNivel1Load)
        {
            skipNextNivel1Load = false;
            return;
        }

        hasStartedThisInstance = true;
        EnsureSpritesLoaded();
        EnsureEventSystemExists();
        BuildAndPlayCutsceneOverlay();
    }

    private void EnsureSpritesLoaded()
    {
        if (slide1Art == null) slide1Art = LoadPngSprite("Images/Cutscenes/Cutscene_01_FiestaPaz.png", null);
        if (slide2Art == null) slide2Art = LoadPngSprite("Images/UI/Menu_KeyArt_3Jefes.png", null);
        if (slide3Art == null) slide3Art = LoadPngSprite("Images/Backgrounds/Bg_Nivel2_TalaveraNoche.png", null);
        if (slide4Art == null) slide4Art = LoadPngSprite("Images/Cutscenes/Cutscene_02_HeroeDespierta.png", null);

        if (ornateFrameSprite == null)         ornateFrameSprite         = LoadPngSprite("Images/UI/UI_PlankPanel_Formal.png", new Vector4(18, 18, 18, 18));
        if (bannerButtonSprite == null)        bannerButtonSprite        = LoadPngSprite("Images/UI/UI_PlankButton_Normal.png", new Vector4(14, 8, 14, 8));
        if (bannerButtonHoverSprite == null)   bannerButtonHoverSprite   = LoadPngSprite("Images/UI/UI_PlankButton_Hover.png", new Vector4(14, 8, 14, 8));
        if (bannerButtonPressedSprite == null) bannerButtonPressedSprite = LoadPngSprite("Images/UI/UI_PlankButton_Pressed.png", new Vector4(14, 8, 14, 8));
    }

    private Sprite LoadPngSprite(string relativeAssetsPath, Vector4? border)
    {
        string fullPath = Path.Combine(Application.dataPath, relativeAssetsPath);
        if (!File.Exists(fullPath))
            return null;

        byte[] bytes = File.ReadAllBytes(fullPath);
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        if (!tex.LoadImage(bytes))
            return null;

        return border.HasValue
            ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border.Value)
            : Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private void EnsureEventSystemExists()
    {
        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null)
            return;

        GameObject es = new GameObject("Cutscene_EventSystem");
        es.AddComponent<EventSystem>();
        System.Type inputMod = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputMod != null)
            es.AddComponent(inputMod);
        else
            es.AddComponent<StandaloneInputModule>();
    }

    private void BuildAndPlayCutsceneOverlay()
    {
        Color colGold  = new Color(0.957f, 0.725f, 0.259f, 1f);
        Color colCream = new Color(0.985f, 0.960f, 0.895f, 1f);
        Color colTerra = new Color(0.800f, 0.310f, 0.200f, 1f);
        Color colBlue  = new Color(0.190f, 0.390f, 0.540f, 1f);
        Color colNight = new Color(0.08f, 0.05f, 0.13f, 0.94f);

        cutsceneCanvasObj = new GameObject("Canvas_IntroCinematica_Nivel1");
        Canvas canvas = cutsceneCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = cutsceneCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        cutsceneCanvasObj.AddComponent<GraphicRaycaster>();
        StoryCutsceneController controller = cutsceneCanvasObj.AddComponent<StoryCutsceneController>();

        GameObject panel = CreateUI("Panel_StoryCutscene", cutsceneCanvasObj.transform);
        StretchFull(panel.GetComponent<RectTransform>());
        panel.AddComponent<Image>().color = Color.black;

        // Ilustración nocturna con paneo/zoom Ken Burns
        GameObject artObj = CreateUI("Cutscene_Artwork", panel.transform);
        StretchFull(artObj.GetComponent<RectTransform>());
        Image artImg = artObj.AddComponent<Image>();
        artImg.sprite = slide1Art;
        artImg.preserveAspect = false;

        // Overlay de transición suave
        GameObject fadeObj = CreateUI("Cutscene_FadeOverlay", panel.transform);
        StretchFull(fadeObj.GetComponent<RectTransform>());
        Image fadeImg = fadeObj.AddComponent<Image>();
        fadeImg.color = new Color(0.04f, 0.02f, 0.08f, 0f);
        fadeImg.raycastTarget = false;

        // Barra superior de cine
        GameObject topBar = CreateUI("Letterbox_Top", panel.transform);
        RectTransform topRt = topBar.GetComponent<RectTransform>();
        topRt.anchorMin = new Vector2(0f, 1f);
        topRt.anchorMax = new Vector2(1f, 1f);
        topRt.pivot = new Vector2(0.5f, 1f);
        topRt.sizeDelta = new Vector2(0f, 76f);
        topBar.AddComponent<Image>().color = new Color(0.04f, 0.02f, 0.08f, 0.94f);

        // Caja narrativa formal (mismo marco pixel-art de madera y piedra que el Menú Principal)
        GameObject dialog = CreateUI("Frame_Narracion", panel.transform);
        RectTransform dRt = dialog.GetComponent<RectTransform>();
        dRt.anchoredPosition = new Vector2(0f, -368f);
        dRt.sizeDelta = new Vector2(1620f, 268f);
        Image dImg = dialog.AddComponent<Image>();
        if (ornateFrameSprite != null)
        {
            dImg.sprite = ornateFrameSprite;
            dImg.type = Image.Type.Sliced;
            dImg.color = Color.white;
        }
        else dImg.color = colNight;

        Text chapterTxt = CreateText("Txt_Capitulo", dialog.transform, "", 24, FontStyle.Bold, colGold, new Vector2(-170f, 88f), new Vector2(1200f, 38f), TextAnchor.MiddleLeft);
        Text bodyTxt    = CreateText("Txt_Narracion", dialog.transform, "", 27, FontStyle.Normal, colCream, new Vector2(-170f, -2f), new Vector2(1200f, 130f), TextAnchor.MiddleLeft);
        Text counterTxt = CreateText("Txt_Contador", dialog.transform, "", 17, FontStyle.Normal, new Color(0.85f, 0.80f, 0.68f, 0.92f), new Vector2(-170f, -92f), new Vector2(1200f, 30f), TextAnchor.MiddleLeft);

        Button btnNext = CreateBannerBtn("Btn_Siguiente", dialog.transform, "SIGUIENTE", Color.white, new Vector2(615f, 32f), new Vector2(290f, 70f), 23);
        Button btnSkip = CreateBannerBtn("Btn_Saltar", dialog.transform, "SALTAR", Color.white, new Vector2(615f, -52f), new Vector2(290f, 64f), 21);

        controller.cutscenePanel = panel;
        controller.slideImage = artImg;
        controller.fadeOverlay = fadeImg;
        controller.chapterTitleText = chapterTxt;
        controller.narrationText = bodyTxt;
        controller.slideCounterText = counterTxt;
        controller.btnNextSlide = btnNext;
        controller.btnSkipCutscene = btnSkip;
        controller.slides = new StoryCutsceneController.CutsceneSlide[]
        {
            new StoryCutsceneController.CutsceneSlide
            {
                chapterTitle = "ACTO I — NOCHE DE FIESTAS PATRONALES",
                narrationText = "Dolores Hidalgo, Cuna de la Independencia de México, se prepara bajo las estrellas para sus fiestas patronales. Las antorchas iluminan la Parroquia y el papel picado viste el Jardín Principal...",
                artwork = slide1Art,
                panDirectionX = 1f
            },
            new StoryCutsceneController.CutsceneSlide
            {
                chapterTitle = "ACTO II — LOS TRES JEFES DEL CAOS VIRAL",
                narrationText = "De pronto, tres figuras exageradas y ridículas se apoderan de la región: El Vendedor Viral (Don Chava), El Munícipe Carismático y El Influencer del Callejón.",
                artwork = slide2Art,
                panDirectionX = -1f
            },
            new StoryCutsceneController.CutsceneSlide
            {
                chapterTitle = "ACTO III — EL PUEBLO CONTAGIADO",
                narrationText = "Su influencia contagia a los habitantes locales, bloqueando el Jardín Principal, los Talleres de Talavera y hasta los emblemáticos Callejones de Guanajuato.",
                artwork = slide3Art,
                panDirectionX = 1f
            },
            new StoryCutsceneController.CutsceneSlide
            {
                chapterTitle = "ACTO IV — ¡A DEFENDER EL JARDÍN PRINCIPAL!",
                narrationText = "Un vecino arraigado al pueblo —conocedor de cada callejón y tradición— empuña su espada para derrotar a los tres jefes y restablecer la paz antes de que las fiestas patronales queden arruinadas.",
                artwork = slide4Art,
                panDirectionX = -1f
            }
        };

        // Pausar física, controles del jugador y menú de pausa mientras corre la cinemática inicial
        IsPlaying = true;
        PauseMenu.BlockPauseDuringTransition = true;
        PlayerMovement pm = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
        if (pm != null)
            pm.SetControlLocked(true);

        Time.timeScale = 0f;
        controller.PlayCutscene(() =>
        {
            IsPlaying = false;
            Time.timeScale = 1f;
            PauseMenu.BlockPauseDuringTransition = false;

            PlayerMovement player = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
            if (player != null)
                player.SetControlLocked(false);

            if (cutsceneCanvasObj != null)
                Destroy(cutsceneCanvasObj);
        });
    }

    private GameObject CreateUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private Text CreateText(string name, Transform parent, string txtContent, int size, FontStyle style, Color col, Vector2 pos, Vector2 dims, TextAnchor align)
    {
        GameObject go = CreateUI(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = dims;

        Text t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (t.font == null)
            t.font = Font.CreateDynamicFontFromOSFont("Arial", size);
        t.text = txtContent;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = col;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    private Button CreateBannerBtn(string name, Transform parent, string label, Color bgCol, Vector2 pos, Vector2 size, int fontSize)
    {
        GameObject go = CreateUI(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = go.AddComponent<Image>();
        if (bannerButtonSprite != null)
        {
            img.sprite = bannerButtonSprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 0.42f;
            img.color = Color.white;
        }
        else
        {
            img.color = bgCol;
        }

        Button btn = go.AddComponent<Button>();
        if (bannerButtonHoverSprite != null && bannerButtonPressedSprite != null)
        {
            btn.transition = Selectable.Transition.SpriteSwap;
            btn.spriteState = new SpriteState
            {
                highlightedSprite = bannerButtonHoverSprite,
                selectedSprite = bannerButtonHoverSprite,
                pressedSprite = bannerButtonPressedSprite
            };
        }

        CreateText("Label", go.transform, label, fontSize, FontStyle.Bold, new Color(0.14f, 0.085f, 0.055f, 1f), new Vector2(0f, 3f), size, TextAnchor.MiddleCenter);
        return btn;
    }
}
