using System.Collections;
using System.IO;
using UnityEngine;

public class SimpleEnemy : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 3;
    public int currentHealth;

    [Header("Damage to Player")]
    public int damageToPlayer = 1;
    public float knockbackForce = 6.5f;
    [Tooltip("Tiempo (en segundos) durante el cual este enemigo NO puede volver a dañar al jugador tras ser golpeado")]
    public float hitDamageCooldown = 1.25f;
    [Tooltip("Tiempo de aturdimiento/empuje tras recibir un golpe para que el retroceso sea limpio")]
    public float hitStunDuration = 0.42f;

    [Header("AI Chase & Detection")]
    public Transform player;
    public bool canPatrol = true;
    public float patrolSpeed = 2.6f;
    public float detectionRange = 11.5f;
    public float patrolDistance = 3f; // Mantenido por compatibilidad de serialización

    [Header("Animación de Esbirro (Garbanzo)")]
    public Sprite[] walkSprites;
    public Sprite[] defeatSprites;
    public float animationFps = 8f;

    [Header("Respawn")]
    public bool canRespawn = false;
    public float respawnDelay = 12f;

    private Vector3 initialSpawnPos;
    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected Color originalColor;
    protected bool isDead = false;
    protected float contactDamageDisabledUntil = 0f;
    protected float hitStunUntil = 0f;
    private Texture2D hpBgTexture;
    private Texture2D hpFillTexture;
    private float animTimer = 0f;
    private int currentFrame = 0;

    // Cache compartido para evitar recargar la textura en cada esbirro
    private static Sprite[] sharedEsbirroWalk;
    private static Sprite[] sharedEsbirroDefeat;
    private static Sprite sharedEsbirroIdle;

    protected virtual void Awake()
    {
        EnsureSpritesLoaded();
    }

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Asegurar física con gravedad activa para que siempre caigan al suelo o plataforma y nunca floten
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Dynamic;
        if (rb.gravityScale < 1.5f)
            rb.gravityScale = 2.5f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        PhysicsMaterial2D enemySlipMat = new PhysicsMaterial2D("EnemySlipMaterial")
        {
            friction = 0f,
            bounciness = 0f
        };
        rb.sharedMaterial = enemySlipMat;

        BoxCollider2D boxCol = GetComponent<BoxCollider2D>();
        if (boxCol != null)
        {
            boxCol.sharedMaterial = enemySlipMat;
            boxCol.edgeRadius = 0.04f;
        }

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        EnsureSpritesLoaded();

        initialSpawnPos = transform.position;

        // Buscar referencia al jugador automáticamente
        FindPlayerReference();

        // Texturas para barra de vida
        hpBgTexture = new Texture2D(1, 1);
        hpBgTexture.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.1f, 0.8f));
        hpBgTexture.Apply();

        hpFillTexture = new Texture2D(1, 1);
        hpFillTexture.SetPixel(0, 0, new Color(0.85f, 0.2f, 0.2f, 0.9f));
        hpFillTexture.Apply();
    }

    protected void FindPlayerReference()
    {
        if (player != null)
            return;

        PlayerMovement pm = Object.FindAnyObjectByType<PlayerMovement>();
        if (pm != null)
        {
            player = pm.transform;
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    // Solo el esbirro base (Nivel 1) usa sprites de Garbanzo; subclases de Nivel 2 y 3 usan sus propios sprites
    protected virtual bool UseGarbanzoSprites => true;

    /// <summary>
    /// Carga robusta de sprites desde el PNG en disco si faltan referencias en el Inspector.
    /// Garantiza que los esbirros Garbanzos NUNCA sean invisibles (sin pisar enemigos de Nivel 2 y 3).
    /// </summary>
    public void EnsureSpritesLoaded()
    {
        if (!UseGarbanzoSprites)
            return;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        bool needsWalk = walkSprites == null || walkSprites.Length == 0 || walkSprites[0] == null;
        bool needsDefeat = defeatSprites == null || defeatSprites.Length == 0 || defeatSprites[0] == null;
        bool needsCurrentSprite = spriteRenderer != null && spriteRenderer.sprite == null;

        if (!needsWalk && !needsDefeat && !needsCurrentSprite)
            return;

        if (sharedEsbirroWalk == null || sharedEsbirroWalk.Length == 0 || sharedEsbirroWalk[0] == null)
        {
            string path = Path.Combine(Application.dataPath, "Images/Nivel1/enemigos/esbirro.png");
            if (!File.Exists(path))
                path = "Assets/Images/Nivel1/enemigos/esbirro.png";

            if (File.Exists(path))
            {
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2048, 2048, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.LoadImage(bytes);

                sharedEsbirroIdle = Sprite.Create(tex, new Rect(88, 1654, 142, 159), new Vector2(0.5f, 0f), 160f);

                sharedEsbirroWalk = new Sprite[]
                {
                    Sprite.Create(tex, new Rect(76, 1156, 165, 165), new Vector2(0.5f, 0f), 160f),
                    Sprite.Create(tex, new Rect(329, 1157, 142, 143), new Vector2(0.5f, 0f), 160f),
                    Sprite.Create(tex, new Rect(583, 1157, 144, 143), new Vector2(0.5f, 0f), 160f),
                    Sprite.Create(tex, new Rect(829, 1153, 165, 175), new Vector2(0.5f, 0f), 160f),
                    Sprite.Create(tex, new Rect(1082, 1167, 157, 165), new Vector2(0.5f, 0f), 160f),
                    Sprite.Create(tex, new Rect(1338, 1157, 118, 174), new Vector2(0.5f, 0f), 160f),
                    Sprite.Create(tex, new Rect(1587, 1157, 117, 168), new Vector2(0.5f, 0f), 160f),
                    Sprite.Create(tex, new Rect(1822, 1156, 170, 164), new Vector2(0.5f, 0f), 160f)
                };

                sharedEsbirroDefeat = new Sprite[]
                {
                    Sprite.Create(tex, new Rect(70, 121, 145, 171), new Vector2(0.5f, 0f), 160f),
                    Sprite.Create(tex, new Rect(337, 123, 165, 163), new Vector2(0.5f, 0f), 160f),
                    Sprite.Create(tex, new Rect(604, 10, 164, 267), new Vector2(0.5f, 0f), 160f),
                    Sprite.Create(tex, new Rect(768, 10, 215, 199), new Vector2(0.5f, 0f), 160f),
                    Sprite.Create(tex, new Rect(1034, 9, 246, 222), new Vector2(0.5f, 0f), 160f),
                    Sprite.Create(tex, new Rect(1280, 9, 256, 269), new Vector2(0.5f, 0f), 160f),
                    Sprite.Create(tex, new Rect(1536, 9, 256, 258), new Vector2(0.5f, 0f), 160f),
                    Sprite.Create(tex, new Rect(1792, 10, 216, 237), new Vector2(0.5f, 0f), 160f)
                };
            }
        }

        if (needsWalk && sharedEsbirroWalk != null)
            walkSprites = sharedEsbirroWalk;

        if (needsDefeat && sharedEsbirroDefeat != null)
            defeatSprites = sharedEsbirroDefeat;

        if (spriteRenderer != null && (spriteRenderer.sprite == null || needsCurrentSprite))
        {
            if (sharedEsbirroIdle != null)
                spriteRenderer.sprite = sharedEsbirroIdle;
            else if (walkSprites != null && walkSprites.Length > 0 && walkSprites[0] != null)
                spriteRenderer.sprite = walkSprites[0];
        }
    }

    protected virtual void Update()
    {
        if (isDead)
            return;

        // Si cae al vacío por accidente
        if (transform.position.y < -12f)
        {
            ResetEnemy();
            return;
        }

        if (player == null)
        {
            FindPlayerReference();
        }

        bool isChasing = false;

        // IA de persecución dinámica hacia el jugador dentro del rango de detección (respetando hitstun tras ser golpeado)
        if (canPatrol && rb != null && player != null)
        {
            if (Time.time < hitStunUntil)
            {
                // Mantener retroceso físico limpio durante el aturdimiento del golpe
                isChasing = false;
            }
            else
            {
                float distToPlayer = Vector2.Distance(transform.position, player.position);
                float deltaX = player.position.x - transform.position.x;

                if (distToPlayer <= detectionRange && Mathf.Abs(deltaX) > 0.12f)
                {
                    isChasing = true;
                    float moveDir = Mathf.Sign(deltaX);

                    rb.linearVelocity = new Vector2(moveDir * patrolSpeed, rb.linearVelocity.y);

                    // Girar el sprite hacia donde esté el jugador (el sprite original mira a la derecha)
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.flipX = moveDir < 0f;
                    }
                }
                else
                {
                    // Frenar movimiento horizontal manteniendo gravedad vertical activa
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

                    if (player != null && spriteRenderer != null && Mathf.Abs(deltaX) > 0.1f)
                    {
                        spriteRenderer.flipX = deltaX < 0f;
                    }
                }
            }
        }

        // Animación de frames de caminar/rodar del esbirro (respetando sprites propios en Nivel 2 y 3)
        if (walkSprites != null && walkSprites.Length > 0 && spriteRenderer != null)
        {
            if (isChasing)
            {
                animTimer += Time.deltaTime;
                if (animTimer >= (1f / animationFps))
                {
                    animTimer = 0f;
                    currentFrame = (currentFrame + 1) % walkSprites.Length;
                    if (walkSprites[currentFrame] != null)
                        spriteRenderer.sprite = walkSprites[currentFrame];
                }
            }
            else if (UseGarbanzoSprites && sharedEsbirroIdle != null)
            {
                spriteRenderer.sprite = sharedEsbirroIdle;
            }
            else if (walkSprites[0] != null)
            {
                spriteRenderer.sprite = walkSprites[0];
            }
        }
    }

    public virtual void TakeDamage(int damage, Vector2 knockbackDir)
    {
        if (isDead)
            return;

        currentHealth -= damage;
        // Al golpear al enemigo, otorgar ventana segura (cooldown) donde NO puede volver a dañar por contacto y sufre aturdimiento
        contactDamageDisabledUntil = Time.time + hitDamageCooldown;
        hitStunUntil = Time.time + hitStunDuration;

        ProceduralSoundManager.Instance?.PlayEnemyHit();
        Debug.Log($"¡Enemigo golpeado! Daño recibido: {damage}. Vida restante: {currentHealth}/{maxHealth}");

        // Efecto de empuje (knockback) real hacia atrás
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            Vector2 push = new Vector2(knockbackDir.x * knockbackForce, Mathf.Max(2.2f, knockbackDir.y * 3.5f));
            rb.AddForce(push, ForceMode2D.Impulse);
        }

        // Parpadeo visual
        StartCoroutine(DamageFlashRoutine());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual IEnumerator DamageFlashRoutine()
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

    protected virtual void Die()
    {
        isDead = true;
        ProceduralSoundManager.Instance?.PlayEnemyDefeat();
        Debug.Log("¡Enemigo derrotado!");

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        StartCoroutine(DefeatSquishRoutine());
    }

    private IEnumerator DefeatSquishRoutine()
    {
        if (defeatSprites != null && defeatSprites.Length > 0 && spriteRenderer != null)
        {
            float frameTime = 0.35f / defeatSprites.Length;
            for (int i = 0; i < defeatSprites.Length; i++)
            {
                spriteRenderer.sprite = defeatSprites[i];
                yield return new WaitForSeconds(frameTime);
            }
        }

        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        if (canRespawn)
        {
            StartCoroutine(RespawnRoutine());
        }
    }

    public virtual void ResetEnemy()
    {
        StopAllCoroutines();
        transform.position = initialSpawnPos;
        currentHealth = maxHealth;
        isDead = false;
        contactDamageDisabledUntil = 0f;
        hitStunUntil = 0f;

        currentFrame = 0;
        animTimer = 0f;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = originalColor;
            if (walkSprites != null && walkSprites.Length > 0)
                spriteRenderer.sprite = walkSprites[0];
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = true;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);
        ResetEnemy();
        Debug.Log("¡Enemigo reapareció listo para más pruebas!");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerMovement playerMov = collision.gameObject.GetComponent<PlayerMovement>();
        if (playerMov != null && !isDead)
        {
            // Si el jugador está haciendo Dash/Esquive, no causarle daño
            if (playerMov.IsDashing || playerMov.IsInvulnerable)
                return;

            // Detección de rebote / pisotón estilo Mario / Sonic desde arriba
            Rigidbody2D playerRb = playerMov.GetComponent<Rigidbody2D>();
            if (playerRb != null && playerRb.linearVelocity.y < 0.1f && collision.transform.position.y > transform.position.y + 0.35f)
            {
                // Rebotar a Finn hacia arriba
                playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 8.5f);
                // Dañar al enemigo por pisotón
                TakeDamage(1, Vector2.down);
                return;
            }
        }

        TryDamagePlayer(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    private void TryDamagePlayer(GameObject target)
    {
        if (isDead || Time.time < contactDamageDisabledUntil)
            return;

        PlayerMovement playerMov = target.GetComponent<PlayerMovement>();
        if (playerMov != null)
        {
            if (playerMov.IsDashing || playerMov.IsInvulnerable)
                return;

            // También aplicar pequeño cooldown tras golpear al jugador para evitar daño doble instantáneo
            contactDamageDisabledUntil = Time.time + 0.85f;

            Vector2 knockback = (target.transform.position - transform.position).normalized;
            // Garantizar empuje vertical mínimo
            if (knockback.y < 0.3f)
                knockback.y = 0.5f;

            playerMov.TakeDamage(damageToPlayer, knockback);
        }
    }

    protected virtual void OnGUI()
    {
        if (isDead || Camera.main == null || hpBgTexture == null || hpFillTexture == null)
            return;

        // Solo mostrar barra si ha recibido daño o está cerca de la cámara
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 0.75f);
        if (screenPos.z > 0 && screenPos.x > 0 && screenPos.x < Screen.width && screenPos.y > 0 && screenPos.y < Screen.height)
        {
            float barW = 44f;
            float barH = 6f;
            float barX = screenPos.x - barW / 2f;
            float barY = Screen.height - screenPos.y;

            // Fondo barra
            GUI.DrawTexture(new Rect(barX - 1, barY - 1, barW + 2, barH + 2), hpBgTexture);

            // Relleno de vida
            float fillPct = Mathf.Clamp01((float)currentHealth / maxHealth);
            GUI.DrawTexture(new Rect(barX, barY, barW * fillPct, barH), hpFillTexture);
        }
    }
}
