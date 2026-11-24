using UnityEngine;

/// <summary>
/// Proyectil de mercancía arrojado por El Vendedor Viral (garbanzo caliente u onda de caldo sísmica).
/// Cruza toda la arena hacia la posición del jugador e inflige daño si no está esquivando con Dash.
/// </summary>
public class GarbanzoProjectile : MonoBehaviour
{
    public int damage = 1;
    public float lifetime = 6.0f;
    public bool isGroundWave = false;
    public bool destroyOnGround = true;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private bool hasHit = false;
    private Vector3 baseScale = Vector3.one;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        sr = GetComponent<SpriteRenderer>();

        // Gravedad ligera para que los garbanzazos crucen toda la arena sin caer a mitad de camino
        rb.gravityScale = 0.35f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null)
            col = gameObject.AddComponent<CircleCollider2D>();

        col.isTrigger = true;
        col.radius = 0.25f;
    }

    void Start()
    {
        baseScale = transform.localScale;
        if (sr == null)
            sr = GetComponent<SpriteRenderer>();

        Destroy(gameObject, lifetime);
    }

    public void Launch(Vector2 initialVelocity)
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        rb.linearVelocity = initialVelocity;
    }

    public void ConfigureAsGroundWave(Vector2 velocity, int dmg, float life)
    {
        isGroundWave = true;
        destroyOnGround = false;
        damage = dmg;
        lifetime = life;

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
        if (sr == null)
            sr = GetComponent<SpriteRenderer>();

        rb.gravityScale = 0f;
        rb.linearVelocity = velocity;

        if (sr != null)
            sr.flipX = velocity.x < 0f;

        CircleCollider2D circle = GetComponent<CircleCollider2D>();
        if (circle != null)
            Destroy(circle);

        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null)
            box = gameObject.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(0.95f, 0.62f);
        box.offset = new Vector2(0f, 0.31f);
    }

    void Update()
    {
        if (isGroundWave)
        {
            float pulseY = 1f + 0.08f * Mathf.Sin(Time.time * 18f);
            transform.localScale = new Vector3(baseScale.x, baseScale.y * pulseY, baseScale.z);
        }
        else
        {
            // Rotación continua mientras vuela por el aire
            transform.Rotate(0f, 0f, -360f * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit)
            return;

        // No chocar con el jefe ni con esbirros ni con otros proyectiles
        if (collision.GetComponent<ViralVendorBoss>() != null ||
            collision.GetComponent<MinotaurBoss>() != null ||
            collision.GetComponent<SimpleEnemy>() != null ||
            collision.GetComponent<GarbanzoProjectile>() != null)
        {
            return;
        }

        // Impacto contra el jugador
        PlayerMovement player = collision.GetComponent<PlayerMovement>();
        if (player == null)
            player = collision.GetComponentInParent<PlayerMovement>();

        if (player != null)
        {
            // Si el jugador está haciendo Dash / Esquive, el garbanzazo o la onda pasa sin dañarlo
            if (player.IsDashing || player.IsInvulnerable)
                return;

            hasHit = true;
            Vector2 knock = (player.transform.position - transform.position).normalized;
            if (knock.y < 0.2f) knock.y = 0.4f;
            player.TakeDamage(damage, knock);
            Destroy(gameObject);
            return;
        }

        // Impacto contra el suelo sólido (solo si destroyOnGround está activo)
        if (destroyOnGround && !collision.isTrigger &&
            (collision.gameObject.layer == LayerMask.NameToLayer("Ground") || collision.CompareTag("Ground")))
        {
            hasHit = true;
            Destroy(gameObject);
        }
    }
}
