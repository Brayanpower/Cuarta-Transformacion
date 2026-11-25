using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Jefe del Nivel 1 (Jardín Principal / Centro Histórico de Dolores Hidalgo):
/// EL VENDEDOR VIRAL — Don Chava de los Garbanzos.
/// Jefe desarrollado en 2 fases con 4 patrones de ataque telegrafiados:
/// 1) Cucharazo Colosal: wind-up dorado + embestida frontal + onda corta de caldo hirviendo.
/// 2) Lluvia de Garbanzos Calientes: ráfaga dirigida en arco (y caída desde el cielo en Fase II).
/// 3) ¡Ollazo Sísmico!: salto hacia el jugador, caída pesada al suelo y ondas gemelas izquierda/derecha.
/// 4) ¡Oferta Viral 2x1!: invoca un Mini-Garbanzo de apoyo y deja ventana breve de castigo.
/// </summary>
public class ViralVendorBoss : SimpleEnemy
{
    [Header("Identidad del Jefe")]
    public string bossTitle = "EL VENDEDOR VIRAL";
    public string bossSubtitle = "Don Chava — Rey de los Garbanzos de Dolores";
    public Transform playerTransform;
    public Transform firePoint;
    public GameObject arenaGate;
    public LevelPortalTrigger levelPortal;
    public float arenaTriggerX = 132f;

    [Header("Estilo Visual")]
    [Tooltip("Usa sprites pixel-art procedurales dedicados de 7 frames para máxima nitidez")]
    public bool useProceduralPixelArt = true;

    [Header("Estadísticas de Combate")]
    public float bossSpeed = 3.6f;
    public float enrageSpeed = 5.0f;
    public float meleeAttackRange = 2.6f;
    public float rangedAttackRange = 24.0f;
    public float attackCooldown = 2.6f;
    public int spoonDamage = 2;

    [Header("Proyectil de Garbanzo (Largo Alcance)")]
    public Sprite projectileSprite;
    public float projectileSpeed = 14.5f;
    public float projectileLifetime = 6.0f;
    public Vector2 firePointOffset = new Vector2(0.95f, 1.10f);

    [Header("Arena Bounds (Estilo Sonic)")]
    public float arenaLeftBound = 131f;
    public float arenaRightBound = 155f;
    public Vector3 arenaCameraPosition = new Vector3(143.0f, -1.2f, -10f);
    public float arenaCameraOrthoSize = 6.2f;

    [Header("Sprites de Animación (Compatibilidad Inspector)")]
    public Sprite[] idleSprites;
    public Sprite[] walkRightSprites;
    public Sprite[] walkLeftSprites;
    public Sprite[] cucharazoSprites;
    public Sprite[] lluviaSprites;

    // Cachés de respaldo PNG
    private static Sprite[] sharedBossIdle;
    private static Sprite[] sharedBossWalkRight;
    private static Sprite[] sharedBossWalkLeft;
    private static Sprite[] sharedBossCucharazo;
    private static Sprite[] sharedBossLluvia;
    private static Sprite[] sharedBossDefeat;
    private static Sprite sharedBossProjectile;

    // 7 frames pixel-art procedurales (0:Idle, 1-2:Walk, 3:Windup, 4:Swing, 5:PotAttack, 6:Defeat)
    private Sprite[] proceduralFrames;
    private Sprite proceduralGarbanzoSprite;
    private Sprite proceduralWaveSprite;

    private Rigidbody2D bossRb;
    private SpriteRenderer bossRenderer;
    private BoxCollider2D bossBoxCol;
    private Color normalColor = Color.white;

    public bool isAwakened = false;
    public bool isEnraged = false;
    public bool IsBossFightActive => isAwakened && !isDefeated && !isDead;
    public Vector3 BossRespawnAnchor => new Vector3(arenaTriggerX - 1.8f, initialSpawnPosition.y + 0.65f, 0f);
    private bool isAttacking = false;
    private bool isDefeated = false;
    private bool isPhaseTransitioning = false;
    private float nextAttackTime = 0f;
    private float nextSteamTime = 0f;
    private int attackCycleIndex = 0;
    private float arenaFloorY = -4f;
    private Vector3 initialSpawnPosition;
    private float walkAnimTimer = 0f;
    private int walkAnimFrame = 0;
    private float displayedHealth = 18f;

    private readonly List<GameObject> summonedMinions = new List<GameObject>();

    // Ráfagas Vocales (Frases virales de Don Chava)
    private string activeVocalQuote = "";
    private float quoteExpireTime = 0f;
    private readonly string[] viralPhrases = new string[]
    {
        "¡LLÉVELO, LLÉVELO, BIEN CALIENTITO!",
        "¡PÁSELE MARCHANTE, CON TODO MENOS CON MIEDO!",
        "¡BARATO, BARATO, PARA LLEVAR O COMER AQUÍ!",
        "¡ESTO SE VA A HACER VIRAL EN DOLORES!",
        "¡50 MIL LIKES POR ESTE CUCHARAZO!",
        "¡PRUÉBEME EL GARBANZO, VA CON GARANTÍA!"
    };

    // Texturas preasignadas una sola vez en Start() (cero fugas de memoria en OnGUI)
    private Texture2D barBgTexture;
    private Texture2D emptyBarTexture;
    private Texture2D barFillTexture;
    private Texture2D barLagTexture;
    private Texture2D enrageFillTexture;
    private Texture2D goldBorderTexture;
    private Texture2D quoteBgTexture;

    protected override bool UseGarbanzoSprites => false;

    protected override void Awake()
    {
        base.Awake();
        maxHealth = 18;
        currentHealth = maxHealth;
        displayedHealth = maxHealth;
        damageToPlayer = 1;
        canPatrol = false;
        EnsureBossSpritesLoaded();
    }

