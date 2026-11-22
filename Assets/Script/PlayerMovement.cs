using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D playerCollider;

    [Header("Movement")]
    public float moveSpeed = 7.5f;
    public float jumpForce = 9.2f;

    [Header("Dash & Dodge (Left Shift)")]
    public float dashSpeed = 18.5f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 0.8f;
    public bool isDashing = false;
    private bool canDash = true;
    private float dashDirection = 1f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public Vector2 groundCheckSize = new Vector2(0.32f, 0.08f);
    public LayerMask groundLayer;

    [Header("Combat & Attack")]
    public float attackRange = 0.7f;
    public Vector2 attackOffset = new Vector2(0.5f, 0f);
    public int attackDamage = 1;
    public LayerMask attackableLayers;

    [Header("Health & Damage")]
    public int maxHealth = 5;
    public int currentHealth;
    public float invulnerabilityDuration = 1.0f;
    public float damageKnockbackForce = 5f;

    [Header("Juice (Squash & Stretch)")]
    public bool enableSquashAndStretch = false;
    public float squashStretchSpeed = 10f;

    private float horizontalMovement;
    private bool isGrounded;
    private bool wasGrounded;
    public bool isInvulnerable = false;
    private bool isDead = false;

    public bool IsDashing => isDashing;
    public bool IsInvulnerable => isInvulnerable || isDashing;

    // Coyote time & jump buffering
    private float coyoteTime = 0.15f;
    private float coyoteTimeCounter;
    private float jumpBufferTime = 0.15f;
    private float jumpBufferCounter;

    private Vector3 originalScale;
    private Vector3 spawnPoint;
    private Color originalSpriteColor = Color.white;
    private float defaultGravityScale = 1f;

    [Header("Límite Izquierdo y Caída al Vacío")]
    public bool blockLeftMapEdge = true;
    public float leftBoundaryMargin = 2.0f;
    public float voidKillY = -14f;
    private float leftBoundaryX;

    // Arena Boss Lock (estilo Sonic)
    private bool isArenaLocked = false;
    private float arenaMinX;
    private float arenaMaxX;

    // Control automático / bloqueo total durante transiciones y tarjetas de nivel estilo Sonic
    private bool externalAutoWalk = false;
    private float externalWalkDir = 0f;
    private bool isControlLocked = false;
    private Coroutine activeDashRoutine;

    public void SetControlLocked(bool locked)
    {
        isControlLocked = locked;
        if (locked)
        {
            if (activeDashRoutine != null)
            {
                StopCoroutine(activeDashRoutine);
                activeDashRoutine = null;
                canDash = true;
            }

            isInvulnerable = true;
            isDashing = false;
            horizontalMovement = 0f;
            jumpBufferCounter = 0f;

            if (rb != null)
            {
                rb.gravityScale = defaultGravityScale > 0f ? defaultGravityScale : 1f;
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
            if (spriteRenderer != null)
                spriteRenderer.color = originalSpriteColor;
            if (animator != null)
                animator.SetFloat("Horizontal", 0f);
        }
        else if (!externalAutoWalk)
        {
            isInvulnerable = false;
        }
    }

    public void SetAutoWalkExit(bool enabled, float directionX = 1f)
    {
        externalAutoWalk = enabled;
        externalWalkDir = enabled ? Mathf.Sign(directionX) : 0f;
        if (enabled)
        {
            if (activeDashRoutine != null)
            {
                StopCoroutine(activeDashRoutine);
                activeDashRoutine = null;
                canDash = true;
            }

            isControlLocked = true;
            isArenaLocked = false;
            isInvulnerable = true;
            isDashing = false;
            horizontalMovement = externalWalkDir;

            if (rb != null)
                rb.gravityScale = defaultGravityScale > 0f ? defaultGravityScale : 1f;
            if (spriteRenderer != null)
                spriteRenderer.color = originalSpriteColor;
        }
        else
        {
            horizontalMovement = 0f;
            if (rb != null)
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>();

        if (rb == null)
            Debug.LogError("No se encontró Rigidbody2D en " + gameObject.name);
        else
        {
            defaultGravityScale = rb.gravityScale;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // Material sin fricción y bordes redondeados para evitar que Finn se trabe jamás en suelo plano
            PhysicsMaterial2D playerSlipMat = new PhysicsMaterial2D("PlayerZeroFriction")
            {
                friction = 0f,
                bounciness = 0f
            };
            rb.sharedMaterial = playerSlipMat;

            BoxCollider2D box = playerCollider as BoxCollider2D;
            if (box != null)
            {
                box.sharedMaterial = playerSlipMat;
                if (box.edgeRadius < 0.03f)
                {
                    float radius = 0.04f;
                    box.size = new Vector2(
                        Mathf.Max(0.18f, box.size.x - radius * 2f),
                        Mathf.Max(0.24f, box.size.y - radius * 2f)
                    );
                    box.edgeRadius = radius;
                }
            }
            else if (playerCollider != null)
            {
                playerCollider.sharedMaterial = playerSlipMat;
            }
        }

        if (animator == null)
            Debug.LogError("No se encontró Animator en " + gameObject.name);

        if (spriteRenderer != null)
            originalSpriteColor = spriteRenderer.color;

        // Garantizar alcance de espada cómodo y ventana justa de invulnerabilidad / vida
        if (attackRange < 1.15f)
            attackRange = 1.15f;
        if (invulnerabilityDuration < 1.45f)
            invulnerabilityDuration = 1.45f;
        if (maxHealth < 6)
            maxHealth = 6;

        currentHealth = maxHealth;
        spawnPoint = transform.position;
        leftBoundaryX = spawnPoint.x - leftBoundaryMargin;

        originalScale = transform.localScale;
        if (Mathf.Approximately(originalScale.x, 0f))
            originalScale = Vector3.one;

        // Garantizar que los Tilemaps de suelo usen CompositeCollider2D sin costuras 1x1
        EnsureSeamlessGroundTilemaps();

        // Por defecto detectar cualquier capa si no se asignó capa específica
        if (attackableLayers.value == 0)
            attackableLayers = ~0;
    }

    private void EnsureSeamlessGroundTilemaps()
    {
        TilemapCollider2D[] tileCols = Object.FindObjectsByType<TilemapCollider2D>();
        foreach (TilemapCollider2D tmCol in tileCols)
        {
            if (tmCol == null || !tmCol.enabled)
                continue;

            GameObject go = tmCol.gameObject;
            Rigidbody2D groundRb = go.GetComponent<Rigidbody2D>();
            if (groundRb == null)
                groundRb = go.AddComponent<Rigidbody2D>();
            groundRb.bodyType = RigidbodyType2D.Static;
            groundRb.simulated = true;

            tmCol.compositeOperation = Collider2D.CompositeOperation.Merge;

            CompositeCollider2D comp = go.GetComponent<CompositeCollider2D>();
            if (comp == null)
                comp = go.AddComponent<CompositeCollider2D>();
            comp.geometryType = CompositeCollider2D.GeometryType.Polygons;
            comp.generationType = CompositeCollider2D.GenerationType.Synchronous;
            comp.GenerateGeometry();
        }
    }

    void Update()
    {
        if (rb == null || animator == null || isDead)
            return;

        // Si cae al vacío por debajo del mapa, morir y reaparecer en vez de caer infinitamente
        if (transform.position.y < voidKillY)
        {
            currentHealth = 0;
            Die();
            return;
        }

        // Durante la animación automática de salida estilo Sonic/Mario ignorar controles manuales
        if (externalAutoWalk)
        {
            UpdateGroundedState();
            horizontalMovement = externalWalkDir;
            animator.SetFloat("Horizontal", Mathf.Abs(externalWalkDir));
            animator.SetBool("IsGrounded", isGrounded);
            CheckFlipDirection();
            return;
        }

        // Bloqueo total de movimiento durante celebración de jefe / tarjeta de título "NIVEL 2 / NIVEL 3"
        if (isControlLocked)
        {
            UpdateGroundedState();
            horizontalMovement = 0f;
            jumpBufferCounter = 0f;
            animator.SetFloat("Horizontal", 0f);
            animator.SetBool("IsGrounded", isGrounded);
            return;
        }

        // Detectar Left Shift para Dash / Esquive (compatible con Input System nuevo y clásico)
        bool shiftPressed = false;
        if (Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame)
        {
            shiftPressed = true;
        }
        else
        {
            try
            {
                if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
                    shiftPressed = true;
            }
            catch
            {
                // Ignorar si el Input antiguo está desactivado
            }
        }

        if (shiftPressed && canDash && !isDashing)
        {
            activeDashRoutine = StartCoroutine(DashRoutine());
        }

        UpdateGroundedState();

        // Coyote Time
        if (isGrounded)
            coyoteTimeCounter = coyoteTime;
        else
            coyoteTimeCounter -= Time.deltaTime;

        // Jump buffer
        if (jumpBufferCounter > 0f && !isDashing)
        {
            jumpBufferCounter -= Time.deltaTime;
            if (coyoteTimeCounter > 0f)
            {
                ExecuteJump();
                jumpBufferCounter = 0f;
            }
        }

        // Landing feedback (Squash)
        if (!wasGrounded && isGrounded && enableSquashAndStretch)
        {
            float dir = Mathf.Sign(transform.localScale.x);
            transform.localScale = new Vector3(dir * Mathf.Abs(originalScale.x) * 1.25f, originalScale.y * 0.75f, originalScale.z);
        }
        wasGrounded = isGrounded;

        // Smooth recovery to original scale
        if (enableSquashAndStretch)
        {
            float targetDir = Mathf.Sign(transform.localScale.x);
            Vector3 targetScale = new Vector3(targetDir * Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * squashStretchSpeed);
        }

        animator.SetFloat("Horizontal", isDashing ? 1f : Mathf.Abs(horizontalMovement));
        animator.SetBool("IsGrounded", isGrounded);

        if (!isDashing)
        {
            CheckFlipDirection();
        }
    }

    void FixedUpdate()
    {
        if (rb == null || isDead)
            return;

        if (externalAutoWalk)
        {
            rb.linearVelocity = new Vector2(externalWalkDir * (moveSpeed * 0.85f), rb.linearVelocity.y);
            return;
        }

        if (isControlLocked)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        if (isDashing)
        {
            rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);
        }
        else
        {
            rb.linearVelocity = new Vector2(
                horizontalMovement * moveSpeed,
                rb.linearVelocity.y
            );
        }

        // Tope izquierdo automático para no poder salirse hacia atrás del mapa
        if (blockLeftMapEdge && transform.position.x < leftBoundaryX)
        {
            transform.position = new Vector3(leftBoundaryX, transform.position.y, transform.position.z);
            if (rb.linearVelocity.x < 0f)
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        // Restringir dentro de la arena del boss (estilo Sonic)
        if (isArenaLocked)
        {
            float clampedX = Mathf.Clamp(transform.position.x, arenaMinX, arenaMaxX);
            if (transform.position.x != clampedX)
            {
                transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
            }
        }
    }

    /// <summary>
    /// Acción de Dash / Esquive con invulnerabilidad y atravesado de enemigos/proyectiles.
    /// </summary>
    public void Dash(InputAction.CallbackContext context)
    {
        if (isControlLocked || externalAutoWalk)
            return;

        if (context.performed && canDash && !isDashing && !isDead)
        {
            activeDashRoutine = StartCoroutine(DashRoutine());
        }
    }

    private IEnumerator DashRoutine()
    {
        canDash = false;
        isDashing = true;
        isInvulnerable = true;
        ProceduralSoundManager.Instance?.PlayDash();

        // Determinar dirección del impulso según hacia dónde se mueve o mira el jugador
        if (Mathf.Abs(horizontalMovement) > 0.05f)
            dashDirection = Mathf.Sign(horizontalMovement);
        else
            dashDirection = transform.localScale.x >= 0f ? 1f : -1f;

        // Asegurar que el sprite mire en la dirección del dash
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * dashDirection;
        transform.localScale = s;

        // Ignorar colisiones físicas con enemigos, jefe y proyectiles durante el dash
        List<Collider2D> ignoredColliders = new List<Collider2D>();
        if (playerCollider != null)
        {
            SimpleEnemy[] enemies = Object.FindObjectsByType<SimpleEnemy>();
            foreach (SimpleEnemy enemy in enemies)
            {
                if (enemy == null) continue;
                Collider2D[] cols = enemy.GetComponentsInChildren<Collider2D>();
                foreach (Collider2D col in cols)
                {
                    if (col != null && col.enabled)
                    {
                        Physics2D.IgnoreCollision(playerCollider, col, true);
                        ignoredColliders.Add(col);
                    }
                }
            }

            GarbanzoProjectile[] projectiles = Object.FindObjectsByType<GarbanzoProjectile>();
            foreach (GarbanzoProjectile proj in projectiles)
            {
                if (proj == null) continue;
                Collider2D col = proj.GetComponent<Collider2D>();
                if (col != null && col.enabled)
                {
                    Physics2D.IgnoreCollision(playerCollider, col, true);
                    ignoredColliders.Add(col);
                }
            }

            FallingPropagandaProjectile[] propProjectiles = Object.FindObjectsByType<FallingPropagandaProjectile>();
            foreach (FallingPropagandaProjectile proj in propProjectiles)
            {
                if (proj == null) continue;
                Collider2D col = proj.GetComponent<Collider2D>();
                if (col != null && col.enabled)
                {
                    Physics2D.IgnoreCollision(playerCollider, col, true);
                    ignoredColliders.Add(col);
                }
            }
        }

        if (rb != null)
        {
            defaultGravityScale = rb.gravityScale > 0f ? rb.gravityScale : 1f;
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);
        }

        // Efecto visual de esquive (parpadeo rápido translúcido)
        float elapsed = 0f;
        bool toggleAlpha = false;
        while (elapsed < dashDuration)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = toggleAlpha
                    ? new Color(0.65f, 0.95f, 1f, 0.35f)
                    : new Color(1f, 1f, 1f, 0.80f);
                toggleAlpha = !toggleAlpha;
            }

            float step = 0.04f;
            yield return new WaitForSeconds(step);
            elapsed += step;
        }

        // Restaurar estado al terminar el dash
        if (rb != null)
        {
            rb.gravityScale = defaultGravityScale;
            rb.linearVelocity = new Vector2(horizontalMovement * moveSpeed, 0f);
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalSpriteColor;
        }

        if (playerCollider != null)
        {
            foreach (Collider2D col in ignoredColliders)
            {
                if (col != null)
                {
                    Physics2D.IgnoreCollision(playerCollider, col, false);
                }
            }
        }

        isDashing = false;
        if (!isControlLocked && !externalAutoWalk)
            isInvulnerable = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
        activeDashRoutine = null;
    }

    /// <summary>
    /// Bloquea al jugador dentro de los límites de la arena del boss.
    /// </summary>
    public void LockInArena(float minX, float maxX)
    {
        isArenaLocked = true;
        arenaMinX = minX;
        arenaMaxX = maxX;
    }

    /// <summary>
    /// Desbloquea al jugador de la arena del boss.
    /// </summary>
    public void UnlockFromArena()
    {
        isArenaLocked = false;
    }

    public void Move(InputAction.CallbackContext context)
    {
        if (isDead || isControlLocked || externalAutoWalk)
        {
            horizontalMovement = 0f;
            return;
        }

        horizontalMovement = context.ReadValue<Vector2>().x;
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (isDead || isControlLocked || externalAutoWalk)
            return;

        if (context.performed)
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else if (context.canceled && rb != null && rb.linearVelocity.y > 0f)
        {
            // Salto variable: soltar el botón corta el salto
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }
    }

    private void ExecuteJump()
    {
        if (rb == null)
            return;

        coyoteTimeCounter = 0f;
        animator.SetTrigger("Jump");
        ProceduralSoundManager.Instance?.PlayJump();

        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            jumpForce
        );

        // Estiramiento al saltar
        if (enableSquashAndStretch)
        {
            float dir = Mathf.Sign(transform.localScale.x);
            transform.localScale = new Vector3(dir * Mathf.Abs(originalScale.x) * 0.8f, originalScale.y * 1.3f, originalScale.z);
        }
    }

    public void Attack(InputAction.CallbackContext context)
    {
        if (isControlLocked || externalAutoWalk)
            return;

        if (context.performed && !isDead && animator != null)
        {
            animator.SetTrigger("Attack");
            ProceduralSoundManager.Instance?.PlaySwordSwing();

            // Detección de impacto
            ExecuteAttackDamage();
        }
    }

    private void ExecuteAttackDamage()
    {
        float facingDir = Mathf.Sign(transform.localScale.x);
        Vector2 attackCenter = (Vector2)transform.position + new Vector2(attackOffset.x * facingDir, attackOffset.y);

        LayerMask mask = attackableLayers.value == 0 ? Physics2D.AllLayers : attackableLayers;
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackCenter, attackRange, mask);

        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == gameObject)
                continue;

            SimpleEnemy enemy = hit.GetComponent<SimpleEnemy>();
            if (enemy != null)
            {
                Vector2 knockback = new Vector2(facingDir, 0.4f).normalized;
                enemy.TakeDamage(attackDamage, knockback);
            }

            ExplosivePotHazard pot = hit.GetComponent<ExplosivePotHazard>();
            if (pot != null)
            {
                pot.TriggerImmediateFuse();
            }
        }
    }

    public void TakeDamage(int damage, Vector2 knockbackDir)
    {
        if (isInvulnerable || isDashing || isDead)
            return;

        currentHealth -= damage;
        ProceduralSoundManager.Instance?.PlayPlayerHit();
        Debug.Log($"¡Finn recibió daño! Vida restante: {currentHealth}/{maxHealth}");

        if (animator != null)
            animator.SetTrigger("TakeDamage");

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(knockbackDir * damageKnockbackForce, ForceMode2D.Impulse);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvulnerabilityRoutine());
        }
    }

    // Sobrecarga sin parámetros para triggers de Unity
    public void TakeDamage()
    {
        TakeDamage(1, Vector2.up);
    }

    private IEnumerator InvulnerabilityRoutine()
    {
        isInvulnerable = true;

        if (spriteRenderer != null)
        {
            for (float t = 0; t < invulnerabilityDuration; t += 0.15f)
            {
                spriteRenderer.color = new Color(1f, 0.3f, 0.3f, 0.5f);
                yield return new WaitForSeconds(0.075f);
                spriteRenderer.color = originalSpriteColor;
                yield return new WaitForSeconds(0.075f);
            }
            spriteRenderer.color = originalSpriteColor;
        }
        else
        {
            yield return new WaitForSeconds(invulnerabilityDuration);
        }

        isInvulnerable = false;
    }

    private void Die()
    {
        isDead = true;
        isDashing = false;
        horizontalMovement = 0f;
        Debug.Log("¡Finn fue derrotado! Reviviendo en 1.5 segundos...");

        // Esconder barra de vida del jefe activo y resetear combate
        ViralVendorBoss vendorBoss = Object.FindAnyObjectByType<ViralVendorBoss>();
        if (vendorBoss != null)
            vendorBoss.ResetBoss();

        CharismaticMayorBoss mayorBoss = Object.FindAnyObjectByType<CharismaticMayorBoss>();
        if (mayorBoss != null)
            mayorBoss.ResetBoss();

        AlleyInfluencerBoss influencerBoss = Object.FindAnyObjectByType<AlleyInfluencerBoss>();
        if (influencerBoss != null)
            influencerBoss.ResetBoss();

        MinotaurBoss boss = Object.FindAnyObjectByType<MinotaurBoss>();
        if (boss != null)
            boss.ResetBoss();

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(1.5f);

        // Restaurar posición y vida
        transform.position = spawnPoint;
        if (rb != null)
        {
            rb.gravityScale = defaultGravityScale;
            rb.linearVelocity = Vector2.zero;
        }

        currentHealth = maxHealth;
        isDead = false;
        isInvulnerable = false;
        isDashing = false;
        canDash = true;

        if (spriteRenderer != null)
            spriteRenderer.color = originalSpriteColor;

        if (animator != null)
            animator.SetTrigger("Revive");

        // Resetear enemigos del nivel al reaparecer
        SimpleEnemy[] enemies = Object.FindObjectsByType<SimpleEnemy>();
        foreach (var enemy in enemies)
        {
            if (enemy != null &&
                !(enemy is MinotaurBoss) &&
                !(enemy is ViralVendorBoss) &&
                !(enemy is CharismaticMayorBoss) &&
                !(enemy is AlleyInfluencerBoss))
            {
                enemy.ResetEnemy();
            }
        }

        Debug.Log("¡Finn ha revivido!");
    }

    public void Revive()
    {
        if (animator != null)
            animator.SetTrigger("Revive");
    }

    void UpdateGroundedState()
    {
        if (groundCheck == null)
        {
            isGrounded = false;
            return;
        }

        isGrounded = Physics2D.OverlapBox(
            groundCheck.position,
            groundCheckSize,
            0f,
            groundLayer
        );
    }

    private void OnDrawGizmosSelected()
    {
        // Gizmo del suelo
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }

        // Gizmo del ataque de espada
        float facingDir = Mathf.Sign(transform.localScale.x);
        Vector2 attackCenter = (Vector2)transform.position + new Vector2(attackOffset.x * facingDir, attackOffset.y);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(attackCenter, attackRange);
    }

    void CheckFlipDirection()
    {
        if (Mathf.Abs(horizontalMovement) < 0.01f)
            return;

        bool movingRight = horizontalMovement > 0;
        bool facingRight = transform.localScale.x > 0;

        if (movingRight != facingRight)
        {
            Vector3 newScale = transform.localScale;
            newScale.x *= -1;
            transform.localScale = newScale;
        }
    }

    private void OnGUI()
    {
        // HUD temático: Jardín Principal de Dolores Hidalgo
        GUIStyle locationStyle = new GUIStyle(GUI.skin.label);
        locationStyle.fontSize = 13;
        locationStyle.fontStyle = FontStyle.Bold;
        locationStyle.normal.textColor = new Color(0.95f, 0.8f, 0.25f);

        string sceneName = SceneManager.GetActiveScene().name;
        string locationTitle = "📍 DOLORES HIDALGO — JARDÍN PRINCIPAL (NIVEL 1)";
        if (sceneName.Contains("2"))
            locationTitle = "📍 DOLORES HIDALGO — TALLERES DE TALAVERA (NIVEL 2)";
        else if (sceneName.Contains("3"))
            locationTitle = "📍 GUANAJUATO — CALLEJONES DEL CAOS (NIVEL 3)";

        GUI.Label(new Rect(20, 15, 620, 20), locationTitle, locationStyle);

        GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 15;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = Color.white;

        string hearts = "";
        for (int i = 0; i < maxHealth; i++)
        {
            hearts += (i < currentHealth) ? "❤️" : "🖤";
        }

        string dashStatus = canDash ? "⚡ Dash: LISTO" : "⏳ Dash...";
        GUI.Label(new Rect(20, 36, 620, 26), $"{hearts}  Vida: {currentHealth}/{maxHealth}   |   🪙 Monedas: {CollectibleItem.TotalCoinsCollected}   |   {dashStatus}", headerStyle);

        GUIStyle hintStyle = new GUIStyle(GUI.skin.label);
        hintStyle.fontSize = 11;
        hintStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f, 0.9f);
        GUI.Label(new Rect(20, 62, 650, 20), "A / D: Mover | Espacio: Saltar / Pisotón | Left Shift: Dash (Esquive) | Clic / Enter: Espadazo", hintStyle);
    }
}
