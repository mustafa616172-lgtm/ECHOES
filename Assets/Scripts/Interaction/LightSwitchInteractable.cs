using UnityEngine;

public class LightSwitchInteractable : MonoBehaviour, IInteractable
{
    [Header("Light Settings")]
    public Transform lightSourceTransform;
    public Color lightColor = new Color(1f, 0.95f, 0.8f, 1f);
    public float lightIntensity = 1.5f;
    public float lightRange = 12f;
    public bool startOn = false;

    [Header("Sound Effects")]
    public AudioClip switchOnSound;
    public AudioClip switchOffSound;
    [Range(0f, 1f)]
    public float volume = 0.4f;

    [Header("Prompts")]
    public string turnOnPrompt = "[E] Turn On Light";
    public string turnOffPrompt = "[E] Turn Off Light";

    [Header("Visual Feedback")]
    public MeshRenderer switchRenderer;
    public Color switchOnColor = new Color(0.2f, 1f, 0.2f);
    public Color switchOffColor = new Color(0.3f, 0.3f, 0.3f);

    private bool isOn = false;
    private Light roomLight;
    private AudioSource audioSource;
    private AudioClip proceduralOnSound;
    private AudioClip proceduralOffSound;
    private Material switchMaterial;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 10f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;

        GenerateProceduralSounds();
        SetupLight();

        if (switchRenderer != null)
        {
            switchMaterial = switchRenderer.material;
        }

        if (startOn)
        {
            isOn = true;
            if (roomLight != null) roomLight.enabled = true;
            UpdateSwitchVisual();
        }
    }

    private void SetupLight()
    {
        if (lightSourceTransform == null)
        {
            Debug.LogWarning("[LightSwitch] No light source transform assigned! Assign the Lamp object.");
            return;
        }

        roomLight = lightSourceTransform.GetComponent<Light>();
        if (roomLight == null)
        {
            roomLight = lightSourceTransform.gameObject.AddComponent<Light>();
        }

        roomLight.type = LightType.Point;
        roomLight.color = lightColor;
        roomLight.intensity = lightIntensity;
        roomLight.range = lightRange;
        roomLight.shadows = LightShadows.Soft;
        roomLight.shadowStrength = 0.6f;
        roomLight.enabled = startOn;
    }

    private void GenerateProceduralSounds()
    {
        int sampleRate = 44100;

        // Switch ON: sharp plastic click
        float onDuration = 0.08f;
        int onSamples = (int)(sampleRate * onDuration);
        proceduralOnSound = AudioClip.Create("SwitchOn", onSamples, 1, sampleRate, false);
        float[] onData = new float[onSamples];

        for (int i = 0; i < onSamples; i++)
        {
            float t = (float)i / onSamples;
            float envelope = Mathf.Exp(-t * 50f);
            float click = Mathf.Sin(2f * Mathf.PI * 2000f * t) * 0.3f;
            click += Mathf.Sin(2f * Mathf.PI * 800f * t) * 0.4f;
            float snap = Mathf.Sin(2f * Mathf.PI * 150f * t) * 0.2f * Mathf.Exp(-t * 80f);
            float noise = (Mathf.PerlinNoise(t * 1000f, 0f) - 0.5f) * 0.05f;
            onData[i] = (click + snap + noise) * envelope * 0.5f;
        }
        proceduralOnSound.SetData(onData, 0);

        // Switch OFF: slightly lower pitched click
        float offDuration = 0.07f;
        int offSamples = (int)(sampleRate * offDuration);
        proceduralOffSound = AudioClip.Create("SwitchOff", offSamples, 1, sampleRate, false);
        float[] offData = new float[offSamples];

        for (int i = 0; i < offSamples; i++)
        {
            float t = (float)i / offSamples;
            float envelope = Mathf.Exp(-t * 55f);
            float click = Mathf.Sin(2f * Mathf.PI * 1500f * t) * 0.3f;
            click += Mathf.Sin(2f * Mathf.PI * 600f * t) * 0.4f;
            float snap = Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.25f * Mathf.Exp(-t * 70f);
            float noise = (Mathf.PerlinNoise(t * 800f, 1f) - 0.5f) * 0.04f;
            offData[i] = (click + snap + noise) * envelope * 0.45f;
        }
        proceduralOffSound.SetData(offData, 0);
    }

    public void Interact()
    {
        isOn = !isOn;

        if (roomLight != null)
        {
            roomLight.enabled = isOn;
        }

        AudioClip clip;
        if (isOn)
            clip = switchOnSound != null ? switchOnSound : proceduralOnSound;
        else
            clip = switchOffSound != null ? switchOffSound : proceduralOffSound;

        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }

        UpdateSwitchVisual();
    }

    public string GetInteractionPrompt()
    {
        return isOn ? turnOffPrompt : turnOnPrompt;
    }

    private void UpdateSwitchVisual()
    {
        if (switchMaterial != null)
        {
            if (isOn)
            {
                switchMaterial.color = switchOnColor;
                switchMaterial.SetColor("_EmissionColor", switchOnColor * 0.5f);
                switchMaterial.EnableKeyword("_EMISSION");
            }
            else
            {
                switchMaterial.color = switchOffColor;
                switchMaterial.SetColor("_EmissionColor", Color.black);
                switchMaterial.DisableKeyword("_EMISSION");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (lightSourceTransform != null)
        {
            Gizmos.color = isOn ? Color.yellow : Color.gray;
            Gizmos.DrawWireSphere(lightSourceTransform.position, lightRange);
            Gizmos.DrawLine(transform.position, lightSourceTransform.position);
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(lightSourceTransform.position, 0.1f);
        }
    }
}
