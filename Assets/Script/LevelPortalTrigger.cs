using UnityEngine;

/// <summary>
/// Portal colonial de fin de nivel colocado al final de la arena de cada jefe.
/// Permanece sellado mientras el jefe sigue activo y se ilumina al ser derrotado,
/// permitiendo al jugador caminar hacia él para avanzar al siguiente nivel o abrir
/// la pantalla de victoria.
/// </summary>
public class LevelPortalTrigger : MonoBehaviour
{
    [Header("Configuración del Portal")]
    public int completedLevelNumber = 1;
    public string nextSceneName = "Nivel2";
    public string defeatedBossName = "EL VENDEDOR VIRAL";
    public string victoryMessage = "¡Sector liberado en Dolores Hidalgo!";
    public bool startUnlocked = false;
    public bool loadNextSceneDirectly = false;

    private SpriteRenderer sr;
    private Collider2D portalCollider;
    private bool isUnlocked = false;
    private bool hasTriggered = false;
    private Vector3 baseScale;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = gameObject.AddComponent<SpriteRenderer>();

        portalCollider = GetComponent<Collider2D>();
        if (portalCollider == null)
        {
            BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.6f, 2.5f);
            box.offset = new Vector2(0f, 1.25f);
            portalCollider = box;
        }
        else
        {
            portalCollider.isTrigger = true;
        }

        sr.sortingOrder = 2;
        baseScale = transform.localScale;
        SetUnlockedVisual(startUnlocked);
    }

    public void ActivatePortal()
    {
        SetUnlockedVisual(true);
        Debug.Log($"[LevelPortalTrigger] ¡Portal del Nivel {completedLevelNumber} desbloqueado hacia {nextSceneName}!");
    }

    public void LockPortal()
    {
        hasTriggered = false;
        SetUnlockedVisual(false);
    }

    private void SetUnlockedVisual(bool unlocked)
    {
        isUnlocked = unlocked;
        if (sr != null)
        {
            sr.sprite = ProceduralPixelArt.GetColonialPortalSprite(unlocked);
            sr.color = unlocked ? Color.white : new Color(0.65f, 0.65f, 0.72f, 0.85f);
        }
    }

    private void Update()
    {
        if (!isUnlocked || sr == null)
            return;

        // Pulso dorado/celeste para llamar la atención del jugador
        float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.04f;
        transform.localScale = new Vector3(baseScale.x * pulse, baseScale.y * pulse, baseScale.z);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isUnlocked || hasTriggered)
            return;

        PlayerMovement player = collision.GetComponent<PlayerMovement>() ?? collision.GetComponentInParent<PlayerMovement>();
        if (player == null)
            return;

        hasTriggered = true;
        ProceduralSoundManager.Instance?.PlayPortalEnter();
        LevelProgressionManager.UnlockNextLevel(completedLevelNumber);

        if (loadNextSceneDirectly && !string.IsNullOrEmpty(nextSceneName) && completedLevelNumber < 3)
        {
            if (LevelProgressionManager.TryLoadScene(nextSceneName))
                return;
        }

        PauseMenu pauseMenu = PauseMenu.Instance != null
            ? PauseMenu.Instance
            : UnityEngine.Object.FindAnyObjectByType<PauseMenu>();

        if (pauseMenu != null)
        {
            pauseMenu.ShowVictoryScreen(completedLevelNumber, defeatedBossName, victoryMessage);
        }
    }
}
