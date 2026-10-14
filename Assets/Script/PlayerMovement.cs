using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public Vector2 groundCheckSize = new Vector2(0.25f, 0.05f);
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
    public bool enableSquashAndStretch = true;
    public float squashStretchSpeed = 10f;

    private float horizontalMovement;
    private bool isGrounded;
    private bool wasGrounded;
    private bool isInvulnerable = false;
    private bool isDead = false;

    // Coyote time & jump buffering
    private float coyoteTime = 0.15f;
    private float coyoteTimeCounter;
    private float jumpBufferTime = 0.15f;
    private float jumpBufferCounter;

    private Vector3 originalScale;
    private Vector3 spawnPoint;
    private Color originalSpriteColor = Color.white;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (rb == null)
            Debug.LogError("No se encontró Rigidbody2D en " + gameObject.name);

        if (animator == null)
            Debug.LogError("No se encontró Animator en " + gameObject.name);

        if (spriteRenderer != null)
            originalSpriteColor = spriteRenderer.color;

        currentHealth = maxHealth;
        spawnPoint = transform.position;

        originalScale = transform.localScale;
        if (Mathf.Approximately(originalScale.x, 0f))
            originalScale = Vector3.one;

        // Por defecto detectar cualquier capa si no se asignó capa específica
        if (attackableLayers.value == 0)
            attackableLayers = ~0;
    }

    void Update()
    {
        if (rb == null || animator == null || isDead)
            return;

        UpdateGroundedState();

        // Coyote Time
        if (isGrounded)
            coyoteTimeCounter = coyoteTime;
        else
            coyoteTimeCounter -= Time.deltaTime;

        // Jump buffer
        if (jumpBufferCounter > 0f)
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

        animator.SetFloat("Horizontal", Mathf.Abs(horizontalMovement));
        animator.SetBool("IsGrounded", isGrounded);

        CheckFlipDirection();
    }

    void FixedUpdate()
    {
        if (rb == null || isDead)
            return;

        rb.linearVelocity = new Vector2(
            horizontalMovement * moveSpeed,
            rb.linearVelocity.y
        );
    }

    public void Move(InputAction.CallbackContext context)
    {
        if (isDead)
        {
            horizontalMovement = 0f;
            return;
        }

        horizontalMovement = context.ReadValue<Vector2>().x;
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (isDead)
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
        if (context.performed && !isDead && animator != null)
        {
            animator.SetTrigger("Attack");

            // Detección de impacto
            ExecuteAttackDamage();
        }
    }

    private void ExecuteAttackDamage()
    {
        float facingDir = Mathf.Sign(transform.localScale.x);
        Vector2 attackCenter = (Vector2)transform.position + new Vector2(attackOffset.x * facingDir, attackOffset.y);

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackCenter, attackRange, attackableLayers);

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
        }
    }

    public void TakeDamage(int damage, Vector2 knockbackDir)
    {
        if (isInvulnerable || isDead)
            return;

        currentHealth -= damage;
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
        horizontalMovement = 0f;
        Debug.Log("¡Finn fue derrotado! Reviviendo en 1.5 segundos...");

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(1.5f);

        // Restaurar posición y vida
        transform.position = spawnPoint;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        currentHealth = maxHealth;
        isDead = false;
        isInvulnerable = false;

        if (spriteRenderer != null)
            spriteRenderer.color = originalSpriteColor;

        if (animator != null)
            animator.SetTrigger("Revive");

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
        // HUD sencillo de Vida y Controles en la esquina superior izquierda
        GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 16;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = Color.white;

        string hearts = "";
        for (int i = 0; i < maxHealth; i++)
        {
            hearts += (i < currentHealth) ? "❤️" : "🖤";
        }

        GUI.Label(new Rect(20, 20, 300, 30), $"{hearts}  Vida: {currentHealth}/{maxHealth}", headerStyle);

        GUIStyle hintStyle = new GUIStyle(GUI.skin.label);
        hintStyle.fontSize = 12;
        hintStyle.normal.textColor = Color.yellow;
        GUI.Label(new Rect(20, 50, 450, 25), "A / D o Flechas: Mover | Espacio: Saltar | Clic o Enter: Atacar", hintStyle);
    }
}