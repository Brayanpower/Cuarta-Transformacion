using UnityEngine;

/// <summary>
/// Control de cámara suave con soporte de límites de nivel
/// y bloqueo de arena estático estilo Sonic durante la batalla contra el jefe.
/// </summary>
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

    [Header("Boss Arena Lock (Estilo Sonic)")]
    public bool isBossLocked = false;
    public Vector3 bossLockPosition = new Vector3(142.5f, -1.8f, -10f);
    public float bossLockOrthographicSize = 4.2f;

    private Vector3 currentVelocity = Vector3.zero;
    private Camera camComponent;
    private float defaultOrthographicSize = 2f;
    private float defaultFieldOfView = 34f;
    private float userZoomScale = LevelProgressionManager.DEFAULT_CAMERA_ZOOM;
    private bool hasCapturedBaseCamera = false;

    void Start()
    {
        EnsureBaseCameraCaptured();
        ApplyUserZoom(LevelProgressionManager.GetCameraZoom(), true);

        // Si no se asignó el target manualmente, buscar el GameObject con tag "Player" o el componente PlayerMovement
        if (target == null)
        {
            PlayerMovement player = Object.FindAnyObjectByType<PlayerMovement>();
            if (player != null)
                target = player.transform;
        }
    }

    private void EnsureBaseCameraCaptured()
    {
        if (camComponent == null)
            camComponent = GetComponent<Camera>();

        if (!hasCapturedBaseCamera && camComponent != null)
        {
            defaultOrthographicSize = camComponent.orthographicSize;
            defaultFieldOfView = camComponent.fieldOfView;
            hasCapturedBaseCamera = true;
        }
    }

    private float GetTargetOrthographicSize()
    {
        if (isBossLocked)
            return Mathf.Clamp(bossLockOrthographicSize * Mathf.Clamp(userZoomScale, 0.80f, 1.35f), 4.2f, 9.0f);

        return Mathf.Clamp(defaultOrthographicSize * userZoomScale, 1.4f, 8.0f);
    }

    public void ApplyUserZoom(float zoomScale, bool immediate = false)
    {
        EnsureBaseCameraCaptured();
        userZoomScale = Mathf.Clamp(zoomScale, LevelProgressionManager.MIN_CAMERA_ZOOM, LevelProgressionManager.MAX_CAMERA_ZOOM);

        if (immediate && camComponent != null)
        {
            if (camComponent.orthographic)
                camComponent.orthographicSize = GetTargetOrthographicSize();
            else
                camComponent.fieldOfView = Mathf.Clamp(defaultFieldOfView * userZoomScale, 25f, 95f);
        }
    }

    public static void ApplyZoomToActiveCamera(float zoomScale, bool immediate = true)
    {
        CameraFollow cf = Object.FindAnyObjectByType<CameraFollow>();
        if (cf != null)
            cf.ApplyUserZoom(zoomScale, immediate);
    }

    /// <summary>
    /// Bloquea la cámara fija en la arena del jefe estilo Sonic.
    /// </summary>
    public void LockBossArena(Vector3 position, float orthoSize = 4.2f)
    {
        isBossLocked = true;
        bossLockPosition = position;
        bossLockOrthographicSize = orthoSize;
        Debug.Log("[CameraFollow] Cámara bloqueada en la arena del Minotauro estilo Sonic.");
    }

    /// <summary>
    /// Desbloquea la cámara para que vuelva a seguir suavemente al jugador.
    /// </summary>
    public void UnlockBossArena()
    {
        isBossLocked = false;
        Debug.Log("[CameraFollow] Cámara desbloqueada, siguiendo a Finn.");
    }

    void LateUpdate()
    {
        EnsureBaseCameraCaptured();

        // Si la cámara está bloqueada en la arena del jefe
        if (isBossLocked)
        {
            transform.position = Vector3.SmoothDamp(transform.position, bossLockPosition, ref currentVelocity, 0.35f);

            if (camComponent != null)
            {
                if (camComponent.orthographic)
                {
                    float targetSize = GetTargetOrthographicSize();
                    camComponent.orthographicSize = Mathf.Lerp(camComponent.orthographicSize, targetSize, Time.deltaTime * 2.5f);
                }
                else
                {
                    float targetFov = Mathf.Clamp(defaultFieldOfView * userZoomScale, 25f, 95f);
                    camComponent.fieldOfView = Mathf.Lerp(camComponent.fieldOfView, targetFov, Time.deltaTime * 2.5f);
                }
            }
            return;
        }

        // Restaurar suavemente el tamaño ortográfico / FOV ajustado por el usuario
        if (camComponent != null)
        {
            if (camComponent.orthographic)
            {
                float targetSize = GetTargetOrthographicSize();
                if (Mathf.Abs(camComponent.orthographicSize - targetSize) > 0.01f)
                    camComponent.orthographicSize = Mathf.Lerp(camComponent.orthographicSize, targetSize, Time.deltaTime * 2.5f);
            }
            else
            {
                float targetFov = Mathf.Clamp(defaultFieldOfView * userZoomScale, 25f, 95f);
                if (Mathf.Abs(camComponent.fieldOfView - targetFov) > 0.05f)
                    camComponent.fieldOfView = Mathf.Lerp(camComponent.fieldOfView, targetFov, Time.deltaTime * 2.5f);
            }
        }

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
