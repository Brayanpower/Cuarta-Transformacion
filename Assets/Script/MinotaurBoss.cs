using System.Collections;
using UnityEngine;

/// <summary>
/// Jefe del Nivel 1: Minotauro.
/// Cuenta con fases de combate (Persecución lenta y pesada, Ataque con Hacha telegrafiado),
/// animaciones fluidas (Idle, Walk, Attack), barra de salud cinemática,
/// bloqueo de cámara estilo Sonic y reinicio automático al morir el jugador.
/// </summary>
public class MinotaurBoss : SimpleEnemy
{
    [Header("Boss Config")]
    public string bossName = "MINOTAURO — GUARDIÁN DEL LABERINTO";
    public Transform playerTransform;
    public GameObject arenaGate;
    public float arenaTriggerX = 125f;

    [Header("Boss Stats (Movimiento lento y pesado)")]
    public float bossSpeed = 0.8f;
    public float enrageSpeed = 1.4f;
    public float attackRange = 2.2f;
    public float attackCooldown = 4.5f;
    public int heavyAttackDamage = 2;

    [Header("Velocidad de Animación (más lento = más pesado)")]
    public float animationSpeed = 0.55f;
    public float enrageAnimationSpeed = 0.7f;

    [Header("Boss State")]
    public bool isAwakened = false;
    public bool isEnraged = false;
    private bool isAttacking = false;
    private float nextAttackTime = 0f;

    [Header("Arena Bounds (Estilo Sonic)")]
    public float arenaLeftBound = 127f;
    public float arenaRightBound = 158f;
    public Vector3 arenaCameraPosition = new Vector3(142.5f, -1.8f, -10f);
    public float arenaCameraOrthoSize = 4.2f;

    private Rigidbody2D bossRb;
    private SpriteRenderer bossRenderer;
    private Animator bossAnimator;
    private Color normalColor;
    private bool isDefeated = false;
    private Vector3 initialSpawnPosition;

    protected override void Awake()
    {
        // Sobrescribir stats de SimpleEnemy
        maxHealth = 15;
        currentHealth = maxHealth;
        damageToPlayer = 1;
        canPatrol = false; // El jefe usa su propia IA de combate
    }

    protected override void Start()
    {
        bossRb = GetComponent<Rigidbody2D>();
        bossRenderer = GetComponent<SpriteRenderer>();
        bossAnimator = GetComponent<Animator>();

        if (bossRenderer != null)
            normalColor = bossRenderer.color;

        initialSpawnPosition = transform.position;

        if (playerTransform == null)
        {
            PlayerMovement pm = Object.FindAnyObjectByType<PlayerMovement>();
            if (pm != null)
                playerTransform = pm.transform;
        }

        if (arenaGate != null)
            arenaGate.SetActive(false);

        // Aplicar velocidad de animación lenta para que se sienta pesado
        if (bossAnimator != null)
            bossAnimator.speed = animationSpeed;
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

        // Detección de entrada a la arena
        if (!isAwakened)
        {
            if (playerTransform.position.x >= arenaTriggerX)
            {
                AwakenBoss();
            }
            return;
        }

        // Verificar fase de furia (menos del 50% de HP)
        if (!isEnraged && currentHealth <= (maxHealth / 2))
        {
            isEnraged = true;
            Debug.Log("¡El Minotauro se ha enfurecido!");

            // Acelerar un poco las animaciones en modo furia
            if (bossAnimator != null)
                bossAnimator.speed = enrageAnimationSpeed;
        }

        if (isAttacking)
            return;

        // Comportamiento de persecución y ataque
        float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        float dirX = Mathf.Sign(playerTransform.position.x - transform.position.x);

        // Orientar sprite (el minotauro en el spritesheet mira a la izquierda por defecto)
        if (bossRenderer != null)
        {
            bossRenderer.flipX = dirX > 0;
        }

        if (distToPlayer <= attackRange && Time.time >= nextAttackTime)
        {
            StartCoroutine(PerformAttack(dirX));
        }
        else
        {
            float currentMoveSpeed = isEnraged ? enrageSpeed : bossSpeed;
            if (bossRb != null)
            {
                bossRb.linearVelocity = new Vector2(dirX * currentMoveSpeed, bossRb.linearVelocity.y);
            }

            if (bossAnimator != null)
            {
                bossAnimator.SetBool("IsMoving", true);
            }
        }

        // Restringir dentro de los límites de la arena
        float clampedX = Mathf.Clamp(transform.position.x, arenaLeftBound, arenaRightBound);
        if (transform.position.x != clampedX)
        {
            transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
        }
    }

