using UnityEngine;

public class HazardSpikes : MonoBehaviour
{
    [Header("Hazard Damage")]
    public int damage = 1;
    public float knockbackUpForce = 7f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryHurtPlayer(collision.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryHurtPlayer(collision.gameObject);
    }

    private void TryHurtPlayer(GameObject target)
    {
        PlayerMovement player = target.GetComponent<PlayerMovement>();
        if (player != null)
        {
            Vector2 knockback = new Vector2(0f, 1f); // Empujar hacia arriba
            player.TakeDamage(damage, knockback);
        }
    }
}
