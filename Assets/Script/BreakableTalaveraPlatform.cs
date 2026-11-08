using System.Collections;
using UnityEngine;

/// <summary>
/// Plataforma de cerámica de Talavera quebradiza para el Nivel 2 (Talleres de Talavera).
/// Al ser pisada por el jugador, vibra mostrando grietas, se rompe tras breakDelay (0.8s)
/// y se reconstruye automáticamente tras respawnDelay (3.0s).
/// </summary>
public class BreakableTalaveraPlatform : MonoBehaviour
{
    [Header("Tiempos de Ruptura y Reconstrucción")]
    public float breakDelay = 0.8f;
    public float respawnDelay = 3.0f;
    public float shakeIntensity = 0.07f;

    [Header("Sprites (auto-generados si están vacíos)")]
    public Sprite intactSprite;
    public Sprite crackedSprite;

    private SpriteRenderer sr;
    private Collider2D platformCollider;
    private Vector3 initialPosition;
    private Color initialColor = Color.white;
    private bool isTriggered = false;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = gameObject.AddComponent<SpriteRenderer>();

        platformCollider = GetComponent<Collider2D>();
        if (platformCollider == null)
            platformCollider = gameObject.AddComponent<BoxCollider2D>();

        EnsureSprites();
        initialPosition = transform.position;
        initialColor = sr.color;

        // Asegurar que esté en la capa Ground si existe para que Finn pueda saltar desde ella
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0)
            gameObject.layer = groundLayer;
    }

    private void EnsureSprites()
    {
        if (intactSprite == null)
            intactSprite = ProceduralPixelArt.GetTalaveraPlatformSprite(false);
        if (crackedSprite == null)
            crackedSprite = ProceduralPixelArt.GetTalaveraPlatformSprite(true);

        if (sr != null && sr.sprite == null)
            sr.sprite = intactSprite;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isTriggered)
            return;

        PlayerMovement player = collision.gameObject.GetComponent<PlayerMovement>();
        if (player == null)
            player = collision.gameObject.GetComponentInParent<PlayerMovement>();

        if (player != null)
        {
            // Activar cuando el jugador pisa por encima (o en el borde superior)
            if (collision.transform.position.y >= transform.position.y - 0.1f)
            {
                StartCoroutine(BreakAndRespawnRoutine());
            }
        }
    }

    private IEnumerator BreakAndRespawnRoutine()
    {
        isTriggered = true;
        ProceduralSoundManager.Instance?.PlayPlatformCrack();

        if (sr != null && crackedSprite != null)
            sr.sprite = crackedSprite;

        // Vibración y advertencia visual previa a romperse
        float elapsed = 0f;
        while (elapsed < breakDelay)
        {
            float ox = Random.Range(-shakeIntensity, shakeIntensity);
            float oy = Random.Range(-shakeIntensity * 0.5f, shakeIntensity * 0.5f);
            transform.position = initialPosition + new Vector3(ox, oy, 0f);

            if (sr != null)
            {
                float pulse = Mathf.PingPong(elapsed * 12f, 1f);
                sr.color = Color.Lerp(initialColor, new Color(1f, 0.72f, 0.55f, 1f), pulse);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ruptura de cerámica
        transform.position = initialPosition;
        SpawnCeramicShards();
        ProceduralSoundManager.Instance?.PlayEnemyHit();

        if (platformCollider != null)
            platformCollider.enabled = false;
        if (sr != null)
            sr.enabled = false;

        // Esperar antes de reconstruir la pieza de Talavera
        yield return new WaitForSeconds(respawnDelay);

        if (sr != null)
        {
            sr.sprite = intactSprite;
            sr.enabled = true;
            sr.color = new Color(initialColor.r, initialColor.g, initialColor.b, 0.35f);
        }

        // Fade-in rápido de reaparición
        float fade = 0f;
        while (fade < 0.25f)
        {
            if (sr != null)
            {
                float a = Mathf.Lerp(0.35f, initialColor.a, fade / 0.25f);
                sr.color = new Color(initialColor.r, initialColor.g, initialColor.b, a);
            }
            fade += Time.deltaTime;
            yield return null;
        }

        if (sr != null)
            sr.color = initialColor;
        if (platformCollider != null)
            platformCollider.enabled = true;

        isTriggered = false;
    }

    private void SpawnCeramicShards()
    {
        Sprite shardSprite = ProceduralPixelArt.GetProjectileSprite("TalaveraShard");
        for (int i = 0; i < 4; i++)
        {
            GameObject shard = new GameObject("Talavera_Shard_FX");
            shard.transform.position = initialPosition + new Vector3((i - 1.5f) * 0.45f, 0f, 0f);
            shard.transform.localScale = Vector3.one * 0.45f;

            SpriteRenderer ssr = shard.AddComponent<SpriteRenderer>();
            ssr.sprite = shardSprite;
            ssr.sortingOrder = 4;

            Rigidbody2D srb = shard.AddComponent<Rigidbody2D>();
            srb.gravityScale = 2.2f;
            srb.linearVelocity = new Vector2(Random.Range(-2.5f, 2.5f), Random.Range(1.5f, 3.8f));
            srb.angularVelocity = Random.Range(-360f, 360f);

            Destroy(shard, 0.85f);
        }
    }
}
