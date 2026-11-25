using UnityEngine;

/// <summary>
/// Esbirro temático exclusivo del Nivel 2 (Talleres de Talavera): Guerrero Vasija de Talavera.
/// Nunca usa sprites de garbanzo: mantiene su arte de cerámica azul y blanca al caminar,
/// al recibir daño y al romperse al morir, y lanza fragmentos de Talavera.
/// </summary>
public class Level2TalaveraEnemy : SimpleEnemy
{
    [Header("Ataque de Fragmentos de Talavera")]
    public float throwRange = 9.5f;
    public float throwCooldown = 2.6f;
    public float shardSpeed = 9.0f;

    private float nextThrowTime = 0f;
    private float talaveraAnimTimer = 0f;
    private int talaveraFrame = 0;
    private Sprite[] talaveraFrames;

    protected override bool UseGarbanzoSprites => false;

    protected override void Awake()
    {
        talaveraFrames = new Sprite[]
        {
            ProceduralPixelArt.GetTalaveraEnemySprite(0),
            ProceduralPixelArt.GetTalaveraEnemySprite(1)
        };

        walkSprites = talaveraFrames;
        defeatSprites = new Sprite[] { ProceduralPixelArt.GetTalaveraEnemySprite(2) };
        maxHealth = 3;
        patrolSpeed = 2.5f;
        detectionRange = 12f;

        base.Awake();
    }

    protected override void Start()
    {
        base.Start();

        if (spriteRenderer != null && talaveraFrames != null && talaveraFrames.Length > 0)
            spriteRenderer.sprite = talaveraFrames[0];

        nextThrowTime = Time.time + Random.Range(0.8f, 1.8f);
    }

    private void LateUpdate()
    {
        if (isDead || player == null || Time.timeScale <= 0f || PauseMenu.IsGamePaused)
            return;

        // Mantener siempre sprites propios de cerámica de Talavera
        talaveraAnimTimer += Time.deltaTime;
        if (talaveraAnimTimer >= 0.16f && spriteRenderer != null && talaveraFrames != null)
        {
            talaveraAnimTimer = 0f;
            talaveraFrame = (talaveraFrame + 1) % talaveraFrames.Length;
            spriteRenderer.sprite = talaveraFrames[talaveraFrame];
        }

        if (Time.time < hitStunUntil)
            return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= throwRange && dist >= 2.2f && Time.time >= nextThrowTime)
        {
            nextThrowTime = Time.time + throwCooldown;
            ThrowTalaveraShard();
        }
    }

    private void ThrowTalaveraShard()
    {
        if (player == null)
            return;

        ProceduralSoundManager.Instance?.PlayBossShoot();

        Vector2 dir = (player.position + Vector3.up * 0.35f - (transform.position + Vector3.up * 0.7f)).normalized;
        GameObject projObj = new GameObject("TalaveraShard_Projectile");
        projObj.transform.position = transform.position + new Vector3(Mathf.Sign(dir.x) * 0.6f, 0.7f, 0f);
        projObj.transform.localScale = Vector3.one * 0.75f;

        FallingPropagandaProjectile proj = projObj.AddComponent<FallingPropagandaProjectile>();
        proj.ConfigureAndLaunch(
            FallingPropagandaProjectile.ProjectileStyle.TalaveraShard,
            new Vector2(dir.x * shardSpeed, Mathf.Clamp(dir.y * shardSpeed + 2.2f, -1f, 5.5f)),
            1,
            0.35f,
            4.5f
        );
    }
}