    protected override void Start()
    {
        bossRb = GetComponent<Rigidbody2D>();
        bossRenderer = GetComponent<SpriteRenderer>();

        if (bossRb == null)
            bossRb = gameObject.AddComponent<Rigidbody2D>();
        if (bossRenderer == null)
            bossRenderer = gameObject.AddComponent<SpriteRenderer>();

        // Normalizar masa y físicas continuas (corrige m_Mass: 30 serializado en Nivel1.unity)
        bossRb.bodyType = RigidbodyType2D.Dynamic;
        bossRb.mass = 1f;
        bossRb.linearDamping = 0f;
        bossRb.angularDamping = 0f;
        bossRb.gravityScale = 2.6f;
        bossRb.constraints = RigidbodyConstraints2D.FreezeRotation;
        bossRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        bossRb.interpolation = RigidbodyInterpolation2D.Interpolate;

        PhysicsMaterial2D slipMat = new PhysicsMaterial2D("BossSlipMaterial")
        {
            friction = 0f,
            bounciness = 0f
        };
        bossRb.sharedMaterial = slipMat;

        bossBoxCol = GetComponent<BoxCollider2D>();
        if (bossBoxCol == null)
            bossBoxCol = gameObject.AddComponent<BoxCollider2D>();
        bossBoxCol.sharedMaterial = slipMat;
        bossBoxCol.edgeRadius = 0.06f;
        bossBoxCol.size = new Vector2(1.10f, 1.95f);
        bossBoxCol.offset = new Vector2(0f, 1.0f);

        // Asegurar velocidad y vida consistentes aunque existan valores viejos en escena
        maxHealth = 18;
        currentHealth = maxHealth;
        displayedHealth = maxHealth;
        if (bossSpeed < 3.4f) bossSpeed = 3.6f;
        if (enrageSpeed < 4.8f) enrageSpeed = 5.0f;
        if (attackCooldown > 2.8f) attackCooldown = 2.6f;

        bossRenderer.sortingOrder = 4;
        normalColor = Color.white;
        bossRenderer.color = normalColor;

        EnsureBossSpritesLoaded();

        initialSpawnPosition = transform.position;
        arenaFloorY = transform.position.y;

        if (playerTransform == null)
        {
            PlayerMovement pm = Object.FindAnyObjectByType<PlayerMovement>();
            if (pm != null)
                playerTransform = pm.transform;
        }

        if (arenaGate != null)
            arenaGate.SetActive(false);

        // Preasignar todas las texturas del HUD una única vez
        barBgTexture       = MakeTex(new Color(0.08f, 0.06f, 0.12f, 0.94f));
        emptyBarTexture    = MakeTex(new Color(0.18f, 0.05f, 0.06f, 0.94f));
        barFillTexture     = MakeTex(new Color(0.92f, 0.68f, 0.16f, 0.98f));
        barLagTexture      = MakeTex(new Color(1.00f, 0.92f, 0.55f, 0.90f));
        enrageFillTexture  = MakeTex(new Color(0.92f, 0.22f, 0.16f, 0.98f));
        goldBorderTexture  = MakeTex(new Color(0.955f, 0.785f, 0.340f, 0.95f));
        quoteBgTexture     = MakeTex(new Color(0.97f, 0.95f, 0.87f, 0.96f));

        FaceDirection(-1f);
        StartCoroutine(IdleAnimationRoutine());
    }

    private void FaceDirection(float dirX)
    {
        bool lookLeft = dirX < 0f;
        if (bossRenderer != null)
            bossRenderer.flipX = lookLeft;

        if (firePoint != null)
        {
            Vector3 localPos = firePoint.localPosition;
            localPos.x = Mathf.Abs(firePointOffset.x) * (lookLeft ? -1f : 1f);
            localPos.y = firePointOffset.y;
            firePoint.localPosition = localPos;
        }
    }

    private float FaceTowardsPlayer()
    {
        if (playerTransform == null)
            return -1f;

        float deltaX = playerTransform.position.x - transform.position.x;
        float dirX = deltaX < 0f ? -1f : 1f;
        FaceDirection(dirX);
        return dirX;
    }

