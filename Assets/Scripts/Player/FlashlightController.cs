using UnityEngine;
using System.Collections;

public class FlashlightController : MonoBehaviour
{
    [Header("Işık Ayarları")]
    public Transform lightOrigin;   // Işığın çıkacağı nokta (modelin ucu)
    private Light flashlight;       // Otomatik oluşturulacak tek ışık
    
    [Header("Tuş Ayarları")]
    public KeyCode toggleKey = KeyCode.L;
    
    [Header("Batarya Ayarları")]
    public float maxBattery = 100f;
    public float batteryDrainRate = 2f;
    public float currentBattery;
    public bool hasInfiniteBattery = false;
    
    [Header("Animasyon Ayarları")]
    public float turnOnSpeed = 8f;
    public float flickerChance = 0.3f;
    public AudioClip toggleOnSound;
    public AudioClip toggleOffSound;
    public AudioClip flickerSound;
    
    [Header("The Forest Tarzı Ayarlar")]
    public float spotAngle = 70f;
    public float spotRange = 20f;
    public float lightIntensity = 2f;
    public Color lightColor = new Color(1f, 0.95f, 0.8f);
    
    private bool isOn = false;
    private float targetIntensity;
    private float currentIntensity;
    private AudioSource audioSource;
    private Coroutine flickerCoroutine;
    
    void Start()
    {
        currentBattery = maxBattery;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        // Işığı otomatik oluştur
        CreateFlashlight();
        
        // Başlangıçta kapalı
        currentIntensity = 0f;
        flashlight.intensity = 0f;
        isOn = false;
    }
    
    void CreateFlashlight()
    {
        // Işık çıkış noktasını bul veya oluştur
        if (lightOrigin == null)
        {
            // Child objelerde ara
            lightOrigin = transform.Find("LightOrigin");
            if (lightOrigin == null)
                lightOrigin = transform.Find("Tip");
            if (lightOrigin == null)
                lightOrigin = transform.Find("light");
            
            // Hala bulunamadıysa yeni oluştur
            if (lightOrigin == null)
            {
                GameObject originObj = new GameObject("LightOrigin");
                originObj.transform.SetParent(transform);
                // Fenerin ucuna yerleştir (varsayılan olarak Z ekseninde ileri)
                originObj.transform.localPosition = new Vector3(0, 0, 0.2f);
                originObj.transform.localRotation = Quaternion.identity;
                lightOrigin = originObj.transform;
            }
        }
        
        // Eğer hali hazırda Light componenti varsa onu kullan, yoksa oluştur
        flashlight = lightOrigin.GetComponent<Light>();
        if (flashlight == null)
        {
            flashlight = lightOrigin.gameObject.AddComponent<Light>();
        }
        
        // Spot ışık ayarları
        flashlight.type = LightType.Spot;
        flashlight.spotAngle = spotAngle;
        flashlight.range = spotRange;
        flashlight.color = lightColor;
        flashlight.shadows = LightShadows.Soft;
        flashlight.intensity = 0f;
        
        Debug.Log("Fener ışığı otomatik oluşturuldu: " + lightOrigin.name);
    }
    
    void Update()
    {
        // L tuşu ile aç/kapa
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleFlashlight();
        }
        
        // Batarya yönetimi
        if (isOn && !hasInfiniteBattery)
        {
            DrainBattery();
        }
        
        // Yumuşak geçiş animasyonu
        AnimateLight();
    }
    
    void ToggleFlashlight()
    {
        if (currentBattery <= 0 && !isOn)
        {
            PlaySound(flickerSound);
            StartCoroutine(DeadBatteryFlicker());
            return;
        }
        
        isOn = !isOn;
        targetIntensity = isOn ? lightIntensity : 0f;
        
        PlaySound(isOn ? toggleOnSound : toggleOffSound);
        
        // Titreme efekti
        if (isOn && currentBattery < maxBattery * 0.2f && flickerCoroutine == null)
        {
            flickerCoroutine = StartCoroutine(BatteryFlicker());
        }
        else if (!isOn && flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            flickerCoroutine = null;
        }
    }
    
    void DrainBattery()
    {
        currentBattery -= batteryDrainRate * Time.deltaTime;
        currentBattery = Mathf.Clamp(currentBattery, 0, maxBattery);
        
        if (currentBattery <= 0)
        {
            currentBattery = 0;
            isOn = false;
            targetIntensity = 0f;
            PlaySound(flickerSound);
        }
    }
    
    void AnimateLight()
    {
        // Yumuşak intensity geçişi
        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * turnOnSpeed);
        flashlight.intensity = currentIntensity;
    }
    
    IEnumerator BatteryFlicker()
    {
        while (isOn && currentBattery > 0)
        {
            float flickerProbability = 1f - (currentBattery / maxBattery);
            
            if (Random.value < flickerProbability * 0.1f)
            {
                float originalIntensity = targetIntensity;
                targetIntensity = Random.Range(0.1f, originalIntensity);
                yield return new WaitForSeconds(Random.Range(0.05f, 0.2f));
                targetIntensity = originalIntensity;
                
                if (flickerSound != null && Random.value > 0.7f)
                    PlaySound(flickerSound);
            }
            
            yield return new WaitForSeconds(Random.Range(0.5f, 2f));
        }
    }
    
    IEnumerator DeadBatteryFlicker()
    {
        for (int i = 0; i < 3; i++)
        {
            flashlight.intensity = lightIntensity * 0.3f;
            yield return new WaitForSeconds(0.05f);
            flashlight.intensity = 0f;
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }
    
    public void RechargeBattery(float amount)
    {
        currentBattery = Mathf.Clamp(currentBattery + amount, 0, maxBattery);
    }
    
    public float GetBatteryPercentage()
    {
        return currentBattery / maxBattery;
    }
    
    void OnDrawGizmosSelected()
    {
        if (lightOrigin != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(lightOrigin.position, 0.05f);
            
            Gizmos.color = new Color(1, 1, 0, 0.2f);
            Vector3 direction = lightOrigin.forward * spotRange;
            Gizmos.DrawRay(lightOrigin.position, direction);
            
            // Spot açısını göster
            float angleRad = spotAngle * Mathf.Deg2Rad;
            Vector3 right = Quaternion.Euler(0, spotAngle/2, 0) * lightOrigin.forward;
            Vector3 left = Quaternion.Euler(0, -spotAngle/2, 0) * lightOrigin.forward;
            
            Gizmos.DrawLine(lightOrigin.position, lightOrigin.position + right * spotRange * 0.3f);
            Gizmos.DrawLine(lightOrigin.position, lightOrigin.position + left * spotRange * 0.3f);
        }
    }
}