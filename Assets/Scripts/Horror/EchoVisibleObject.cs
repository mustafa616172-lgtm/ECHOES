using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ECHOES - Echo Visible Object
/// Objects that are only visible during an Echo pulse.
/// Used for: ghost figure behind glass, sound wave traces on walls.
/// Starts invisible, fades in during pulse, fades out after.
/// 
/// NOTE: Materials are instanced to avoid shared material pollution.
/// Emission is managed per-instance and cleaned up on destroy.
/// </summary>
public class EchoVisibleObject : MonoBehaviour
{
    [Header("Visibility")]
    [Tooltip("Duration to stay visible after pulse reaches this object")]
    [SerializeField] private float visibleDuration = 3f;

    [Tooltip("Fade in/out duration")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Glow Effect")]
    [SerializeField] private Color glowColor = new Color(0.2f, 0.8f, 1f, 0.8f);
    [SerializeField] private float glowIntensity = 2f;

    [Header("Options")]
    [Tooltip("Only show when story reaches this step")]
    [SerializeField] private bool requireStoryState = false;
    [SerializeField] private int minStoryStep = 4;

    [Tooltip("Minimum seconds between pulse triggers (prevents multi-hit)")]
    [SerializeField] private float pulseCooldown = 1f;

    private Renderer[] renderers;
    private Material[] materials;  // instanced materials (we own these)
    private Color[] originalColors;
    private bool isVisible = false;
    private Coroutine visibilityCoroutine;
    private float lastPulseTime = -999f;
    private bool isInitialized = false;

    void Awake()
    {
        InitializeMaterials();
    }

    void InitializeMaterials()
    {
        if (isInitialized) return;

        renderers = GetComponentsInChildren<Renderer>(true);
        materials = new Material[renderers.Length];
        originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            // .material creates an instance - we own it and must clean up
            materials[i] = renderers[i].material;
            if (materials[i].HasProperty("_Color"))
            {
                originalColors[i] = materials[i].color;
            }

            // Ensure rendering mode is transparent for alpha fade
            SetMaterialTransparent(materials[i]);
        }

        // Start invisible
        SetAlpha(0f);
        SetGlow(0f);

        isInitialized = true;
    }

    /// <summary>
    /// Called when an Echo pulse reaches this object's area.
    /// Has cooldown to prevent multi-triggering from overlapping pulses.
    /// </summary>
    public void OnPulseReached()
    {
        // Cooldown check
        if (Time.time - lastPulseTime < pulseCooldown) return;
        if (isVisible) return;

        // Check story state if required
        if (requireStoryState)
        {
            StorySequenceManager story = StorySequenceManager.Instance;
            if (story == null)
                story = FindFirstObjectByType<StorySequenceManager>();
            if (story == null || (int)story.CurrentState < minStoryStep)
                return;
        }

        lastPulseTime = Time.time;

        if (visibilityCoroutine != null)
            StopCoroutine(visibilityCoroutine);

        // Ensure object is active before starting coroutine
        gameObject.SetActive(true);
        visibilityCoroutine = StartCoroutine(PulseVisibility());
    }

    /// <summary>
    /// Force show this object (for story events)
    /// </summary>
    public void ForceShow()
    {
        if (visibilityCoroutine != null)
            StopCoroutine(visibilityCoroutine);

        gameObject.SetActive(true);
        visibilityCoroutine = StartCoroutine(PulseVisibility());
    }

    /// <summary>
    /// Force hide this object
    /// </summary>
    public void ForceHide()
    {
        if (visibilityCoroutine != null)
            StopCoroutine(visibilityCoroutine);

        visibilityCoroutine = StartCoroutine(FadeOut());
    }

    IEnumerator PulseVisibility()
    {
        if (!isInitialized) InitializeMaterials();

        isVisible = true;

        // Fade in with glow
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            // Smooth ease-in
            float smooth = t * t * (3f - 2f * t);
            SetAlpha(smooth);
            SetGlow(smooth * glowIntensity);
            yield return null;
        }
        SetAlpha(1f);
        SetGlow(glowIntensity);

        // Stay visible
        yield return new WaitForSeconds(visibleDuration);

        // Fade out
        yield return FadeOut();
    }

    IEnumerator FadeOut()
    {
        float elapsed = 0f;
        float startAlpha = GetCurrentAlpha();
        float startGlow = startAlpha * glowIntensity;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Clamp01(elapsed / fadeDuration);
            float smooth = t * t * (3f - 2f * t);
            SetAlpha(smooth * startAlpha);
            SetGlow(smooth * startGlow);
            yield return null;
        }

        SetAlpha(0f);
        SetGlow(0f);
        gameObject.SetActive(false);
        isVisible = false;
        visibilityCoroutine = null;
    }

    float GetCurrentAlpha()
    {
        if (materials == null || materials.Length == 0) return 0f;
        if (materials[0] != null && materials[0].HasProperty("_Color"))
            return materials[0].color.a;
        return isVisible ? 1f : 0f;
    }

    void SetAlpha(float alpha)
    {
        if (materials == null) return;
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] == null) continue;
            if (materials[i].HasProperty("_Color"))
            {
                Color c = originalColors[i];
                c.a = alpha;
                materials[i].color = c;
            }
        }
    }

    void SetGlow(float intensity)
    {
        if (materials == null) return;
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] == null) continue;
            if (materials[i].HasProperty("_EmissionColor"))
            {
                materials[i].EnableKeyword("_EMISSION");
                materials[i].SetColor("_EmissionColor", glowColor * intensity);
            }
        }
    }

    /// <summary>
    /// Set material to transparent rendering mode for alpha fade support.
    /// </summary>
    void SetMaterialTransparent(Material mat)
    {
        if (mat == null) return;

        // Standard shader transparency setup
        if (mat.HasProperty("_Mode"))
        {
            mat.SetFloat("_Mode", 3); // Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }
    }

    /// <summary>
    /// Clean up instanced materials to prevent memory leaks.
    /// </summary>
    void OnDestroy()
    {
        if (materials == null) return;
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] != null)
            {
                Destroy(materials[i]);
                materials[i] = null;
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.15f);
        Gizmos.DrawSphere(transform.position, 0.5f);

#if UNITY_EDITOR
        // Show label in editor
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.8f, "Echo Visible");
#endif
    }
}
