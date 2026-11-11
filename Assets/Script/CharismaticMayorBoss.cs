using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Jefe del Nivel 2 (Talleres de Talavera — Dolores Hidalgo):
/// EL MUNÍCIPE CARISMÁTICO — Político local caracterizado por promesas incumplidas.
/// Máquina de estados completa:
/// - Persecución activa sin atascos sobre TilemapCollider2D.
/// - Ataque 1: Discurso Demagógico / Ráfaga de Megáfono (onda expansiva frontal).
/// - Ataque 2: Lluvia de Promesas Incumplidas (panfletos en arco y sobres desde el cielo).
/// - Ataque 3: Escudo de Burocracia / Guardaespaldas (podio invulnerable, invoca esbirro de Talavera y carga frontal).
/// - Fase de Furia al 50% de vida ("¡EN MI CAMPAÑA SÍ SE CUMPLE!").
/// - Desbloquea el Nivel 3 mediante LevelProgressionManager.UnlockNextLevel(2).
/// </summary>
public class CharismaticMayorBoss : SimpleEnemy
{
    [Header("Identidad del Jefe 2")]
    public string bossTitle = "EL MUNÍCIPE CARISMÁTICO";
    public string bossSubtitle = "Promesas Incumplidas S.A. — Talleres de Talavera";
    public Transform playerTransform;
    public GameObject arenaGate;
    public LevelPortalTrigger levelPortal;
    public float arenaTriggerX = 118f;

    [Header("Estadísticas de Combate")]
    public float bossSpeed = 3.6f;
    public float enrageSpeed = 5.0f;
    public float meleeAttackRange = 3.2f;
    public float rangedAttackRange = 24f;
    public float attackCooldown = 2.9f;
    public int chargeDamage = 2;

    [Header("Arena Bounds (Estilo Sonic)")]
    public float arenaLeftBound = 117f;
    public float arenaRightBound = 143f;
    public Vector3 arenaCameraPosition = new Vector3(130f, -0.5f, -10f);
    public float arenaCameraOrthoSize = 6.5f;

    private Rigidbody2D bossRb;
    private SpriteRenderer bossRenderer;
    private Color normalColor = Color.white;

    private Sprite[] animSprites; // 0:Idle/WalkA, 1:WalkB, 2:Megaphone, 3:PodiumShield, 4:Defeat
    private readonly List<GameObject> summonedMinions = new List<GameObject>();

    public bool isAwakened = false;
    public bool isEnraged = false;
    private bool isAttacking = false;
    private bool isShielded = false;
    private bool isDefeated = false;
    private float nextAttackTime = 0f;
    private int attackCycleIndex = 0;
    private float walkAnimTimer = 0f;
    private int walkAnimFrame = 0;
    private Vector3 initialSpawnPosition;

    // Frases políticas virales
    private string activeQuote = "";
    private float quoteExpireTime = 0f;
    private readonly string[] politicalPhrases = new string[]
    {
        "¡VOTEN POR MÍ Y PAVIMENTO HASTA LAS NUBES!",
        "¡ESTAMOS TRABAJANDO EN UNA COMISIÓN PARA RESOLVERLO!",
        "¡ESO SE ARREGLA EN EL PRÓXIMO PRESUPUESTO!",
        "¡MI COMPROMISO ES TOTAL... HASTA QUE PASEN LAS ELECCIONES!",
        "¡FUERA OPOSICIÓN, QUE VIVA LA BUROCRACIA!",
        "¡FIRMO ANTE NOTARIO QUE NO TE VOY A DEJAR PASAR!"
    };

    private Texture2D barBgTex;
    private Texture2D barFillTex;
    private Texture2D emptyBarTex;
    private Texture2D quoteBgTex;

    protected override bool UseGarbanzoSprites => false;

    protected override void Awake()
    {
        maxHealth = 22;
        currentHealth = maxHealth;
        damageToPlayer = 1;
        canPatrol = false;

        LoadProceduralSprites();
        walkSprites = new Sprite[] { animSprites[0], animSprites[1] };
        defeatSprites = new Sprite[] { animSprites[4] };

        base.Awake();
    }

    private void LoadProceduralSprites()
    {
        animSprites = new Sprite[5];
        for (int i = 0; i < 5; i++)
            animSprites[i] = ProceduralPixelArt.GetMayorBossSprite(i);
    }