    private void AwakenBoss()
    {
        isAwakened = true;
        Debug.Log("¡ALERTA DE JEFE! El Minotauro ha despertado.");
        
        // Cerrar compuerta de arena
        if (arenaGate != null)
            arenaGate.SetActive(true);

        // Bloquear cámara estilo Sonic en la arena
        CameraFollow cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null)
        {
            cam.LockBossArena(arenaCameraPosition, arenaCameraOrthoSize);
        }

        // Bloquear al jugador dentro de la arena (estilo Sonic)
        if (playerTransform != null)
        {
            PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
            if (pm != null)
                pm.LockInArena(arenaLeftBound, arenaRightBound);
        }
    }

    private IEnumerator PerformAttack(float dirX)
    {
        isAttacking = true;
        nextAttackTime = Time.time + attackCooldown;

        // Frenar para preparar el golpe
        if (bossRb != null)
            bossRb.linearVelocity = new Vector2(0f, bossRb.linearVelocity.y);

        if (bossAnimator != null)
        {
            bossAnimator.SetBool("IsMoving", false);
            bossAnimator.SetTrigger("Attack");
        }

        // Anticipación y telégrafo visual (alzar hacha lentamente)
        if (bossRenderer != null)
            bossRenderer.color = isEnraged ? Color.red : new Color(1f, 0.85f, 0.4f);

        yield return new WaitForSeconds(1.2f); // 1.2s de viento previo lento para que el jugador reaccione

        // Embestida e impacto de hacha
        if (bossRb != null)
            bossRb.AddForce(new Vector2(dirX * (isEnraged ? 3f : 1.8f), 0.3f), ForceMode2D.Impulse);

        yield return new WaitForSeconds(0.15f);

        // Detectar si impactó al jugador durante el golpe
        if (playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist <= attackRange + 0.6f)
            {
                PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
                if (pm != null)
                {
                    Vector2 knockback = new Vector2(dirX * 1.5f, 0.8f).normalized;
                    int dmg = isEnraged ? heavyAttackDamage : damageToPlayer;
                    pm.TakeDamage(dmg, knockback);
                }
            }
        }

        yield return new WaitForSeconds(1.0f); // Recuperación lenta tras el golpe — ventana de ataque para el jugador

        if (bossRenderer != null)
            bossRenderer.color = normalColor;

        isAttacking = false;
    }

    public override void TakeDamage(int damage, Vector2 knockbackDir)
    {
        if (isDefeated)
            return;

        currentHealth -= damage;
        Debug.Log($"¡Minotauro herido! HP: {currentHealth}/{maxHealth}");

        // El Minotauro es pesado: resistencia al empuje
        if (bossRb != null)
        {
            bossRb.AddForce(knockbackDir * 1.5f, ForceMode2D.Impulse);
        }

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
            yield return new WaitForSeconds(0.12f);
            bossRenderer.color = Color.red;
            yield return new WaitForSeconds(0.12f);
            bossRenderer.color = normalColor;
        }
    }

    private void DefeatBoss()
    {
        isDefeated = true;
        Debug.Log("¡¡¡MINOTAURO DERROTADO!!! ¡NIVEL 1 COMPLETADO!");

        if (bossRb != null)
            bossRb.linearVelocity = Vector2.zero;

        if (bossAnimator != null)
            bossAnimator.SetBool("IsMoving", false);

        if (arenaGate != null)
            arenaGate.SetActive(false); // Abrir salida

        // Liberar cámara al derrotar al jefe
        CameraFollow cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null)
            cam.UnlockBossArena();

        // Desbloquear al jugador de la arena
        if (playerTransform != null)
        {
            PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
            if (pm != null)
                pm.UnlockFromArena();
        }

        StartCoroutine(DefeatAnimationRoutine());
    }

    /// <summary>
    /// Resetea el jefe y esconde la barra de vida al morir el jugador.
    /// </summary>
    public void ResetBoss()
    {
        StopAllCoroutines();
        isAwakened = false; // Esto oculta de inmediato la barra de vida
        isEnraged = false;
        isAttacking = false;
        isDefeated = false;
        currentHealth = maxHealth;

        transform.position = initialSpawnPosition;

        if (bossRb != null)
            bossRb.linearVelocity = Vector2.zero;

        if (bossRenderer != null)
        {
            bossRenderer.color = normalColor;
            bossRenderer.enabled = true;
        }

        if (bossAnimator != null)
        {
            bossAnimator.SetBool("IsMoving", false);
            bossAnimator.speed = animationSpeed; // Restaurar velocidad lenta
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = true;

        if (arenaGate != null)
            arenaGate.SetActive(false);

        // Liberar cámara de vuelta a Finn
        CameraFollow cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null)
            cam.UnlockBossArena();

        // Desbloquear al jugador de la arena
        if (playerTransform != null)
        {
            PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
            if (pm != null)
                pm.UnlockFromArena();
        }

        Debug.Log("[MinotaurBoss] Jefe reiniciado y barra de vida oculta tras derrota del jugador.");
    }

    private IEnumerator DefeatAnimationRoutine()
    {
        for (int i = 0; i < 6; i++)
        {
            if (bossRenderer != null)
                bossRenderer.enabled = !bossRenderer.enabled;
            yield return new WaitForSeconds(0.15f);
        }

        if (bossRenderer != null)
            bossRenderer.enabled = true;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;
    }

    protected override void OnGUI()
    {
        // Si no está despierto o ya fue derrotado, no mostrar la barra
        if (!isAwakened || isDefeated)
            return;

        // Barra de Vida estilizada para el Jefe en la parte superior de la pantalla
        float screenW = Screen.width;
        float barWidth = Mathf.Min(400f, screenW * 0.7f);
        float barHeight = 24f;
        float startX = (screenW - barWidth) / 2f;
        float startY = 30f;

        // Fondo oscuro
        GUI.Box(new Rect(startX - 10, startY - 25, barWidth + 20, barHeight + 35), "");

        // Título del Jefe
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.fontSize = 13;
        titleStyle.normal.textColor = isEnraged ? new Color(1f, 0.3f, 0.3f) : Color.white;

        string displayTitle = isEnraged ? $"⚠️ {bossName} (¡ENFURECIDO!) ⚠️" : bossName;
        if (isDefeated)
            displayTitle = "¡VICTORIA! ¡MINOTAURO DERROTADO!";

        GUI.Label(new Rect(startX, startY - 22, barWidth, 20), displayTitle, titleStyle);

        // Barra de fondo gris oscuro
        GUI.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        GUI.DrawTexture(new Rect(startX, startY, barWidth, barHeight), Texture2D.whiteTexture);

        // Barra de relleno de vida (Rojo sangre / carmesí)
        float healthPct = Mathf.Clamp01((float)Mathf.Max(0, currentHealth) / maxHealth);
        Color barColor = isEnraged ? new Color(1f, 0.15f, 0.1f) : new Color(0.85f, 0.2f, 0.2f);
        GUI.color = barColor;
        GUI.DrawTexture(new Rect(startX + 2, startY + 2, (barWidth - 4) * healthPct, barHeight - 4), Texture2D.whiteTexture);

        // Texto de puntos de vida
        GUI.color = Color.white;
        GUIStyle hpStyle = new GUIStyle(GUI.skin.label);
        hpStyle.alignment = TextAnchor.MiddleCenter;
        hpStyle.fontStyle = FontStyle.Bold;
        hpStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(startX, startY, barWidth, barHeight), $"{Mathf.Max(0, currentHealth)} / {maxHealth} HP", hpStyle);
    }
}
