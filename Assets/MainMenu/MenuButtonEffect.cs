using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Mouse olaylarýný algýlamak için gerekli
using TMPro; // TextMeshPro kullanýyorsan gerekli

public class MenuButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Görsel Ayarlar")]
    public TextMeshProUGUI buttonText; // Eðer normal Text kullanýyorsan burayý 'Text' olarak deðiþtir
    public Color normalColor = Color.gray; // Normal hali (Örn: Gri)
    public Color hoverColor = Color.white; // Üzerine gelinceki renk (Örn: Beyaz/Parlak)
    
    [Range(1f, 1.5f)]
    public float hoverScale = 1.1f; // Üzerine gelince ne kadar büyüsün? (1.1 = %10 büyüme)
    public bool enableGlowEffect = true; // Materyal glow'unu açýp kapatmak ister misin?

    [Header("Ses Ayarlarý")]
    public AudioSource audioSource; // Sesin çýkacaðý kaynak
    public AudioClip hoverSound;    // Üzerine gelme sesi (bip, hýþýrtý vs.)
    public AudioClip clickSound;    // Týklama sesi

    private Vector3 initialScale;

    void Start()
    {
        // Baþlangýç boyutunu ve rengini kaydet
        if (buttonText == null) 
            buttonText = GetComponentInChildren<TextMeshProUGUI>();

        initialScale = transform.localScale;
        
        // Baþlangýçta normal renge döndür
        if (buttonText != null)
            buttonText.color = normalColor;
            
        // Eðer materyal glow kullanýyorsan baþlangýçta kapat
        if (enableGlowEffect && buttonText != null)
            buttonText.fontSharedMaterial.EnableKeyword(ShaderUtilities.Keyword_Glow);
    }

    // Mouse butonun üzerine geldiðinde çalýþýr
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 1. Yazý Rengini Deðiþtir (Parlaklýk hissi)
        if (buttonText != null)
            buttonText.color = hoverColor;

        // 2. Butonu Hafifçe Büyüt
        transform.localScale = initialScale * hoverScale;

        // 3. (Opsiyonel) TMP Glow Efektini aç (Neon etkisi için)
        if (enableGlowEffect && buttonText != null)
            buttonText.fontMaterial.SetFloat(ShaderUtilities.ID_GlowPower, 0.5f); // Parlaklýðý artýr

        // 4. Sesi Çal
        if (audioSource != null && hoverSound != null)
            audioSource.PlayOneShot(hoverSound);
    }

    // Mouse butonun üzerinden gittiðinde çalýþýr
    public void OnPointerExit(PointerEventData eventData)
    {
        // Her þeyi eski haline getir
        if (buttonText != null)
            buttonText.color = normalColor;

        transform.localScale = initialScale;
        
        // Glow efektini sýfýrla
        if (enableGlowEffect && buttonText != null)
            buttonText.fontMaterial.SetFloat(ShaderUtilities.ID_GlowPower, 0f);
    }

    // Butona týklandýðýnda çalýþýr
    public void OnPointerClick(PointerEventData eventData)
    {
        if (audioSource != null && clickSound != null)
            audioSource.PlayOneShot(clickSound);
            
        // Burada oyun baþlatma kodu da çaðrýlabilir ama onu ayrý tutmak daha temizdir.
    }
}