    protected override void Start()
    {
        bossRb = GetComponent<Rigidbody2D>();
        bossRenderer = GetComponent<SpriteRenderer>();

        if (bossRb == null)
            bossRb = gameObject.AddComponent<Rigidbody2D>();
        if (bossRenderer == null)
            bossRenderer = gameObject.AddComponent<SpriteRenderer>();

        bossRb.bodyType = RigidbodyType2D.Dynamic;
        bossRb.gravityScale = 2.5f;
        bossRb.constraints = RigidbodyConstraints2D.FreezeRotation;
        bossRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Material sin fricción + edgeRadius para evitar atorarse en uniones de TilemapCollider2D
        PhysicsMaterial2D slipMat = new PhysicsMaterial2D("MayorBossSlip")
        {
            friction = 0f,
            bounciness = 0f
        };
        bossRb.sharedMaterial = slipMat;

        BoxCollider2D boxCol = GetComponent<BoxCollider2D>();
        if (boxCol == null)
            boxCol = gameObject.AddComponent<BoxCollider2D>();
        boxCol.sharedMaterial = slipMat;
        boxCol.edgeRadius = 0.06f;
        boxCol.size = new Vector2(1.1f, 2.0f);
        boxCol.offset = new Vector2(0f, 1.05f);

        if (animSprites == null || animSprites.Length < 5)
            LoadProceduralSprites();

        bossRenderer.sprite = animSprites[0];
        bossRenderer.sortingOrder = 4;
        normalColor = bossRenderer.color;
        initialSpawnPosition = transform.position;

        if (playerTransform == null)
        {
            PlayerMovement pm = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
            if (pm != null)
                playerTransform = pm.transform;
        }

        if (arenaGate != null)
            arenaGate.SetActive(false);

        barBgTex = MakeTex(new Color(0.08f, 0.11f, 0.22f, 0.92f));
        emptyBarTex = MakeTex(new Color(0.18f, 0.06f, 0.08f, 0.92f));
        barFillTex = MakeTex(new Color(0.18f, 0.56f, 0.92f, 0.96f));
        quoteBgTex = MakeTex(new Color(0.96f, 0.95f, 0.88f, 0.96f));

        FaceDirection(-1f);
    }

    private void FaceDirection(float dirX)
    {
        if (bossRenderer != null)
            bossRenderer.flipX = dirX < 0f;
    }

    private float FaceTowardsPlayer()
    {
        if (playerTransform == null)
            return -1f;

        float dirX = playerTransform.position.x < transform.position.x ? -1f : 1f;
        FaceDirection(dirX);
        return dirX;
    }

    protected override void Update()
    {
        if (isDefeated)
            return;

        if (playerTransform == null)
        {
            PlayerMovement pm = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
            if (pm != null)
                playerTransform = pm.transform;
            return;
        }

        FaceTowardsPlayer();

        if (!isAwakened)
        {
            if (playerTransform.position.x >= arenaTriggerX)
                AwakenBoss();
            return;
        }

        // Fase de furia al 50% HP
        if (!isEnraged && currentHealth <= maxHealth / 2)
        {
            isEnraged = true;
            ProceduralSoundManager.Instance?.PlayBossAlert();
            TriggerQuote("¡¡EN MI CAMPAÑA SÍ SE CUMPLE!! ¡¡DOBLE PRESUPUESTO!!");
        }

        if (isAttacking)
            return;

        float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (Time.time >= nextAttackTime && distToPlayer <= rangedAttackRange)
        {
            int choice = attackCycleIndex % 3;
            attackCycleIndex++;

            if (distToPlayer <= meleeAttackRange && choice != 2)
            {
                StartCoroutine(PerformMegaphoneBlast());
                return;
            }

            switch (choice)
            {
                case 0:
                    StartCoroutine(PerformMegaphoneBlast());
                    return;
                case 1:
                    StartCoroutine(PerformPromiseRain());
                    return;
                default:
                    StartCoroutine(PerformBureaucracyShield());
                    return;
            }
        }

        MoveHorizontallyTowardsPlayer(1f, true);
    }

