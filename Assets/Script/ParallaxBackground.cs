using UnityEngine;

/// <summary>
/// Acompaña suavemente a la cámara principal durante todo el recorrido del Nivel 1
/// (Parroquia de Dolores Hidalgo), aplicando un desplazamiento suave y auto-ajuste
/// de escala para cubrir siempre el encuadre sin quedarse fijo al inicio.
/// </summary>
[ExecuteAlways]
public class ParallaxBackground : MonoBehaviour
{
    [Header("Referencia de Cámara")]
    public Camera targetCamera;

    [Header("Configuración de Seguimiento Suave y Parallax")]
    [Tooltip("1.0 = Acompaña totalmente a la cámara; 0.95 = Ligero desplazamiento parallax")]
    [Range(0.85f, 1.0f)]
    public float followFactorX = 0.98f;

    [Range(0.85f, 1.0f)]
    public float followFactorY = 0.95f;

    [Tooltip("Velocidad de suavizado al seguir a la cámara")]
    public float smoothSpeed = 12f;

    public Vector3 positionOffset = new Vector3(0f, 0f, 10f);

    [Header("Auto-Ajuste al Viewport")]
    public bool autoFitCamera = true;
    public float scaleMargin = 1.22f;

    private SpriteRenderer spriteRenderer;
    private Vector3 currentVelocity;

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        spriteRenderer = GetComponent<SpriteRenderer>();
        SnapToCameraImmediate();
    }

    void LateUpdate()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null) return;
        }

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        Vector3 camPos = targetCamera.transform.position;

        // Acompaña a la cámara a lo largo de todo el recorrido con ligero efecto de profundidad
        float targetX = (camPos.x * followFactorX) + positionOffset.x;
        float targetY = (camPos.y * followFactorY) + positionOffset.y;
        float targetZ = camPos.z + positionOffset.z;

        Vector3 targetPosition = new Vector3(targetX, targetY, targetZ);

        if (!Application.isPlaying)
        {
            transform.position = targetPosition;
        }
        else
        {
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref currentVelocity,
                1f / Mathf.Max(1f, smoothSpeed)
            );
        }

        // Mantener el tamaño del fondo cubriendo el viewport completo con margen para el parallax
        if (autoFitCamera && spriteRenderer != null && spriteRenderer.sprite != null && targetCamera.orthographic)
        {
            float camHeight = targetCamera.orthographicSize * 2f;
            float camWidth = camHeight * targetCamera.aspect;

            // Si hay ligero desfase por followFactorX < 1, compensar ancho extra
            float maxTravelOffset = Mathf.Abs(camPos.x * (1f - followFactorX)) * 2f;
            float requiredWidth = (camWidth + maxTravelOffset) * scaleMargin;
            float requiredHeight = camHeight * scaleMargin;

            Vector2 spriteSize = spriteRenderer.sprite.rect.size / spriteRenderer.sprite.pixelsPerUnit;
            if (spriteSize.x > 0.01f && spriteSize.y > 0.01f)
            {
                float scaleX = requiredWidth / spriteSize.x;
                float scaleY = requiredHeight / spriteSize.y;
                float finalScale = Mathf.Max(scaleX, scaleY);
                transform.localScale = new Vector3(finalScale, finalScale, 1f);
            }
        }
    }

    public void SnapToCameraImmediate()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
        if (targetCamera == null)
            return;

        Vector3 camPos = targetCamera.transform.position;
        transform.position = new Vector3(
            (camPos.x * followFactorX) + positionOffset.x,
            (camPos.y * followFactorY) + positionOffset.y,
            camPos.z + positionOffset.z
        );
    }
}

