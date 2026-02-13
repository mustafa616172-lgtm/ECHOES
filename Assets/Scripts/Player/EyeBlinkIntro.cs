using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ECHOES - Eye Blink Intro Effect
/// Oyuna ilk giriste goz acilma efekti.
/// Ekran tamamen siyahtan baslar, goz kirpma animasyonu ile acilir.
/// Efekt sirasinda diger canvaslar kapatilir, bitince tekrar acilir.
/// </summary>
public class EyeBlinkIntro : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Tamamen karanlikta bekleme suresi (saniye)")]
    [SerializeField] private float initialBlackoutDuration = 1.5f;
    
    [Tooltip("Hizli goz kirpma sayisi")]
    [SerializeField] private int blinkCount = 3;
    
    [Tooltip("Her kirpmada gozun ne kadar acilacagi (0-1)")]
    [SerializeField] private float blinkOpenAmount = 0.15f;
    
    [Tooltip("Kirpma hizi (dusuk = hizli)")]
    [SerializeField] private float blinkSpeed = 0.15f;
    
    [Tooltip("Kirpmalar arasi bekleme")]
    [SerializeField] private float blinkInterval = 0.35f;
    
    [Tooltip("Son acilma suresi (saniye)")]
    [SerializeField] private float finalOpenDuration = 2.0f;
    
    // Private references
    private Canvas blinkCanvas;
    private RectTransform topLidRect;
    private RectTransform bottomLidRect;
    private PlayerController playerController;
    private float screenHeight;
    
    // Canvas management
    private List<Canvas> disabledCanvases = new List<Canvas>();
    
    /// <summary>
    /// Efekti baslatir. SinglePlayerManager tarafindan cagirilir.
    /// </summary>
    public void StartEffect()
    {
        Debug.Log("[EyeBlinkIntro] StartEffect cagirildi");
        CreateBlinkUI();
        DisableOtherCanvases();
        StartCoroutine(MainSequence());
    }
    
    void CreateBlinkUI()
    {
        // Canvas olustur - en ustte olacak
        GameObject canvasObj = new GameObject("EyeBlinkCanvas");
        canvasObj.transform.SetParent(transform);
        blinkCanvas = canvasObj.AddComponent<Canvas>();
        blinkCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        blinkCanvas.sortingOrder = 9999; // En ustte
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        screenHeight = scaler.referenceResolution.y;
        
        // Ust goz kapagi - ekranin tum ust yarisini kaplar
        GameObject topObj = new GameObject("TopLid");
        topObj.transform.SetParent(canvasObj.transform, false);
        Image topImage = topObj.AddComponent<Image>();
        topImage.color = Color.black;
        topImage.raycastTarget = false;
        
        topLidRect = topImage.rectTransform;
        topLidRect.anchorMin = new Vector2(0, 0.5f);
        topLidRect.anchorMax = new Vector2(1, 1f);
        topLidRect.offsetMin = Vector2.zero;
        topLidRect.offsetMax = Vector2.zero;
        // Ekstra yukseklik ekle - ekrani tamamen kapatmasi icin
        topLidRect.sizeDelta = new Vector2(0, screenHeight);
        topLidRect.pivot = new Vector2(0.5f, 0f); // Alt kenardan pivot
        topLidRect.anchoredPosition = Vector2.zero;
        
        // Alt goz kapagi - ekranin tum alt yarisini kaplar
        GameObject bottomObj = new GameObject("BottomLid");
        bottomObj.transform.SetParent(canvasObj.transform, false);
        Image bottomImage = bottomObj.AddComponent<Image>();
        bottomImage.color = Color.black;
        bottomImage.raycastTarget = false;
        
        bottomLidRect = bottomImage.rectTransform;
        bottomLidRect.anchorMin = new Vector2(0, 0f);
        bottomLidRect.anchorMax = new Vector2(1, 0.5f);
        bottomLidRect.offsetMin = Vector2.zero;
        bottomLidRect.offsetMax = Vector2.zero;
        // Ekstra yukseklik ekle
        bottomLidRect.sizeDelta = new Vector2(0, screenHeight);
        bottomLidRect.pivot = new Vector2(0.5f, 1f); // Ust kenardan pivot
        bottomLidRect.anchoredPosition = Vector2.zero;
        
        Debug.Log("[EyeBlinkIntro] Blink UI olusturuldu");
    }
    
    /// <summary>
    /// Diger tum canvaslari kapatir, efekt bitince tekrar acar.
    /// </summary>
    void DisableOtherCanvases()
    {
        disabledCanvases.Clear();
        
        Canvas[] allCanvases = FindObjectsOfType<Canvas>(true);
        foreach (Canvas c in allCanvases)
        {
            if (c == blinkCanvas) continue;
            
            if (c.gameObject.activeSelf)
            {
                c.gameObject.SetActive(false);
                disabledCanvases.Add(c);
                Debug.Log("[EyeBlinkIntro] Canvas kapatildi: " + c.gameObject.name);
            }
        }
    }
    
    /// <summary>
    /// Kapatilan canvaslari tekrar acar.
    /// </summary>
    void ReEnableCanvases()
    {
        foreach (Canvas c in disabledCanvases)
        {
            if (c != null)
            {
                c.gameObject.SetActive(true);
                Debug.Log("[EyeBlinkIntro] Canvas tekrar acildi: " + c.gameObject.name);
            }
        }
        disabledCanvases.Clear();
    }
    
    /// <summary>
    /// Goz kapagi pozisyonunu ayarlar.
    /// openAmount: 0 = tamamen kapali, 1 = tamamen acik
    /// </summary>
    void SetLidPosition(float openAmount)
    {
        openAmount = Mathf.Clamp01(openAmount);
        
        // openAmount=0: kapaklar ortada birlesik (ekran kapali)
        // openAmount=1: kapaklar ekran disina kayar (ekran acik)
        // anchoredPosition ile kaydiriyoruz (pivot tabanli)
        
        float offset = openAmount * (screenHeight * 0.5f);
        
        if (topLidRect != null)
        {
            topLidRect.anchoredPosition = new Vector2(0, offset);
        }
        
        if (bottomLidRect != null)
        {
            bottomLidRect.anchoredPosition = new Vector2(0, -offset);
        }
    }
    
    IEnumerator MainSequence()
    {
        // 1 frame bekle - PlayerController.Start() calissin
        yield return null;
        
        // Input'u kilitle
        playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            playerController.SetInputLock(true);
            Debug.Log("[EyeBlinkIntro] Input kilitlendi");
        }
        
        // Ana animasyon
        yield return StartCoroutine(BlinkSequence());
    }
    
    IEnumerator BlinkSequence()
    {
        Debug.Log("[EyeBlinkIntro] Goz acilma efekti basladi");
        
        // 1. Tamamen karanlikta bekle
        SetLidPosition(0f);
        yield return new WaitForSeconds(initialBlackoutDuration);
        
        // 2. Hizli goz kirpmalar
        float currentMaxOpen = blinkOpenAmount;
        
        for (int i = 0; i < blinkCount; i++)
        {
            Debug.Log("[EyeBlinkIntro] Blink " + (i + 1) + "/" + blinkCount);
            
            // Goz ac (kismi)
            yield return StartCoroutine(AnimateLids(0f, currentMaxOpen, blinkSpeed));
            
            // Kisa bekleme - acik kal
            yield return new WaitForSeconds(0.06f);
            
            // Goz kapa
            yield return StartCoroutine(AnimateLids(currentMaxOpen, 0f, blinkSpeed * 0.6f));
            
            // Kirpmalar arasi bekleme
            yield return new WaitForSeconds(blinkInterval);
            
            // Her kirpmada biraz daha fazla ac
            currentMaxOpen += blinkOpenAmount * 0.6f;
            currentMaxOpen = Mathf.Min(currentMaxOpen, 0.45f);
        }
        
        // 3. Son acilma - tamamen ac
        Debug.Log("[EyeBlinkIntro] Son acilma basladi");
        yield return new WaitForSeconds(0.25f);
        yield return StartCoroutine(AnimateLids(0f, 1f, finalOpenDuration));
        
        // 4. Efekt bitti - temizle
        Debug.Log("[EyeBlinkIntro] Efekt tamamlandi");
        FinishEffect();
    }
    
    IEnumerator AnimateLids(float from, float to, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // Smooth ease-in-out
            float smoothT = t * t * (3f - 2f * t); // Smoothstep
            float currentOpen = Mathf.Lerp(from, to, smoothT);
            SetLidPosition(currentOpen);
            
            yield return null;
        }
        
        SetLidPosition(to);
    }
    
    void FinishEffect()
    {
        Debug.Log("[EyeBlinkIntro] Temizlik yapiliyor...");
        
        // Input kilidini kaldir
        if (playerController != null)
        {
            playerController.SetInputLock(false);
            Debug.Log("[EyeBlinkIntro] Input kilidi kaldirildi");
        }
        
        // Canvaslari tekrar ac
        ReEnableCanvases();
        
        // Canvas'i yok et
        if (blinkCanvas != null)
        {
            Destroy(blinkCanvas.gameObject);
        }
        
        // Kendini yok et
        Destroy(this);
        
        Debug.Log("[EyeBlinkIntro] Efekt tamamen bitti, component silindi");
    }
}