    private void MoveHorizontallyTowardsPlayer(float speedMultiplier, bool animateWalk)
    {
        if (playerTransform == null || isDefeated)
            return;

        float dirX = FaceTowardsPlayer();
        float horizontalDist = Mathf.Abs(playerTransform.position.x - transform.position.x);
        float speed = (isEnraged ? enrageSpeed : bossSpeed) * speedMultiplier;

        if (horizontalDist > 0.55f)
        {
            float targetVx = dirX * speed;
            if (bossRb != null)
                bossRb.linearVelocity = new Vector2(targetVx, bossRb.linearVelocity.y);

            // Respaldo anti-atasco sobre uniones de TilemapCollider2D
            if (bossRb == null || Mathf.Abs(bossRb.linearVelocity.x) < speed * 0.45f)
            {
                float nextX = Mathf.Clamp(transform.position.x + targetVx * Time.deltaTime, arenaLeftBound, arenaRightBound);
                transform.position = new Vector3(nextX, transform.position.y, transform.position.z);
            }

            if (animateWalk)
                AnimateWalking();
        }
        else if (bossRb != null)
        {
            bossRb.linearVelocity = new Vector2(0f, bossRb.linearVelocity.y);
        }

        float clampedX = Mathf.Clamp(transform.position.x, arenaLeftBound, arenaRightBound);
        if (transform.position.x != clampedX)
            transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
    }

    private void AnimateWalking()
    {
        if (bossRenderer == null || animSprites == null)
            return;

        walkAnimTimer += Time.deltaTime;
        float fps = isEnraged ? 10f : 7f;
        if (walkAnimTimer >= 1f / fps)
        {
            walkAnimTimer = 0f;
            walkAnimFrame = (walkAnimFrame + 1) % 2;
            bossRenderer.sprite = animSprites[walkAnimFrame];
        }
    }

    private void AwakenBoss()
    {
        isAwakened = true;
        nextAttackTime = Time.time + 1.3f;
        ProceduralSoundManager.Instance?.PlayBossAlert();
        ProceduralSoundManager.Instance?.PlayBossMusic();
        TriggerQuote(politicalPhrases[0]);

        if (arenaGate != null)
            arenaGate.SetActive(true);

        CameraFollow cam = UnityEngine.Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null)
            cam.LockBossArena(arenaCameraPosition, arenaCameraOrthoSize);

