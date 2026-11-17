using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Sistema de Cinemáticas Narrativas para "Cuarta Transformación" (Dolores Hidalgo & Guanajuato).
/// Muestra la historia oficial del GDD con ilustraciones pixel-art nocturnas, efecto
/// Ken Burns (zoom/paneo suave), texto estilo máquina de escribir, barras de cine,
/// música narrativa y controles de Avanzar / Saltar.
/// </summary>
public class StoryCutsceneController : MonoBehaviour
{
    [System.Serializable]
    public class CutsceneSlide
    {
        public string chapterTitle;
        [TextArea(2, 4)]
        public string narrationText;
        public Sprite artwork;
        public float panDirectionX = 1f;
    }

    [Header("Referencias de UI de la Cinemática")]
    public GameObject cutscenePanel;
    public Image slideImage;
    public Image fadeOverlay;
    public Text chapterTitleText;
    public Text narrationText;
    public Text slideCounterText;
    public Button btnNextSlide;
    public Button btnSkipCutscene;

    [Header("Diapositivas de la Historia")]
    public CutsceneSlide[] slides;

    [Header("Ritmo y Animación")]
    public float charsPerSecond = 44f;
    public float kenBurnsScaleStart = 1.02f;
    public float kenBurnsScaleEnd = 1.12f;
    public float kenBurnsPanPixels = 28f;

    private int currentSlideIndex = 0;
    private bool isPlaying = false;
    private bool isTyping = false;
    private string fullCurrentText = "";
    private Coroutine typingCoroutine;
    private float slideElapsed = 0f;
    private System.Action onCompleteCallback;

    private void Awake()
    {
        if (slides == null || slides.Length == 0)
            BuildDefaultStorySlides();
    }

    private void Start()
    {
        if (btnNextSlide != null)
        {
            btnNextSlide.onClick.RemoveListener(OnNextPressed);
            btnNextSlide.onClick.AddListener(OnNextPressed);
        }

        if (btnSkipCutscene != null)
        {
            btnSkipCutscene.onClick.RemoveListener(SkipCutscene);
            btnSkipCutscene.onClick.AddListener(SkipCutscene);
        }
    }

    private void BuildDefaultStorySlides()
    {
        slides = new CutsceneSlide[]
        {
            new CutsceneSlide
            {
                chapterTitle = "ACTO I — NOCHE DE FIESTAS PATRONALES",
                narrationText = "Dolores Hidalgo, Cuna de la Independencia de México, se viste de gala bajo las estrellas. Los faroles iluminan la Parroquia y el papel picado ondea sobre el Jardín Principal...",
                panDirectionX = 1f
            },
            new CutsceneSlide
            {
                chapterTitle = "ACTO II — LA INVASIÓN DEL CAOS VIRAL",
                narrationText = "Pero la calma se rompe cuando tres figuras exageradas y ridículas se apoderan de la región: El Vendedor Viral, El Munícipe Carismático y El Influencer del Callejón.",
                panDirectionX = -1f
            },
            new CutsceneSlide
            {
                chapterTitle = "ACTO III — EL PUEBLO CONTAGIADO",
                narrationText = "Su influencia contagia a los habitantes locales, bloqueando el tránsito público desde el Jardín Principal y los Talleres de Talavera hasta los Callejones de Guanajuato.",
                panDirectionX = 1f
            },
            new CutsceneSlide
            {
                chapterTitle = "ACTO IV — EL VECINO AL RESCATE",
                narrationText = "En medio del desorden, un vecino arraigado al pueblo —conocedor de cada callejón y tradición— empuña su espada para derrotar a los tres jefes y salvar las fiestas patronales.",
                panDirectionX = -1f
            }
        };
    }

