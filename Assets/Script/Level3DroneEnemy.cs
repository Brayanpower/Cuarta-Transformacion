using UnityEngine;

/// <summary>
/// Esbirro volador exclusivo del Nivel 3 (Callejones de Guanajuato): Mini-Dron Paparazzi.
/// Sobrescribe Update() y UseGarbanzoSprites = false para mantener siempre su arte de dron
/// con hélices neón y lente rojo REC (jamás se reemplaza por un garbanzo).
/// </summary>
public class Level3DroneEnemy : SimpleEnemy
{
    [Header("Vuelo y Disparo del Dron")]
    public float hoverHeightAbovePlayer = 2.3f;
    public float flySpeed = 3.2f;
    public float shootRange = 11f;
    public float shootCooldown = 2.4f;
    public float shotSpeed = 10.5f;

    private float nextShootTime = 0f;
    private float droneAnimTimer = 0f;
    private int droneFrame = 0;
    private Sprite[] droneFrames;
    private Vector3 homePosition;

    protected override bool UseGarbanzoSprites => false;

    protected override void Awake()
    {
        droneFrames = new Sprite[]
        {
            ProceduralPixelArt.GetDroneEnemySprite(0),
            ProceduralPixelArt.GetDroneEnemySprite(1)
        };

        walkSprites = droneFrames;
        defeatSprites = new Sprite[] { ProceduralPixelArt.GetDroneEnemySprite(2) };
        maxHealth = 2;
        canPatrol = false;
        detectionRange = 14f;

        base.Awake();
    }

    protected override void Start()
    {
        base.Start();

        homePosition = transform.position;

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
        }

        if (spriteRenderer != null && droneFrames != null && droneFrames.Length > 0)
            spriteRenderer.sprite = droneFrames[0];

        nextShootTime = Time.time + Random.Range(0.7f, 1.6f);
    }

    protected override void Update()
    {
        if (isDead)
            return;

        if (player == null)
            FindPlayerReference();

        // Animación continua de hélices del dron paparazzi
        droneAnimTimer += Time.deltaTime;
        if (droneAnimTimer >= 0.08f && spriteRenderer != null && droneFrames != null)
        {
            droneAnimTimer = 0f;
            droneFrame = (droneFrame + 1) % droneFrames.Length;
            spriteRenderer.sprite = droneFrames[droneFrame];
        }

        if (player == null || rb == null)
            return;

        if (Time.time < hitStunUntil)
            return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= detectionRange)
        {
            Vector3 desiredPos = new Vector3(
                player.position.x,
                player.position.y + hoverHeightAbovePlayer + Mathf.Sin(Time.time * 3.5f) * 0.35f,
                transform.position.z
            );

            Vector2 toDesired = desiredPos - transform.position;
            if (toDesired.magnitude > 0.35f)
                rb.linearVelocity = toDesired.normalized * flySpeed;
            else
                rb.linearVelocity = Vector2.zero;

            if (spriteRenderer != null)
                spriteRenderer.flipX = player.position.x < transform.position.x;

            if (dist <= shootRange && Time.time >= nextShootTime)
            {
                nextShootTime = Time.time + shootCooldown;
                FireDroneShot();
            }
        }
        else
        {
            float bobY = homePosition.y + Mathf.Sin(Time.time * 2.5f) * 0.4f;
            Vector2 back = new Vector2(homePosition.x - transform.position.x, bobY - transform.position.y);
            rb.linearVelocity = back.magnitude > 0.2f ? back.normalized * (flySpeed * 0.5f) : Vector2.zero;
        }
    }

    private void FireDroneShot()
    {
        if (player == null)
            return;

        ProceduralSoundManager.Instance?.PlayBossShoot();

        Vector2 dir = ((Vector2)player.position + Vector2.up * 0.25f - (Vector2)transform.position).normalized;
        GameObject shot = new GameObject("Drone_LaserShot");
        shot.transform.position = transform.position + (Vector3)(dir * 0.45f);
        shot.transform.localScale = Vector3.one * 0.65f;

        FallingPropagandaProjectile proj = shot.AddComponent<FallingPropagandaProjectile>();
        proj.ConfigureAndLaunch(
            FallingPropagandaProjectile.ProjectileStyle.DroneShot,
            dir * shotSpeed,
            1,
            0f,
            4.0f
        );
    }

    public override void ResetEnemy()
    {
        base.ResetEnemy();
        if (rb != null)
            rb.gravityScale = 0f;
        if (spriteRenderer != null && droneFrames != null && droneFrames.Length > 0)
            spriteRenderer.sprite = droneFrames[0];
    }
}
