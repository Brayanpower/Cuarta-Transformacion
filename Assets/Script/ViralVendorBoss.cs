using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Jefe del Nivel 1 (Jardín Principal / Centro Histórico de Dolores Hidalgo):
/// EL VENDEDOR VIRAL — Don Chava de los Garbanzos.
/// Comerciante ambulante convertido en fenómeno de internet por su frase pegajosa.
/// Cuenta con:
/// - Orientación dinámica izquierda/derecha mirando SIEMPRE al jugador.
/// - Persecución activa en tiempo real por toda la arena para acorralar al jugador.
/// - Ataque 1: Cucharazo colosal telegrafiado con empuje frontal.
/// - Ataque 2: Lluvia de mercancía (garbanzazos de largo alcance dirigidos al jugador que cruzan toda la arena).
/// - Ráfagas Vocales: Globos de diálogo flotantes con frases virales de meme.
/// - Fase de furia al 50% de vida ("¡Me volví viral!").
/// - Bloqueo de cámara y jugador estilo Sonic en la arena.
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

    [Header("Estadísticas de Combate")]
    public float bossSpeed = 3.5f;
    public float enrageSpeed = 4.8f;
    public float meleeAttackRange = 2.4f;
    public float rangedAttackRange = 24.0f;
    public float attackCooldown = 3.2f;
    public int spoonDamage = 2;

    [Header("Proyectil de Garbanzo (Largo Alcance)")]
    public Sprite projectileSprite;
    public float projectileSpeed = 14.5f;
    public float projectileLifetime = 6.0f;
    public Vector2 firePointOffset = new Vector2(0.9f, 1.15f);

    [Header("Arena Bounds (Estilo Sonic)")]
    public float arenaLeftBound = 131f;
    public float arenaRightBound = 155f;
    public Vector3 arenaCameraPosition = new Vector3(143.0f, -1.2f, -10f);
    public float arenaCameraOrthoSize = 6.2f;

    [Header("Sprites de Animación")]
    public Sprite[] idleSprites;
    public Sprite[] walkRightSprites;
    public Sprite[] walkLeftSprites;
    public Sprite[] cucharazoSprites;
    public Sprite[] lluviaSprites;
    public new Sprite[] defeatSprites;

    // Cache estático compartido para asegurar carga de sprites
    private static Sprite[] sharedBossIdle;
    private static Sprite[] sharedBossWalkRight;
    private static Sprite[] sharedBossWalkLeft;
    private static Sprite[] sharedBossCucharazo;
    private static Sprite[] sharedBossLluvia;
    private static Sprite[] sharedBossDefeat;
    private static Sprite sharedBossProjectile;

    private Rigidbody2D bossRb;
    private SpriteRenderer bossRenderer;
    private Color normalColor;

    public bool isAwakened = false;
    public bool isEnraged = false;
    private bool isAttacking = false;
    private bool isDefeated = false;
    private float nextAttackTime = 0f;
    private Vector3 initialSpawnPosition;
    private float walkAnimTimer = 0f;
    private int walkAnimFrame = 0;

    // Ráfagas Vocales (Frases pegajosas virales de meme)
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

    // Texturas para UI cinemática
    private Texture2D barBgTexture;
    private Texture2D barFillTexture;
    private Texture2D quoteBgTexture;

    protected override bool UseGarbanzoSprites => false;

    protected override void Awake()
    {
        base.Awake();
        maxHealth = 16;
        currentHealth = maxHealth;
        damageToPlayer = 1;
        canPatrol = false; // Maneja su propia IA de jefe
        EnsureBossSpritesLoaded();
    }

    protected override void Start()
    {
        bossRb = GetComponent<Rigidbody2D>();
        bossRenderer = GetComponent<SpriteRenderer>();

        if (bossRb == null)
            bossRb = gameObject.AddComponent<Rigidbody2D>();

        bossRb.bodyType = RigidbodyType2D.Dynamic;
        if (bossRb.gravityScale < 1.5f)
            bossRb.gravityScale = 2.5f;
        bossRb.constraints = RigidbodyConstraints2D.FreezeRotation;
        bossRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Material sin fricción y bordes redondeados para que NUNCA se atore en las uniones de tiles del suelo
        PhysicsMaterial2D slipMat = new PhysicsMaterial2D("BossSlipMaterial")
        {
            friction = 0f,
            bounciness = 0f
        };
        bossRb.sharedMaterial = slipMat;

        BoxCollider2D boxCol = GetComponent<BoxCollider2D>();
        if (boxCol != null)
        {
            boxCol.sharedMaterial = slipMat;
            boxCol.edgeRadius = 0.06f;
            boxCol.size = new Vector2(1.05f, 1.88f);
            boxCol.offset = new Vector2(0f, 1.0f);
        }

        // Asegurar velocidad de persecución activa aunque la escena tenga valores antiguos serializados
        if (bossSpeed < 3.2f)
            bossSpeed = 3.5f;
        if (enrageSpeed < 4.5f)
            enrageSpeed = 4.8f;

        if (bossRenderer != null)
            normalColor = bossRenderer.color;

        EnsureBossSpritesLoaded();

        initialSpawnPosition = transform.position;

        if (playerTransform == null)
        {
            PlayerMovement pm = Object.FindAnyObjectByType<PlayerMovement>();
            if (pm != null)
                playerTransform = pm.transform;
        }

        if (arenaGate != null)
            arenaGate.SetActive(false);

        // Inicializar texturas de barra de vida
        barBgTexture = MakeTex(2, 2, new Color(0.12f, 0.08f, 0.05f, 0.9f));
        barFillTexture = MakeTex(2, 2, new Color(0.85f, 0.65f, 0.15f, 0.95f));
        quoteBgTexture = MakeTex(2, 2, new Color(0.95f, 0.95f, 0.85f, 0.95f));

        // Orientar inicialmente hacia la izquierda (donde llega el jugador)
        FaceDirection(-1f);

        // Iniciar bucle de animación cuando está en reposo antes de despertar
        StartCoroutine(IdleAnimationRoutine());
    }

    /// <summary>
    /// Orienta el sprite del jefe y su punto de disparo hacia la dirección indicada (-1 = izquierda, +1 = derecha).
    /// En vendedor_garbanos.png las filas principales (Idle, WalkRight, Cucharazo, Lluvia, Defeat) miran hacia la DERECHA por defecto,
    /// por lo que flipX = true hace que Don Chava mire a la IZQUIERDA y flipX = false hace que mire a la DERECHA.
    /// </summary>
    private void FaceDirection(float dirX)
    {
        bool lookLeft = dirX < 0f;
        if (bossRenderer != null)
        {
            bossRenderer.flipX = lookLeft;
        }

        if (firePoint != null)
        {
            Vector3 localPos = firePoint.localPosition;
            localPos.x = Mathf.Abs(firePointOffset.x) * (lookLeft ? -1f : 1f);
            localPos.y = firePointOffset.y;
            firePoint.localPosition = localPos;
        }
    }

    /// <summary>
    /// Orientación continua hacia la posición actual del jugador.
    /// </summary>
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
    /// Carga robusta de sprites desde el PNG en disco si faltan referencias en el Inspector.
    /// Garantiza que El Vendedor Viral NUNCA sea invisible.
    /// </summary>
    public void EnsureBossSpritesLoaded()
    {
        if (bossRenderer == null)
            bossRenderer = GetComponent<SpriteRenderer>();

        bool needsIdle = idleSprites == null || idleSprites.Length == 0 || idleSprites[0] == null;
        bool needsWalkR = walkRightSprites == null || walkRightSprites.Length == 0 || walkRightSprites[0] == null;
        bool needsWalkL = walkLeftSprites == null || walkLeftSprites.Length == 0 || walkLeftSprites[0] == null;
        bool needsCucharazo = cucharazoSprites == null || cucharazoSprites.Length == 0 || cucharazoSprites[0] == null;
        bool needsLluvia = lluviaSprites == null || lluviaSprites.Length == 0 || lluviaSprites[0] == null;
        bool needsDefeat = defeatSprites == null || defeatSprites.Length == 0 || defeatSprites[0] == null;
        bool needsCurrentSprite = bossRenderer != null && bossRenderer.sprite == null;

        if (!needsIdle && !needsWalkR && !needsWalkL && !needsCucharazo && !needsLluvia && !needsDefeat && !needsCurrentSprite)
            return;

        if (sharedBossIdle == null || sharedBossIdle.Length == 0 || sharedBossIdle[0] == null)
        {
            string path = Path.Combine(Application.dataPath, "Images/Nivel1/Boos/vendedor_garbanos.png");
            if (!File.Exists(path))
                path = "Assets/Images/Nivel1/Boos/vendedor_garbanos.png";

            if (File.Exists(path))
            {
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(1792, 2400, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.LoadImage(bytes);

                sharedBossIdle = new Sprite[]
                {
                    Sprite.Create(tex, new Rect(1, 1998, 297, 308), new Vector2(0.5f, 0f), 150f),
                    Sprite.Create(tex, new Rect(298, 1998, 298, 308), new Vector2(0.5f, 0f), 150f),
                    Sprite.Create(tex, new Rect(596, 1998, 282, 308), new Vector2(0.5f, 0f), 150f),
                    Sprite.Create(tex, new Rect(923, 1998, 233, 308), new Vector2(0.5f, 0f), 150f),
                    Sprite.Create(tex, new Rect(1201, 1998, 289, 308), new Vector2(0.5f, 0f), 150f),
                    Sprite.Create(tex, new Rect(1490, 1998, 286, 308), new Vector2(0.5f, 0f), 150f)
                };

                sharedBossWalkRight = new Sprite[]
                {
                    Sprite.Create(tex, new Rect(0, 1605, 298, 290), new Vector2(0.5f, 0f), 150f),
                    Sprite.Create(tex, new Rect(298, 1605, 298, 292), new Vector2(0.5f, 0f), 150f),
                    Sprite.Create(tex, new Rect(596, 1605, 298, 291), new Vector2(0.5f, 0f), 150f),
                    Sprite.Create(tex, new Rect(894, 1605, 279, 290), new Vector2(0.5f, 0f), 150f),
                    Sprite.Create(tex, new Rect(1195, 1606, 295, 290), new Vector2(0.5f, 0f), 150f),
                    Sprite.Create(tex, new Rect(1490, 1605, 242, 290), new Vector2(0.5f, 0f), 150f)
                };

                sharedBossWalkLeft = new Sprite[]
                {
                    Sprite.Create(tex, new Rect(36, 1215, 262, 287), new Vector2(0.5f, 0f), 150f),
                    Sprite.Create(tex, new Rect(298, 1215, 298, 287), new Vector2(0.5f, 0f), 150f),
                    Sprite.Create(tex, new Rect(596, 1215, 297, 294), new Vector2(0.5f, 0f), 150f),
                    Sprite.Create(tex, new Rect(908, 1215, 284, 287), new Vector2(0.5f, 0f), 150f),
                    Sprite.Create(tex, new Rect(1192, 1215, 298, 287), new Vector2(0.5f, 0f), 150f),
                    Sprite.Create(tex, new Rect(1490, 1215, 298, 293), new Vector2(0.5f, 0f), 150f)
                };

                sharedBossCucharazo = new Sprite[]
                {
                    Sprite.Create(tex, new Rect(36, 815, 321, 294), new Vector2(0.5f, 0f), 150f),
                    Sprite.Create(tex, new Rect(364, 815, 352, 294), new Vector2(0.5f, 0f), 150f),
                    Sprite.Create(tex, new Rect(716, 815, 358, 291), new Vector2(0.5f, 0f), 150f),
                    Sprite.Create(tex, new Rect(1074, 815, 358, 288), new Vector2(0.5f, 0f), 150f),
                    Sprite.Create(tex, new Rect(1432, 815, 334, 291), new Vector2(0.5f, 0f), 150f)
                };

                sharedBossLluvia = new Sprite[]
                {
                    Sprite.Create(tex, new Rect(36, 420, 322, 303), new Vector2(0.5f, 0f), 150f),
                    Sprite.Create(tex, new Rect(358, 420, 358, 299), new Vector2(0.5f, 0f), 150f),
                    Sprite.Create(tex, new Rect(716, 420, 358, 300), new Vector2(0.5f, 0f), 150f),
                    Sprite.Create(tex, new Rect(1074, 420, 358, 300), new Vector2(0.5f, 0f), 150f),
                    Sprite.Create(tex, new Rect(1432, 426, 354, 269), new Vector2(0.5f, 0f), 150f)
                };

                sharedBossDefeat = new Sprite[]
                {
                    Sprite.Create(tex, new Rect(36, 30, 321, 302), new Vector2(0.5f, 0f), 150f),
                    Sprite.Create(tex, new Rect(403, 31, 312, 303), new Vector2(0.5f, 0f), 150f),
                    Sprite.Create(tex, new Rect(756, 31, 318, 303), new Vector2(0.5f, 0f), 150f),
                    Sprite.Create(tex, new Rect(1074, 34, 358, 246), new Vector2(0.5f, 0f), 150f),
                    Sprite.Create(tex, new Rect(1432, 30, 356, 274), new Vector2(0.5f, 0f), 150f)
                };

                sharedBossProjectile = Sprite.Create(tex, new Rect(140, 1998, 40, 40), new Vector2(0.5f, 0.5f), 100f);
            }
        }

        if (needsIdle && sharedBossIdle != null) idleSprites = sharedBossIdle;
        if (needsWalkR && sharedBossWalkRight != null) walkRightSprites = sharedBossWalkRight;
        if (needsWalkL && sharedBossWalkLeft != null) walkLeftSprites = sharedBossWalkLeft;
        if (needsCucharazo && sharedBossCucharazo != null) cucharazoSprites = sharedBossCucharazo;
        if (needsLluvia && sharedBossLluvia != null) lluviaSprites = sharedBossLluvia;
        if (needsDefeat && sharedBossDefeat != null) defeatSprites = sharedBossDefeat;
        if (projectileSprite == null && sharedBossProjectile != null) projectileSprite = sharedBossProjectile;

        if (bossRenderer != null && (bossRenderer.sprite == null || needsCurrentSprite))
        {
            if (idleSprites != null && idleSprites.Length > 0 && idleSprites[0] != null)
                bossRenderer.sprite = idleSprites[0];
        }
    }

    protected override void Update()
    {
        if (isDefeated)
            return;

        if (playerTransform == null)
        {
            PlayerMovement pm = Object.FindAnyObjectByType<PlayerMovement>();
            if (pm != null)
                playerTransform = pm.transform;
            return;
        }

        // SIEMPRE orientar el sprite hacia donde esté el jugador (izquierda o derecha)
        float dirX = FaceTowardsPlayer();

        // Activación de la arena del jefe
        if (!isAwakened)
        {
            if (playerTransform.position.x >= arenaTriggerX)
            {
                AwakenBoss();
            }
            return;
        }

        // Furia al 50% de vida
        if (!isEnraged && currentHealth <= (maxHealth / 2))
        {
            isEnraged = true;
            TriggerVocalShout("¡¡ME HICE VIRAL EN REDES, NADIE ME DETIENE!!");
            Debug.Log("[ViralVendorBoss] ¡El Vendedor Viral entró en modo FURIA TENDENCIA!");
        }

        if (isAttacking)
            return;

        // Cálculo dinámico de distancia hacia la posición actual del jugador
        float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // Decisión de ataque (con enfriamiento real entre ataques para que siempre camine y persiga)
        if (Time.time >= nextAttackTime)
        {
            if (distToPlayer <= meleeAttackRange)
            {
                StartCoroutine(PerformCucharazoAttack());
                return;
            }
            else if (distToPlayer <= rangedAttackRange)
            {
                StartCoroutine(PerformGarbanzoRainAttack());
                return;
            }
        }

        // Persecución real y continua hacia la posición actual del jugador por toda la arena
        MoveHorizontallyTowardsPlayer(1f, true);
    }

    /// <summary>
    /// Mueve a Don Chava horizontalmente hacia el jugador garantizando desplazamiento fluido
    /// incluso sobre uniones de TilemapCollider2D.
    /// </summary>
    private void MoveHorizontallyTowardsPlayer(float speedMultiplier, bool updateWalkSprite)
    {
        if (playerTransform == null || isDefeated)
            return;

        float dirX = FaceTowardsPlayer();
        float horizontalDist = Mathf.Abs(playerTransform.position.x - transform.position.x);
        float currentSpeed = (isEnraged ? enrageSpeed : bossSpeed) * speedMultiplier;

        if (horizontalDist > 0.45f)
        {
            float targetVelX = dirX * currentSpeed;
            if (bossRb != null)
            {
                bossRb.linearVelocity = new Vector2(targetVelX, bossRb.linearVelocity.y);
            }

            // Desplazamiento directo de respaldo si alguna unión de tile frena la velocidad física
            if (bossRb == null || Mathf.Abs(bossRb.linearVelocity.x) < currentSpeed * 0.45f)
            {
                float nextX = Mathf.Clamp(
                    transform.position.x + targetVelX * Time.deltaTime,
                    arenaLeftBound,
                    arenaRightBound
                );
                transform.position = new Vector3(nextX, transform.position.y, transform.position.z);
            }

            if (updateWalkSprite)
            {
                AnimateWalking();
            }
        }
        else if (bossRb != null)
        {
            bossRb.linearVelocity = new Vector2(0f, bossRb.linearVelocity.y);
        }

        // Confinamiento dentro de la arena
        float clampedX = Mathf.Clamp(transform.position.x, arenaLeftBound, arenaRightBound);
        if (transform.position.x != clampedX)
        {
            transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
        }
    }

    private void AnimateWalking()
    {
        if (walkRightSprites == null || walkRightSprites.Length == 0 || bossRenderer == null)
            return;

        walkAnimTimer += Time.deltaTime;
        float fps = isEnraged ? 13f : 10f;
        if (walkAnimTimer >= (1f / fps))
        {
            walkAnimTimer = 0f;
            walkAnimFrame = (walkAnimFrame + 1) % walkRightSprites.Length;
            if (walkRightSprites[walkAnimFrame] != null)
                bossRenderer.sprite = walkRightSprites[walkAnimFrame];
        }
    }

    private void AwakenBoss()
    {
        isAwakened = true;
        // Dar 1.4 segundos iniciales de persecución pura caminando hacia el jugador antes del primer ataque
        nextAttackTime = Time.time + 1.4f;
        ProceduralSoundManager.Instance?.PlayBossAlert();
        ProceduralSoundManager.Instance?.PlayBossMusic();
        TriggerVocalShout(viralPhrases[Random.Range(0, viralPhrases.Length)]);
        Debug.Log("¡¡ALERTA DE JEFE!! El Vendedor Viral ha bloqueado el Jardín Principal.");

        if (arenaGate != null)
            arenaGate.SetActive(true);

        // Bloquear cámara estilo Sonic
        CameraFollow cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null)
            cam.LockBossArena(arenaCameraPosition, arenaCameraOrthoSize);

        // Bloquear a Finn en la arena
        if (playerTransform != null)
        {
            PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
            if (pm != null)
                pm.LockInArena(arenaLeftBound, arenaRightBound);
        }
    }

    // ═══ ATAQUE 1: CUCHARAZO COLOSAL ═══
    private IEnumerator PerformCucharazoAttack()
    {
        isAttacking = true;

        float dirX = FaceTowardsPlayer();

        if (bossRb != null)
            bossRb.linearVelocity = new Vector2(0f, bossRb.linearVelocity.y);

        TriggerVocalShout(viralPhrases[Random.Range(0, viralPhrases.Length)]);

        // Animación de alzar la cuchara (wind-up telegrafiado)
        if (cucharazoSprites != null && cucharazoSprites.Length >= 2 && bossRenderer != null && cucharazoSprites[0] != null)
            bossRenderer.sprite = cucharazoSprites[0];

        if (bossRenderer != null)
            bossRenderer.color = isEnraged ? Color.red : new Color(1f, 0.85f, 0.3f);

        yield return new WaitForSeconds(0.55f); // Ventana para que el jugador esquive con Shift o salte

        // Re-orientar hacia la posición actual del jugador justo al lanzar el golpe
        dirX = FaceTowardsPlayer();

        // Impacto frontal con la cuchara
        ProceduralSoundManager.Instance?.PlayBossMelee();
        if (cucharazoSprites != null && cucharazoSprites.Length >= 3 && bossRenderer != null && cucharazoSprites[2] != null)
            bossRenderer.sprite = cucharazoSprites[2];

        if (bossRb != null)
            bossRb.AddForce(new Vector2(dirX * (isEnraged ? 5.5f : 3.8f), 0.5f), ForceMode2D.Impulse);

        yield return new WaitForSeconds(0.12f);

        // Daño si conecta con el jugador (respetando invulnerabilidad de Dash)
        if (playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist <= meleeAttackRange + 0.65f)
            {
                PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
                if (pm != null && !pm.IsDashing && !pm.IsInvulnerable)
                {
                    Vector2 knockback = new Vector2(dirX * 1.8f, 1.0f).normalized;
                    pm.TakeDamage(spoonDamage, knockback);
                }
            }
        }

        yield return new WaitForSeconds(0.35f); // Recuperación breve para volver a caminar de inmediato

        if (bossRenderer != null)
            bossRenderer.color = normalColor;

        nextAttackTime = Time.time + attackCooldown;
        isAttacking = false;
    }

    // ═══ ATAQUE 2: LLUVIA DE MERCANCÍA (GARBANZAZOS DE LARGO ALCANCE EN MOVIMIENTO) ═══
    private IEnumerator PerformGarbanzoRainAttack()
    {
        isAttacking = true;

        FaceTowardsPlayer();
        TriggerVocalShout("¡AHÍ LES VA LA PROBADITA DE GARBANZO!");

        if (lluviaSprites != null && lluviaSprites.Length >= 1 && bossRenderer != null && lluviaSprites[0] != null)
            bossRenderer.sprite = lluviaSprites[0];

        // Mientras prepara la olla sigue avanzando hacia el jugador
        float prepElapsed = 0f;
        while (prepElapsed < 0.35f)
        {
            MoveHorizontallyTowardsPlayer(0.75f, false);
            prepElapsed += Time.deltaTime;
            yield return null;
        }

        // Sacudida de olla y lanzamiento dirigido hacia el jugador sin dejar de acorralarlo
        if (lluviaSprites != null && lluviaSprites.Length >= 3 && bossRenderer != null && lluviaSprites[2] != null)
            bossRenderer.sprite = lluviaSprites[2];

        int projectileCount = isEnraged ? 4 : 3;
        for (int i = 0; i < projectileCount; i++)
        {
            float currentDirX = FaceTowardsPlayer();
            SpawnGarbanzoProjectile(currentDirX, i, projectileCount);

            float shotWait = 0f;
            while (shotWait < 0.16f)
            {
                MoveHorizontallyTowardsPlayer(0.65f, false);
                shotWait += Time.deltaTime;
                yield return null;
            }
        }

        if (bossRenderer != null)
            bossRenderer.color = normalColor;

        nextAttackTime = Time.time + attackCooldown;
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
        if (projectileSprite != null)
            sr.sprite = projectileSprite;
        sr.color = new Color(0.35f, 0.90f, 0.22f, 1f); // Verde garbanzo brillante
        go.transform.localScale = new Vector3(0.42f, 0.42f, 1f);
        sr.sortingOrder = 6;

        GarbanzoProjectile proj = go.AddComponent<GarbanzoProjectile>();
        proj.damage = 1;
        proj.lifetime = Mathf.Max(6.0f, projectileLifetime);

        // Calcular velocidad dirigida hacia la posición actual del jugador para cruzar toda la arena
        Vector2 targetPos = playerTransform != null
            ? (Vector2)playerTransform.position + new Vector2(0f, 0.35f)
            : (Vector2)spawnPos + new Vector2(dirX * 15f, 0f);

        Vector2 toTarget = targetPos - (Vector2)spawnPos;
        float horizontalSign = Mathf.Abs(toTarget.x) > 0.05f ? Mathf.Sign(toTarget.x) : dirX;
        float distX = Mathf.Max(3.5f, Mathf.Abs(toTarget.x));

        // Velocidad rápida con abanico ligero por cada garbanzazo de la ráfaga
        float speed = projectileSpeed + (index * 1.6f);
        float flightTime = distX / speed;

        // Compensar la caída gravitatoria del proyectil (gravityScale = 0.35f) para que llegue directo al jugador y cruce toda la arena
        float gravity = Mathf.Abs(Physics2D.gravity.y) * 0.35f;
        float vy = (toTarget.y / flightTime) + (0.5f * gravity * flightTime) + (index * 0.65f);
        vy = Mathf.Clamp(vy, -4f, 9.5f);

        Vector2 launchVelocity = new Vector2(horizontalSign * speed, vy);
        proj.Launch(launchVelocity);
    }

    public void TriggerVocalShout(string quote)
    {
        activeVocalQuote = quote;
        quoteExpireTime = Time.time + 3.2f;
    }

    public override void TakeDamage(int damage, Vector2 knockbackDir)
    {
        if (isDefeated)
            return;

        currentHealth -= damage;
        contactDamageDisabledUntil = Time.time + 1.0f;
        ProceduralSoundManager.Instance?.PlayEnemyHit();
        Debug.Log($"¡Vendedor Viral golpeado! Salud restante: {currentHealth}/{maxHealth}");

        if (bossRb != null)
            bossRb.AddForce(knockbackDir * 1.2f, ForceMode2D.Impulse);

        StartCoroutine(BossDamageFlash());

        if (currentHealth <= 0)
        {
            DefeatBoss();
        }
    }

    private IEnumerator BossDamageFlash()
    {
        if (bossRenderer != null)
        {
            bossRenderer.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            bossRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            bossRenderer.color = normalColor;
        }
    }

    private void DefeatBoss()
    {
        isDefeated = true;
        StopAllCoroutines();
        ProceduralSoundManager.Instance?.PlayVictoryFanfare();
        TriggerVocalShout("¡YA ESTUVO MARCHANTE! ME QUEDÉ SIN GARBANZOS...");
        Debug.Log("¡¡¡EL VENDEDOR VIRAL DERROTADO!!! ¡EL JARDÍN PRINCIPAL ESTÁ EN PAZ!");

        // Guardar progresión: Desbloquear Nivel 2 en PlayerPrefs
        LevelProgressionManager.UnlockNextLevel(1);

        if (levelPortal != null)
            levelPortal.ActivatePortal();

        if (bossRb != null)
            bossRb.linearVelocity = Vector2.zero;

        // Sprite de rendición sentado junto a su olla
        if (defeatSprites != null && defeatSprites.Length > 0 && bossRenderer != null)
        {
            Sprite s = defeatSprites[defeatSprites.Length - 1];
            if (s != null)
                bossRenderer.sprite = s;
        }

        if (arenaGate != null)
            arenaGate.SetActive(false); // Abrir salida del nivel

        // Liberar cámara y jugador
        CameraFollow cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null)
            cam.UnlockBossArena();

        if (playerTransform != null)
        {
            PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
            if (pm != null)
                pm.UnlockFromArena();
        }

        // Lanzar animación festiva de fin de nivel (fuegos artificiales, festejo de Finn y listón triunfal)
        LevelCompletionCelebration.PlayForLevel(
            1,
            bossTitle,
            "¡El Jardín Principal de Dolores Hidalgo está en paz!",
            transform.position,
            playerTransform
        );
    }

    private IEnumerator TriggerVictoryMenuRoutine()
    {
        yield return new WaitForSeconds(1.8f);

        PauseMenu pauseMenu = PauseMenu.Instance != null
            ? PauseMenu.Instance
            : Object.FindAnyObjectByType<PauseMenu>();

        if (pauseMenu != null)
        {
            pauseMenu.ShowVictoryScreen(
                1,
                bossTitle,
                "¡El Jardín Principal de Dolores Hidalgo está en paz!"
            );
        }
    }

    public void ResetBoss()
    {
        StopAllCoroutines();
        isAwakened = false;
        isEnraged = false;
        isAttacking = false;
        isDefeated = false;
        currentHealth = maxHealth;
        activeVocalQuote = "";

        transform.position = initialSpawnPosition;

        if (bossRb != null)
            bossRb.linearVelocity = Vector2.zero;

        if (bossRenderer != null)
        {
            bossRenderer.color = normalColor;
            bossRenderer.enabled = true;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = true;

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
        while (!isDefeated)
        {
            // Solo animar Idle cuando aún no está persiguiendo ni atacando
            if (!isAwakened && !isAttacking && idleSprites != null && idleSprites.Length > 0 && bossRenderer != null)
            {
                Sprite s = idleSprites[frame % idleSprites.Length];
                if (s != null)
                    bossRenderer.sprite = s;
                frame++;
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    protected override void OnGUI()
    {
        // Globo de diálogo flotante (Ráfaga Vocal Viral)
        if (Time.time < quoteExpireTime && !string.IsNullOrEmpty(activeVocalQuote) && Camera.main != null)
        {
            Vector3 worldHead = transform.position + Vector3.up * 2.3f;
            Vector3 screenHead = Camera.main.WorldToScreenPoint(worldHead);
            if (screenHead.z > 0)
            {
                GUIStyle quoteStyle = new GUIStyle(GUI.skin.box);
                quoteStyle.fontSize = 13;
                quoteStyle.fontStyle = FontStyle.Bold;
                quoteStyle.normal.textColor = new Color(0.15f, 0.15f, 0.15f);
                quoteStyle.normal.background = quoteBgTexture;
                quoteStyle.alignment = TextAnchor.MiddleCenter;
                quoteStyle.wordWrap = true;

                float bubbleW = 260f;
                float bubbleH = 45f;
                float bubbleX = screenHead.x - bubbleW / 2f;
                float bubbleY = Screen.height - screenHead.y - bubbleH;

                GUI.Box(new Rect(bubbleX, bubbleY, bubbleW, bubbleH), $"💬 \"{activeVocalQuote}\"", quoteStyle);
            }
        }

        // Barra de Vida Cinemática del Jefe
        if (!isAwakened || isDefeated)
            return;

        float screenW = Screen.width;
        float barW = Mathf.Min(460f, screenW * 0.7f);
        float barH = 22f;
        float barX = (screenW - barW) / 2f;
        float barY = 32f;

        // Fondo oscuro
        GUI.DrawTexture(new Rect(barX - 4, barY - 22, barW + 8, barH + 34), barBgTexture);

        // Título del Jefe
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 14;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = isEnraged ? Color.red : new Color(1f, 0.85f, 0.3f);
        GUI.Label(new Rect(barX, barY - 20, barW, 20), $"⚔️  {bossTitle}  ⚔️", titleStyle);

        // Barra de fondo vacía
        Texture2D emptyBg = MakeTex(2, 2, new Color(0.2f, 0.05f, 0.05f, 0.9f));
        GUI.DrawTexture(new Rect(barX, barY, barW, barH), emptyBg);

        // Barra de relleno
        float healthPct = Mathf.Clamp01((float)currentHealth / maxHealth);
        GUI.DrawTexture(new Rect(barX, barY, barW * healthPct, barH), barFillTexture);

        // Texto de vida
        GUIStyle hpStyle = new GUIStyle(GUI.skin.label);
        hpStyle.fontSize = 12;
        hpStyle.fontStyle = FontStyle.Bold;
        hpStyle.alignment = TextAnchor.MiddleCenter;
        hpStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(barX, barY + 1, barW, barH), $"{currentHealth} / {maxHealth} HP", hpStyle);

        // Subtítulo
        GUIStyle subStyle = new GUIStyle(GUI.skin.label);
        subStyle.fontSize = 10;
        subStyle.fontStyle = FontStyle.Italic;
        subStyle.alignment = TextAnchor.MiddleCenter;
        subStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 0.9f);
        GUI.Label(new Rect(barX, barY + barH + 2, barW, 16), bossSubtitle, subStyle);
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Texture2D pix = new Texture2D(width, height);
        Color[] p = new Color[width * height];
        for (int i = 0; i < p.Length; i++) p[i] = col;
        pix.SetPixels(p);
        pix.Apply();
        return pix;
    }
}
