using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class MenuButtonEffect : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [Header("Yazı Ayarları")]
    public TextMeshProUGUI buttonText;

    public Color normalColor = Color.gray;
    public Color hoverColor = Color.white;

    [Range(1f, 1.5f)]
    public float hoverScale = 1.1f;

    public bool enableGlowEffect = true;

    [Header("Glow Ayarları")]
    public Color glowColor = Color.white;
    [Range(0f, 1f)]
    public float glowOuter = 0.5f;
    [Range(0f, 2f)]
    public float glowPower = 1.2f;

    [Header("Ses Ayarları")]
    public AudioSource audioSource;
    public AudioClip hoverSound;
    public AudioClip clickSound;

    private Vector3 initialScale;
    private Material textMaterial;

    void Start()
    {
        if (buttonText == null)
            buttonText = GetComponentInChildren<TextMeshProUGUI>();

        initialScale = transform.localScale;

        // Yazı rengi
        buttonText.color = normalColor;

        // 🔥 MATERIAL INSTANCE (ÇOK ÖNEMLİ)
        textMaterial = new Material(buttonText.fontMaterial);
        buttonText.fontMaterial = textMaterial;

        // Glow başlangıçta kapalı
        textMaterial.SetFloat(ShaderUtilities.ID_GlowPower, 0f);
        textMaterial.SetFloat(ShaderUtilities.ID_GlowOuter, 0f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Renk değişimi
        buttonText.color = hoverColor;

        // Büyüme
        transform.localScale = initialScale * hoverScale;

        // Glow aç
        if (enableGlowEffect)
        {
            textMaterial.SetColor(
                ShaderUtilities.ID_GlowColor,
                glowColor
            );

            textMaterial.SetFloat(
                ShaderUtilities.ID_GlowOuter,
                glowOuter
            );

            textMaterial.SetFloat(
                ShaderUtilities.ID_GlowPower,
                glowPower
            );
        }

        // Hover sesi
        if (audioSource && hoverSound)
            audioSource.PlayOneShot(hoverSound);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Eski haline dön
        buttonText.color = normalColor;
        transform.localScale = initialScale;

        // Glow kapat
        if (enableGlowEffect)
        {
            textMaterial.SetFloat(
                ShaderUtilities.ID_GlowPower,
                0f
            );
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Tıklama sesi
        if (audioSource && clickSound)
            audioSource.PlayOneShot(clickSound);
    }
}