    /// <summary>
    /// Carga los 7 frames pixel-art dedicados de Don Chava (y respaldos PNG si se desactiva useProceduralPixelArt).
    /// </summary>
    public void EnsureBossSpritesLoaded()
    {
        if (bossRenderer == null)
            bossRenderer = GetComponent<SpriteRenderer>();

        if (proceduralFrames == null || proceduralFrames.Length < 7)
        {
            proceduralFrames = new Sprite[7];
            for (int i = 0; i < 7; i++)
                proceduralFrames[i] = ProceduralPixelArt.GetVendorBossSprite(i);
        }

        if (proceduralGarbanzoSprite == null)
            proceduralGarbanzoSprite = ProceduralPixelArt.GetGarbanzoProjectileSprite();
        if (proceduralWaveSprite == null)
            proceduralWaveSprite = ProceduralPixelArt.GetBrothShockwaveSprite();

        if (useProceduralPixelArt)
        {
            idleSprites      = new Sprite[] { proceduralFrames[0] };
            walkRightSprites = new Sprite[] { proceduralFrames[1], proceduralFrames[2] };
            walkLeftSprites  = walkRightSprites;
            cucharazoSprites = new Sprite[] { proceduralFrames[3], proceduralFrames[4] };
            lluviaSprites    = new Sprite[] { proceduralFrames[5] };
            defeatSprites    = new Sprite[] { proceduralFrames[6] };
            projectileSprite = proceduralGarbanzoSprite;

            if (bossRenderer != null)
                bossRenderer.sprite = proceduralFrames[0];
            return;
        }

        // Respaldo legacy desde vendedor_garbanos.png si useProceduralPixelArt == false
        bool needsIdle = idleSprites == null || idleSprites.Length == 0 || idleSprites[0] == null;
        bool needsWalkR = walkRightSprites == null || walkRightSprites.Length == 0 || walkRightSprites[0] == null;
        bool needsWalkL = walkLeftSprites == null || walkLeftSprites.Length == 0 || walkLeftSprites[0] == null;
        bool needsCucharazo = cucharazoSprites == null || cucharazoSprites.Length == 0 || cucharazoSprites[0] == null;
        bool needsLluvia = lluviaSprites == null || lluviaSprites.Length == 0 || lluviaSprites[0] == null;
        bool needsDefeat = defeatSprites == null || defeatSprites.Length == 0 || defeatSprites[0] == null;
        bool needsCurrentSprite = bossRenderer != null && bossRenderer.sprite == null;

        if (needsIdle || needsWalkR || needsWalkL || needsCucharazo || needsLluvia || needsDefeat || needsCurrentSprite)
        {
            if (sharedBossIdle == null || sharedBossIdle.Length == 0 || sharedBossIdle[0] == null)
            {
                string path = Path.Combine(Application.dataPath, "Images/Nivel1/Boos/vendedor_garbanos.png");
                if (!File.Exists(path))
                    path = "Assets/Images/Nivel1/Boos/vendedor_garbanos.png";

                if (File.Exists(path))
                {
                    byte[] bytes = File.ReadAllBytes(path);
                    Texture2D tex = new Texture2D(1792, 2400, TextureFormat.RGBA32, false)
                    {
                        filterMode = FilterMode.Point,
                        wrapMode = TextureWrapMode.Clamp
                    };
                    tex.LoadImage(bytes);

                    sharedBossIdle = new Sprite[]
                    {
                        Sprite.Create(tex, new Rect(1, 1998, 297, 308), new Vector2(0.5f, 0f), 150f),
                        Sprite.Create(tex, new Rect(298, 1998, 298, 308), new Vector2(0.5f, 0f), 150f)
                    };
                    sharedBossWalkRight = new Sprite[]
                    {
                        Sprite.Create(tex, new Rect(0, 1605, 298, 290), new Vector2(0.5f, 0f), 150f),
                        Sprite.Create(tex, new Rect(298, 1605, 298, 292), new Vector2(0.5f, 0f), 150f)
                    };
                    sharedBossWalkLeft = sharedBossWalkRight;
                    sharedBossCucharazo = new Sprite[]
                    {
                        Sprite.Create(tex, new Rect(36, 815, 321, 294), new Vector2(0.5f, 0f), 150f),
                        Sprite.Create(tex, new Rect(716, 815, 358, 291), new Vector2(0.5f, 0f), 150f)
                    };
                    sharedBossLluvia = new Sprite[]
                    {
                        Sprite.Create(tex, new Rect(36, 420, 322, 303), new Vector2(0.5f, 0f), 150f),
                        Sprite.Create(tex, new Rect(716, 420, 358, 300), new Vector2(0.5f, 0f), 150f)
                    };
                    sharedBossDefeat = new Sprite[]
                    {
                        Sprite.Create(tex, new Rect(1432, 30, 356, 274), new Vector2(0.5f, 0f), 150f)
                    };
                    sharedBossProjectile = proceduralGarbanzoSprite;
                }
            }

            if (needsIdle && sharedBossIdle != null) idleSprites = sharedBossIdle;
            if (needsWalkR && sharedBossWalkRight != null) walkRightSprites = sharedBossWalkRight;
            if (needsWalkL && sharedBossWalkLeft != null) walkLeftSprites = sharedBossWalkLeft;
            if (needsCucharazo && sharedBossCucharazo != null) cucharazoSprites = sharedBossCucharazo;
            if (needsLluvia && sharedBossLluvia != null) lluviaSprites = sharedBossLluvia;
            if (needsDefeat && sharedBossDefeat != null) defeatSprites = sharedBossDefeat;
            if (projectileSprite == null && sharedBossProjectile != null) projectileSprite = sharedBossProjectile;
        }

        if (bossRenderer != null && bossRenderer.sprite == null && idleSprites != null && idleSprites.Length > 0)
            bossRenderer.sprite = idleSprites[0];
    }

    private void SetBossFrame(int proceduralIndex, Sprite[] fallbackGroup = null, int fallbackIdx = 0)
    {
        if (bossRenderer == null)
            return;

        if (useProceduralPixelArt && proceduralFrames != null && proceduralIndex >= 0 && proceduralIndex < proceduralFrames.Length)
        {
            bossRenderer.sprite = proceduralFrames[proceduralIndex];
            return;
        }

        if (fallbackGroup != null && fallbackGroup.Length > 0)
        {
            Sprite s = fallbackGroup[Mathf.Clamp(fallbackIdx, 0, fallbackGroup.Length - 1)];
            if (s != null)
                bossRenderer.sprite = s;
        }
    }

    protected override void Update()
    {
        // Suavizar barra de daño rezagada en HUD
        if (displayedHealth > currentHealth)
            displayedHealth = Mathf.MoveTowards(displayedHealth, currentHealth, Time.unscaledDeltaTime * 7.5f);
        else
            displayedHealth = currentHealth;

        if (isDefeated || isDead || Time.timeScale <= 0f || PauseMenu.IsGamePaused)
            return;

        if (playerTransform == null)
        {
            PlayerMovement pm = Object.FindAnyObjectByType<PlayerMovement>();
            if (pm != null)
                playerTransform = pm.transform;
            return;
        }

        if (!isAwakened)
        {
            FaceTowardsPlayer();
            if (playerTransform.position.x >= arenaTriggerX)
                AwakenBoss();
            return;
        }

        // Partículas de vapor ardiente en Fase II (Furia Viral)
        if (isEnraged && Time.time >= nextSteamTime)
        {
            nextSteamTime = Time.time + 0.18f;
            SpawnSteamSpark(transform.position + new Vector3(Random.Range(-0.55f, 0.55f), Random.Range(0.7f, 1.8f), 0f),
                            new Color(1f, 0.45f, 0.18f, 0.9f), 1.0f);
        }

        if (isPhaseTransitioning || isAttacking)
            return;

        FaceTowardsPlayer();

        float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (Time.time >= nextAttackTime && distToPlayer <= rangedAttackRange)
        {
            // Ciclo inteligente de 4 patrones distintos
            int pattern = attackCycleIndex % 4;
            attackCycleIndex++;

            if (distToPlayer <= meleeAttackRange && pattern != 2)
            {
                StartCoroutine(PerformCucharazoAttack());
                return;
            }

            switch (pattern)
            {
                case 0:
                    StartCoroutine(PerformCucharazoAttack());
                    return;
                case 1:
                    StartCoroutine(PerformGarbanzoRainAttack());
                    return;
                case 2:
                    StartCoroutine(PerformPotSlamAttack());
                    return;
                default:
                    StartCoroutine(PerformViralOfferSummon());
                    return;
            }
        }

        MoveHorizontallyTowardsPlayer(1f, true);
    }

