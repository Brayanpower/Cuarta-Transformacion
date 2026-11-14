using System.Collections;
using UnityEngine;

/// <summary>
/// Jefe Final del Nivel 3 (Callejones de Guanajuato — El Callejón del Caos):
/// EL INFLUENCER DEL CALLEJÓN — Obsesionado con cámaras, drones y likes.
/// Máquina de estados completa:
/// - Persecución veloz sin atascos sobre TilemapCollider2D.
/// - Ataque 1: Flashazo Cegador / Ring Light Laser (rayo frontal telegrafiado para esquivar con Dash o saltar).
/// - Ataque 2: Enjambre de Drones de Grabación (disparos teledirigidos + lluvia de likes/emojis en modo furia).
/// - Ataque 3: Dash de Selfie Stick (embestida de extremo a extremo de la arena).
/// - Fase de Furia al 50% de vida ("¡¡ESTAMOS EN VIVO CON 100 MIL VIEWERS!!").
/// - Al derrotarlo llama a LevelProgressionManager.UnlockNextLevel(3) y muestra la Victoria Final.
/// </summary>
public class AlleyInfluencerBoss : SimpleEnemy
{
    [Header("Identidad del Jefe Final")]
    public string bossTitle = "EL INFLUENCER DEL CALLEJÓN";
    public string bossSubtitle = "Rey del Algoritmo — Callejones de Guanajuato";
    public Transform playerTransform;
    public GameObject arenaGate;
    public LevelPortalTrigger levelPortal;
    public float arenaTriggerX = 120f;

    [Header("Estadísticas de Combate")]
    public float bossSpeed = 4.2f;
    public float enrageSpeed = 5.8f;
    public float rangedAttackRange = 26f;
    public float attackCooldown = 2.5f;
    public int selfieDashDamage = 2;

    [Header("Arena Bounds (Estilo Sonic)")]
    public float arenaLeftBound = 119f;
    public float arenaRightBound = 145f;
    public Vector3 arenaCameraPosition = new Vector3(132f, 2.5f, -10f);
    public float arenaCameraOrthoSize = 6.8f;

    private Rigidbody2D bossRb;
    private SpriteRenderer bossRenderer;
    private Color normalColor = Color.white;

    private Sprite[] animSprites; // 0:Idle/WalkA, 1:WalkB, 2:RingLightFlash, 3:SelfieDash, 4:Defeat

    public bool isAwakened = false;
    public bool isEnraged = false;
    private bool isAttacking = false;
    private bool isDefeated = false;
    private float nextAttackTime = 0f;
    private int attackCycleIndex = 0;
    private float walkAnimTimer = 0f;
    private int walkAnimFrame = 0;
    private Vector3 initialSpawnPosition;

    private string activeQuote = "";
    private float quoteExpireTime = 0f;
    private readonly string[] influencerPhrases = new string[]
    {
        "¡NO OLVIDEN DARLE LIKE Y SUSCRIBIRSE AL CAOS!",
        "¡CIERREN EL CALLEJÓN QUE ESTOY GRABANDO UN TIKTOK!",
        "¡SIN FILTRO NO HAY CONTENIDO, CRACKS!",
        "¡MÁNDENME ROSITAS EN EL LIVE MIENTRAS LO DERROTO!",
        "¡ESTE CLIP SE VA DIRECTO A TENDENCIAS MUNDIALES!",
        "¡SONRÍE PARA EL RING LIGHT, VECINO!"
    };

    private Texture2D barBgTex;
    private Texture2D barFillTex;
    private Texture2D emptyBarTex;
    private Texture2D quoteBgTex;

    protected override bool UseGarbanzoSprites => false;

    protected override void Awake()
    {
        maxHealth = 28;
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
            animSprites[i] = ProceduralPixelArt.GetInfluencerBossSprite(i);
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

        PhysicsMaterial2D slipMat = new PhysicsMaterial2D("InfluencerBossSlip")
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
        boxCol.size = new Vector2(1.05f, 2.0f);
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

        barBgTex = MakeTex(new Color(0.14f, 0.04f, 0.22f, 0.94f));
        emptyBarTex = MakeTex(new Color(0.20f, 0.06f, 0.14f, 0.92f));
        barFillTex = MakeTex(new Color(0.98f, 0.18f, 0.60f, 0.96f));
        quoteBgTex = MakeTex(new Color(0.10f, 0.08f, 0.18f, 0.95f));

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
            TriggerQuote("¡¡ESTAMOS EN VIVO CON 100 MIL VIEWERS!! ¡¡MODO VIRAL!!");
        }

