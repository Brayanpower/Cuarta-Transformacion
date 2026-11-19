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
    public Slider cameraZoomSlider;
    public Text cameraZoomValueLabel;
    public Toggle fullscreenToggle;
    public Dropdown resolutionDropdown;

    private Resolution[] availableResolutions;

    private void OnEnable()
    {
        LoadSettings();
    }

    private void Start()
    {
        EnsureCameraZoomControlExists();
        InitializeResolutions();
        LoadSettings();

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(SetVolume);
            volumeSlider.onValueChanged.AddListener(SetVolume);
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

    private void EnsureCameraZoomControlExists()
    {
        if (cameraZoomSlider != null || volumeSlider == null)
            return;

        Transform parent = volumeSlider.transform.parent;
        if (parent == null)
            return;

        RectTransform frameRt = parent as RectTransform;
        if (frameRt != null)
            frameRt.sizeDelta = new Vector2(frameRt.sizeDelta.x, Mathf.Max(frameRt.sizeDelta.y, 760f));

        SetChildAnchoredY(parent, "Title_Ajustes", 295f);
        SetChildAnchoredY(parent, "Label_Volumen", 190f);
        SetChildAnchoredY(parent, "Value_VolumenPct", 190f);
        SetChildAnchoredY(parent, "Slider_VolumenGeneral", 130f);
        SetChildAnchoredY(parent, "Toggle_PantallaCompleta", -115f);
        SetChildAnchoredY(parent, "Btn_GuardarYVolver", -255f);

        Transform volLabelTf = parent.Find("Label_Volumen");
        if (volLabelTf != null)
        {
            GameObject zoomLabelObj = Object.Instantiate(volLabelTf.gameObject, parent);
            zoomLabelObj.name = "Label_CameraZoom";
            RectTransform zLblRt = zoomLabelObj.GetComponent<RectTransform>();
            zLblRt.anchoredPosition = new Vector2(-110f, 42f);
            zLblRt.sizeDelta = new Vector2(500f, 40f);
            Text zLbl = zoomLabelObj.GetComponent<Text>();
            if (zLbl != null)
                zLbl.text = "FOV / ZOOM DE CÁMARA";
        }
        else
        {
            GameObject zoomLabelObj = new GameObject("Label_CameraZoom");
            zoomLabelObj.transform.SetParent(parent, false);
            RectTransform zLblRt = zoomLabelObj.AddComponent<RectTransform>();
            zLblRt.anchoredPosition = new Vector2(-110f, 42f);
            zLblRt.sizeDelta = new Vector2(500f, 40f);
            Text zLbl = zoomLabelObj.AddComponent<Text>();
            zLbl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            zLbl.text = "FOV / ZOOM DE CÁMARA";
            zLbl.fontSize = 24;
            zLbl.fontStyle = FontStyle.Bold;
            zLbl.alignment = TextAnchor.MiddleCenter;
            zLbl.color = new Color(0.965f, 0.940f, 0.875f, 1f);
        }

        if (volumeValueLabel != null)
        {
            GameObject zoomValObj = Object.Instantiate(volumeValueLabel.gameObject, parent);
            zoomValObj.name = "Value_CameraZoom";
            RectTransform zValRt = zoomValObj.GetComponent<RectTransform>();
            zValRt.anchoredPosition = new Vector2(255f, 42f);
            zValRt.sizeDelta = new Vector2(220f, 40f);
            cameraZoomValueLabel = zoomValObj.GetComponent<Text>();
        }
        else
        {
            GameObject zoomValObj = new GameObject("Value_CameraZoom");
            zoomValObj.transform.SetParent(parent, false);
            RectTransform zValRt = zoomValObj.AddComponent<RectTransform>();
            zValRt.anchoredPosition = new Vector2(255f, 42f);
            zValRt.sizeDelta = new Vector2(220f, 40f);
            cameraZoomValueLabel = zoomValObj.AddComponent<Text>();
            cameraZoomValueLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cameraZoomValueLabel.fontSize = 24;
            cameraZoomValueLabel.fontStyle = FontStyle.Bold;
            cameraZoomValueLabel.alignment = TextAnchor.MiddleCenter;
            cameraZoomValueLabel.color = new Color(0.955f, 0.785f, 0.340f, 1f);
        }

        GameObject zoomSliderObj = Object.Instantiate(volumeSlider.gameObject, parent);
        zoomSliderObj.name = "Slider_CameraZoom";
        RectTransform zSliderRt = zoomSliderObj.GetComponent<RectTransform>();
        zSliderRt.anchoredPosition = new Vector2(0f, -18f);

        Slider clonedSlider = zoomSliderObj.GetComponent<Slider>();
        if (clonedSlider != null)
        {
            // Desvincular por completo los listeners serializados de volumen del prefab clonado
            clonedSlider.onValueChanged = new Slider.SliderEvent();
            clonedSlider.minValue = LevelProgressionManager.MIN_CAMERA_ZOOM;
            clonedSlider.maxValue = LevelProgressionManager.MAX_CAMERA_ZOOM;
            clonedSlider.wholeNumbers = false;
            cameraZoomSlider = clonedSlider;
        }
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
        float savedVolume = PlayerPrefs.GetFloat(LevelProgressionManager.VOLUME_KEY, 0.85f);
        bool isFullscreen = PlayerPrefs.GetInt(LevelProgressionManager.FULLSCREEN_KEY, 1) == 1;
        float savedZoom = LevelProgressionManager.GetCameraZoom();

        AudioListener.volume = savedVolume;
        Screen.fullScreen = isFullscreen;
        CameraFollow.ApplyZoomToActiveCamera(savedZoom, true);

        if (volumeSlider != null)
            volumeSlider.SetValueWithoutNotify(savedVolume);

        if (volumeValueLabel != null)
            volumeValueLabel.text = $"{Mathf.RoundToInt(savedVolume * 100f)}%";

        if (cameraZoomSlider != null)
            cameraZoomSlider.SetValueWithoutNotify(savedZoom);

        if (cameraZoomValueLabel != null)
            cameraZoomValueLabel.text = LevelProgressionManager.FormatCameraZoomLabel(savedZoom);

        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(isFullscreen);
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(LevelProgressionManager.VOLUME_KEY, AudioListener.volume);
        PlayerPrefs.Save();

        if (volumeValueLabel != null)
            volumeValueLabel.text = $"{Mathf.RoundToInt(AudioListener.volume * 100f)}%";
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