    /// <summary>
    /// Inicia la cinemática de historia completa. Al finalizar o saltarla ejecuta onComplete.
    /// </summary>
    public void PlayCutscene(System.Action onComplete = null)
    {
        if (slides == null || slides.Length == 0)
            BuildDefaultStorySlides();

        onCompleteCallback = onComplete;
        currentSlideIndex = 0;
        isPlaying = true;

        if (cutscenePanel != null)
            cutscenePanel.SetActive(true);

        ProceduralSoundManager.Instance?.PlayButtonClick();
        ShowSlide(0);
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        // Animación Ken Burns (zoom + paneo suave sobre el pixel-art nocturno)
        if (slideImage != null)
        {
            slideElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(slideElapsed / 10f);
            float scale = Mathf.Lerp(kenBurnsScaleStart, kenBurnsScaleEnd, t);
            slideImage.rectTransform.localScale = new Vector3(scale, scale, 1f);

            float dir = (slides != null && currentSlideIndex < slides.Length) ? slides[currentSlideIndex].panDirectionX : 1f;
            float offsetX = Mathf.Lerp(-kenBurnsPanPixels * dir, kenBurnsPanPixels * dir, t);
            slideImage.rectTransform.anchoredPosition = new Vector2(offsetX, 0f);
        }

        // Permitir avanzar con Espacio o Enter y saltar con Escape
        bool advanceKey = false;
        bool skipKey = false;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)
                advanceKey = true;
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
                skipKey = true;
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                advanceKey = true;
            if (Input.GetKeyDown(KeyCode.Escape))
                skipKey = true;
        }

        if (skipKey)
            SkipCutscene();
        else if (advanceKey)
            OnNextPressed();
    }

    private void ShowSlide(int index)
    {
        if (slides == null || index < 0 || index >= slides.Length)
        {
            FinishCutscene();
            return;
        }

        currentSlideIndex = index;
        slideElapsed = 0f;
        CutsceneSlide slide = slides[index];

        if (slideImage != null)
        {
            if (slide.artwork != null)
                slideImage.sprite = slide.artwork;
            slideImage.rectTransform.localScale = new Vector3(kenBurnsScaleStart, kenBurnsScaleStart, 1f);
            slideImage.rectTransform.anchoredPosition = Vector2.zero;
        }

        if (chapterTitleText != null)
            chapterTitleText.text = slide.chapterTitle;

        if (slideCounterText != null)
            slideCounterText.text = $"ESCENA {index + 1} / {slides.Length}   •   [ESPACIO / CLIC PARA CONTINUAR]";

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypewriterRoutine(slide.narrationText));
    }

    private IEnumerator TypewriterRoutine(string text)
    {
        isTyping = true;
        fullCurrentText = text ?? "";

        if (fadeOverlay != null)
        {
            fadeOverlay.color = new Color(0.04f, 0.02f, 0.08f, 0.9f);
            float f = 0f;
            while (f < 0.35f)
            {
                fadeOverlay.color = new Color(0.04f, 0.02f, 0.08f, Mathf.Lerp(0.9f, 0f, f / 0.35f));
                f += Time.unscaledDeltaTime;
                yield return null;
            }
            fadeOverlay.color = new Color(0.04f, 0.02f, 0.08f, 0f);
        }

        if (narrationText != null)
        {
            narrationText.text = "";
            float delay = 1f / Mathf.Max(10f, charsPerSecond);
            for (int i = 0; i < fullCurrentText.Length; i++)
            {
                narrationText.text += fullCurrentText[i];
                if (i % 7 == 0 && fullCurrentText[i] != ' ')
                    ProceduralSoundManager.Instance?.PlayButtonClick();
                yield return new WaitForSecondsRealtime(delay);
            }
        }

        isTyping = false;
    }

    public void OnNextPressed()
    {
        if (!isPlaying)
            return;

        if (isTyping)
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);
            if (narrationText != null)
                narrationText.text = fullCurrentText;
            if (fadeOverlay != null)
                fadeOverlay.color = new Color(0.04f, 0.02f, 0.08f, 0f);
            isTyping = false;
            return;
        }

        ProceduralSoundManager.Instance?.PlayButtonClick();
        if (currentSlideIndex + 1 < slides.Length)
            ShowSlide(currentSlideIndex + 1);
        else
            FinishCutscene();
    }

    public void SkipCutscene()
    {
        if (!isPlaying)
            return;

        ProceduralSoundManager.Instance?.PlayButtonClick();
        FinishCutscene();
    }

    private void FinishCutscene()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        isPlaying = false;
        isTyping = false;

        if (cutscenePanel != null)
            cutscenePanel.SetActive(false);

        System.Action cb = onCompleteCallback;
        onCompleteCallback = null;
        cb?.Invoke();
    }
}
