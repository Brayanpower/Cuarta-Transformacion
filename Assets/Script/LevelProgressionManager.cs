using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestiona la progresión y desbloqueo de niveles mediante PlayerPrefs.
/// Nivel 1 (Jardín Principal) = Siempre desbloqueado (1)
/// Nivel 2 (Talleres de Talavera) = Se desbloquea al vencer al Jefe 1 (2)
/// Nivel 3 (Callejones de Guanajuato) = Se desbloquea al vencer al Jefe 2 (3)
/// </summary>
public static class LevelProgressionManager
{
    public const string UNLOCKED_LEVEL_KEY = "CuartaTransformacion_UnlockedLevel";
    public const string VOLUME_KEY = "CuartaTransformacion_Volume";
    public const string MUSIC_VOLUME_KEY = "CuartaTransformacion_MusicVolume";
    public const string SFX_VOLUME_KEY = "CuartaTransformacion_SfxVolume";
    public const string FULLSCREEN_KEY = "CuartaTransformacion_Fullscreen";
    public const string CAMERA_ZOOM_KEY = "CuartaTransformacion_CameraZoom";
    public const string TOUCH_CONTROLS_MODE_KEY = "CuartaTransformacion_TouchControlsMode";
    public const float DEFAULT_VOLUME = 0.85f;
    public const float DEFAULT_CAMERA_ZOOM = 1.0f;
    public const float MIN_CAMERA_ZOOM = 0.70f;
    public const float MAX_CAMERA_ZOOM = 3.00f;

    public static float GetMusicVolume()
    {
        float fallback = PlayerPrefs.GetFloat(VOLUME_KEY, DEFAULT_VOLUME);
        return Mathf.Clamp01(PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, fallback));
    }

    public static void SetMusicVolume(float volume)
    {
        float clamped = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, clamped);
        PlayerPrefs.Save();
    }

    public static float GetSfxVolume()
    {
        float fallback = PlayerPrefs.GetFloat(VOLUME_KEY, DEFAULT_VOLUME);
        return Mathf.Clamp01(PlayerPrefs.GetFloat(SFX_VOLUME_KEY, fallback));
    }

    public static void SetSfxVolume(float volume)
    {
        float clamped = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, clamped);
        PlayerPrefs.Save();
    }

    public static float GetCameraZoom()
    {
        return Mathf.Clamp(PlayerPrefs.GetFloat(CAMERA_ZOOM_KEY, DEFAULT_CAMERA_ZOOM), MIN_CAMERA_ZOOM, MAX_CAMERA_ZOOM);
    }

    public static void SetCameraZoom(float zoomScale)
    {
        float clamped = Mathf.Clamp(zoomScale, MIN_CAMERA_ZOOM, MAX_CAMERA_ZOOM);
        PlayerPrefs.SetFloat(CAMERA_ZOOM_KEY, clamped);
        PlayerPrefs.Save();
    }

    public static string FormatCameraZoomLabel(float zoomScale)
    {
        float clamped = Mathf.Clamp(zoomScale, MIN_CAMERA_ZOOM, MAX_CAMERA_ZOOM);
        int pct = Mathf.RoundToInt(clamped * 100f);
        string tag;
        if (clamped <= 0.85f)
            tag = "Cerca";
        else if (clamped <= 1.15f)
            tag = "Normal";
        else if (clamped <= 1.75f)
            tag = "Amplio";
        else
            tag = "Panorámico";
        return $"{pct}% ({tag})";
    }

    /// <summary>
    /// Modo de controles táctiles: 0 = Auto (Móvil / WebGL), 1 = Siempre Visible, 2 = Oculto.
    /// </summary>
    public static int GetTouchControlsMode()
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(TOUCH_CONTROLS_MODE_KEY, 0), 0, 2);
    }

    public static void SetTouchControlsMode(int mode)
    {
        int clamped = Mathf.Clamp(mode, 0, 2);
        PlayerPrefs.SetInt(TOUCH_CONTROLS_MODE_KEY, clamped);
        PlayerPrefs.Save();
    }

    public static bool ShouldShowTouchControls()
    {
        int mode = GetTouchControlsMode();
        if (mode == 1) return true;
        if (mode == 2) return false;

        return Application.isMobilePlatform
            || SystemInfo.deviceType == DeviceType.Handheld
            || Input.touchSupported
            || Application.platform == RuntimePlatform.WebGLPlayer;
    }

    public static string FormatTouchControlsModeLabel(int mode)
    {
        switch (Mathf.Clamp(mode, 0, 2))
        {
            case 1:  return "SIEMPRE VISIBLE";
            case 2:  return "OCULTO";
            default: return "AUTO (MÓVIL/WEB)";
        }
    }

    /// <summary>
    /// Retorna el número del nivel más alto desbloqueado (por defecto 1).
    /// </summary>
    public static int GetUnlockedLevel()
    {
        return PlayerPrefs.GetInt(UNLOCKED_LEVEL_KEY, 1);
    }

    /// <summary>
    /// Verifica si un nivel específico (1, 2 o 3) está desbloqueado.
    /// </summary>
    public static bool IsLevelUnlocked(int levelNumber)
    {
        return levelNumber <= GetUnlockedLevel();
    }

    /// <summary>
    /// Desbloquea el siguiente nivel cuando el jugador completa el nivel actual.
    /// </summary>
    public static void UnlockNextLevel(int completedLevel)
    {
        int currentUnlocked = GetUnlockedLevel();
        int nextLevel = Mathf.Clamp(completedLevel + 1, 1, 3);

        if (nextLevel > currentUnlocked)
        {
            PlayerPrefs.SetInt(UNLOCKED_LEVEL_KEY, nextLevel);
            PlayerPrefs.Save();
            Debug.Log($"[LevelProgressionManager] ¡Nivel {nextLevel} desbloqueado y guardado en PlayerPrefs!");
        }
        else
        {
            Debug.Log($"[LevelProgressionManager] Nivel {completedLevel} completado (Nivel {nextLevel} ya estaba desbloqueado).");
        }
    }

    /// <summary>
    /// Reinicia el progreso para dejar únicamente el Nivel 1 desbloqueado.
    /// </summary>
    public static void ResetProgression()
    {
        PlayerPrefs.SetInt(UNLOCKED_LEVEL_KEY, 1);
        PlayerPrefs.Save();
        Debug.Log("[LevelProgressionManager] Progresión reiniciada al Nivel 1.");
    }

    /// <summary>
    /// Alterna entre desbloquear todos los niveles (3) y reiniciar a solo Nivel 1 (1) para pruebas rápidas.
    /// </summary>
    public static void ToggleDebugProgression()
    {
        int current = GetUnlockedLevel();
        int nextState = current >= 3 ? 1 : 3;
        PlayerPrefs.SetInt(UNLOCKED_LEVEL_KEY, nextState);
        PlayerPrefs.Save();
        Debug.Log($"[LevelProgressionManager] Modo Debug: Nivel desbloqueado ajustado a {nextState}.");
    }

    /// <summary>
    /// Intenta cargar una escena por nombre si existe en Build Settings.
    /// </summary>
    public static bool TryLoadScene(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
            return true;
        }

        Debug.LogWarning($"[LevelProgressionManager] La escena '{sceneName}' aún no está registrada en File > Build Settings.");
        return false;
    }
}
