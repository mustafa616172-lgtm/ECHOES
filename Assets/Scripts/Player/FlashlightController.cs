using UnityEngine;
using System.Collections;

/// <summary>
/// ECHOES - El Feneri Sistemi
/// Kamera ile tam senkronize, The Forest tarzi el feneri.
/// L tusu ile ac/kapa. Batarya sistemi, flicker efektleri.
/// Bu script PlayerCapsule uzerine eklenir, otomatik olarak
/// kameraya baglanir ve bakis yonunu takip eder.
/// </summary>
public class FlashlightController : MonoBehaviour
{
    [Header("Isik Ayarlari")]
    public Transform lightOrigin;
    private Light flashlight;

    [Header("Tus Ayarlari")]
    public KeyCode toggleKey = KeyCode.L;

    [Header("Batarya Ayarlari")]
    public float maxBattery = 100f;
    public float batteryDrainRate = 2f;
    public float currentBattery;
    public bool hasInfiniteBattery = false;

    [Header("Animasyon Ayarlari")]
    public float turnOnSpeed = 8f;
    public float flickerChance = 0.3f;
    public AudioClip toggleOnSound;
    public AudioClip toggleOffSound;
    public AudioClip flickerSound;

    [Header("The Forest Tarzi Ayarlar")]
    public float spotAngle = 70f;
    public float spotRange = 20f;
    public float lightIntensity = 2f;
    public Color lightColor = new Color(1f, 0.95f, 0.8f);

    [Header("Kamera Takip Ayarlari")]
    [Tooltip("Fenerin kameraya gore el pozisyonu offseti")]
    public Vector3 handOffset = new Vector3(0.25f, -0.2f, 0.4f);
    [Tooltip("Pozisyonel sway miktari (sadece konum, rotasyon degil)")]
    public float swayAmount = 0.01f;
    [Tooltip("Sway efektinin yumusakligi")]
    public float swaySmooth = 8f;

    [Header("Bob (Yurume Sallanma) Ayarlari")]
    [Tooltip("Yururken sallanma miktari")]
    public float bobAmount = 0.015f;
    [Tooltip("Yururken sallanma hizi")]
    public float bobSpeed = 10f;
    [Tooltip("Kosarken sallanma carpani")]
    public float runBobMultiplier = 1.5f;

    // State
    private bool isOn = false;
    private float targetIntensity;
    private float currentIntensity;
    private AudioSource audioSource;
    private Coroutine flickerCoroutine;

    // Kamera takip
    private Transform cameraTransform;
    private Vector3 currentSwayOffset;
    private float bobTimer;
    private Vector3 smoothFollowPosition;
    private CharacterController characterController;

    // Flashlight model referansi
    private Transform flashlightModel;
    private GameObject flashlightModelObj;

    // Onceki kamera rotasyonu (gercek kamera degisimini olcmek icin)
    private Quaternion lastCameraRotation;

    void Start()
    {
        currentBattery = maxBattery;

        // AudioSource ayarla
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // CharacterController referansini al
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
            characterController = GetComponentInParent<CharacterController>();

        // Kamerayi bul
        FindCamera();

        // El feneri modelini ve isigi kur
        SetupFlashlight();

        // Baslangicta kapali
        currentIntensity = 0f;
        if (flashlight != null)
            flashlight.intensity = 0f;
        isOn = false;

        if (cameraTransform != null)
            lastCameraRotation = cameraTransform.rotation;

        Debug.Log("[FlashlightController] Initialized - Camera: " + (cameraTransform != null ? cameraTransform.name : "NULL"));
    }

    void FindCamera()
    {
        // Once childlarda ara
        Camera cam = GetComponentInChildren<Camera>();
        if (cam != null)
        {
            cameraTransform = cam.transform;
            return;
        }

        // Ana kamerayi dene
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
            return;
        }

        // Sahnedeki herhangi bir kamerayi bul
        cam = FindObjectOfType<Camera>();
        if (cam != null)
        {
            cameraTransform = cam.transform;
        }

