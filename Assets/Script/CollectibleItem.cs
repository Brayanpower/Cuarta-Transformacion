using UnityEngine;

/// <summary>
/// Ítem coleccionable (Monedas/Gemas y Corazones) con animación flotante,
/// feedback visual y recolección automática por contacto con Finn.
/// Funciona de forma autónoma con sprites o con formas 2D procedurales.
/// </summary>
public class CollectibleItem : MonoBehaviour
{
    public enum ItemType
    {
        Coin,
        Heart
    }

    [Header("Tipo de Coleccionable")]
    public ItemType itemType = ItemType.Coin;
    public int value = 1;

    [Header("Efecto Flotante")]
    public float bobSpeed = 3.5f;
    public float bobHeight = 0.12f;

    private Vector3 basePosition;
    private float timeOffset;
    private bool isCollected = false;

    // Conteo global de monedas recolectadas
    public static int TotalCoinsCollected = 0;

    void Start()
    {
        basePosition = transform.position;
        timeOffset = Random.Range(0f, 6.28f);

        // Asegurar que tenga un Collider2D trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            CircleCollider2D circle = gameObject.AddComponent<CircleCollider2D>();
            circle.isTrigger = true;
            circle.radius = 0.35f;
        }
        else
        {
            col.isTrigger = true;
        }

        // Si no tiene SpriteRenderer o sprite, asignar sprite pixel-art procedural
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = gameObject.AddComponent<SpriteRenderer>();

        if (sr.sprite == null)
        {
            sr.sprite = ProceduralPixelArt.GetCollectibleSprite(itemType == ItemType.Heart);
            sr.color = Color.white;
            sr.sortingOrder = 3;
        }
    }

    void Update()
    {
        if (isCollected || Time.timeScale <= 0f || PauseMenu.IsGamePaused)
            return;

        // Movimiento flotante senoidal
        float newY = basePosition.y + Mathf.Sin(Time.time * bobSpeed + timeOffset) * bobHeight;
        transform.position = new Vector3(basePosition.x, newY, basePosition.z);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCollected)
            return;

        PlayerMovement player = collision.GetComponent<PlayerMovement>();
        if (player == null)
            player = collision.GetComponentInParent<PlayerMovement>();

        if (player != null)
        {
            Collect(player);
        }
    }

    private void Collect(PlayerMovement player)
    {
        isCollected = true;

        if (itemType == ItemType.Coin)
        {
            TotalCoinsCollected += value;
            ProceduralSoundManager.Instance?.PlayCoinPickup();
            Debug.Log($"¡Moneda recolectada! Total: {TotalCoinsCollected}");
        }
        else if (itemType == ItemType.Heart)
        {
            ProceduralSoundManager.Instance?.PlayHeartPickup();
            if (player.currentHealth < player.maxHealth)
            {
                player.currentHealth = Mathf.Min(player.maxHealth, player.currentHealth + value);
                Debug.Log($"¡Corazón recolectado! Salud: {player.currentHealth}/{player.maxHealth}");
            }
        }

        Destroy(gameObject);
    }
}