    private void MoveHorizontallyTowardsPlayer(float speedMultiplier, bool animateWalk)
    {
        if (playerTransform == null || isDefeated || isDead)
            return;

        float dirX = FaceTowardsPlayer();
        float horizontalDist = Mathf.Abs(playerTransform.position.x - transform.position.x);
        float currentSpeed = (isEnraged ? enrageSpeed : bossSpeed) * speedMultiplier;

        if (horizontalDist > 0.50f)
        {
            float targetVelX = dirX * currentSpeed;
            if (bossRb != null)
                bossRb.linearVelocity = new Vector2(targetVelX, bossRb.linearVelocity.y);

            if (bossRb == null || Mathf.Abs(bossRb.linearVelocity.x) < currentSpeed * 0.45f)
            {
                float nextX = Mathf.Clamp(
                    transform.position.x + targetVelX * Time.deltaTime,
                    arenaLeftBound,
                    arenaRightBound
                );
                transform.position = new Vector3(nextX, transform.position.y, transform.position.z);
            }

            if (animateWalk)
                AnimateWalking();
        }
        else if (bossRb != null)
        {
            bossRb.linearVelocity = new Vector2(0f, bossRb.linearVelocity.y);
            if (animateWalk)
                SetBossFrame(0, idleSprites, 0);
        }

        float clampedX = Mathf.Clamp(transform.position.x, arenaLeftBound, arenaRightBound);
        if (transform.position.x != clampedX)
            transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
    }

    private void AnimateWalking()
    {
        if (bossRenderer == null)
            return;

        walkAnimTimer += Time.deltaTime;
        float fps = isEnraged ? 12f : 8.5f;
        if (walkAnimTimer >= (1f / fps))
        {
            walkAnimTimer = 0f;
            walkAnimFrame = (walkAnimFrame + 1) % 2;
            SetBossFrame(1 + walkAnimFrame, walkRightSprites, walkAnimFrame);
        }
    }

    private void AwakenBoss()
    {
        isAwakened = true;
        isAttacking = true;
        arenaFloorY = transform.position.y;
        ProceduralSoundManager.Instance?.PlayBossAlert();
        ProceduralSoundManager.Instance?.PlayBossMusic();
        Debug.Log("¡¡ALERTA DE JEFE!! El Vendedor Viral ha bloqueado el Jardín Principal.");

        if (arenaGate != null)
            arenaGate.SetActive(true);

        CameraFollow cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null)
            cam.LockBossArena(arenaCameraPosition, arenaCameraOrthoSize);

        if (playerTransform != null)
        {
            PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
            if (pm != null)
                pm.LockInArena(arenaLeftBound, arenaRightBound);
        }

