using System.Collections;
using UnityEngine;

/// <summary>
/// Jarrón artesanal explosivo para el Nivel 2 (Talleres de Talavera).
/// Se activa por cercanía de Finn o al ser golpeado, parpadea con mecha encendida
/// y detona en un radio de explosión (respetando la invulnerabilidad del Dash).
/// </summary>
public class ExplosivePotHazard : MonoBehaviour
{
    [Header("Configuración de Detonación")]
    public float triggerRadius = 2.4f;
    public float explosionRadius = 2.6f;
    public float fuseTime = 0.85f;
    public int damage = 1;
    public bool canRespawn = true;
    public float respawnDelay = 6.0f;

    private SpriteRenderer sr;
    private Collider2D potCollider;
    private Sprite idleSprite;
    private Sprite primedSprite;
    private Vector3 initialPos;
    private bool isPrimed = false;
    private bool isExploded = false;
    private Transform playerTransform;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = gameObject.AddComponent<SpriteRenderer>();

        potCollider = GetComponent<Collider2D>();
        if (potCollider == null)
        {
            BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(0.85f, 1.0f);
            box.offset = new Vector2(0f, 0.5f);
            potCollider = box;
        }

        idleSprite = ProceduralPixelArt.GetExplosivePotSprite(false);
        primedSprite = ProceduralPixelArt.GetExplosivePotSprite(true);
        if (sr.sprite == null)
            sr.sprite = idleSprite;
        sr.sortingOrder = 3;

        initialPos = transform.position;
    }

    private void Update()
    {
        if (isPrimed || isExploded)
            return;

        if (playerTransform == null)
        {
            PlayerMovement pm = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
            if (pm != null)
                playerTransform = pm.transform;
            return;
        }

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist <= triggerRadius)
        {
            StartCoroutine(FuseAndExplodeRoutine());
        }
    }

    public void TriggerImmediateFuse()
    {
        if (!isPrimed && !isExploded)
            StartCoroutine(FuseAndExplodeRoutine());
    }

    private IEnumerator FuseAndExplodeRoutine()
    {
        isPrimed = true;
        ProceduralSoundManager.Instance?.PlayPlatformCrack();

        float elapsed = 0f;
        bool flash = false;
        while (elapsed < fuseTime)
        {
            flash = !flash;
            if (sr != null)
            {
                sr.sprite = flash ? primedSprite : idleSprite;
                sr.color = flash ? new Color(1f, 0.8f, 0.6f, 1f) : Color.white;
            }

            float shake = 0.06f * (elapsed / fuseTime + 0.3f);
            transform.position = initialPos + new Vector3(Random.Range(-shake, shake), 0f, 0f);
            float scaleBoost = 1f + Mathf.Sin(elapsed * 26f) * 0.08f;
            transform.localScale = new Vector3(scaleBoost, scaleBoost, 1f);

            yield return new WaitForSeconds(0.06f);
            elapsed += 0.06f;
        }

        Detonate();
    }

    private void Detonate()
    {
        isExploded = true;
        transform.position = initialPos;
        transform.localScale = Vector3.one;

        ProceduralSoundManager.Instance?.PlayExplosion();
        SpawnExplosionVisual();

        // Dañar al jugador y a enemigos cercanos dentro del radio de explosión
        Collider2D[] hits = Physics2D.OverlapCircleAll(initialPos + Vector3.up * 0.5f, explosionRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit == null || hit.gameObject == gameObject)
                continue;

            PlayerMovement player = hit.GetComponent<PlayerMovement>() ?? hit.GetComponentInParent<PlayerMovement>();
            if (player != null && !player.IsDashing && !player.IsInvulnerable)
            {
                Vector2 dir = (player.transform.position - transform.position).normalized;
                if (dir.y < 0.4f) dir.y = 0.6f;
                player.TakeDamage(damage, dir.normalized);
            }

            SimpleEnemy enemy = hit.GetComponent<SimpleEnemy>();
            if (enemy != null)
            {
                Vector2 dir = (enemy.transform.position - transform.position).normalized;
                enemy.TakeDamage(2, dir);
            }
        }

        if (sr != null) sr.enabled = false;
        if (potCollider != null) potCollider.enabled = false;

        if (canRespawn)
            StartCoroutine(RespawnPotRoutine());
        else
            Destroy(gameObject, 0.5f);
    }

    private IEnumerator RespawnPotRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);
        isPrimed = false;
        isExploded = false;
        if (sr != null)
        {
            sr.sprite = idleSprite;
            sr.color = Color.white;
            sr.enabled = true;
        }
        if (potCollider != null)
            potCollider.enabled = true;
    }

    private void SpawnExplosionVisual()
    {
        Sprite shard = ProceduralPixelArt.GetProjectileSprite("TalaveraShard");
        for (int i = 0; i < 8; i++)
        {
            float angle = (i / 8f) * Mathf.PI * 2f;
            GameObject fx = new GameObject("Pot_Explosion_FX");
            fx.transform.position = initialPos + Vector3.up * 0.5f;
            fx.transform.localScale = Vector3.one * 0.65f;

            SpriteRenderer fxSr = fx.AddComponent<SpriteRenderer>();
            fxSr.sprite = shard;
            fxSr.color = (i % 2 == 0) ? new Color(1f, 0.55f, 0.15f, 1f) : new Color(1f, 0.92f, 0.35f, 1f);
            fxSr.sortingOrder = 7;

            Rigidbody2D rb = fx.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1.2f;
            rb.linearVelocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) + 0.35f) * Random.Range(4.5f, 7.5f);
            rb.angularVelocity = Random.Range(-500f, 500f);

            Destroy(fx, 0.55f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, triggerRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, explosionRadius);
    }
}
