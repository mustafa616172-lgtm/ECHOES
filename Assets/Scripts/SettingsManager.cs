using UnityEngine;

/// <summary>
/// ECHOES - Settings Manager
/// Oyun ayarlarini yonetir ve kayit eder (PlayerPrefs).
/// Singleton pattern - sahneler arasi korunur.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    private static SettingsManager _instance;
    public static SettingsManager Instance
    {
        get { return _instance; }
    }
    
    private int graphicsQuality = 1;
    private float masterVolume = 1f;
    private float musicVolume = 0.8f;
    private float sfxVolume = 0.8f;
    private float mouseSensitivity = 2f;
    
    public int GraphicsQuality => graphicsQuality;
    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;
    public float MouseSensitivity => mouseSensitivity;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        LoadSettings();
        Debug.Log("[SettingsManager] Initialized");
    }
    
    public void SetGraphicsQuality(int level)
    {
        graphicsQuality = Mathf.Clamp(level, 0, 2);
        QualitySettings.SetQualityLevel(graphicsQuality);
        Debug.Log("[Settings] Graphics: " + graphicsQuality);
    }
    
    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        AudioListener.volume = masterVolume;
        Debug.Log("[Settings] Master Volume: " + masterVolume);
    }
    
    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
    }
    
    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
    }
    
    public void SetMouseSensitivity(float value)
    {
        mouseSensitivity = Mathf.Clamp(value, 0.5f, 5f);
        Debug.Log("[Settings] Mouse Sensitivity: " + mouseSensitivity);
    }
    
    public void SaveSettings()
    {
        PlayerPrefs.SetInt("GraphicsQuality", graphicsQuality);
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SfxVolume", sfxVolume);
        PlayerPrefs.SetFloat("MouseSensitivity", mouseSensitivity);
        PlayerPrefs.Save();
        
        Debug.Log("[SettingsManager] Settings saved");
    }
    
    public void LoadSettings()
    {
        graphicsQuality = PlayerPrefs.GetInt("GraphicsQuality", 1);
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        sfxVolume = PlayerPrefs.GetFloat("SfxVolume", 0.8f);
        mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 2f);
        
        SetGraphicsQuality(graphicsQuality);
        SetMasterVolume(masterVolume);
        
        Debug.Log("[SettingsManager] Settings loaded");
    }
}