        StartCoroutine(IntroRoarRoutine());
    }

    private IEnumerator IntroRoarRoutine()
    {
        TriggerVocalShout("¡¡PÁSELE MARCHANTE, LLEGÓ EL REY DE LOS GARBANZOS!!");
        SetBossFrame(3, cucharazoSprites, 0);

        if (bossRb != null)
            bossRb.linearVelocity = new Vector2(0f, 6.2f);

        for (int i = 0; i < 6; i++)
            SpawnSteamSpark(transform.position + new Vector3(Random.Range(-0.7f, 0.7f), Random.Range(0.4f, 1.5f), 0f),
                            new Color(0.98f, 0.85f, 0.30f, 1f), 1.35f);

        yield return new WaitForSeconds(0.65f);

        SetBossFrame(0, idleSprites, 0);
        nextAttackTime = Time.time + 0.85f;
        isAttacking = false;
    }

    // ═══ ATAQUE 1: CUCHARAZO COLOSAL + ONDA DE CALDO ═══
    private IEnumerator PerformCucharazoAttack()
    {
        isAttacking = true;
        float dirX = FaceTowardsPlayer();

        if (bossRb != null)
            bossRb.linearVelocity = new Vector2(0f, bossRb.linearVelocity.y);

        TriggerVocalShout(viralPhrases[Random.Range(0, viralPhrases.Length)]);
        SetBossFrame(3, cucharazoSprites, 0);

        if (bossRenderer != null)
            bossRenderer.color = isEnraged ? new Color(1f, 0.35f, 0.35f) : new Color(1f, 0.88f, 0.38f);

        yield return new WaitForSeconds(isEnraged ? 0.42f : 0.52f);

        dirX = FaceTowardsPlayer();
        ProceduralSoundManager.Instance?.PlayBossMelee();
        SetBossFrame(4, cucharazoSprites, cucharazoSprites != null && cucharazoSprites.Length > 1 ? 1 : 0);

        if (bossRenderer != null)
            bossRenderer.color = normalColor;

        // Embestida horizontal directa y controlada durante 0.26s
        float lungeSpeed = isEnraged ? 13.5f : 10.5f;
        float lungeElapsed = 0f;
        bool meleeHitDealt = false;
        while (lungeElapsed < 0.26f)
        {
            if (bossRb != null)
                bossRb.linearVelocity = new Vector2(dirX * lungeSpeed, bossRb.linearVelocity.y);

            if (bossRb == null || Mathf.Abs(bossRb.linearVelocity.x) < lungeSpeed * 0.45f)
            {
                float nx = Mathf.Clamp(transform.position.x + dirX * lungeSpeed * Time.deltaTime, arenaLeftBound, arenaRightBound);
                transform.position = new Vector3(nx, transform.position.y, transform.position.z);
            }

            if (!meleeHitDealt && playerTransform != null)
            {
                float dist = Vector2.Distance(transform.position, playerTransform.position);
                if (dist <= meleeAttackRange + 0.7f)
                {
                    PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
                    if (pm != null && !pm.IsDashing && !pm.IsInvulnerable)
                    {
                        meleeHitDealt = true;
                        Vector2 knock = new Vector2(dirX * 1.8f, 1.0f).normalized;
                        pm.TakeDamage(spoonDamage, knock);
                    }
                }
            }

            lungeElapsed += Time.deltaTime;
            yield return null;
        }

        if (bossRb != null)
            bossRb.linearVelocity = new Vector2(0f, bossRb.linearVelocity.y);

        // Onda corta de caldo hirviendo lanzada desde la cuchara
        SpawnGroundBrothWave(dirX, isEnraged ? 12.5f : 10.5f, 0.85f);

        yield return new WaitForSeconds(0.28f);
        SetBossFrame(0, idleSprites, 0);

        nextAttackTime = Time.time + (isEnraged ? attackCooldown * 0.76f : attackCooldown);
        isAttacking = false;
    }

    // ═══ ATAQUE 2: LLUVIA DE GARBANZOS CALIENTES ═══
    private IEnumerator PerformGarbanzoRainAttack()
    {
        isAttacking = true;
        FaceTowardsPlayer();
        TriggerVocalShout("¡AHÍ LES VA LA PROBADITA DE GARBANZO HIRVIENDO!");
        SetBossFrame(5, lluviaSprites, 0);

        float prepElapsed = 0f;
        while (prepElapsed < 0.35f)
        {
            MoveHorizontallyTowardsPlayer(0.65f, false);
            prepElapsed += Time.deltaTime;
            yield return null;
        }

        int projectileCount = isEnraged ? 5 : 3;
        for (int i = 0; i < projectileCount; i++)
        {
            float currentDirX = FaceTowardsPlayer();
            SpawnGarbanzoProjectile(currentDirX, i, projectileCount);

            if (isEnraged && i % 2 == 0)
                SpawnSkyGarbanzoDrop(i);

            float shotWait = 0f;
            while (shotWait < 0.16f)
            {
                MoveHorizontallyTowardsPlayer(0.55f, false);
                shotWait += Time.deltaTime;
                yield return null;
            }
        }

        if (bossRenderer != null)
            bossRenderer.color = normalColor;
        SetBossFrame(0, idleSprites, 0);

        nextAttackTime = Time.time + (isEnraged ? attackCooldown * 0.76f : attackCooldown);
        isAttacking = false;
    }

    // ═══ ATAQUE 3: ¡OLLAZO SÍSMICO! (SALTO + CAÍDA + ONDAS GEMELAS) ═══
    private IEnumerator PerformPotSlamAttack()
    {
        isAttacking = true;
        if (bossRb != null)
            bossRb.linearVelocity = new Vector2(0f, bossRb.linearVelocity.y);

        TriggerVocalShout("¡¡AGUAS CON LA OLLA HIRVIENDO!!");
        SetBossFrame(5, lluviaSprites, 0);
        if (bossRenderer != null)
            bossRenderer.color = new Color(1f, 0.72f, 0.28f);

        yield return new WaitForSeconds(0.38f);

        float targetX = playerTransform != null
            ? Mathf.Clamp(playerTransform.position.x, arenaLeftBound + 1.2f, arenaRightBound - 1.2f)
            : transform.position.x;

        // Salto vertical persiguiendo horizontalmente hacia la posición del jugador
        if (bossRb != null)
            bossRb.linearVelocity = new Vector2((targetX - transform.position.x) * 1.35f, 12.2f);
        ProceduralSoundManager.Instance?.PlayJump();

        if (bossRenderer != null)
            bossRenderer.color = normalColor;

        float riseElapsed = 0f;
        while (riseElapsed < 0.48f)
        {
            if (bossRb != null)
            {
                float clampX = Mathf.Clamp(transform.position.x, arenaLeftBound + 0.8f, arenaRightBound - 0.8f);
                if (transform.position.x != clampX)
                    transform.position = new Vector3(clampX, transform.position.y, transform.position.z);
            }
            riseElapsed += Time.deltaTime;
            yield return null;
        }

        // Breve suspensión en el aire antes de caer con la olla
        if (bossRb != null)
            bossRb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(0.14f);

        // Caída pesada hasta tocar el suelo
        if (bossRb != null)
            bossRb.linearVelocity = new Vector2(0f, -17.5f);

        float slamTimeout = 0f;
        while (slamTimeout < 1.1f && transform.position.y > arenaFloorY + 0.15f)
        {
            if (bossRb != null)
                bossRb.linearVelocity = new Vector2(0f, -17.5f);
            slamTimeout += Time.deltaTime;
            yield return null;
        }

        if (bossRb != null)
            bossRb.linearVelocity = Vector2.zero;

        // Impacto en suelo: explosión de vapor + daño cercano + 2 ondas gemelas
        ProceduralSoundManager.Instance?.PlayExplosion();
        for (int s = 0; s < 10; s++)
            SpawnSteamSpark(transform.position + new Vector3(Random.Range(-1.2f, 1.2f), 0.2f, 0f),
                            s % 2 == 0 ? new Color(1f, 0.78f, 0.25f) : new Color(0.45f, 0.95f, 0.30f), 1.4f);

        if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= 2.1f)
        {
            PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
            if (pm != null && !pm.IsDashing && !pm.IsInvulnerable)
            {
                float pushDir = playerTransform.position.x >= transform.position.x ? 1f : -1f;
                pm.TakeDamage(1, new Vector2(pushDir * 1.4f, 1.2f).normalized);
            }
        }

        float waveSpeed = isEnraged ? 13.0f : 10.5f;
        SpawnGroundBrothWave(-1f, waveSpeed, 2.4f);
        SpawnGroundBrothWave(1f, waveSpeed, 2.4f);

        yield return new WaitForSeconds(0.42f);
        SetBossFrame(0, idleSprites, 0);

        nextAttackTime = Time.time + (isEnraged ? attackCooldown * 0.78f : attackCooldown);
        isAttacking = false;
    }

    // ═══ ATAQUE 4: ¡OFERTA VIRAL 2x1! (INVOCACIÓN + VENTANA DE CASTIGO) ═══
    private IEnumerator PerformViralOfferSummon()
    {
        isAttacking = true;
        float dirX = FaceTowardsPlayer();

        if (bossRb != null)
            bossRb.linearVelocity = new Vector2(0f, bossRb.linearVelocity.y);

        SetBossFrame(3, cucharazoSprites, 0);

        summonedMinions.RemoveAll(m => m == null);
        if (summonedMinions.Count < 2)
        {
            TriggerVocalShout("¡OFERTA 2x1 MARCHANTE! ¡ATÁQUENLO GARBANZOS!");
            ProceduralSoundManager.Instance?.PlayBossAlert();

            GameObject minionObj = new GameObject("Garbanzo_Oferta2x1");
            float spawnX = Mathf.Clamp(transform.position.x + dirX * 2.0f, arenaLeftBound + 1f, arenaRightBound - 1f);
            minionObj.transform.position = new Vector3(spawnX, transform.position.y + 0.45f, 0f);
            minionObj.transform.localScale = new Vector3(0.75f, 0.75f, 1f);

            SpriteRenderer msr = minionObj.AddComponent<SpriteRenderer>();
            msr.sortingOrder = 3;

            BoxCollider2D mcol = minionObj.AddComponent<BoxCollider2D>();
            mcol.size = new Vector2(0.85f, 0.90f);
            mcol.offset = new Vector2(0f, 0.45f);

            SimpleEnemy minion = minionObj.AddComponent<SimpleEnemy>();
            minion.maxHealth = 2;
            minion.patrolSpeed = isEnraged ? 3.4f : 2.9f;
            minion.detectionRange = 20f;
            minion.canRespawn = false;

            summonedMinions.Add(minionObj);

            for (int i = 0; i < 6; i++)
                SpawnSteamSpark(minionObj.transform.position, new Color(0.45f, 0.96f, 0.30f), 1.1f);
        }
        else
        {
            TriggerVocalShout("¡RESPIRE TANTITO QUE YA VIENE EL SIGUIENTE CUCHARAZO!");
        }

        // Breve pausa recuperando aliento para premiar al jugador que se acerque a golpear
        if (bossRenderer != null)
            bossRenderer.color = new Color(0.80f, 0.92f, 1f);
        SetBossFrame(0, idleSprites, 0);

        yield return new WaitForSeconds(0.70f);

        if (bossRenderer != null)
            bossRenderer.color = normalColor;

        nextAttackTime = Time.time + (isEnraged ? attackCooldown * 0.78f : attackCooldown);
        isAttacking = false;
    }

    private void SpawnGarbanzoProjectile(float dirX, int index, int total)
    {
        ProceduralSoundManager.Instance?.PlayBossShoot();
        Vector3 spawnPos = firePoint != null
            ? firePoint.position
            : transform.position + new Vector3(dirX * Mathf.Abs(firePointOffset.x), firePointOffset.y, 0f);

        GameObject go = new GameObject("Garbanzo_Projectile");
        go.transform.position = spawnPos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = proceduralGarbanzoSprite != null ? proceduralGarbanzoSprite : projectileSprite;
        sr.color = Color.white;
        go.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
        sr.sortingOrder = 6;

        GarbanzoProjectile proj = go.AddComponent<GarbanzoProjectile>();
        proj.damage = 1;
        proj.lifetime = Mathf.Max(6.0f, projectileLifetime);

        Vector2 targetPos = playerTransform != null
            ? (Vector2)playerTransform.position + new Vector2(0f, 0.35f)
            : (Vector2)spawnPos + new Vector2(dirX * 15f, 0f);

        Vector2 toTarget = targetPos - (Vector2)spawnPos;
        float horizontalSign = Mathf.Abs(toTarget.x) > 0.05f ? Mathf.Sign(toTarget.x) : dirX;
        float distX = Mathf.Max(3.5f, Mathf.Abs(toTarget.x));

        float speed = projectileSpeed + (index * 1.4f);
        float flightTime = distX / speed;

        float gravity = Mathf.Abs(Physics2D.gravity.y) * 0.35f;
        float vy = (toTarget.y / flightTime) + (0.5f * gravity * flightTime) + (index * 0.55f);
        vy = Mathf.Clamp(vy, -4f, 9.5f);

        proj.Launch(new Vector2(horizontalSign * speed, vy));
    }

    private void SpawnSkyGarbanzoDrop(int index)
    {
        float dropX = playerTransform != null
            ? Mathf.Clamp(playerTransform.position.x + Random.Range(-3.2f, 3.2f), arenaLeftBound + 1f, arenaRightBound - 1f)
            : Mathf.Lerp(arenaLeftBound + 2f, arenaRightBound - 2f, index * 0.25f);

        GameObject go = new GameObject("Garbanzo_SkyDrop");
        go.transform.position = new Vector3(dropX, arenaFloorY + 9.0f, 0f);
        go.transform.localScale = Vector3.one;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = proceduralGarbanzoSprite != null ? proceduralGarbanzoSprite : projectileSprite;
        sr.color = Color.white;
        sr.sortingOrder = 6;

        GarbanzoProjectile proj = go.AddComponent<GarbanzoProjectile>();
        proj.damage = 1;
        proj.lifetime = 4.5f;
        proj.Launch(new Vector2(0f, -4.5f));
    }

    private void SpawnGroundBrothWave(float dirX, float speed, float life)
    {
        GameObject wave = new GameObject("Broth_Shockwave");
        wave.transform.position = new Vector3(transform.position.x + dirX * 0.85f, arenaFloorY, 0f);
        wave.transform.localScale = new Vector3(1.15f, 1.15f, 1f);

        SpriteRenderer sr = wave.AddComponent<SpriteRenderer>();
        sr.sprite = proceduralWaveSprite != null ? proceduralWaveSprite : projectileSprite;
        sr.sortingOrder = 6;

        GarbanzoProjectile proj = wave.AddComponent<GarbanzoProjectile>();
        proj.ConfigureAsGroundWave(new Vector2(dirX * speed, 0f), 1, life);
    }

    private void SpawnSteamSpark(Vector3 origin, Color color, float scale)
    {
        GameObject spark = new GameObject("Boss_SteamSpark");
        spark.transform.position = origin;
        spark.transform.localScale = Vector3.one * scale;

        SpriteRenderer sr = spark.AddComponent<SpriteRenderer>();
        sr.sprite = ProceduralPixelArt.GetLavaEmberSprite();
        sr.color = color;
        sr.sortingOrder = 6;

        Rigidbody2D srb = spark.AddComponent<Rigidbody2D>();
        srb.gravityScale = -0.35f;
        srb.linearVelocity = new Vector2(Random.Range(-1.8f, 1.8f), Random.Range(1.5f, 3.6f));
        srb.linearDamping = 1.2f;

        Destroy(spark, Random.Range(0.35f, 0.65f));
    }

    public void TriggerVocalShout(string quote)
    {
        activeVocalQuote = quote;
        quoteExpireTime = Time.time + 3.2f;
    }

    public override void TakeDamage(int damage, Vector2 knockbackDir)
    {
        if (isDefeated || isDead || isPhaseTransitioning)
            return;

        currentHealth -= damage;
        contactDamageDisabledUntil = Time.time + 1.0f;
        ProceduralSoundManager.Instance?.PlayEnemyHit();

        if (bossRb != null)
            bossRb.AddForce(new Vector2(knockbackDir.x * 1.1f, 0.25f), ForceMode2D.Impulse);

        for (int i = 0; i < 4; i++)
            SpawnSteamSpark(transform.position + Vector3.up * 1.1f, new Color(1f, 0.92f, 0.45f), 0.95f);

        StartCoroutine(BossDamageFlash());

        if (currentHealth <= 0)
        {
            DefeatBoss();
            return;
        }

        if (!isEnraged && currentHealth <= maxHealth / 2)
        {
            StopAllCoroutines();
            isAttacking = false;
            StartCoroutine(EnterEnragePhaseRoutine());
        }
    }

    private IEnumerator EnterEnragePhaseRoutine()
    {
        isEnraged = true;
        isPhaseTransitioning = true;
        ProceduralSoundManager.Instance?.PlayBossAlert();
        TriggerVocalShout("¡¡ME HICE TENDENCIA EN DOLORES!! ¡¡GARBANZOS AL DOBLE!!");
        Debug.Log("[ViralVendorBoss] ¡El Vendedor Viral entró en modo FURIA TENDENCIA!");

        if (bossRb != null)
            bossRb.linearVelocity = new Vector2(0f, 5.5f);

        SetBossFrame(5, lluviaSprites, 0);

        for (int i = 0; i < 12; i++)
            SpawnSteamSpark(transform.position + new Vector3(Random.Range(-0.9f, 0.9f), Random.Range(0.4f, 1.9f), 0f),
                            i % 2 == 0 ? Color.red : new Color(1f, 0.85f, 0.2f), 1.4f);

        float elapsed = 0f;
        bool blink = false;
        while (elapsed < 0.75f)
        {
            blink = !blink;
            if (bossRenderer != null)
                bossRenderer.color = blink ? new Color(1f, 0.42f, 0.32f) : Color.white;
            yield return new WaitForSeconds(0.09f);
            elapsed += 0.09f;
        }

        if (bossRenderer != null)
            bossRenderer.color = normalColor;
        SetBossFrame(0, idleSprites, 0);

        isPhaseTransitioning = false;
        isAttacking = false;
        nextAttackTime = Time.time + 0.35f;
    }

    private IEnumerator BossDamageFlash()
    {
        if (bossRenderer != null)
        {
            bossRenderer.color = Color.white;
            yield return new WaitForSeconds(0.08f);
            bossRenderer.color = new Color(1f, 0.35f, 0.30f);
            yield return new WaitForSeconds(0.08f);
            bossRenderer.color = normalColor;
        }
    }

    private void DefeatBoss()
    {
        isDefeated = true;
        isDead = true;
        isAttacking = false;
        isPhaseTransitioning = false;
        StopAllCoroutines();

        ProceduralSoundManager.Instance?.PlayVictoryFanfare();
        TriggerVocalShout("¡YA ESTUVO MARCHANTE! ME QUEDÉ SIN GARBANZOS...");
        Debug.Log("¡¡¡EL VENDEDOR VIRAL DERROTADO!!! ¡EL JARDÍN PRINCIPAL ESTÁ EN PAZ!");

        // Destruir esbirros y proyectiles activos de la arena
        foreach (GameObject m in summonedMinions)
            if (m != null) Destroy(m);
        summonedMinions.Clear();

        foreach (GarbanzoProjectile gp in Object.FindObjectsByType<GarbanzoProjectile>())
            if (gp != null) Destroy(gp.gameObject);

        LevelProgressionManager.UnlockNextLevel(1);

        if (levelPortal != null)
            levelPortal.ActivatePortal();

        if (bossRb != null)
            bossRb.linearVelocity = Vector2.zero;

        SetBossFrame(6, defeatSprites, defeatSprites != null ? defeatSprites.Length - 1 : 0);

        if (bossRenderer != null)
            bossRenderer.color = normalColor;

        if (bossBoxCol != null)
            bossBoxCol.enabled = false;

        if (arenaGate != null)
            arenaGate.SetActive(false);

        CameraFollow cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null)
            cam.UnlockBossArena();

        if (playerTransform != null)
        {
            PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
            if (pm != null)
                pm.UnlockFromArena();
        }

        LevelCompletionCelebration.PlayForLevel(
            1,
            bossTitle,
            "¡El Jardín Principal de Dolores Hidalgo está en paz!",
            transform.position,
            playerTransform
        );
    }

    public void ResetBoss()
    {
        StopAllCoroutines();
        isAwakened = false;
        isEnraged = false;
        isAttacking = false;
        isPhaseTransitioning = false;
        isDefeated = false;
        isDead = false;
        currentHealth = maxHealth;
        displayedHealth = maxHealth;
        attackCycleIndex = 0;
        activeVocalQuote = "";

        foreach (GameObject m in summonedMinions)
            if (m != null) Destroy(m);
        summonedMinions.Clear();

        foreach (GarbanzoProjectile gp in Object.FindObjectsByType<GarbanzoProjectile>())
            if (gp != null) Destroy(gp.gameObject);

        transform.position = initialSpawnPosition;

        if (bossRb != null)
            bossRb.linearVelocity = Vector2.zero;

        if (bossRenderer != null)
        {
            bossRenderer.color = normalColor;
            bossRenderer.enabled = true;
            SetBossFrame(0, idleSprites, 0);
        }

        if (bossBoxCol != null)
            bossBoxCol.enabled = true;
        else
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = true;
        }

        if (arenaGate != null)
            arenaGate.SetActive(false);
        if (levelPortal != null)
            levelPortal.LockPortal();

        ProceduralSoundManager.Instance?.RestoreLevelMusic();

        CameraFollow cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null)
            cam.UnlockBossArena();

        if (playerTransform != null)
        {
            PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
            if (pm != null)
                pm.UnlockFromArena();
        }

        FaceDirection(-1f);
        StartCoroutine(IdleAnimationRoutine());
        Debug.Log("[ViralVendorBoss] Jefe reiniciado.");
    }

    private IEnumerator IdleAnimationRoutine()
    {
        int frame = 0;
        while (!isDefeated && !isDead)
        {
            if (!isAwakened && !isAttacking && !isPhaseTransitioning && bossRenderer != null)
            {
                if (useProceduralPixelArt)
                {
                    SetBossFrame(frame % 2 == 0 ? 0 : 1);
                }
                else if (idleSprites != null && idleSprites.Length > 0)
                {
                    Sprite s = idleSprites[frame % idleSprites.Length];
                    if (s != null) bossRenderer.sprite = s;
                }
                frame++;
            }
            yield return new WaitForSeconds(0.26f);
        }
    }

    protected override void OnGUI()
    {
        // Globo de diálogo flotante sin emojis sueltos
        if (Time.time < quoteExpireTime && !string.IsNullOrEmpty(activeVocalQuote) && Camera.main != null && quoteBgTexture != null)
        {
            Vector3 worldHead = transform.position + Vector3.up * 2.35f;
            Vector3 screenHead = Camera.main.WorldToScreenPoint(worldHead);
            if (screenHead.z > 0f)
            {
                GUIStyle quoteStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    padding = new RectOffset(8, 8, 5, 5)
                };
                quoteStyle.normal.textColor = new Color(0.14f, 0.09f, 0.06f, 1f);
                quoteStyle.normal.background = quoteBgTexture;

                float bubbleW = 280f;
                float bubbleH = 46f;
                float bubbleX = screenHead.x - bubbleW * 0.5f;
                float bubbleY = Screen.height - screenHead.y - bubbleH;

                GUI.Box(new Rect(bubbleX, bubbleY, bubbleW, bubbleH), $"\"{activeVocalQuote}\"", quoteStyle);
            }
        }

        // Barra de Vida Formal del Jefe (sin asignaciones de Texture2D por frame)
        if (!isAwakened || isDefeated || barBgTexture == null || goldBorderTexture == null)
            return;

        float screenW = Screen.width;
        float barW = Mathf.Min(490f, screenW * 0.72f);
        float barH = 22f;
        float barX = (screenW - barW) * 0.5f;
        float barY = 34f;

        Rect panelRect = new Rect(barX - 8f, barY - 26f, barW + 16f, barH + 44f);
        GUI.DrawTexture(new Rect(panelRect.x - 2f, panelRect.y - 2f, panelRect.width + 4f, panelRect.height + 4f), goldBorderTexture);
        GUI.DrawTexture(panelRect, barBgTexture);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = isEnraged
            ? new Color(1.00f, 0.42f, 0.32f, 1f)
            : new Color(0.955f, 0.785f, 0.340f, 1f);
        string headerLabel = isEnraged ? $"{bossTitle}  —  FASE II: FURIA VIRAL" : bossTitle;
        GUI.Label(new Rect(barX, barY - 23f, barW, 20f), headerLabel, titleStyle);

        GUI.DrawTexture(new Rect(barX, barY, barW, barH), emptyBarTexture);

        float lagPct = Mathf.Clamp01(displayedHealth / maxHealth);
        if (lagPct > 0.001f)
            GUI.DrawTexture(new Rect(barX, barY, barW * lagPct, barH), barLagTexture);

        float healthPct = Mathf.Clamp01((float)currentHealth / maxHealth);
        if (healthPct > 0.001f)
            GUI.DrawTexture(new Rect(barX, barY, barW * healthPct, barH), isEnraged ? enrageFillTexture : barFillTexture);

        GUIStyle hpStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        hpStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(barX, barY + 1f, barW, barH), $"{currentHealth} / {maxHealth} HP", hpStyle);

        GUIStyle subStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleCenter
        };
        subStyle.normal.textColor = new Color(0.92f, 0.89f, 0.82f, 0.92f);
        GUI.Label(new Rect(barX, barY + barH + 2f, barW, 16f), bossSubtitle, subStyle);
    }

    private static Texture2D MakeTex(Color col)
    {
        Texture2D pix = new Texture2D(2, 2);
        Color[] p = { col, col, col, col };
        pix.SetPixels(p);
        pix.Apply();
        return pix;
    }
}