        if (playerTransform != null)
        {
            PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
            if (pm != null)
                pm.LockInArena(arenaLeftBound, arenaRightBound);
        }
    }

    // ═══ ATAQUE 1: DISCURSO DEMAGÓGICO / RÁFAGA DE MEGÁFONO ═══
    private IEnumerator PerformMegaphoneBlast()
    {
        isAttacking = true;
        float dirX = FaceTowardsPlayer();

        if (bossRb != null)
            bossRb.linearVelocity = new Vector2(0f, bossRb.linearVelocity.y);

        TriggerQuote(politicalPhrases[Random.Range(0, politicalPhrases.Length)]);
        if (bossRenderer != null)
        {
            bossRenderer.sprite = animSprites[2];
            bossRenderer.color = new Color(1f, 0.88f, 0.35f, 1f);
        }

        yield return new WaitForSeconds(0.55f);

        int waves = isEnraged ? 2 : 1;
        for (int w = 0; w < waves; w++)
        {
            dirX = FaceTowardsPlayer();
            ProceduralSoundManager.Instance?.PlayMegaphoneBlast();

            GameObject waveObj = new GameObject("Megaphone_Shockwave");
            waveObj.transform.position = transform.position + new Vector3(dirX * 1.15f, 1.1f, 0f);
            waveObj.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

            FallingPropagandaProjectile proj = waveObj.AddComponent<FallingPropagandaProjectile>();
            proj.destroyOnGround = false;
            proj.ConfigureAndLaunch(
                FallingPropagandaProjectile.ProjectileStyle.MegaphoneWave,
                new Vector2(dirX * (isEnraged ? 15.5f : 12.5f), 0f),
                isEnraged ? 2 : 1,
                0f,
                3.5f
            );

            if (w + 1 < waves)
                yield return new WaitForSeconds(0.32f);
        }

        yield return new WaitForSeconds(0.30f);
        if (bossRenderer != null)
            bossRenderer.color = normalColor;

        nextAttackTime = Time.time + (isEnraged ? attackCooldown * 0.78f : attackCooldown);
        isAttacking = false;
    }

    // ═══ ATAQUE 2: LLUVIA DE PROMESAS INCUMPLIDAS (PANFLETOS Y SOBRES) ═══
    private IEnumerator PerformPromiseRain()
    {
        isAttacking = true;
        TriggerQuote("¡PROMESAS Y SOBRES PARA TODO EL PUEBLO!");

        if (bossRenderer != null)
            bossRenderer.sprite = animSprites[2];

        int arcCount = isEnraged ? 5 : 3;
        for (int i = 0; i < arcCount; i++)
        {
            float dirX = FaceTowardsPlayer();
            ProceduralSoundManager.Instance?.PlayBossShoot();

            // Panfleto lanzado en arco hacia el jugador
            GameObject pObj = new GameObject("Mayor_Pamphlet");
            pObj.transform.position = transform.position + new Vector3(dirX * 0.9f, 1.4f, 0f);
            pObj.transform.localScale = Vector3.one * 0.95f;

            FallingPropagandaProjectile proj = pObj.AddComponent<FallingPropagandaProjectile>();
            float speedX = 9.5f + i * 1.8f;
            proj.ConfigureAndLaunch(
                i % 2 == 0 ? FallingPropagandaProjectile.ProjectileStyle.MayorPamphlet : FallingPropagandaProjectile.ProjectileStyle.GoldenEnvelope,
                new Vector2(dirX * speedX, 5.2f + i * 0.6f),
                1,
                0.55f,
                5.0f
            );

            // Además, sobre cayendo verticalmente sobre la arena
            if (playerTransform != null)
            {
                GameObject drop = new GameObject("Falling_Envelope");
                float dropX = Mathf.Clamp(playerTransform.position.x + Random.Range(-3.5f, 3.5f), arenaLeftBound + 1f, arenaRightBound - 1f);
                drop.transform.position = new Vector3(dropX, transform.position.y + 7.5f, 0f);
                drop.transform.localScale = Vector3.one * 0.95f;

                FallingPropagandaProjectile dropProj = drop.AddComponent<FallingPropagandaProjectile>();
                dropProj.ConfigureAndLaunch(
                    FallingPropagandaProjectile.ProjectileStyle.GoldenEnvelope,
                    new Vector2(0f, -3.5f),
                    1,
                    0.35f,
                    5.0f
                );
            }

            float wait = 0f;
            while (wait < 0.22f)
            {
                MoveHorizontallyTowardsPlayer(0.65f, false);
                wait += Time.deltaTime;
                yield return null;
            }
        }

        if (bossRenderer != null)
            bossRenderer.color = normalColor;

        nextAttackTime = Time.time + (isEnraged ? attackCooldown * 0.80f : attackCooldown);
        isAttacking = false;
    }

    // ═══ ATAQUE 3: ESCUDO DE BUROCRACIA + INVOCACIÓN Y CARGA ═══
    private IEnumerator PerformBureaucracyShield()
    {
        isAttacking = true;
        isShielded = true;

        if (bossRb != null)
            bossRb.linearVelocity = new Vector2(0f, bossRb.linearVelocity.y);

        TriggerQuote("¡TRÁMITE EN PROCESO! ¡ATRAS, GUARDAESPALDAS!");
        if (bossRenderer != null)
        {
            bossRenderer.sprite = animSprites[3]; // Podio de burocracia
            bossRenderer.color = new Color(0.75f, 0.92f, 1f, 1f);
        }

        // Limpiar referencias muertas e invocar 1 esbirro de Talavera si hay menos de 2
        summonedMinions.RemoveAll(m => m == null);
        if (summonedMinions.Count < 2)
        {
            float dirX = FaceTowardsPlayer();
            GameObject guard = new GameObject("Talavera_Guard_Summoned");
            float spawnX = Mathf.Clamp(transform.position.x + dirX * 2.2f, arenaLeftBound + 1f, arenaRightBound - 1f);
            guard.transform.position = new Vector3(spawnX, transform.position.y + 0.4f, 0f);

            SpriteRenderer gSr = guard.AddComponent<SpriteRenderer>();
            gSr.sortingOrder = 3;
            BoxCollider2D gCol = guard.AddComponent<BoxCollider2D>();
            gCol.size = new Vector2(0.8f, 0.95f);
            gCol.offset = new Vector2(0f, 0.48f);

            Level2TalaveraEnemy minion = guard.AddComponent<Level2TalaveraEnemy>();
            minion.maxHealth = 2;
            minion.canRespawn = false;
            summonedMinions.Add(guard);
        }

        yield return new WaitForSeconds(1.1f);

        // Quitar escudo y cargar frontalmente hacia el jugador
        isShielded = false;
        float chargeDir = FaceTowardsPlayer();
        ProceduralSoundManager.Instance?.PlayBossMelee();

        if (bossRenderer != null)
        {
            bossRenderer.sprite = animSprites[1];
            bossRenderer.color = isEnraged ? new Color(1f, 0.4f, 0.4f, 1f) : new Color(1f, 0.8f, 0.35f, 1f);
        }

        float chargeElapsed = 0f;
        float chargeSpeed = isEnraged ? 11.5f : 8.5f;
        bool dealtChargeHit = false;
        while (chargeElapsed < 0.55f)
        {
            float vx = chargeDir * chargeSpeed;
            if (bossRb != null)
                bossRb.linearVelocity = new Vector2(vx, bossRb.linearVelocity.y);

            float nextX = Mathf.Clamp(transform.position.x + vx * Time.deltaTime, arenaLeftBound, arenaRightBound);
            if (bossRb == null || Mathf.Abs(bossRb.linearVelocity.x) < chargeSpeed * 0.45f)
                transform.position = new Vector3(nextX, transform.position.y, transform.position.z);

            if (!dealtChargeHit && playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= 1.7f)
            {
                PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
                if (pm != null && !pm.IsDashing && !pm.IsInvulnerable)
                {
                    dealtChargeHit = true;
                    pm.TakeDamage(chargeDamage, new Vector2(chargeDir * 1.6f, 0.9f).normalized);
                }
            }

            chargeElapsed += Time.deltaTime;
            yield return null;
        }

        if (bossRb != null)
            bossRb.linearVelocity = new Vector2(0f, bossRb.linearVelocity.y);
        if (bossRenderer != null)
            bossRenderer.color = normalColor;

        nextAttackTime = Time.time + (isEnraged ? attackCooldown * 0.85f : attackCooldown);
        isAttacking = false;
    }

    public override void TakeDamage(int damage, Vector2 knockbackDir)
    {
        if (isDefeated)
            return;

        if (isShielded)
        {
            ProceduralSoundManager.Instance?.PlayPlatformCrack();
            TriggerQuote("¡SIN OFICIO SELLADO NO ME PUEDES TOCAR!");
            return;
        }

        currentHealth -= damage;
        contactDamageDisabledUntil = Time.time + 1.0f;
        ProceduralSoundManager.Instance?.PlayEnemyHit();

        if (bossRb != null)
            bossRb.AddForce(knockbackDir * 1.1f, ForceMode2D.Impulse);

        StartCoroutine(BossFlashRoutine());

        if (currentHealth <= 0)
            DefeatBoss();
    }

    private IEnumerator BossFlashRoutine()
    {
        if (bossRenderer != null)
        {
            bossRenderer.color = Color.white;
            yield return new WaitForSeconds(0.08f);
            bossRenderer.color = Color.red;
            yield return new WaitForSeconds(0.08f);
            bossRenderer.color = normalColor;
        }
    }

    private void DefeatBoss()
    {
        isDefeated = true;
        isShielded = false;
        StopAllCoroutines();

        ProceduralSoundManager.Instance?.PlayVictoryFanfare();
        TriggerQuote("¡RENUNCIO! ¡ME VOY DE PLURINOMINAL!");

        // Limpiar esbirros invocados
        foreach (GameObject m in summonedMinions)
            if (m != null) Destroy(m);
        summonedMinions.Clear();

        // Guardar progresión: desbloquear Nivel 3
        LevelProgressionManager.UnlockNextLevel(2);

        if (bossRb != null)
            bossRb.linearVelocity = Vector2.zero;

        if (bossRenderer != null && animSprites != null && animSprites.Length >= 5)
            bossRenderer.sprite = animSprites[4];

        if (arenaGate != null)
            arenaGate.SetActive(false);

        if (levelPortal != null)
            levelPortal.ActivatePortal();

        CameraFollow cam = UnityEngine.Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null)
            cam.UnlockBossArena();

        if (playerTransform != null)
        {
            PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
            if (pm != null)
                pm.UnlockFromArena();
        }

        // Animación festiva de fin de nivel (fuegos artificiales, confeti y festejo de Finn)
        LevelCompletionCelebration.PlayForLevel(
            2,
            bossTitle,
            "¡Los Talleres de Talavera de Dolores Hidalgo están a salvo!",
            transform.position,
            playerTransform
        );
    }

    private IEnumerator ShowVictoryDelayed()
    {
        yield return new WaitForSeconds(2.0f);

        PauseMenu pauseMenu = PauseMenu.Instance != null
            ? PauseMenu.Instance
            : UnityEngine.Object.FindAnyObjectByType<PauseMenu>();

        if (pauseMenu != null)
        {
            pauseMenu.ShowVictoryScreen(
                2,
                bossTitle,
                "¡Los Talleres de Talavera de Dolores Hidalgo están a salvo!"
            );
        }
    }

    public void ResetBoss()
    {
        StopAllCoroutines();
        isAwakened = false;
        isEnraged = false;
        isAttacking = false;
        isShielded = false;
        isDefeated = false;
        currentHealth = maxHealth;
        activeQuote = "";
        attackCycleIndex = 0;

        foreach (GameObject m in summonedMinions)
            if (m != null) Destroy(m);
        summonedMinions.Clear();

        transform.position = initialSpawnPosition;
        if (bossRb != null)
            bossRb.linearVelocity = Vector2.zero;

        if (bossRenderer != null)
        {
            bossRenderer.color = normalColor;
            bossRenderer.enabled = true;
            if (animSprites != null && animSprites.Length > 0)
                bossRenderer.sprite = animSprites[0];
        }

        if (arenaGate != null)
            arenaGate.SetActive(false);
        if (levelPortal != null)
            levelPortal.LockPortal();

        CameraFollow cam = UnityEngine.Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null)
            cam.UnlockBossArena();

        if (playerTransform != null)
        {
            PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
            if (pm != null)
                pm.UnlockFromArena();
        }

        ProceduralSoundManager.Instance?.RestoreLevelMusic();
        FaceDirection(-1f);
    }

    private void TriggerQuote(string quote)
    {
        activeQuote = quote;
        quoteExpireTime = Time.time + 3.2f;
    }

    protected override void OnGUI()
    {
        if (Time.time < quoteExpireTime && !string.IsNullOrEmpty(activeQuote) && Camera.main != null)
        {
            Vector3 screenHead = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2.5f);
            if (screenHead.z > 0)
            {
                GUIStyle quoteStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };
                quoteStyle.normal.textColor = new Color(0.10f, 0.14f, 0.25f);
                quoteStyle.normal.background = quoteBgTex;

                float bw = 280f, bh = 48f;
                GUI.Box(new Rect(screenHead.x - bw * 0.5f, Screen.height - screenHead.y - bh, bw, bh), $"📢 \"{activeQuote}\"", quoteStyle);
            }
        }

        if (!isAwakened || isDefeated || barBgTex == null)
            return;

        float screenW = Screen.width;
        float barW = Mathf.Min(480f, screenW * 0.72f);
        float barH = 22f;
        float barX = (screenW - barW) * 0.5f;
        float barY = 32f;

        GUI.DrawTexture(new Rect(barX - 4, barY - 22, barW + 8, barH + 36), barBgTex);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = isEnraged ? new Color(1f, 0.35f, 0.30f) : new Color(0.98f, 0.82f, 0.28f);
        GUI.Label(new Rect(barX, barY - 20, barW, 20), $"🏛️  {bossTitle}  🏛️", titleStyle);

        GUI.DrawTexture(new Rect(barX, barY, barW, barH), emptyBarTex);
        float pct = Mathf.Clamp01((float)currentHealth / maxHealth);
        GUI.DrawTexture(new Rect(barX, barY, barW * pct, barH), barFillTex);

        GUIStyle hpStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        hpStyle.normal.textColor = Color.white;
        string shieldTag = isShielded ? " [ESCUDO ACTIVO]" : "";
        GUI.Label(new Rect(barX, barY + 1, barW, barH), $"{currentHealth} / {maxHealth} HP{shieldTag}", hpStyle);

        GUIStyle subStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleCenter
        };
        subStyle.normal.textColor = new Color(0.85f, 0.88f, 0.96f, 0.9f);
        GUI.Label(new Rect(barX, barY + barH + 2, barW, 16), bossSubtitle, subStyle);
    }

    private static Texture2D MakeTex(Color col)
    {
        Texture2D tex = new Texture2D(2, 2);
        tex.SetPixels(new[] { col, col, col, col });
        tex.Apply();
        return tex;
    }
}
