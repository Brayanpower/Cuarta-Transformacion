using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controlador del Menú de Ajustes del Pueblo (Volumen de la Música y Gritos,
/// FOV / Zoom de Cámara, Pantalla Completa y Resolución), con persistencia en PlayerPrefs.
/// </summary>
public class SettingsMenuUI : MonoBehaviour
{
    [Header("Controles de UI")]
    public Slider volumeSlider;
    public Text volumeValueLabel;
    public Slider sfxVolumeSlider;
    public Text sfxVolumeValueLabel;
    public Slider cameraZoomSlider;
    public Text cameraZoomValueLabel;
    public Toggle fullscreenToggle;
    public Dropdown resolutionDropdown;

    private Resolution[] availableResolutions;
    private float nextSfxPreviewTime = 0f;

    private void OnEnable()
    {
        LoadSettings();
    }

    private void Start()
    {
        EnsureSettingsControlsExist();
        InitializeResolutions();
        LoadSettings();

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(SetVolume);
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveListener(SetSfxVolume);
            sfxVolumeSlider.onValueChanged.AddListener(SetSfxVolume);
        }

        if (cameraZoomSlider != null)
        {
            cameraZoomSlider.minValue = LevelProgressionManager.MIN_CAMERA_ZOOM;
            cameraZoomSlider.maxValue = LevelProgressionManager.MAX_CAMERA_ZOOM;
            cameraZoomSlider.wholeNumbers = false;
            cameraZoomSlider.onValueChanged.RemoveListener(SetCameraZoom);
            cameraZoomSlider.onValueChanged.AddListener(SetCameraZoom);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged.RemoveListener(SetResolution);
            resolutionDropdown.onValueChanged.AddListener(SetResolution);
        }
    }

    private void EnsureSettingsControlsExist()
    {
        if (volumeSlider == null)
            return;

        Transform parent = volumeSlider.transform.parent;
        if (parent == null)
            return;

        bool needsLayoutUpdate = (sfxVolumeSlider == null || cameraZoomSlider == null);
        if (!needsLayoutUpdate)
            return;

        RectTransform frameRt = parent as RectTransform;
        if (frameRt != null)
            frameRt.sizeDelta = new Vector2(frameRt.sizeDelta.x, Mathf.Max(frameRt.sizeDelta.y, 860f));

        SetChildAnchoredY(parent, "Title_Ajustes", 350f);
        SetChildAnchoredY(parent, "Label_Volumen", 260f);
        SetChildAnchoredY(parent, "Value_VolumenPct", 260f);
        SetChildAnchoredY(parent, "Slider_VolumenGeneral", 205f);

        Transform musicLabelTf = parent.Find("Label_Volumen");
        if (musicLabelTf != null)
        {
            Text musicLbl = musicLabelTf.GetComponent<Text>();
            if (musicLbl != null)
                musicLbl.text = "VOLUMEN DE MÚSICA";
        }

        if (sfxVolumeSlider == null)
        {
            CreateOrCloneLabel(parent, "Label_VolumenSfx", "Label_Volumen", "VOLUMEN DE EFECTOS", new Vector2(-130f, 125f), new Vector2(460f, 40f));
            sfxVolumeValueLabel = CreateOrCloneValueText(parent, "Value_VolumenSfxPct", volumeValueLabel, new Vector2(260f, 125f), new Vector2(140f, 40f), "85%");
            sfxVolumeSlider = CloneSliderClean(volumeSlider, parent, "Slider_VolumenSfx", new Vector2(0f, 70f), 0f, 1f);
        }
        else
        {
            SetChildAnchoredY(parent, "Label_VolumenSfx", 125f);
            SetChildAnchoredY(parent, "Value_VolumenSfxPct", 125f);
            SetChildAnchoredY(parent, "Slider_VolumenSfx", 70f);
        }

        if (cameraZoomSlider == null)
        {
            CreateOrCloneLabel(parent, "Label_CameraZoom", "Label_Volumen", "FOV / ZOOM DE CÁMARA", new Vector2(-110f, -10f), new Vector2(500f, 40f));
            cameraZoomValueLabel = CreateOrCloneValueText(parent, "Value_CameraZoom", volumeValueLabel, new Vector2(255f, -10f), new Vector2(220f, 40f), "100% (Normal)");
            cameraZoomSlider = CloneSliderClean(volumeSlider, parent, "Slider_CameraZoom", new Vector2(0f, -65f), LevelProgressionManager.MIN_CAMERA_ZOOM, LevelProgressionManager.MAX_CAMERA_ZOOM);
        }
        else
        {
            SetChildAnchoredY(parent, "Label_CameraZoom", -10f);
            SetChildAnchoredY(parent, "Value_CameraZoom", -10f);
            SetChildAnchoredY(parent, "Slider_CameraZoom", -65f);
        }

        SetChildAnchoredY(parent, "Toggle_PantallaCompleta", -165f);
        SetChildAnchoredY(parent, "Btn_GuardarYVolver", -315f);
    }

    private static void CreateOrCloneLabel(Transform parent, string newName, string templateName, string content, Vector2 pos, Vector2 size)
    {
        Transform existing = parent.Find(newName);
        if (existing != null)
        {
            RectTransform ert = existing as RectTransform;
            if (ert != null)
            {
                ert.anchoredPosition = pos;
                ert.sizeDelta = size;
            }
            Text et = existing.GetComponent<Text>();
            if (et != null) et.text = content;
            return;
        }

        Transform templateTf = parent.Find(templateName);
        if (templateTf != null)
        {
            GameObject clone = Object.Instantiate(templateTf.gameObject, parent);
            clone.name = newName;
            RectTransform rt = clone.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            Text txt = clone.GetComponent<Text>();
            if (txt != null)
                txt.text = content;
            return;
        }

        GameObject go = new GameObject(newName);
        go.transform.SetParent(parent, false);
        RectTransform grt = go.AddComponent<RectTransform>();
        grt.anchoredPosition = pos;
        grt.sizeDelta = size;
        Text gtxt = go.AddComponent<Text>();
        gtxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        gtxt.text = content;
        gtxt.fontSize = 24;
        gtxt.fontStyle = FontStyle.Bold;
        gtxt.alignment = TextAnchor.MiddleCenter;
        gtxt.color = new Color(0.965f, 0.940f, 0.875f, 1f);
    }

    private static Text CreateOrCloneValueText(Transform parent, string newName, Text templateText, Vector2 pos, Vector2 size, string initialText)
    {
        Transform existing = parent.Find(newName);
        if (existing != null)
        {
            RectTransform ert = existing as RectTransform;
            if (ert != null)
            {
                ert.anchoredPosition = pos;
                ert.sizeDelta = size;
            }
            return existing.GetComponent<Text>();
        }

        if (templateText != null)
        {
            GameObject clone = Object.Instantiate(templateText.gameObject, parent);
            clone.name = newName;
            RectTransform rt = clone.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return clone.GetComponent<Text>();
        }

        GameObject go = new GameObject(newName);
        go.transform.SetParent(parent, false);
        RectTransform grt = go.AddComponent<RectTransform>();
        grt.anchoredPosition = pos;
        grt.sizeDelta = size;
        Text txt = go.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = initialText;
        txt.fontSize = 24;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = new Color(0.955f, 0.785f, 0.340f, 1f);
        return txt;
    }

    private static Slider CloneSliderClean(Slider sourceSlider, Transform parent, string newName, Vector2 pos, float minVal, float maxVal)
    {
        Transform existing = parent.Find(newName);
        if (existing != null)
        {
            RectTransform ert = existing as RectTransform;
            if (ert != null)
                ert.anchoredPosition = pos;
            return existing.GetComponent<Slider>();
        }

        GameObject cloneObj = Object.Instantiate(sourceSlider.gameObject, parent);
        cloneObj.name = newName;
        RectTransform rt = cloneObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;

        Slider s = cloneObj.GetComponent<Slider>();
        if (s != null)
        {
            // Desvincular por completo los listeners serializados de volumen de música del prefab clonado
            s.onValueChanged = new Slider.SliderEvent();
            s.minValue = minVal;
            s.maxValue = maxVal;
            s.wholeNumbers = false;
        }
        return s;
    }

    private static void SetChildAnchoredY(Transform parent, string childName, float newY)
    {
        Transform child = parent.Find(childName);
        if (child == null)
            return;

        RectTransform rt = child as RectTransform;
        if (rt != null)
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, newY);
    }

    private void InitializeResolutions()
    {
        if (resolutionDropdown == null)
            return;

        availableResolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < availableResolutions.Length; i++)
        {
            string option = $"{availableResolutions[i].width} x {availableResolutions[i].height}";
            options.Add(option);

            if (availableResolutions[i].width == Screen.currentResolution.width &&
                availableResolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    public void LoadSettings()
    {
        float savedMusicVol = LevelProgressionManager.GetMusicVolume();
        float savedSfxVol = LevelProgressionManager.GetSfxVolume();
        bool isFullscreen = PlayerPrefs.GetInt(LevelProgressionManager.FULLSCREEN_KEY, 1) == 1;
        float savedZoom = LevelProgressionManager.GetCameraZoom();

        AudioListener.volume = 1f;
        ProceduralSoundManager.Instance?.RefreshVolumesFromPrefs();
        Screen.fullScreen = isFullscreen;
        CameraFollow.ApplyZoomToActiveCamera(savedZoom, true);

        if (volumeSlider != null)
            volumeSlider.SetValueWithoutNotify(savedMusicVol);

        if (volumeValueLabel != null)
            volumeValueLabel.text = $"{Mathf.RoundToInt(savedMusicVol * 100f)}%";

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.SetValueWithoutNotify(savedSfxVol);

        if (sfxVolumeValueLabel != null)
            sfxVolumeValueLabel.text = $"{Mathf.RoundToInt(savedSfxVol * 100f)}%";

        if (cameraZoomSlider != null)
            cameraZoomSlider.SetValueWithoutNotify(savedZoom);

        if (cameraZoomValueLabel != null)
            cameraZoomValueLabel.text = LevelProgressionManager.FormatCameraZoomLabel(savedZoom);

        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(isFullscreen);
    }

    public void SetVolume(float volume)
    {
        SetMusicVolume(volume);
    }

    public void SetMusicVolume(float volume)
    {
        LevelProgressionManager.SetMusicVolume(volume);
        float clamped = LevelProgressionManager.GetMusicVolume();
        ProceduralSoundManager.Instance?.SetMusicVolume(clamped);

        if (volumeValueLabel != null)
            volumeValueLabel.text = $"{Mathf.RoundToInt(clamped * 100f)}%";
    }

    public void SetSfxVolume(float volume)
    {
        LevelProgressionManager.SetSfxVolume(volume);
        float clamped = LevelProgressionManager.GetSfxVolume();
        ProceduralSoundManager.Instance?.SetSfxVolume(clamped);

        if (sfxVolumeValueLabel != null)
            sfxVolumeValueLabel.text = $"{Mathf.RoundToInt(clamped * 100f)}%";

        if (Time.unscaledTime >= nextSfxPreviewTime)
        {
            nextSfxPreviewTime = Time.unscaledTime + 0.22f;
            ProceduralSoundManager.Instance?.PlayButtonClick();
        }
    }

    public void SetCameraZoom(float zoomScale)
    {
        LevelProgressionManager.SetCameraZoom(zoomScale);
        float clamped = LevelProgressionManager.GetCameraZoom();
        CameraFollow.ApplyZoomToActiveCamera(clamped, true);

        if (cameraZoomValueLabel != null)
            cameraZoomValueLabel.text = LevelProgressionManager.FormatCameraZoomLabel(clamped);
    }

    public void ResetCameraZoom()
    {
        SetCameraZoom(LevelProgressionManager.DEFAULT_CAMERA_ZOOM);
        if (cameraZoomSlider != null)
            cameraZoomSlider.SetValueWithoutNotify(LevelProgressionManager.DEFAULT_CAMERA_ZOOM);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(LevelProgressionManager.FULLSCREEN_KEY, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetResolution(int resolutionIndex)
    {
        if (availableResolutions == null || resolutionIndex < 0 || resolutionIndex >= availableResolutions.Length)
            return;

        Resolution res = availableResolutions[resolutionIndex];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
    }
}
