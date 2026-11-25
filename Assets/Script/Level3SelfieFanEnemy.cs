using UnityEngine;

/// <summary>
/// Esbirro terrestre exclusivo del Nivel 3 (Callejones de Guanajuato): Fan Obsesionado con Selfies.
/// Persigue a Finn sosteniendo su celular iluminado y dispara flashes / emojis virales.
/// </summary>
public class Level3SelfieFanEnemy : SimpleEnemy
{
    [Header("Ataque de Flash de Celular")]
    public float flashRange = 8.5f;
    public float flashCooldown = 2.7f;
    public float shotSpeed = 10.5f;

    private float nextFlashTime = 0f;
    private float fanAnimTimer = 0f;
    private int fanFrame = 0;
    private Sprite[] fanFrames;

    protected override bool UseGarbanzoSprites => false;

    protected override void Awake()
    {
        fanFrames = new Sprite[]
        {
            ProceduralPixelArt.GetSelfieFanEnemySprite(0),
            ProceduralPixelArt.GetSelfieFanEnemySprite(1)
        };

        walkSprites = fanFrames;
        defeatSprites = new Sprite[] { ProceduralPixelArt.GetSelfieFanEnemySprite(2) };
        maxHealth = 3;
        patrolSpeed = 2.9f;
        detectionRange = 12.5f;

        base.Awake();
    }

    protected override void Start()
    {
        base.Start();

        if (spriteRenderer != null && fanFrames != null && fanFrames.Length > 0)
            spriteRenderer.sprite = fanFrames[0];

        nextFlashTime = Time.time + Random.Range(0.9f, 1.8f);
    }

    private void LateUpdate()
    {
        if (isDead || player == null || Time.timeScale <= 0f || PauseMenu.IsGamePaused)
            return;

        fanAnimTimer += Time.deltaTime;
        if (fanAnimTimer >= 0.15f && spriteRenderer != null && fanFrames != null)
        {
            fanAnimTimer = 0f;
            fanFrame = (fanFrame + 1) % fanFrames.Length;
            spriteRenderer.sprite = fanFrames[fanFrame];
        }

        if (Time.time < hitStunUntil)
            return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= flashRange && dist >= 2.4f && Time.time >= nextFlashTime)
        {
            nextFlashTime = Time.time + flashCooldown;
            FireSelfieFlash();
        }
    }

    private void FireSelfieFlash()
    {
        if (player == null)
            return;

        ProceduralSoundManager.Instance?.PlayLaserFlash();

        float dirX = player.position.x < transform.position.x ? -1f : 1f;
        GameObject flashObj = new GameObject("SelfieFan_LikeShot");
        flashObj.transform.position = transform.position + new Vector3(dirX * 0.65f, 0.85f, 0f);
        flashObj.transform.localScale = Vector3.one * 0.8f;

        FallingPropagandaProjectile proj = flashObj.AddComponent<FallingPropagandaProjectile>();
        proj.ConfigureAndLaunch(
            FallingPropagandaProjectile.ProjectileStyle.ViralEmojiLike,
            new Vector2(dirX * shotSpeed, 1.2f),
            1,
            0.18f,
            4.0f
        );
    }
}
