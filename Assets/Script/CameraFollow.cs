using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Follow Settings")]
    public Vector3 offset = new Vector3(0f, 1.2f, -10f);
    public float smoothTime = 0.2f;

    [Header("Bounds (Optional)")]
    public bool enableBounds = false;
    public Vector2 minBounds = new Vector2(-20f, -2f);
    public Vector2 maxBounds = new Vector2(50f, 10f);

    private Vector3 currentVelocity = Vector3.zero;

    void Start()
    {
        // Si no se asignó el target manualmente, buscar el GameObject con tag "Player" o el componente PlayerMovement
        if (target == null)
        {
            PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>();
            if (player != null)
                target = player.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        // Calcular posición objetivo con el offset
        Vector3 targetPosition = target.position + offset;

        // Limitar la posición si están habilitados los límites
        if (enableBounds)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);
        }

        // Mantener la Z de la cámara
        targetPosition.z = offset.z;

        // Desplazamiento suave con SmoothDamp
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
    }
}
