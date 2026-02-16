using UnityEngine;

/// <summary>
/// ECHOES - Speaker Switch (HoparlorSwitch)
/// Attach to the HoparlorSwitch object. When the player presses E,
/// all speakers in the scene are turned off with a fade-out effect.
/// Implements IInteractable for the existing interaction system.
/// 
/// Can optionally toggle (press E again to turn back on).
/// </summary>
public class SpeakerSwitch : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    [Tooltip("Allow toggling speakers back on with E")]
    [SerializeField] private bool allowToggle = false;

    [Tooltip("Fade duration when turning off")]
    [Range(0.1f, 3f)]
    [SerializeField] private float fadeOutDuration = 1.5f;

    [Tooltip("Fade duration when turning on")]
    [Range(0.1f, 3f)]
    [SerializeField] private float fadeInDuration = 1f;

    [Header("Visual Feedback")]
    [Tooltip("Change material color when switched")]
    [SerializeField] private bool changeColor = true;
    [SerializeField] private Color offColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    [Header("Sound")]
    [SerializeField] private AudioClip switchSound;

    private bool speakersOff = false;
    private SpeakerStaticNoise[] allSpeakers;
    private Renderer objectRenderer;
    private Color originalColor;
    private AudioSource audioSource;

    void Start()
    {
        allSpeakers = FindObjectsOfType<SpeakerStaticNoise>();
        objectRenderer = GetComponentInChildren<Renderer>();

        if (objectRenderer != null && objectRenderer.material.HasProperty("_Color"))
        {
            originalColor = objectRenderer.material.color;
        }

        // Setup audio for switch click
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.minDistance = 0.5f;
        audioSource.maxDistance = 8f;

        // Generate a simple click sound if none assigned
        if (switchSound == null)
        {
            switchSound = GenerateClickSound();
        }

        Debug.Log("[SpeakerSwitch] Initialized - found " + allSpeakers.Length + " speakers");
    }

    public void Interact()
    {
        if (!speakersOff)
        {
            TurnOffAllSpeakers();
        }
        else if (allowToggle)
        {
            TurnOnAllSpeakers();
        }
    }

    void TurnOffAllSpeakers()
    {
        speakersOff = true;

        // Play switch sound
        if (switchSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(switchSound, 0.5f);
        }

        // Turn off all speakers with staggered fade
        for (int i = 0; i < allSpeakers.Length; i++)
        {
            if (allSpeakers[i] != null)
            {
                float stagger = i * 0.15f; // Each speaker fades slightly later
                StartCoroutine(DelayedTurnOff(allSpeakers[i], stagger));
            }
        }

        // Visual feedback
        if (changeColor && objectRenderer != null && objectRenderer.material.HasProperty("_Color"))
        {
            objectRenderer.material.color = offColor;
        }

        // Show UI feedback
        if (InteractionUI.Instance != null)
        {
            InteractionUI.Instance.ShowPrompt("Hoparlorler kapatildi");
        }

        Debug.Log("[SpeakerSwitch] All speakers turned OFF");
    }

    void TurnOnAllSpeakers()
    {
        speakersOff = false;

        if (switchSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(switchSound, 0.5f);
        }

        for (int i = 0; i < allSpeakers.Length; i++)
        {
            if (allSpeakers[i] != null)
            {
                float stagger = i * 0.1f;
                StartCoroutine(DelayedTurnOn(allSpeakers[i], stagger));
            }
        }

        if (changeColor && objectRenderer != null && objectRenderer.material.HasProperty("_Color"))
        {
            objectRenderer.material.color = originalColor;
        }

        if (InteractionUI.Instance != null)
        {
            InteractionUI.Instance.ShowPrompt("Hoparlorler acildi");
        }

        Debug.Log("[SpeakerSwitch] All speakers turned ON");
    }

    System.Collections.IEnumerator DelayedTurnOff(SpeakerStaticNoise speaker, float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);
        speaker.TurnOff(fadeOutDuration);
    }

    System.Collections.IEnumerator DelayedTurnOn(SpeakerStaticNoise speaker, float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);
        speaker.TurnOn(fadeInDuration);
    }

    AudioClip GenerateClickSound()
    {
        int sampleRate = 44100;
        int samples = (int)(sampleRate * 0.08f);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float click = Mathf.Sin(2f * Mathf.PI * 1800f * t) * 0.4f * Mathf.Exp(-t * 80f);
            click += Mathf.Sin(2f * Mathf.PI * 600f * t) * 0.3f * Mathf.Exp(-t * 40f);
            data[i] = click * 0.5f;
        }

        AudioClip clip = AudioClip.Create("SwitchClick", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public string GetInteractionPrompt()
    {
        if (speakersOff && allowToggle)
            return "[E] Hoparloleri Ac";
        else if (speakersOff)
            return "Hoparlorler kapali";
        else
            return "[E] Hoparloleri Kapat";
    }
}
