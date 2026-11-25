using UnityEngine;

/// <summary>
/// Proyectil temático versátil para enemigos y jefes de los Niveles 2 y 3:
/// Fragmentos de Talavera, Panfletos/Sobres del Munícipe, Ondas de Megáfono,
/// Disparos de Drones, Emojis/Likes Virales y Rayos Flash.
/// Respeta la invulnerabilidad de Dash (Left Shift) de Finn.
/// </summary>
public class FallingPropagandaProjectile : MonoBehaviour
{
    public enum ProjectileStyle
    {
        TalaveraShard,
        MayorPamphlet,
        GoldenEnvelope,
        MegaphoneWave,
        DroneShot,
        ViralEmojiLike,
        FlashBeam
    }

    [Header("Comportamiento del Proyectil")]
    public ProjectileStyle style = ProjectileStyle.MayorPamphlet;
    public int damage = 1;
    public float lifetime = 5.5f;
    public float gravityScale = 0.45f;
    public float spinSpeed = -260f;
    public float homingStrength = 0f;
    public float maxSpeed = 12f;
    public bool destroyOnGround = true;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Transform homingTarget;
    private bool hasHit = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.gravityScale = gravityScale;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null)
            col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.28f;

        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 6;
    }

    private void Start()
    {
        ApplyStyleVisuals();
        Destroy(gameObject, lifetime);
    }

    public void ConfigureAndLaunch(
        ProjectileStyle projectileStyle,
        Vector2 initialVelocity,
        int projectileDamage = 1,
        float customGravity = 0.45f,
        float customLifetime = 5.5f,
        float customHoming = 0f,
        Transform target = null)
    {
        style = projectileStyle;
        damage = projectileDamage;
        gravityScale = customGravity;
        lifetime = customLifetime;
        homingStrength = customHoming;
        homingTarget = target;
        maxSpeed = Mathf.Max(6f, initialVelocity.magnitude);

        if (rb == null)
            rb = GetComponent<Rigidbody2D>() ?? gameObject.AddComponent<Rigidbody2D>();

        rb.gravityScale = gravityScale;
        rb.linearVelocity = initialVelocity;

        ApplyStyleVisuals();

        // Orientar proyectiles direccionales (ondas de megáfono / flash)
        if (style == ProjectileStyle.MegaphoneWave || style == ProjectileStyle.FlashBeam)
        {
            spinSpeed = 0f;
            float angle = Mathf.Atan2(initialVelocity.y, initialVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private void ApplyStyleVisuals()
    {
        if (sr == null)
            sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            return;

        if (sr.sprite == null)
            sr.sprite = ProceduralPixelArt.GetProjectileSprite(style.ToString());
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || PauseMenu.IsGamePaused)
            return;

        if (Mathf.Abs(spinSpeed) > 0.1f)
            transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);

        if (homingStrength > 0f && homingTarget != null && rb != null)
        {
            Vector2 desired = ((Vector2)homingTarget.position + Vector2.up * 0.35f - (Vector2)transform.position).normalized * maxSpeed;
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, desired, homingStrength * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit || collision == null)
            return;

        // Ignorar enemigos, jefes y otros proyectiles
        if (collision.GetComponent<SimpleEnemy>() != null ||
            collision.GetComponent<FallingPropagandaProjectile>() != null ||
            collision.GetComponent<GarbanzoProjectile>() != null)
        {
            return;
        }

        // Impacto contra Finn
        PlayerMovement player = collision.GetComponent<PlayerMovement>() ?? collision.GetComponentInParent<PlayerMovement>();
        if (player != null)
        {
            if (player.IsDashing || player.IsInvulnerable)
                return;

            hasHit = true;
            Vector2 knock = (player.transform.position - transform.position).normalized;
            if (knock.y < 0.25f) knock.y = 0.45f;
            player.TakeDamage(damage, knock.normalized);
            Destroy(gameObject);
            return;
        }

        // Impacto contra el terreno sólido
        if (destroyOnGround && !collision.isTrigger &&
            (collision.gameObject.layer == LayerMask.NameToLayer("Ground") || collision.CompareTag("Ground")))
        {
            hasHit = true;
            Destroy(gameObject);
        }
    }
}
