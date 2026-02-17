using UnityEngine;
using TMPro;
using System.Collections;

public class InfoPanelController : MonoBehaviour
{
    public static InfoPanelController Instance { get; private set; }

    [Header("Panel Bileşenleri")]
    [SerializeField] private RectTransform panelRectTransform;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI infoText;

    [Header("Animasyon Ayarları")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float offScreenXPosition = 500f; // Ekran dışı pozisyon
    [SerializeField] private float onScreenXPosition = -300f; // Ekran içi pozisyon (sağdan)

    private Coroutine hideCoroutine;
    private bool isAnimating = false;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Bileşenleri otomatik bul
        if (panelRectTransform == null)
            panelRectTransform = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (infoText == null)
            infoText = GetComponentInChildren<TextMeshProUGUI>();

        // Başlangıçta paneli gizle
        InitializePanel();
    }

    private void InitializePanel()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        if (panelRectTransform != null)
        {
            panelRectTransform.anchoredPosition = new Vector2(offScreenXPosition, panelRectTransform.anchoredPosition.y);
        }
    }

    /// <summary>
    /// Paneli belirtilen mesajla gösterir
    /// </summary>
    public void ShowPanel(string message)
    {
        if (isAnimating)
        {
            StopAllCoroutines();
        }

        SetText(message);
        StartCoroutine(ShowPanelCoroutine());
    }

    /// <summary>
    /// Panel metnini günceller
    /// </summary>
    public void SetText(string text)
    {
        if (infoText != null)
        {
            infoText.text = text;
        }
    }

    /// <summary>
    /// Paneli gösterme animasyonu
    /// </summary>
    private IEnumerator ShowPanelCoroutine()
    {
        isAnimating = true;

        float elapsedTime = 0f;
        Vector2 startPos = new Vector2(offScreenXPosition, panelRectTransform.anchoredPosition.y);
        Vector2 endPos = new Vector2(onScreenXPosition, panelRectTransform.anchoredPosition.y);

        // Panel içeri kayar ve görünür olur
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / animationDuration;
            
            // Ease-out animasyonu için
            t = 1f - Mathf.Pow(1f - t, 3f);

            if (panelRectTransform != null)
            {
                panelRectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = t;
            }

            yield return null;
        }

        // Final pozisyonu garantile
        if (panelRectTransform != null)
        {
            panelRectTransform.anchoredPosition = endPos;
        }
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        isAnimating = false;

        // Belirtilen süre sonra paneli otomatik gizle
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }
        hideCoroutine = StartCoroutine(HidePanelAfterDelay());
    }

    /// <summary>
    /// Belirtilen süre sonra paneli gizler
    /// </summary>
    private IEnumerator HidePanelAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        yield return StartCoroutine(HidePanelCoroutine());
    }

    /// <summary>
    /// Paneli gizleme animasyonu
    /// </summary>
    private IEnumerator HidePanelCoroutine()
    {
        isAnimating = true;

        float elapsedTime = 0f;
        Vector2 startPos = panelRectTransform.anchoredPosition;
        Vector2 endPos = new Vector2(offScreenXPosition, panelRectTransform.anchoredPosition.y);

        // Panel dışarı kayar ve kaybolur
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / animationDuration;

            // Ease-in animasyonu için
            t = Mathf.Pow(t, 3f);

            if (panelRectTransform != null)
            {
                panelRectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f - t;
            }

            yield return null;
        }

        // Final pozisyonu garantile
        if (panelRectTransform != null)
        {
            panelRectTransform.anchoredPosition = endPos;
        }
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        isAnimating = false;
    }

    /// <summary>
    /// Paneli hemen gizler (animasyonsuz)
    /// </summary>
    public void HidePanel()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        StopAllCoroutines();
        InitializePanel();
        isAnimating = false;
    }
}