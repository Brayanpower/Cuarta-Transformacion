using System.Collections;
using UnityEngine;

public class SimpleEnemy : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 3;
    public int currentHealth;

    [Header("Damage to Player")]
    public int damageToPlayer = 1;
    public float knockbackForce = 6f;

    [Header("Patrol Movement")]
    public bool canPatrol = true;
    public float patrolSpeed = 2f;
    public float patrolDistance = 3f;

    private float startX;
    private int patrolDirection = 1;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        startX = transform.position.x;
    }

    void Update()
    {
        if (isDead)
            return;

        if (canPatrol && rb != null)
        {
            float distFromStart = transform.position.x - startX;

            if (distFromStart > patrolDistance && patrolDirection > 0)
                patrolDirection = -1;
            else if (distFromStart < -patrolDistance && patrolDirection < 0)
                patrolDirection = 1;

            rb.linearVelocity = new Vector2(patrolDirection * patrolSpeed, rb.linearVelocity.y);

            // Voltear sprite según dirección
            if (spriteRenderer != null)
                spriteRenderer.flipX = patrolDirection < 0;
        }
    }

    public void TakeDamage(int damage, Vector2 knockbackDir)
    {
        if (isDead)
            return;

        currentHealth -= damage;
        Debug.Log($"¡Enemigo golpeado! Daño recibido: {damage}. Vida restante: {currentHealth}/{maxHealth}");

        // Efecto de empuje (knockback)
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);
        }

        // Parpadeo visual
        StartCoroutine(DamageFlashRoutine());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator DamageFlashRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("¡Enemigo derrotado!");
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        // Desactivar temporalmente
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        // Esperar 3 segundos y revivir
        yield return new WaitForSeconds(3f);

        transform.position = new Vector3(startX, transform.position.y, transform.position.z);
        currentHealth = maxHealth;
        isDead = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = originalColor;
        }

        if (col != null)
            col.enabled = true;

        Debug.Log("¡Enemigo reapareció listo para más pruebas!");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    private void TryDamagePlayer(GameObject target)
    {
        if (isDead)
            return;

        PlayerMovement player = target.GetComponent<PlayerMovement>();
        if (player != null)
        {
            Vector2 knockback = (target.transform.position - transform.position).normalized;
            // Garantizar empuje vertical mínimo
            if (knockback.y < 0.3f)
                knockback.y = 0.5f;

            player.TakeDamage(damageToPlayer, knockback);
        }
    }

    private void OnGUI()
    {
        if (isDead || Camera.main == null)
            return;

        // Mostrar barra de vida encima del enemigo en pantalla
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 0.8f);
        if (screenPos.z > 0)
        {
            Rect rect = new Rect(screenPos.x - 40, Screen.height - screenPos.y - 10, 80, 20);
            GUI.color = Color.yellow;
            GUI.Label(rect, $"HP: {currentHealth}/{maxHealth}");
        }
    }
}
