using UnityEngine;

/// <summary>
/// Reflector de cámara / set de grabación para el Nivel 3 (Callejones de Guanajuato).
/// Oscila como un foco de callejón: si atrapa a Finn sin hacer Dash, aplica un flash
/// dañino periódico y empuje lateral.
/// </summary>
public class SpotlightHazard : MonoBehaviour
{
    [Header("Oscilación del Reflector")]
    public float swingAngle = 28f;
    public float swingSpeed = 1.6f;
    public float phaseOffset = 0f;

    [Header("Daño de Flash")]
    public int damage = 1;
    public float damageInterval = 1.0f;

    private SpriteRenderer sr;
    private BoxCollider2D beamCollider;
    private float nextDamageTime = 0f;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = gameObject.AddComponent<SpriteRenderer>();

        if (sr.sprite == null)
            sr.sprite = ProceduralPixelArt.GetSpotlightBeamSprite();
        sr.sortingOrder = 2;

        beamCollider = GetComponent<BoxCollider2D>();
        if (beamCollider == null)
        {
            beamCollider = gameObject.AddComponent<BoxCollider2D>();
            beamCollider.isTrigger = true;
            beamCollider.size = new Vector2(0.85f, 2.6f);
            beamCollider.offset = new Vector2(0f, -1.5f);
        }
        else
        {
            beamCollider.isTrigger = true;
        }
    }

    private void Update()
    {
        float angle = Mathf.Sin(Time.time * swingSpeed + phaseOffset) * swingAngle;
        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (Time.time < nextDamageTime)
            return;

        PlayerMovement player = collision.GetComponent<PlayerMovement>() ?? collision.GetComponentInParent<PlayerMovement>();
        if (player == null || player.IsDashing || player.IsInvulnerable)
            return;

        nextDamageTime = Time.time + damageInterval;
        ProceduralSoundManager.Instance?.PlayLaserFlash();

        Vector2 pushDir = (player.transform.position - transform.position).normalized;
        if (pushDir.y < 0.35f) pushDir.y = 0.45f;
        player.TakeDamage(damage, pushDir.normalized);
    }
}
