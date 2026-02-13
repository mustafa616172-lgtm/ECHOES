using UnityEngine;

public class DrawerInteractable : MonoBehaviour, IInteractable
{
    [Header("Drawer Settings")]
    public Vector3 openOffset = new Vector3(0f, 0f, -0.4f);
    public float moveSpeed = 3f;

    [Header("Sound Effects")]
    public AudioClip openSound;
    public AudioClip closeSound;
    [Range(0f, 1f)]
    public float volume = 0.35f;

    [Header("Prompts")]
    public string openPrompt = "[E] Open Drawer";
    public string closePrompt = "[E] Close Drawer";

    private bool isOpen = false;
    private bool isMoving = false;
    private Vector3 closedPosition;
    private Vector3 openPosition;
    private AudioSource audioSource;

    private AudioClip proceduralOpenSound;
    private AudioClip proceduralCloseSound;
    private AudioClip slideLoopSound;
    private AudioSource slideSource;
    private float slideVolumeTarget = 0f;

    private void Awake()
    {
        closedPosition = transform.localPosition;
        openPosition = closedPosition + openOffset;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 8f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;

        slideSource = gameObject.AddComponent<AudioSource>();
        slideSource.playOnAwake = false;
        slideSource.spatialBlend = 1f;
        slideSource.minDistance = 1f;
        slideSource.maxDistance = 6f;
        slideSource.rolloffMode = AudioRolloffMode.Linear;
        slideSource.loop = true;
        slideSource.volume = 0f;

        GenerateProceduralSounds();
    }

    private void GenerateProceduralSounds()
    {
        int sampleRate = 44100;

        // OPEN SOUND: Soft wood knock
        float openDuration = 0.15f;
        int openSamples = (int)(sampleRate * openDuration);
        proceduralOpenSound = AudioClip.Create("DrawerOpen", openSamples, 1, sampleRate, false);
        float[] openData = new float[openSamples];

        for (int i = 0; i < openSamples; i++)
        {
            float t = (float)i / openSamples;
            float envelope = Mathf.Exp(-t * 25f);

            float knock = Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.5f;
            knock += Mathf.Sin(2f * Mathf.PI * 85f * t) * 0.3f;
            float creak = Mathf.Sin(2f * Mathf.PI * 350f * t + Mathf.Sin(t * 80f) * 0.5f) * 0.15f;
            float noise = (Mathf.PerlinNoise(t * 500f, 0f) - 0.5f) * 0.08f;

            openData[i] = (knock + creak + noise) * envelope * 0.4f;
        }
        proceduralOpenSound.SetData(openData, 0);

        // CLOSE SOUND: Slightly stronger thud
        float closeDuration = 0.12f;
        int closeSamples = (int)(sampleRate * closeDuration);
        proceduralCloseSound = AudioClip.Create("DrawerClose", closeSamples, 1, sampleRate, false);
        float[] closeData = new float[closeSamples];

        for (int i = 0; i < closeSamples; i++)
        {
            float t = (float)i / closeSamples;
            float envelope = Mathf.Exp(-t * 35f);

            float thud = Mathf.Sin(2f * Mathf.PI * 95f * t) * 0.6f;
            thud += Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.3f;
            float click = Mathf.Sin(2f * Mathf.PI * 400f * t) * 0.1f * Mathf.Exp(-t * 60f);

            closeData[i] = (thud + click) * envelope * 0.45f;
        }
        proceduralCloseSound.SetData(closeData, 0);

        // SLIDE LOOP: Subtle wood friction
        float slideDuration = 0.5f;
        int slideSamples = (int)(sampleRate * slideDuration);
        slideLoopSound = AudioClip.Create("DrawerSlide", slideSamples, 1, sampleRate, false);
        float[] slideData = new float[slideSamples];

        for (int i = 0; i < slideSamples; i++)
        {
            float t = (float)i / slideSamples;
            float raw = Mathf.PerlinNoise(t * 300f, 1.5f) * 2f - 1f;

            if (i > 0)
                raw = slideData[i - 1] * 0.7f + raw * 0.3f;

            raw += Mathf.Sin(2f * Mathf.PI * 200f * t + Mathf.Sin(t * 30f) * 2f) * 0.05f;

            float loopFade = 1f;
            if (t < 0.05f) loopFade = t / 0.05f;
            if (t > 0.95f) loopFade = (1f - t) / 0.05f;

            slideData[i] = raw * 0.12f * loopFade;
        }
        slideLoopSound.SetData(slideData, 0);
        slideSource.clip = slideLoopSound;
    }

    public void Interact()
    {
        if (isMoving) return;

        isOpen = !isOpen;

        AudioClip clip;
        if (isOpen)
            clip = openSound != null ? openSound : proceduralOpenSound;
        else
            clip = closeSound != null ? closeSound : proceduralCloseSound;

        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }

        if (slideSource != null && !slideSource.isPlaying)
        {
            slideSource.Play();
        }
        slideVolumeTarget = volume * 0.5f;
    }

    public string GetInteractionPrompt()
    {
        if (isMoving)
        {
            return isOpen ? "Opening..." : "Closing...";
        }
        return isOpen ? closePrompt : openPrompt;
    }

    private void Update()
    {
        Vector3 targetPos = isOpen ? openPosition : closedPosition;
        float dist = Vector3.Distance(transform.localPosition, targetPos);

        if (dist > 0.001f)
        {
            isMoving = true;
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                targetPos,
                Time.deltaTime * moveSpeed
            );

            if (slideSource != null)
            {
                float speed = dist / (openOffset.magnitude + 0.001f);
                slideSource.volume = Mathf.Lerp(slideSource.volume, slideVolumeTarget * speed, Time.deltaTime * 8f);
            }
        }
        else
        {
            if (isMoving)
            {
                transform.localPosition = targetPos;
                isMoving = false;
                slideVolumeTarget = 0f;

                if (slideSource != null)
                {
                    slideSource.volume = 0f;
                    slideSource.Stop();
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 worldOpenPos;
        if (Application.isPlaying)
        {
            worldOpenPos = transform.parent != null
                ? transform.parent.TransformPoint(openPosition)
                : openPosition;
        }
        else
        {
            worldOpenPos = transform.parent != null
                ? transform.parent.TransformPoint(transform.localPosition + openOffset)
                : transform.position + openOffset;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(worldOpenPos, Vector3.one * 0.1f);
        Gizmos.DrawLine(transform.position, worldOpenPos);
    }
}
