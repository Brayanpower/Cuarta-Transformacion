using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controles táctiles y joystick virtual multi-touch para Móvil (Android/iOS) y Web (WebGL).
/// Se auto-construye en tiempo de ejecución en Nivel1, Nivel2 y Nivel3 y se oculta
/// automáticamente durante el Menú Principal, Pausa o cinemáticas.
/// </summary>
public class MobileTouchControls : MonoBehaviour
{
    public static MobileTouchControls Instance { get; private set; }

    private GameObject canvasRoot;
    private RectTransform controlsContainer;
    private RectTransform joystickHandleRect;
    private VirtualJoystickPad joystickPad;
    private Image dashButtonImage;
    private Text dashLabelText;
    private PlayerMovement cachedPlayer;
    private Font uiFont;

    private Sprite ringSprite;
    private Sprite fillSprite;
    private bool currentlyVisible = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance != null)
            return;

        MobileTouchControls existing = UnityEngine.Object.FindAnyObjectByType<MobileTouchControls>();
        if (existing != null)
        {
            Instance = existing;
            return;
        }

        GameObject go = new GameObject("_MobileTouchControls");
        Instance = go.AddComponent<MobileTouchControls>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        cachedPlayer = null;
        if (!IsMenuScene(scene.name))
            EnsureEventSystemExists();

        ResetInputs();
        RefreshVisibility();
    }

    private void Update()
    {
        bool shouldShow = ShouldBeVisibleNow();
        if (shouldShow != currentlyVisible)
        {
            ApplyVisibility(shouldShow);
        }

        if (!currentlyVisible)
            return;

        EnsurePlayerCached();
        UpdateDashVisualState();
    }

    public void RefreshVisibility()
    {
        ApplyVisibility(ShouldBeVisibleNow());
    }

    private bool ShouldBeVisibleNow()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (IsMenuScene(sceneName))
            return false;

        if (!LevelProgressionManager.ShouldShowTouchControls())
            return false;

        if (PauseMenu.IsGamePaused ||
            PauseMenu.BlockPauseDuringTransition ||
            Level1IntroCutsceneTrigger.IsPlaying ||
            LevelTitleCard.IsActive ||
            GrandFinaleCutscene.IsPlaying)
        {
            return false;
        }

        return true;
    }

    private static bool IsMenuScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return true;
        return sceneName.ToLowerInvariant().Contains("menu");
    }

    private void ApplyVisibility(bool visible)
    {
        currentlyVisible = visible;
        if (controlsContainer != null && controlsContainer.gameObject.activeSelf != visible)
            controlsContainer.gameObject.SetActive(visible);

        if (!visible)
            ResetInputs();
        else
            EnsureEventSystemExists();
    }

    private void ResetInputs()
    {
        if (joystickPad != null)
            joystickPad.ResetPad();
        else if (joystickHandleRect != null)
            joystickHandleRect.anchoredPosition = Vector2.zero;

        EnsurePlayerCached();
        if (cachedPlayer != null)
            cachedPlayer.SetVirtualHorizontalInput(0f);
    }

    private void EnsurePlayerCached()
    {
        if (cachedPlayer == null)
            cachedPlayer = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
    }

    private void UpdateDashVisualState()
    {
        if (dashButtonImage == null || cachedPlayer == null)
            return;

        bool ready = cachedPlayer.CanDash;
        float ratio = cachedPlayer.DashChargeRatio;
        dashButtonImage.color = ready
            ? new Color(0.14f, 0.56f, 0.72f, 0.78f)
            : new Color(0.10f, 0.24f, 0.34f, Mathf.Lerp(0.32f, 0.62f, ratio));

        if (dashLabelText != null)
        {
            dashLabelText.color = ready
                ? new Color(0.97f, 0.94f, 0.86f, 0.96f)
                : new Color(0.75f, 0.85f, 0.92f, 0.70f);
        }
    }

    private void OnJoystickHorizontal(float axisX)
    {
        EnsurePlayerCached();
        if (cachedPlayer != null)
            cachedPlayer.SetVirtualHorizontalInput(axisX);
    }

    private void OnJumpDown()
    {
        EnsurePlayerCached();
        if (cachedPlayer != null)
            cachedPlayer.TriggerVirtualJumpDown();
    }

    private void OnJumpUp()
    {
        EnsurePlayerCached();
        if (cachedPlayer != null)
            cachedPlayer.TriggerVirtualJumpUp();
    }

    private void OnAttackDown()
    {
        EnsurePlayerCached();
        if (cachedPlayer != null)
            cachedPlayer.TriggerVirtualAttack();
    }

    private void OnDashDown()
    {
        EnsurePlayerCached();
        if (cachedPlayer != null)
            cachedPlayer.TriggerVirtualDash();
    }

    private void OnPauseDown()
    {
        if (!PauseMenu.IsGamePaused && !PauseMenu.BlockPauseDuringTransition)
            PauseMenu.Instance?.PauseGame();
    }

    private void BuildUI()
    {
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null)
            uiFont = Font.CreateDynamicFontFromOSFont("Arial", 24);

        ringSprite = CreateCircleSprite(128, 7f, new Color(0.955f, 0.785f, 0.340f, 0.92f), new Color(0.08f, 0.06f, 0.12f, 0.62f));
        fillSprite = CreateCircleSprite(128, 5f, new Color(0.955f, 0.785f, 0.340f, 0.95f), Color.white);

        canvasRoot = new GameObject("MobileTouchControls_Canvas");
        canvasRoot.transform.SetParent(transform, false);

        Canvas canvas = canvasRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;

        CanvasScaler scaler = canvasRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasRoot.AddComponent<GraphicRaycaster>();

        GameObject containerGo = new GameObject("ControlsContainer", typeof(RectTransform));
        containerGo.transform.SetParent(canvasRoot.transform, false);
        controlsContainer = containerGo.GetComponent<RectTransform>();
        controlsContainer.anchorMin = Vector2.zero;
        controlsContainer.anchorMax = Vector2.one;
        controlsContainer.offsetMin = Vector2.zero;
        controlsContainer.offsetMax = Vector2.zero;

        BuildVirtualJoystick(controlsContainer);
        BuildActionButtons(controlsContainer);
        BuildPauseButton(controlsContainer);

        ApplyVisibility(false);
    }

    private void BuildVirtualJoystick(Transform parent)
    {
        GameObject joyBaseGo = new GameObject("VirtualJoystick_Base", typeof(RectTransform), typeof(Image));
        joyBaseGo.transform.SetParent(parent, false);

        RectTransform baseRect = joyBaseGo.GetComponent<RectTransform>();
        baseRect.anchorMin = new Vector2(0f, 0f);
        baseRect.anchorMax = new Vector2(0f, 0f);
        baseRect.pivot = new Vector2(0.5f, 0.5f);
        baseRect.anchoredPosition = new Vector2(185f, 165f);
        baseRect.sizeDelta = new Vector2(224f, 224f);

        Image baseImg = joyBaseGo.GetComponent<Image>();
        baseImg.sprite = ringSprite;
        baseImg.color = new Color(1f, 1f, 1f, 0.85f);
        baseImg.raycastTarget = true;

        // Indicadores sutiles izquierda / derecha
        CreateDirectionHint(baseRect, "<", new Vector2(-74f, 0f));
        CreateDirectionHint(baseRect, ">", new Vector2(74f, 0f));

        GameObject handleGo = new GameObject("VirtualJoystick_Handle", typeof(RectTransform), typeof(Image));
        handleGo.transform.SetParent(baseRect, false);

        joystickHandleRect = handleGo.GetComponent<RectTransform>();
        joystickHandleRect.anchorMin = new Vector2(0.5f, 0.5f);
        joystickHandleRect.anchorMax = new Vector2(0.5f, 0.5f);
        joystickHandleRect.pivot = new Vector2(0.5f, 0.5f);
        joystickHandleRect.anchoredPosition = Vector2.zero;
        joystickHandleRect.sizeDelta = new Vector2(92f, 92f);

        Image handleImg = handleGo.GetComponent<Image>();
        handleImg.sprite = fillSprite;
        handleImg.color = new Color(0.84f, 0.62f, 0.24f, 0.92f);
        handleImg.raycastTarget = false;

        joystickPad = joyBaseGo.AddComponent<VirtualJoystickPad>();
        joystickPad.Initialize(baseRect, joystickHandleRect, 78f, 0.15f, OnJoystickHorizontal);
    }

    private void CreateDirectionHint(Transform parent, string glyph, Vector2 offset)
    {
        GameObject hintGo = new GameObject("DirHint_" + glyph, typeof(RectTransform), typeof(Text));
        hintGo.transform.SetParent(parent, false);

        RectTransform rt = hintGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = offset;
        rt.sizeDelta = new Vector2(44f, 44f);

        Text t = hintGo.GetComponent<Text>();
        t.text = glyph;
        t.font = uiFont;
        t.fontSize = 26;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(0.955f, 0.785f, 0.340f, 0.52f);
        t.raycastTarget = false;
    }

    private void BuildActionButtons(Transform parent)
    {
        // SALTAR (esquina inferior derecha principal)
        CreateRoundActionButton(
            parent,
            "Btn_TouchJump",
            "SALTAR",
            new Vector2(-140f, 145f),
            new Vector2(144f, 144f),
            new Color(0.14f, 0.62f, 0.32f, 0.80f),
            24,
            OnJumpDown,
            OnJumpUp
        );

        // ESPADA (izquierda de Saltar)
        CreateRoundActionButton(
            parent,
            "Btn_TouchSword",
            "ESPADA",
            new Vector2(-304f, 128f),
            new Vector2(124f, 124f),
            new Color(0.76f, 0.18f, 0.22f, 0.80f),
            22,
            OnAttackDown,
            null
        );

        // DASH (arriba de Saltar)
        TouchActionRefs dashRefs = CreateRoundActionButton(
            parent,
            "Btn_TouchDash",
            "DASH",
            new Vector2(-156f, 306f),
            new Vector2(114f, 114f),
            new Color(0.14f, 0.56f, 0.72f, 0.78f),
            21,
            OnDashDown,
            null
        );
        dashButtonImage = dashRefs.buttonImage;
        dashLabelText = dashRefs.labelText;
    }

    private void BuildPauseButton(Transform parent)
    {
        GameObject pauseGo = new GameObject("Btn_TouchPause", typeof(RectTransform), typeof(Image));
        pauseGo.transform.SetParent(parent, false);

        RectTransform rt = pauseGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(-74f, -56f);
        rt.sizeDelta = new Vector2(88f, 68f);

        Image img = pauseGo.GetComponent<Image>();
        img.sprite = fillSprite;
        img.color = new Color(0.14f, 0.10f, 0.18f, 0.84f);
        img.raycastTarget = true;

        GameObject lblGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        lblGo.transform.SetParent(rt, false);
        RectTransform lrt = lblGo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        Text lbl = lblGo.GetComponent<Text>();
        lbl.text = "II";
        lbl.font = uiFont;
        lbl.fontSize = 28;
        lbl.fontStyle = FontStyle.Bold;
        lbl.alignment = TextAnchor.MiddleCenter;
        lbl.color = new Color(0.955f, 0.785f, 0.340f, 0.98f);
        lbl.raycastTarget = false;

        TouchActionButton btn = pauseGo.AddComponent<TouchActionButton>();
        btn.Initialize(rt, OnPauseDown, null);
    }

    private struct TouchActionRefs
    {
        public Image buttonImage;
        public Text labelText;
    }

    private TouchActionRefs CreateRoundActionButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchoredPos,
        Vector2 size,
        Color fillColor,
        int fontSize,
        Action onDown,
        Action onUp)
    {
        GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(Image));
        btnGo.transform.SetParent(parent, false);

        RectTransform rt = btnGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = btnGo.GetComponent<Image>();
        img.sprite = fillSprite;
        img.color = fillColor;
        img.raycastTarget = true;

        GameObject lblGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        lblGo.transform.SetParent(rt, false);
        RectTransform lrt = lblGo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        Text lbl = lblGo.GetComponent<Text>();
        lbl.text = label;
        lbl.font = uiFont;
        lbl.fontSize = fontSize;
        lbl.fontStyle = FontStyle.Bold;
        lbl.alignment = TextAnchor.MiddleCenter;
        lbl.color = new Color(0.97f, 0.94f, 0.86f, 0.96f);
        lbl.raycastTarget = false;

        Outline outline = lblGo.AddComponent<Outline>();
        outline.effectColor = new Color(0.03f, 0.02f, 0.06f, 0.92f);
        outline.effectDistance = new Vector2(2f, -2f);

        TouchActionButton handler = btnGo.AddComponent<TouchActionButton>();
        handler.Initialize(rt, onDown, onUp);

        return new TouchActionRefs { buttonImage = img, labelText = lbl };
    }

    private static void EnsureEventSystemExists()
    {
        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null)
            return;

        GameObject es = new GameObject("MobileTouch_EventSystem");
        es.AddComponent<EventSystem>();
        Type inputMod = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputMod != null)
            es.AddComponent(inputMod);
        else
            es.AddComponent<StandaloneInputModule>();
    }

    private static Sprite CreateCircleSprite(int size, float borderPx, Color borderCol, Color innerCol)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        float half = size * 0.5f;
        float outerR = half - 1.5f;
        float innerR = outerR - borderPx;

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) - half;
                float dy = (y + 0.5f) - half;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                Color c;
                if (dist > outerR + 1f)
                {
                    c = Color.clear;
                }
                else if (dist > outerR - 1f)
                {
                    float aa = Mathf.Clamp01((outerR + 1f - dist) * 0.5f);
                    c = new Color(borderCol.r, borderCol.g, borderCol.b, borderCol.a * aa);
                }
                else if (dist >= innerR)
                {
                    c = borderCol;
                }
                else
                {
                    c = innerCol;
                }

                pixels[y * size + x] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private class VirtualJoystickPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private RectTransform padRect;
        private RectTransform handleRect;
        private float maxRadius;
        private float deadzone;
        private Action<float> onAxisXChanged;
        private int activePointerId = int.MinValue;

        public void Initialize(
            RectTransform pad,
            RectTransform handle,
            float radius,
            float dz,
            Action<float> axisCallback)
        {
            padRect = pad;
            handleRect = handle;
            maxRadius = Mathf.Max(10f, radius);
            deadzone = Mathf.Clamp01(dz);
            onAxisXChanged = axisCallback;
        }

        public void ResetPad()
        {
            activePointerId = int.MinValue;
            if (handleRect != null)
                handleRect.anchoredPosition = Vector2.zero;
        }

        private void OnDisable()
        {
            ResetPad();
            onAxisXChanged?.Invoke(0f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (activePointerId != int.MinValue && activePointerId != eventData.pointerId)
                return;

            activePointerId = eventData.pointerId;
            UpdateStick(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId)
                return;

            UpdateStick(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId)
                return;

            ResetPad();
            onAxisXChanged?.Invoke(0f);
        }

        private void UpdateStick(PointerEventData eventData)
        {
            if (padRect == null || handleRect == null)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    padRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
                return;

            if (localPoint.magnitude > maxRadius)
                localPoint = localPoint.normalized * maxRadius;

            handleRect.anchoredPosition = localPoint;

            float normX = Mathf.Clamp(localPoint.x / maxRadius, -1f, 1f);
            float absX = Mathf.Abs(normX);
            if (absX < deadzone)
            {
                onAxisXChanged?.Invoke(0f);
            }
            else
            {
                float scaled = (absX - deadzone) / (1f - deadzone);
                onAxisXChanged?.Invoke(Mathf.Sign(normX) * Mathf.Clamp01(scaled));
            }
        }
    }

    private class TouchActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private RectTransform targetRect;
        private Action onDown;
        private Action onUp;
        private int activePointerId = int.MinValue;

        public void Initialize(RectTransform rect, Action downCallback, Action upCallback)
        {
            targetRect = rect;
            onDown = downCallback;
            onUp = upCallback;
        }

        private void OnDisable()
        {
            Release(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (activePointerId != int.MinValue && activePointerId != eventData.pointerId)
                return;

            activePointerId = eventData.pointerId;
            if (targetRect != null)
                targetRect.localScale = Vector3.one * 0.90f;

            onDown?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId)
                return;

            Release(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId)
                return;

            Release(true);
        }

        private void Release(bool invokeUp)
        {
            activePointerId = int.MinValue;
            if (targetRect != null)
                targetRect.localScale = Vector3.one;

            if (invokeUp)
                onUp?.Invoke();
        }
    }
}
