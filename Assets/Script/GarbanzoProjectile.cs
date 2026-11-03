using UnityEngine;

/// <summary>
/// Proyectil de mercancía arrojado por El Vendedor Viral (garbanzo caliente).
/// Cruza toda la arena hacia la posición del jugador e inflige daño si no está esquivando con Dash.
/// </summary>
public class GarbanzoProjectile : MonoBehaviour
{
    public int damage = 1;
    public float lifetime = 6.0f;

    private Rigidbody2D rb;
    private bool hasHit = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

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
        Destroy(gameObject, lifetime);
    }

    public void Launch(Vector2 initialVelocity)
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        rb.linearVelocity = initialVelocity;
    }

    void Update()
    {
        // Rotación continua mientras vuela
        transform.Rotate(0f, 0f, -360f * Time.deltaTime);
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
            // Si el jugador está haciendo Dash / Esquive, el garbanzazo pasa de largo sin dañarlo
            if (player.IsDashing || player.IsInvulnerable)
                return;

            hasHit = true;
            Vector2 knock = (player.transform.position - transform.position).normalized;
            if (knock.y < 0.2f) knock.y = 0.4f;
            player.TakeDamage(damage, knock);
            Destroy(gameObject);
            return;
        }

        // Impacto contra el suelo sólido (ignorar otros triggers decorativos)
        if (!collision.isTrigger && (collision.gameObject.layer == LayerMask.NameToLayer("Ground") || collision.CompareTag("Ground")))
        {
            hasHit = true;
            Destroy(gameObject);
        }
    }
}
