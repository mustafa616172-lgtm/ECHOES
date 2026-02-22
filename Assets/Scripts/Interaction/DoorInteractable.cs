using UnityEngine;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    [Tooltip("Rotation angle when door is open (degrees)")]
    public float openAngle = 90f;

    [Tooltip("Rotation axis (usually Y for standard doors)")]
    public Vector3 rotationAxis = Vector3.up;

    [Tooltip("Rotation speed")]
    public float rotateSpeed = 3f;

    [Tooltip("Start with door open?")]
    public bool startOpen = false;

    [Header("Sound Effects")]
    public AudioClip customOpenSound;
    public AudioClip customCloseSound;
    [Range(0f, 1f)]
    public float volume = 0.4f;

    [Header("Prompts")]
    public string openPrompt = "[E] Open Door";
    public string closePrompt = "[E] Close Door";

    // State
    private bool isOpen = false;
    private bool isMoving = false;
    private Quaternion closedRotation;
    private Quaternion openRotation;
    private Quaternion targetRotation;

    // Audio
    private AudioSource audioSource;
    private AudioSource creakSource;
    private AudioClip proceduralOpenSound;
    private AudioClip proceduralCloseSound;
    private AudioClip creakLoopSound;
    private float creakVolumeTarget = 0f;

    private void Awake()
    {
        closedRotation = transform.localRotation;
        openRotation = closedRotation * Quaternion.AngleAxis(openAngle, rotationAxis);
        targetRotation = startOpen ? openRotation : closedRotation;

        if (startOpen)
        {
            isOpen = true;
            transform.localRotation = openRotation;
        }

        // Main audio source for open/close sounds
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 12f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;

        // Creak loop source
        creakSource = gameObject.AddComponent<AudioSource>();
        creakSource.playOnAwake = false;
        creakSource.spatialBlend = 1f;
        creakSource.minDistance = 1f;
        creakSource.maxDistance = 8f;
        creakSource.rolloffMode = AudioRolloffMode.Linear;
        creakSource.loop = true;
        creakSource.volume = 0f;

        GenerateProceduralSounds();
    }

    private void GenerateProceduralSounds()
    {
        int sampleRate = 44100;

        // OPEN SOUND: Door latch click + initial creak
        float openDuration = 0.2f;
        int openSamples = (int)(sampleRate * openDuration);
        proceduralOpenSound = AudioClip.Create("DoorOpen", openSamples, 1, sampleRate, false);
        float[] openData = new float[openSamples];

        for (int i = 0; i < openSamples; i++)
        {
            float t = (float)i / openSamples;

            // Latch click - sharp metallic
            float latch = Mathf.Sin(2f * Mathf.PI * 500f * t) * 0.4f * Mathf.Exp(-t * 40f);
            latch += Mathf.Sin(2f * Mathf.PI * 1200f * t) * 0.2f * Mathf.Exp(-t * 60f);

            // Initial creak - modulated sine
            float creak = Mathf.Sin(2f * Mathf.PI * 250f * t + Mathf.Sin(t * 40f) * 3f) * 0.15f;
            creak *= Mathf.Clamp01(t * 10f) * Mathf.Exp(-t * 8f);

            // Wood thud
            float thud = Mathf.Sin(2f * Mathf.PI * 80f * t) * 0.2f * Mathf.Exp(-t * 30f);

            openData[i] = (latch + creak + thud) * 0.5f;
        }
        proceduralOpenSound.SetData(openData, 0);

        // CLOSE SOUND: Heavier thud + latch catch
        float closeDuration = 0.18f;
        int closeSamples = (int)(sampleRate * closeDuration);
        proceduralCloseSound = AudioClip.Create("DoorClose", closeSamples, 1, sampleRate, false);
        float[] closeData = new float[closeSamples];

        for (int i = 0; i < closeSamples; i++)
        {
            float t = (float)i / closeSamples;

            // Heavy door thud
            float thud = Mathf.Sin(2f * Mathf.PI * 65f * t) * 0.5f * Mathf.Exp(-t * 25f);
            thud += Mathf.Sin(2f * Mathf.PI * 45f * t) * 0.3f * Mathf.Exp(-t * 20f);

            // Latch catch click
            float latch = Mathf.Sin(2f * Mathf.PI * 800f * t) * 0.25f * Mathf.Exp(-t * 50f);

            // Frame impact
            float impact = Mathf.Sin(2f * Mathf.PI * 150f * t) * 0.2f * Mathf.Exp(-t * 35f);

            // Tiny rattle
            float rattle = (Mathf.PerlinNoise(t * 2000f, 0f) - 0.5f) * 0.06f * Mathf.Exp(-t * 15f);

            closeData[i] = (thud + latch + impact + rattle) * 0.55f;
        }
        proceduralCloseSound.SetData(closeData, 0);

        // CREAK LOOP: Subtle hinge creak during movement
        float creakDuration = 0.8f;
        int creakSamples = (int)(sampleRate * creakDuration);
        creakLoopSound = AudioClip.Create("DoorCreak", creakSamples, 1, sampleRate, false);
        float[] creakData = new float[creakSamples];

        for (int i = 0; i < creakSamples; i++)
        {
            float t = (float)i / creakSamples;

            // Modulated creaking - multiple harmonics
            float freq1 = 180f + Mathf.Sin(t * 12f) * 40f;
            float freq2 = 320f + Mathf.Sin(t * 8f) * 60f;

            float c1 = Mathf.Sin(2f * Mathf.PI * freq1 * t) * 0.3f;
            float c2 = Mathf.Sin(2f * Mathf.PI * freq2 * t + Mathf.Sin(t * 25f) * 2f) * 0.2f;

            // Filtered noise for texture
            float n = Mathf.PerlinNoise(t * 400f, 2f) * 2f - 1f;
            if (i > 0) n = creakData[i - 1] * 0.6f + n * 0.4f;

            // Loop fade
            float fade = 1f;
            if (t < 0.05f) fade = t / 0.05f;
            if (t > 0.95f) fade = (1f - t) / 0.05f;

            creakData[i] = (c1 + c2 + n * 0.1f) * 0.1f * fade;
        }
        creakLoopSound.SetData(creakData, 0);
        creakSource.clip = creakLoopSound;
    }

    public void Interact()
    {
        if (isMoving) return;

        isOpen = !isOpen;
        targetRotation = isOpen ? openRotation : closedRotation;

        // Play open/close sound
        AudioClip clip;
        if (isOpen)
            clip = customOpenSound != null ? customOpenSound : proceduralOpenSound;
        else
            clip = customCloseSound != null ? customCloseSound : proceduralCloseSound;

        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }

        // Start creak loop
        if (creakSource != null && !creakSource.isPlaying)
        {
            creakSource.Play();
        }
        creakVolumeTarget = volume * 0.4f;
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
        float angle = Quaternion.Angle(transform.localRotation, targetRotation);

        if (angle > 0.5f)
        {
            isMoving = true;
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                targetRotation,
                Time.deltaTime * rotateSpeed
            );

            // Creak volume based on rotation speed
            if (creakSource != null)
            {
                float speed = angle / openAngle;
                creakSource.volume = Mathf.Lerp(creakSource.volume, creakVolumeTarget * speed, Time.deltaTime * 6f);
            }
        }
        else
        {
            if (isMoving)
            {
                transform.localRotation = targetRotation;
                isMoving = false;
                creakVolumeTarget = 0f;

                if (creakSource != null)
                {
                    creakSource.volume = 0f;
                    creakSource.Stop();
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Show door swing arc
        Quaternion closed = Application.isPlaying ? closedRotation : transform.localRotation;
        Quaternion open = closed * Quaternion.AngleAxis(openAngle, rotationAxis);

        Vector3 doorForward = transform.TransformDirection(Vector3.forward) * 1.5f;
        Vector3 openForward = transform.parent != null
            ? transform.parent.rotation * (open * Vector3.forward) * 1.5f
            : (open * Vector3.forward) * 1.5f;

        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, doorForward);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, openForward);

        // Draw arc
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        int steps = 20;
        for (int i = 0; i < steps; i++)
        {
            float t1 = (float)i / steps;
            float t2 = (float)(i + 1) / steps;

            Quaternion r1 = closed * Quaternion.AngleAxis(openAngle * t1, rotationAxis);
            Quaternion r2 = closed * Quaternion.AngleAxis(openAngle * t2, rotationAxis);

            Vector3 p1 = transform.position + (transform.parent != null
                ? transform.parent.rotation * (r1 * Vector3.forward) * 1.2f
                : (r1 * Vector3.forward) * 1.2f);
            Vector3 p2 = transform.position + (transform.parent != null
                ? transform.parent.rotation * (r2 * Vector3.forward) * 1.2f
                : (r2 * Vector3.forward) * 1.2f);

            Gizmos.DrawLine(p1, p2);
        }
    }
}
