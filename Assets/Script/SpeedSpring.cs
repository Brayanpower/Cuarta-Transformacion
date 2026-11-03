using UnityEngine;

/// <summary>
/// Resorte / Trampolín estilo Sonic que impulsa al jugador con gran velocidad vertical u horizontal.
/// </summary>
public class SpeedSpring : MonoBehaviour
{
    [Header("Impulso")]
    public Vector2 springForce = new Vector2(0f, 14f);
    public bool resetVerticalVelocity = true;

    [Header("Animación de Rebote")]
    public float squashAmount = 0.5f;
    public float restoreSpeed = 8f;

    private Vector3 originalScale;
    private bool isSquashing = false;

    void Start()
    {
        originalScale = transform.localScale;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = gameObject.AddComponent<SpriteRenderer>();
        if (sr.sprite == null)
        {
            sr.sprite = ProceduralPixelArt.GetSpringSprite();
            sr.sortingOrder = 3;
        }
    }

    void Update()
    {
        if (isSquashing)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * restoreSpeed);
            if (Vector3.Distance(transform.localScale, originalScale) < 0.02f)
            {
                transform.localScale = originalScale;
                isSquashing = false;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        ActivateSpring(collision.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        ActivateSpring(collision.gameObject);
    }

    private void ActivateSpring(GameObject obj)
    {
        PlayerMovement player = obj.GetComponent<PlayerMovement>();
        if (player == null)
            player = obj.GetComponentInParent<PlayerMovement>();

        if (player != null)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                float targetVx = springForce.x != 0f ? springForce.x : rb.linearVelocity.x;
                float targetVy = springForce.y > 0f ? springForce.y : rb.linearVelocity.y;
                rb.linearVelocity = new Vector2(targetVx, targetVy);
            }

            // Sonido y efecto visual de resorte
            ProceduralSoundManager.Instance?.PlaySpringBounce();
            transform.localScale = new Vector3(originalScale.x * 1.3f, originalScale.y * squashAmount, originalScale.z);
            isSquashing = true;
        }
    }
}