        if (cameraTransform == null)
        {
            Debug.LogWarning("[FlashlightController] Kamera bulunamadi! Fener kamerayi takip edemeyecek.");
        }
    }

    void SetupFlashlight()
    {
        if (cameraTransform == null)
        {
            CreateFlashlightOnSelf();
            return;
        }

        // Fener containerini kameranin childi olarak olustur
        Transform existingContainer = cameraTransform.Find("FlashlightContainer");
        if (existingContainer != null)
        {
            flashlightModel = existingContainer;
        }
        else
        {
            GameObject container = new GameObject("FlashlightContainer");
            container.transform.SetParent(cameraTransform);
            container.transform.localPosition = handOffset;
            container.transform.localRotation = Quaternion.identity;
            flashlightModel = container.transform;
        }

        // Fener 3D modelini yukle
        LoadFlashlightModel();

        // Isik kaynagini olustur
        SetupLight();

        // Smooth follow baslangic degerleri
        smoothFollowPosition = flashlightModel.localPosition;
    }

    void LoadFlashlightModel()
    {
        if (flashlightModel.childCount > 0) return;

        // Resourcesdan fener modelini yukle
        GameObject prefab = Resources.Load<GameObject>("flashlight/source/flashlightfbx");
        if (prefab != null)
        {
            flashlightModelObj = Instantiate(prefab, flashlightModel);
            flashlightModelObj.name = "FlashlightModel";
            flashlightModelObj.transform.localPosition = Vector3.zero;
            flashlightModelObj.transform.localRotation = Quaternion.identity;
            flashlightModelObj.transform.localScale = Vector3.one * 0.01f;
            Debug.Log("[FlashlightController] 3D fener modeli yuklendi");
        }
        else
        {
            flashlightModelObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flashlightModelObj.name = "FlashlightModel_TEMP";
            flashlightModelObj.transform.SetParent(flashlightModel);
            flashlightModelObj.transform.localPosition = Vector3.zero;
            flashlightModelObj.transform.localRotation = Quaternion.identity;
            flashlightModelObj.transform.localScale = new Vector3(0.04f, 0.04f, 0.2f);

            Collider col = flashlightModelObj.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer rend = flashlightModelObj.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mat != null)
                {
                    mat.color = new Color(0.15f, 0.15f, 0.15f);
                    rend.material = mat;
                }
            }
            Debug.Log("[FlashlightController] Gecici fener modeli olusturuldu");
        }
    }

    void SetupLight()
    {
        if (lightOrigin == null)
        {
            lightOrigin = flashlightModel.Find("LightOrigin");
            if (lightOrigin == null)
                lightOrigin = flashlightModel.Find("Tip");
            if (lightOrigin == null)
                lightOrigin = flashlightModel.Find("light");

            if (lightOrigin == null)
            {
                GameObject originObj = new GameObject("LightOrigin");
                originObj.transform.SetParent(flashlightModel);
                originObj.transform.localPosition = new Vector3(0, 0, 0.12f);
                originObj.transform.localRotation = Quaternion.identity;
                lightOrigin = originObj.transform;
            }
        }

        flashlight = lightOrigin.GetComponent<Light>();
        if (flashlight == null)
        {
            flashlight = lightOrigin.gameObject.AddComponent<Light>();
        }

        flashlight.type = LightType.Spot;
        flashlight.spotAngle = spotAngle;
        flashlight.range = spotRange;
        flashlight.color = lightColor;
        flashlight.shadows = LightShadows.Soft;
        flashlight.intensity = 0f;

        Debug.Log("[FlashlightController] Fener isigi hazir: " + lightOrigin.name);
    }

    void CreateFlashlightOnSelf()
    {
        if (lightOrigin == null)
        {
            lightOrigin = transform.Find("LightOrigin");
            if (lightOrigin == null)
            {
                GameObject originObj = new GameObject("LightOrigin");
                originObj.transform.SetParent(transform);
                originObj.transform.localPosition = new Vector3(0, 0, 0.2f);
                originObj.transform.localRotation = Quaternion.identity;
                lightOrigin = originObj.transform;
            }
        }

        flashlight = lightOrigin.GetComponent<Light>();
        if (flashlight == null)
            flashlight = lightOrigin.gameObject.AddComponent<Light>();

        flashlight.type = LightType.Spot;
        flashlight.spotAngle = spotAngle;
        flashlight.range = spotRange;
        flashlight.color = lightColor;
        flashlight.shadows = LightShadows.Soft;
        flashlight.intensity = 0f;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleFlashlight();
        }

        if (isOn && !hasInfiniteBattery)
        {
            DrainBattery();
        }

        AnimateLight();
    }

    void LateUpdate()
    {
        if (cameraTransform == null || flashlightModel == null) return;

        // ROTASYON: Her zaman kamera ile BIREBIR ayni yonde
        // localRotation = identity demek = kameranin tam bakis yonunde
        flashlightModel.localRotation = Quaternion.identity;

        // POZISYON: Hand offset + sadece pozisyonel sway + bob
        UpdatePositionalSway();
        UpdateBob();
        ApplyPosition();

        // Kamera rotasyonunu kaydet
        lastCameraRotation = cameraTransform.rotation;
    }

    void UpdatePositionalSway()
    {
        // Gercek kamera degisimini olc (mouse input degil!)
        // Boylece kamera sinira ulastiginda sway da durur
        Quaternion deltaRotation = cameraTransform.rotation * Quaternion.Inverse(lastCameraRotation);
        deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);

        // Aci farkini normalize et (-180 ile 180 arasi)
        if (angle > 180f) angle -= 360f;

        // Kamera hareket ettiyse sway uygula, etmediyse sifirla
        Vector3 targetSway = Vector3.zero;
        if (Mathf.Abs(angle) > 0.01f)
        {
            // Kameranin LOCAL eksenlerindeki degisimi hesapla
            Vector3 localAxis = cameraTransform.InverseTransformDirection(axis);
            float swayX = -localAxis.y * angle * swayAmount;  // Yatay donme -> yatay sway
            float swayY = localAxis.x * angle * swayAmount;   // Dikey donme -> dikey sway
            targetSway = new Vector3(swayX, swayY, 0);
        }

        currentSwayOffset = Vector3.Lerp(currentSwayOffset, targetSway, Time.deltaTime * swaySmooth);
    }

    void UpdateBob()
    {
        if (characterController == null) return;

        float speed = characterController.velocity.magnitude;
        bool isMoving = speed > 0.5f;
        bool isRunning = speed > 6f;

        if (isMoving && characterController.isGrounded)
        {
            float multiplier = isRunning ? runBobMultiplier : 1f;
            bobTimer += Time.deltaTime * bobSpeed * multiplier;
        }
        else
        {
            bobTimer = Mathf.Lerp(bobTimer, 0f, Time.deltaTime * 2f);
        }
    }

    void ApplyPosition()
    {
        // Bob offset hesapla
        float bobOffsetX = Mathf.Sin(bobTimer) * bobAmount * 0.5f;
        float bobOffsetY = Mathf.Cos(bobTimer * 2f) * bobAmount;

        // Hedef pozisyon = hand offset + sway (sadece pozisyonel) + bob
        Vector3 targetLocalPos = handOffset + currentSwayOffset + new Vector3(bobOffsetX, bobOffsetY, 0);

        // Smooth pozisyon takibi
        smoothFollowPosition = Vector3.Lerp(smoothFollowPosition, targetLocalPos, Time.deltaTime * 15f);
        flashlightModel.localPosition = smoothFollowPosition;
    }

    // ===== TOGGLE & BATARYA SISTEMI =====

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
        if (flashlight == null) return;
        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * turnOnSpeed);
        flashlight.intensity = currentIntensity;
    }

    // ===== EFEKTLER =====

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
            if (flashlight != null)
            {
                flashlight.intensity = lightIntensity * 0.3f;
                yield return new WaitForSeconds(0.05f);
                flashlight.intensity = 0f;
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    // ===== PUBLIC API =====

    public void RechargeBattery(float amount)
    {
        currentBattery = Mathf.Clamp(currentBattery + amount, 0, maxBattery);
    }

    public float GetBatteryPercentage()
    {
        return currentBattery / maxBattery;
    }

    public bool IsOn => isOn;

    // ===== GIZMOS =====

    void OnDrawGizmosSelected()
    {
        if (lightOrigin != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(lightOrigin.position, 0.05f);

            Gizmos.color = new Color(1, 1, 0, 0.3f);
            Vector3 direction = lightOrigin.forward * spotRange;
            Gizmos.DrawRay(lightOrigin.position, direction);

            Vector3 right = Quaternion.Euler(0, spotAngle / 2, 0) * lightOrigin.forward;
            Vector3 left = Quaternion.Euler(0, -spotAngle / 2, 0) * lightOrigin.forward;

            Gizmos.DrawLine(lightOrigin.position, lightOrigin.position + right * spotRange * 0.3f);
            Gizmos.DrawLine(lightOrigin.position, lightOrigin.position + left * spotRange * 0.3f);
        }

        if (cameraTransform != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 expectedPos = cameraTransform.TransformPoint(handOffset);
            Gizmos.DrawWireSphere(expectedPos, 0.03f);
            Gizmos.DrawLine(cameraTransform.position, expectedPos);
        }
    }
}