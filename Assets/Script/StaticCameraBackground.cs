using UnityEngine;

/// <summary>
/// Mantiene la imagen de fondo fija en la vista de la cámara (efecto de fondo estático),
/// ajustando opcionalmente la escala para cubrir exactamente el viewport de la cámara ortográfica.
/// </summary>
[ExecuteAlways]
public class StaticCameraBackground : MonoBehaviour
{
    [Header("Cámara Objetivo")]
    public Camera targetCamera;

    [Header("Ajuste de Posición")]
    public Vector3 offset = new Vector3(0f, 0f, 10f);
    public bool followX = true;
    public bool followY = true;

    [Header("Auto-Ajuste al Viewport (Opcional)")]
    public bool autoFitCamera = true;
    public float scaleMargin = 1.1f; // Margen para asegurar que nunca se vean bordes

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdatePositionAndScale();
    }

    void LateUpdate()
    {
        UpdatePositionAndScale();
    }

    private void UpdatePositionAndScale()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null) return;
        }

        Vector3 camPos = targetCamera.transform.position;
        float posX = followX ? camPos.x + offset.x : transform.position.x;
        float posY = followY ? camPos.y + offset.y : transform.position.y;
        float posZ = camPos.z + offset.z;

        transform.position = new Vector3(posX, posY, posZ);

        if (autoFitCamera && spriteRenderer != null && spriteRenderer.sprite != null && targetCamera.orthographic)
        {
            float camHeight = targetCamera.orthographicSize * 2f;
            float camWidth = camHeight * targetCamera.aspect;

            Vector2 spriteSize = spriteRenderer.sprite.rect.size / spriteRenderer.sprite.pixelsPerUnit;
            if (spriteSize.x > 0 && spriteSize.y > 0)
            {
                float scaleX = (camWidth / spriteSize.x) * scaleMargin;
                float scaleY = (camHeight / spriteSize.y) * scaleMargin;
                float finalScale = Mathf.Max(scaleX, scaleY);
                transform.localScale = new Vector3(finalScale, finalScale, 1f);
            }
        }
    }
}
