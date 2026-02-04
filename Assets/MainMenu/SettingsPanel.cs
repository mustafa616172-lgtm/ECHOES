using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ECHOES - Settings Panel
/// Oyun ayarlarini yonetir: grafik, ses, fare hassasiyeti.
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    [Header("Graphics Buttons")]
    [SerializeField] private Button graphicsFastButton;
    [SerializeField] private Button graphicsNormalButton;
    [SerializeField] private Button graphicsHighButton;
    
    [Header("Audio Sliders")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    
    [Header("Mouse Sensitivity")]
    [SerializeField] private Slider mouseSensitivitySlider;
    
    [Header("Buttons")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button backButton;
    
    [Header("Menu Reference")]
    [SerializeField] private MenuManager menuManager;
    
    private Color selectedColor = new Color(0.3f, 0.8f, 0.3f);
    private Color unselectedColor = Color.white;
    
    private int tempGraphicsLevel;
    private float tempMasterVolume;
    private float tempMusicVolume;
    private float tempSfxVolume;
    private float tempMouseSensitivity;

    void Start()
    {
        SetupButtons();
        SetupSliders();
        LoadCurrentSettings();
    }
    
    void SetupButtons()
    {
        if (graphicsFastButton != null)
            graphicsFastButton.onClick.AddListener(() => SetGraphicsLevel(0));
        if (graphicsNormalButton != null)
            graphicsNormalButton.onClick.AddListener(() => SetGraphicsLevel(1));
        if (graphicsHighButton != null)
            graphicsHighButton.onClick.AddListener(() => SetGraphicsLevel(2));
        
        if (saveButton != null)
            saveButton.onClick.AddListener(OnSaveClicked);
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
    }
    
    void SetupSliders()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0;
            masterVolumeSlider.maxValue = 1;
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0;
            musicVolumeSlider.maxValue = 1;
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0;
            sfxVolumeSlider.maxValue = 1;
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }
        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.minValue = 0.5f;
            mouseSensitivitySlider.maxValue = 5f;
            mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
        }
    }
    
    void LoadCurrentSettings()
    {
        if (SettingsManager.Instance == null) return;
        
        tempGraphicsLevel = SettingsManager.Instance.GraphicsQuality;
        tempMasterVolume = SettingsManager.Instance.MasterVolume;
        tempMusicVolume = SettingsManager.Instance.MusicVolume;
        tempSfxVolume = SettingsManager.Instance.SfxVolume;
        tempMouseSensitivity = SettingsManager.Instance.MouseSensitivity;
        
        UpdateUI();
    }
    
    void UpdateUI()
    {
        UpdateGraphicsButtons();
        
        if (masterVolumeSlider != null) masterVolumeSlider.value = tempMasterVolume;
        if (musicVolumeSlider != null) musicVolumeSlider.value = tempMusicVolume;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = tempSfxVolume;
        if (mouseSensitivitySlider != null) mouseSensitivitySlider.value = tempMouseSensitivity;
    }
    
    void UpdateGraphicsButtons()
    {
        if (graphicsFastButton != null)
            graphicsFastButton.GetComponent<Image>().color = tempGraphicsLevel == 0 ? selectedColor : unselectedColor;
        if (graphicsNormalButton != null)
            graphicsNormalButton.GetComponent<Image>().color = tempGraphicsLevel == 1 ? selectedColor : unselectedColor;
        if (graphicsHighButton != null)
            graphicsHighButton.GetComponent<Image>().color = tempGraphicsLevel == 2 ? selectedColor : unselectedColor;
    }
    
    void SetGraphicsLevel(int level)
    {
        tempGraphicsLevel = level;
        UpdateGraphicsButtons();
        
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.SetGraphicsQuality(level);
    }
    
    void OnMasterVolumeChanged(float value)
    {
        tempMasterVolume = value;
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.SetMasterVolume(value);
    }
    
    void OnMusicVolumeChanged(float value)
    {
        tempMusicVolume = value;
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.SetMusicVolume(value);
    }
    
    void OnSfxVolumeChanged(float value)
    {
        tempSfxVolume = value;
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.SetSfxVolume(value);
    }
    
    void OnMouseSensitivityChanged(float value)
    {
        tempMouseSensitivity = value;
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.SetMouseSensitivity(value);
    }
    
    void OnSaveClicked()
    {
        Debug.Log("[SettingsPanel] Settings saved");
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.SaveSettings();
        
        GoBack();
    }
    
    void OnBackClicked()
    {
        LoadCurrentSettings();
        GoBack();
    }
    
    void GoBack()
    {
        if (menuManager != null)
        {
            menuManager.CloseSettings();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