        if (isAttacking)
            return;

        float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (Time.time >= nextAttackTime && distToPlayer <= rangedAttackRange)
        {
            int choice = attackCycleIndex % 3;
            attackCycleIndex++;

            switch (choice)
            {
                case 0:
                    StartCoroutine(PerformRingLightFlash());
                    return;
                case 1:
                    StartCoroutine(PerformDroneSwarm());
                    return;
                default:
                    StartCoroutine(PerformSelfieStickDash());
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

        if (horizontalDist > 0.6f)
        {
            float targetVx = dirX * speed;
            if (bossRb != null)
                bossRb.linearVelocity = new Vector2(targetVx, bossRb.linearVelocity.y);

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
        float fps = isEnraged ? 12f : 8.5f;
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
        nextAttackTime = Time.time + 1.2f;
        ProceduralSoundManager.Instance?.PlayBossAlert();
        ProceduralSoundManager.Instance?.PlayBossMusic();
        TriggerQuote(influencerPhrases[0]);

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

    // ═══ ATAQUE 1: FLASHAAZO CEGADOR / RING LIGHT LASER ═══
    private IEnumerator PerformRingLightFlash()
    {
        isAttacking = true;
        if (bossRb != null)
            bossRb.linearVelocity = new Vector2(0f, bossRb.linearVelocity.y);

        TriggerQuote("¡SONRÍE PARA EL FLASH DE 10 MILLONES DE LUMENS!");
        if (bossRenderer != null)
            bossRenderer.sprite = animSprites[2];

        // Telegrafiado intermitente cian/blanco para que Finn prepare el Dash (Left Shift) o salto
        float charge = 0f;
        bool blink = false;
        while (charge < 0.62f)
        {
            blink = !blink;
            if (bossRenderer != null)
                bossRenderer.color = blink ? new Color(0.35f, 1f, 1f, 1f) : Color.white;
            yield return new WaitForSeconds(0.08f);
            charge += 0.08f;
        }

        int beamCount = isEnraged ? 2 : 1;
        for (int b = 0; b < beamCount; b++)
        {
            float dirX = FaceTowardsPlayer();
            ProceduralSoundManager.Instance?.PlayLaserFlash();

            GameObject beam = new GameObject("RingLight_FlashBeam");
            beam.transform.position = transform.position + new Vector3(dirX * 1.2f, 1.05f, 0f);
            beam.transform.localScale = new Vector3(2.8f, 1.2f, 1f);

            FallingPropagandaProjectile proj = beam.AddComponent<FallingPropagandaProjectile>();
            proj.destroyOnGround = false;
            proj.ConfigureAndLaunch(
                FallingPropagandaProjectile.ProjectileStyle.FlashBeam,
                new Vector2(dirX * (isEnraged ? 19f : 16f), 0f),
                isEnraged ? 2 : 1,
                0f,
                2.8f
            );

            if (b + 1 < beamCount)
                yield return new WaitForSeconds(0.28f);
        }

        yield return new WaitForSeconds(0.28f);
        if (bossRenderer != null)
            bossRenderer.color = normalColor;

        nextAttackTime = Time.time + (isEnraged ? attackCooldown * 0.78f : attackCooldown);
        isAttacking = false;
    }

    // ═══ ATAQUE 2: ENJAMBRE DE DRONES DE GRABACIÓN + LLUVIA DE LIKES ═══
    private IEnumerator PerformDroneSwarm()
    {
        isAttacking = true;
        TriggerQuote(isEnraged ? "¡LLUVIA DE LIKES Y DRONES EN 4K!" : "¡DRONES, SÍGANLO QUE ES CONTENIDO VIRAL!");

        if (bossRenderer != null)
            bossRenderer.sprite = animSprites[2];

        int count = isEnraged ? 4 : 3;
        for (int i = 0; i < count; i++)
        {
            float dirX = FaceTowardsPlayer();
            ProceduralSoundManager.Instance?.PlayBossShoot();

            Vector3 spawnPos = transform.position + new Vector3(dirX * 0.8f, 1.8f + (i % 2) * 0.5f, 0f);
            Vector2 initDir = playerTransform != null
                ? ((Vector2)playerTransform.position + Vector2.up * 0.4f - (Vector2)spawnPos).normalized
                : new Vector2(dirX, 0.2f).normalized;

            GameObject droneShot = new GameObject("Influencer_HomingDrone");
            droneShot.transform.position = spawnPos;
            droneShot.transform.localScale = Vector3.one * 0.9f;

            FallingPropagandaProjectile proj = droneShot.AddComponent<FallingPropagandaProjectile>();
            proj.ConfigureAndLaunch(
                FallingPropagandaProjectile.ProjectileStyle.DroneShot,
                initDir * (isEnraged ? 10.5f : 8.5f),
                1,
                0f,
                4.2f,
                isEnraged ? 2.1f : 1.5f,
                playerTransform
            );

            // En fase de furia, también llueven corazones/likes virales desde arriba
            if (isEnraged)
            {
                GameObject likeObj = new GameObject("Viral_Like_Drop");
                float dropX = Mathf.Lerp(arenaLeftBound + 2f, arenaRightBound - 2f, (float)i / Mathf.Max(1, count - 1));
                likeObj.transform.position = new Vector3(dropX, transform.position.y + 8.0f, 0f);
                likeObj.transform.localScale = Vector3.one * 1.05f;

                FallingPropagandaProjectile likeProj = likeObj.AddComponent<FallingPropagandaProjectile>();
                likeProj.ConfigureAndLaunch(
                    FallingPropagandaProjectile.ProjectileStyle.ViralEmojiLike,
                    new Vector2(0f, -4.2f),
                    1,
                    0.28f,
                    5.0f
                );
            }

            float wait = 0f;
            while (wait < 0.22f)
            {
                MoveHorizontallyTowardsPlayer(0.70f, false);
                wait += Time.deltaTime;
                yield return null;
            }
        }

        if (bossRenderer != null)
            bossRenderer.color = normalColor;

        nextAttackTime = Time.time + (isEnraged ? attackCooldown * 0.78f : attackCooldown);
        isAttacking = false;
    }

    // ═══ ATAQUE 3: DASH DE SELFIE STICK (DE EXTREMO A EXTREMO) ═══
    private IEnumerator PerformSelfieStickDash()
    {
        isAttacking = true;
        float dashDir = FaceTowardsPlayer();

        if (bossRb != null)
            bossRb.linearVelocity = new Vector2(0f, bossRb.linearVelocity.y);

        TriggerQuote("¡SELFIE EXTREMA EN VIVO! ¡QUÍTATE QUE SALES MOVIDO!");
        if (bossRenderer != null)
        {
            bossRenderer.sprite = animSprites[3];
            bossRenderer.color = new Color(1f, 0.25f, 0.75f, 1f);
        }

        yield return new WaitForSeconds(0.50f);

        ProceduralSoundManager.Instance?.PlayDash();
        ProceduralSoundManager.Instance?.PlayBossMelee();

        float dashSpeed = isEnraged ? 16.5f : 13.0f;
        float dashDuration = 0.85f;
        float elapsed = 0f;
        bool hasDamaged = false;

        while (elapsed < dashDuration)
        {
            float vx = dashDir * dashSpeed;
            if (bossRb != null)
                bossRb.linearVelocity = new Vector2(vx, bossRb.linearVelocity.y);

            if (bossRb == null || Mathf.Abs(bossRb.linearVelocity.x) < dashSpeed * 0.45f)
            {
                float nextX = Mathf.Clamp(transform.position.x + vx * Time.deltaTime, arenaLeftBound, arenaRightBound);
                transform.position = new Vector3(nextX, transform.position.y, transform.position.z);
            }

            // Rebotar en el borde de la arena si está en modo furia
            if (transform.position.x <= arenaLeftBound + 0.4f && dashDir < 0f)
            {
                if (isEnraged && elapsed < dashDuration * 0.6f)
                {
                    dashDir = 1f;
                    FaceDirection(dashDir);
                    hasDamaged = false;
                }
                else break;
            }
            else if (transform.position.x >= arenaRightBound - 0.4f && dashDir > 0f)
            {
                if (isEnraged && elapsed < dashDuration * 0.6f)
                {
                    dashDir = -1f;
                    FaceDirection(dashDir);
                    hasDamaged = false;
                }
                else break;
            }

            if (!hasDamaged && playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= 1.85f)
            {
                PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
                if (pm != null && !pm.IsDashing && !pm.IsInvulnerable)
                {
                    hasDamaged = true;
                    pm.TakeDamage(selfieDashDamage, new Vector2(dashDir * 1.8f, 1.0f).normalized);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (bossRb != null)
            bossRb.linearVelocity = new Vector2(0f, bossRb.linearVelocity.y);
        if (bossRenderer != null)
            bossRenderer.color = normalColor;

        yield return new WaitForSeconds(0.25f);
        nextAttackTime = Time.time + (isEnraged ? attackCooldown * 0.80f : attackCooldown);
        isAttacking = false;
    }

    public override void TakeDamage(int damage, Vector2 knockbackDir)
    {
        if (isDefeated)
            return;

        currentHealth -= damage;
        contactDamageDisabledUntil = Time.time + 1.0f;
        ProceduralSoundManager.Instance?.PlayEnemyHit();

        if (bossRb != null)
            bossRb.AddForce(knockbackDir * 1.0f, ForceMode2D.Impulse);

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
            bossRenderer.color = new Color(1f, 0.2f, 0.6f, 1f);
            yield return new WaitForSeconds(0.08f);
            bossRenderer.color = normalColor;
        }
    }

    private void DefeatBoss()
    {
        isDefeated = true;
        StopAllCoroutines();

        ProceduralSoundManager.Instance?.PlayVictoryFanfare();
        TriggerQuote("¡NOOO! ¡ME QUEDÉ SIN SEÑAL NI BATERÍA... CANCELADO!");

        LevelProgressionManager.UnlockNextLevel(3);

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

        // Animación festiva de Gran Final (doble show de fuegos artificiales, confeti y festejo de Finn)
        LevelCompletionCelebration.PlayForLevel(
            3,
            bossTitle,
            "¡VICTORIA TOTAL! Salvaste las Fiestas Patronales de Dolores Hidalgo y Guanajuato.",
            transform.position,
            playerTransform
        );
    }

    private IEnumerator ShowFinalVictoryDelayed()
    {
        yield return new WaitForSeconds(2.0f);

        PauseMenu pauseMenu = PauseMenu.Instance != null
            ? PauseMenu.Instance
            : UnityEngine.Object.FindAnyObjectByType<PauseMenu>();

        if (pauseMenu != null)
        {
            pauseMenu.ShowVictoryScreen(
                3,
                bossTitle,
                "¡VICTORIA TOTAL! Salvaste las Fiestas Patronales de Dolores Hidalgo y Guanajuato."
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
        activeQuote = "";
        attackCycleIndex = 0;

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
                quoteStyle.normal.textColor = new Color(0.30f, 0.96f, 1f);
                quoteStyle.normal.background = quoteBgTex;

                float bw = 290f, bh = 48f;
                GUI.Box(new Rect(screenHead.x - bw * 0.5f, Screen.height - screenHead.y - bh, bw, bh), $"📱 \"{activeQuote}\"", quoteStyle);
            }
        }

        if (!isAwakened || isDefeated || barBgTex == null)
            return;

        float screenW = Screen.width;
        float barW = Mathf.Min(500f, screenW * 0.74f);
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
        titleStyle.normal.textColor = isEnraged ? new Color(0.25f, 1f, 1f) : new Color(1f, 0.42f, 0.80f);
        GUI.Label(new Rect(barX, barY - 20, barW, 20), $"📸  {bossTitle}  📸", titleStyle);

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
        string liveTag = isEnraged ? " [EN VIVO - 100K VIEWERS]" : "";
        GUI.Label(new Rect(barX, barY + 1, barW, barH), $"{currentHealth} / {maxHealth} HP{liveTag}", hpStyle);

        GUIStyle subStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleCenter
        };
        subStyle.normal.textColor = new Color(0.88f, 0.80f, 0.98f, 0.9f);
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
