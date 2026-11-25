using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HazardSpikes : MonoBehaviour
{
    [Header("Hazard Damage")]
    public int damage = 1;
    public float knockbackUpForce = 7f;

    [Header("Lava Pixel-Art Animada")]
    public bool renderAsLava = true;
    public float lavaHeight = 0.82f;
    public float lavaBounceForce = 13.5f;
    public float animationFps = 6.5f;

    private readonly List<SpriteRenderer> lavaSegmentRenderers = new List<SpriteRenderer>();
    private Sprite[] lavaFrames;
    private SpriteRenderer glowRenderer;
    private float lavaWidth = 4f;
    private float glowHeight = 1.25f;
    private float emberMinSpeedY = 1.2f;
    private float emberMaxSpeedY = 2.3f;
    private float emberMinLife = 0.7f;
    private float emberMaxLife = 1.1f;
    private float animTimer = 0f;
    private int animFrame = 0;
    private float nextEmberTime = 0f;
    private float nextBounceTime = -10f;
    private bool isFlatSurfacePit = false;

    private void Start()
    {
        if (!renderAsLava)
            return;

        string sceneName = SceneManager.GetActiveScene().name;

        // Solo Nivel 1: desactivar los 21 objetos heredados FireballAssets apilados sobre el mismo foso
        // y asentar la lava al ras del suelo empedrado sin tapar los puentes superiores
        if (sceneName == "Nivel1")
        {
            if (gameObject.name.StartsWith("FireballAssets"))
            {
                gameObject.SetActive(false);
                return;
            }
        }

        lavaWidth = Mathf.Max(1f, Mathf.Abs(transform.localScale.x));
        float bottomY = transform.position.y - Mathf.Abs(transform.localScale.y) * 0.5f;
        float centerX = transform.position.x;

        bool isEarlyNivel1Pit = sceneName == "Nivel1" &&
            (gameObject.name == "Hazard_Spikes_1" || gameObject.name == "Hazard_Spikes_2");
        bool isFlatNivel3Strip = sceneName == "Nivel3" &&
            (gameObject.name == "Hazard_Callejon_Bajo1" || gameObject.name == "Hazard_Callejon_Bajo2");
        isFlatSurfacePit = isEarlyNivel1Pit || isFlatNivel3Strip;

        if (isEarlyNivel1Pit)
        {
            lavaHeight = 0.36f;
            bottomY = -2.14f;
            glowHeight = 0.50f;
            emberMinSpeedY = 0.55f;
            emberMaxSpeedY = 1.05f;
            emberMinLife = 0.40f;
            emberMaxLife = 0.65f;
        }
        else if (sceneName == "Nivel1")
        {
            lavaHeight = 0.72f;
        }
        // Sellar los fosos de hornos en Nivel 2 de pared a pared sin huecos donde quedarse atrapado
        else if (sceneName == "Nivel2")
        {
            if (gameObject.name == "Hazard_Horno_1")
            {
                centerX = 37.0f;
                bottomY = -5.0f;
                lavaWidth = 16.0f;
            }
            else if (gameObject.name == "Hazard_Horno_2")
            {
                centerX = 56.5f;
                bottomY = -5.0f;
                lavaWidth = 15.0f;
            }
            else if (gameObject.name == "Hazard_Ext_N2_1")
            {
                centerX = 143.0f;
                bottomY = -5.0f;
                lavaWidth = 18.0f;
            }
            else if (gameObject.name == "Hazard_Ext_N2_2")
            {
                centerX = 165.0f;
                bottomY = -5.0f;
                lavaWidth = 18.0f;
            }
        }
        else if (sceneName == "Nivel3")
        {
            if (isFlatNivel3Strip)
            {
                lavaHeight = 0.36f;
                bottomY = -3.14f;
                glowHeight = 0.50f;
                emberMinSpeedY = 0.55f;
                emberMaxSpeedY = 1.05f;
                emberMinLife = 0.40f;
                emberMaxLife = 0.65f;
            }
            else
            {
                lavaHeight = 0.56f;
                glowHeight = 0.80f;

                if (gameObject.name == "Hazard_Azotea_Foso1")
                {
                    centerX = 87.5f;
                    bottomY = -5.10f;
                    lavaWidth = 5.0f;
                }
                else if (gameObject.name == "Hazard_Azotea_Foso2")
                {
                    centerX = 104.5f;
                    bottomY = -5.10f;
                    lavaWidth = 5.0f;
                }
                else if (gameObject.name.StartsWith("Hazard_Ext_N3_"))
                {
                    bottomY = -6.10f;
                }
            }
        }

        transform.position = new Vector3(centerX, bottomY, transform.position.z);
        transform.localScale = Vector3.one;

        SpriteRenderer parentSr = GetComponent<SpriteRenderer>();
        if (parentSr != null)
            parentSr.enabled = false;

        BuildLavaVisuals();
        UpdateTriggerBounds();
        nextEmberTime = Time.time + Random.Range(0.15f, 0.45f);
    }

    private void BuildLavaVisuals()
    {
        lavaFrames = ProceduralPixelArt.GetLavaTileFrames();
        lavaSegmentRenderers.Clear();

        float tileWorldHeight = 0.75f; // 24 px / 32 PPU
        float segScaleY = lavaHeight / tileWorldHeight;

        float offset = 0f;
        int idx = 0;
        while (offset < lavaWidth - 0.001f)
        {
            float segW = Mathf.Min(1.0f, lavaWidth - offset);
            float localX = (-lavaWidth * 0.5f) + offset + (segW * 0.5f);

            GameObject segObj = new GameObject($"Lava_Seg_{idx}");
            segObj.transform.SetParent(transform, false);
            segObj.transform.localPosition = new Vector3(localX, 0f, 0f);
            segObj.transform.localScale = new Vector3(segW, segScaleY, 1f);

            SpriteRenderer sr = segObj.AddComponent<SpriteRenderer>();
            sr.sprite = lavaFrames[0];
            sr.sortingOrder = 5; // Material Sprites-Default para auto-iluminarse de noche
            lavaSegmentRenderers.Add(sr);

            offset += segW;
            idx++;
        }

        GameObject glowObj = new GameObject("Lava_AmbientGlow");
        glowObj.transform.SetParent(transform, false);
        glowObj.transform.localPosition = new Vector3(0f, lavaHeight * 0.45f, 0f);
        glowObj.transform.localScale = new Vector3(lavaWidth, glowHeight, 1f);

        glowRenderer = glowObj.AddComponent<SpriteRenderer>();
        glowRenderer.sprite = ProceduralPixelArt.GetLavaGlowSprite();
        glowRenderer.color = new Color(1f, 0.72f, 0.24f, 0.30f);
        glowRenderer.sortingOrder = 4;
    }

    private void UpdateTriggerBounds()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null)
            col = gameObject.AddComponent<BoxCollider2D>();

        col.isTrigger = true;
        if (isFlatSurfacePit)
        {
            col.size = new Vector2(Mathf.Max(0.5f, lavaWidth - 0.12f), lavaHeight * 0.72f);
            col.offset = new Vector2(0f, lavaHeight * 0.36f);
        }
        else
        {
            col.size = new Vector2(lavaWidth + 0.10f, lavaHeight * 0.85f);
            col.offset = new Vector2(0f, lavaHeight * 0.425f);
        }
    }

    private void Update()
    {
        if (!renderAsLava || lavaFrames == null || lavaFrames.Length == 0 || Time.timeScale <= 0f)
            return;

        animTimer += Time.deltaTime;
        float frameInterval = 1f / Mathf.Max(1f, animationFps);
        if (animTimer >= frameInterval)
        {
            animTimer -= frameInterval;
            animFrame = (animFrame + 1) % lavaFrames.Length;
            Sprite frameSprite = lavaFrames[animFrame];
            for (int i = 0; i < lavaSegmentRenderers.Count; i++)
            {
                if (lavaSegmentRenderers[i] != null)
                    lavaSegmentRenderers[i].sprite = frameSprite;
            }
        }

        if (glowRenderer != null)
        {
            float pulse = 0.22f + (0.5f + 0.5f * Mathf.Sin(Time.time * 3.5f)) * 0.16f;
            glowRenderer.color = new Color(1f, 0.72f, 0.24f, pulse);
        }

        if (Time.time >= nextEmberTime)
        {
            SpawnAmbientEmber();
            float densityFactor = Mathf.Clamp(8f / Mathf.Max(4f, lavaWidth), 0.55f, 1.15f);
            nextEmberTime = Time.time + Random.Range(0.25f, 0.55f) * densityFactor;
        }
    }

    private void SpawnAmbientEmber()
    {
        float halfSpan = Mathf.Max(0.3f, lavaWidth * 0.5f - 0.25f);
        Vector3 pos = transform.position + new Vector3(Random.Range(-halfSpan, halfSpan), lavaHeight * 0.85f, 0f);
        SpawnEmberParticle(pos, new Vector2(Random.Range(-0.45f, 0.45f), Random.Range(emberMinSpeedY, emberMaxSpeedY)), Random.Range(emberMinLife, emberMaxLife), 0.85f, 0f);
    }

    private void SpawnLavaSplash(Vector3 origin)
    {
        for (int i = 0; i < 8; i++)
        {
            float vx = Random.Range(-3.4f, 3.4f);
            float vy = Random.Range(3.2f, 6.2f);
            SpawnEmberParticle(
                origin + new Vector3(Random.Range(-0.35f, 0.35f), 0.2f, 0f),
                new Vector2(vx, vy),
                Random.Range(0.45f, 0.75f),
                Random.Range(1.1f, 1.55f),
                9.5f
            );
        }
    }

    private void SpawnEmberParticle(Vector3 worldPos, Vector2 initialVelocity, float life, float scale, float gravity)
    {
        GameObject go = new GameObject("Lava_Ember");
        go.transform.position = worldPos;
        go.transform.localScale = Vector3.one * scale;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ProceduralPixelArt.GetLavaEmberSprite();
        sr.sortingOrder = 6;

        LavaParticle lp = go.AddComponent<LavaParticle>();
        lp.Init(sr, initialVelocity, life, gravity);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryHurtPlayer(collision.gameObject);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TryHurtPlayer(collision.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryHurtPlayer(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryHurtPlayer(collision.gameObject);
    }

    private void TryHurtPlayer(GameObject target)
    {
        PlayerMovement player = target.GetComponent<PlayerMovement>() ?? target.GetComponentInParent<PlayerMovement>();
        if (player == null)
            return;

        player.TakeDamage(damage, Vector2.up);

        // Rebote de rescate estilo Mario para poder salir del foso de lava sin quedarse encerrado
        if (renderAsLava && player.currentHealth > 0 && Time.time >= nextBounceTime)
        {
            nextBounceTime = Time.time + 0.30f;

            Rigidbody2D prb = player.GetComponent<Rigidbody2D>();
            if (prb != null)
            {
                float vx = Mathf.Abs(prb.linearVelocity.x) < 1.5f
                    ? (player.transform.localScale.x >= 0f ? 2.5f : -2.5f)
                    : prb.linearVelocity.x;
                float vy = Mathf.Max(prb.linearVelocity.y, lavaBounceForce);
                prb.linearVelocity = new Vector2(vx, vy);
            }

            SpawnLavaSplash(player.transform.position);
            ProceduralSoundManager.Instance?.PlayExplosion();
        }
    }

    private sealed class LavaParticle : MonoBehaviour
    {
        private SpriteRenderer sr;
        private Vector2 velocity;
        private float lifetime;
        private float gravity;
        private float elapsed;
        private float swaySeed;

        public void Init(SpriteRenderer renderer, Vector2 vel, float life, float grav)
        {
            sr = renderer;
            velocity = vel;
            lifetime = Mathf.Max(0.2f, life);
            gravity = grav;
            swaySeed = Random.Range(0f, 10f);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            if (gravity > 0f)
                velocity.y -= gravity * Time.deltaTime;

            float swayX = gravity <= 0f ? Mathf.Sin(Time.time * 7f + swaySeed) * 0.45f : 0f;
            transform.position += new Vector3((velocity.x + swayX) * Time.deltaTime, velocity.y * Time.deltaTime, 0f);

            if (sr != null)
            {
                float alpha = 1f - Mathf.Clamp01(elapsed / lifetime);
                sr.color = new Color(1f, 1f, 1f, alpha);
            }
        }
    }
}
