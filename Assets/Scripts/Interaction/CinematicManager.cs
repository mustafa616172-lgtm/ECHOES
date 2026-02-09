using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;

public class CinematicManager : MonoBehaviour
{
    public static CinematicManager Instance;

    [Header("UI References")]
    public GameObject cinematicCanvas;
    public Image backgroundImage; // Black background for fades
    public RawImage contentDisplay; // Shared for both Document Image and Video
    public GameObject skipPrompt;
    public Text skipText;

    [Header("Components")]
    public VideoPlayer videoPlayer;

    [Header("Settings")]
    public RenderTexture targetRenderTexture;
    public float readingTime = 3.0f; // How long to show the document before video
    public float fadeDuration = 1.0f;

    private PlayerController playerController;
    private Coroutine currentSequence;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (cinematicCanvas == null)
        {
            CreateCinematicUI();
        }
        else
        {
            // Update Background or find existing
            if (backgroundImage == null) backgroundImage = cinematicCanvas.transform.Find("Background")?.GetComponent<Image>();
            
            // Support legacy "VideoDisplay" or new "ContentDisplay"
            if (contentDisplay == null) 
            {
                contentDisplay = cinematicCanvas.transform.Find("ContentDisplay")?.GetComponent<RawImage>();
                if (contentDisplay == null) contentDisplay = cinematicCanvas.transform.Find("VideoDisplay")?.GetComponent<RawImage>();
            }
            
            // Try find Panel first, then Text for fallback
            if (skipPrompt == null) skipPrompt = cinematicCanvas.transform.Find("SkipPanel")?.gameObject;
            if (skipPrompt == null) skipPrompt = cinematicCanvas.transform.Find("SkipText")?.gameObject;
            
            if (skipPrompt != null && skipText == null) skipText = skipPrompt.GetComponentInChildren<Text>();
        }
        
        // SELF HEALING: If parts are missing, create them!
        if (cinematicCanvas != null)
        {
             // 1. Check Content Display
             if (contentDisplay == null)
             {
                 GameObject rawImgObj = new GameObject("ContentDisplay");
                 rawImgObj.transform.SetParent(cinematicCanvas.transform, false);
                 contentDisplay = rawImgObj.AddComponent<RawImage>();
                 contentDisplay.color = Color.clear;
                 contentDisplay.rectTransform.anchorMin = Vector2.zero;
                 contentDisplay.rectTransform.anchorMax = Vector2.one;
                 contentDisplay.rectTransform.sizeDelta = Vector2.zero;
                 rawImgObj.transform.SetSiblingIndex(1); // Behind Skip, Above BG
             }

             // 2. Check Skip Prompt
             if (skipPrompt == null)
             {
                GameObject skipObj = new GameObject("SkipPanel");
                skipObj.transform.SetParent(cinematicCanvas.transform, false);
                Image skipImg = skipObj.AddComponent<Image>();
                skipImg.color = new Color(0, 0, 0, 0.7f);
                
                RectTransform skipRect = skipImg.rectTransform;
                skipRect.anchorMin = new Vector2(1, 0);
                skipRect.anchorMax = new Vector2(1, 0);
                skipRect.pivot = new Vector2(1, 0);
                skipRect.anchoredPosition = new Vector2(-20, 20);
                skipRect.sizeDelta = new Vector2(250, 50);
                
                skipPrompt = skipObj;

                GameObject textObj = new GameObject("SkipText");
                textObj.transform.SetParent(skipObj.transform, false);
                skipText = textObj.AddComponent<Text>();
                skipText.text = "S - SKIP";
                skipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (skipText.font == null) skipText.font = Resources.FindObjectsOfTypeAll<Font>()[0];
                skipText.color = Color.white;
                skipText.alignment = TextAnchor.MiddleCenter;
                skipText.fontSize = 24;
                
                RectTransform textRect = skipText.rectTransform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
                textRect.anchoredPosition = Vector2.zero;
             }
        }
        
        // Ensure contentDisplay is not null logic removed from here as it is handled above now

        playerController = FindObjectOfType<PlayerController>();
        
        if (cinematicCanvas != null) cinematicCanvas.SetActive(false);
        if (backgroundImage != null) backgroundImage.color = Color.black; // Ensure black start
    }
    
    // ...

    private void CreateCinematicUI()
    {
        GameObject canvasObj = new GameObject("CinematicCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        cinematicCanvas = canvasObj;

        // Black Background (Always there behind content)
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        backgroundImage = bgObj.AddComponent<Image>();
        backgroundImage.color = Color.black;
        backgroundImage.rectTransform.anchorMin = Vector2.zero;
        backgroundImage.rectTransform.anchorMax = Vector2.one;
        backgroundImage.rectTransform.sizeDelta = Vector2.zero;

        // Content Display
        GameObject rawImgObj = new GameObject("ContentDisplay");
        rawImgObj.transform.SetParent(canvasObj.transform, false);
        contentDisplay = rawImgObj.AddComponent<RawImage>();
        contentDisplay.color = Color.clear; // Start invisible
        RectTransform rawRect = contentDisplay.rectTransform;
        rawRect.anchorMin = Vector2.zero;
        rawRect.anchorMax = Vector2.one;
        rawRect.sizeDelta = Vector2.zero;

        // Skip Panel (Background)
        GameObject skipObj = new GameObject("SkipPanel");
        skipObj.transform.SetParent(canvasObj.transform, false);
        Image skipImg = skipObj.AddComponent<Image>();
        skipImg.color = new Color(0, 0, 0, 0.7f); // Semi-transparent black background
        
        RectTransform skipRect = skipImg.rectTransform;
        skipRect.anchorMin = new Vector2(1, 0); // Bottom Right
        skipRect.anchorMax = new Vector2(1, 0);
        skipRect.pivot = new Vector2(1, 0);
        skipRect.anchoredPosition = new Vector2(-20, 20);
        skipRect.sizeDelta = new Vector2(250, 50); // Panel size
        
        skipPrompt = skipObj;

        // Skip Text (Child of Panel)
        GameObject textObj = new GameObject("SkipText");
        textObj.transform.SetParent(skipObj.transform, false);
        skipText = textObj.AddComponent<Text>();
        skipText.text = "S - SKIP";
        skipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (skipText.font == null)
        {
            // Fallback
             skipText.font = Resources.FindObjectsOfTypeAll<Font>()[0];
        }
        skipText.color = Color.white;
        skipText.alignment = TextAnchor.MiddleCenter;
        skipText.fontSize = 24;
        
        RectTransform textRect = skipText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero; // Fill parent
        textRect.anchoredPosition = Vector2.zero;
    }
    
    // ...

    public void StartCinematicSequence(Texture docImage, VideoClip video)
    {
        if (currentSequence != null) StopCoroutine(currentSequence);
        currentSequence = StartCoroutine(PlaySequenceRoutine(docImage, video));
    }

    IEnumerator PlaySequenceRoutine(Texture docImage, VideoClip video)
    {
        // 1. Setup & Lock Input
        if (playerController != null) playerController.SetInputLock(true);
        InteractionUI.Instance.HidePrompt();
        
        if (cinematicCanvas != null) cinematicCanvas.SetActive(true);
        if (skipPrompt != null) skipPrompt.SetActive(false); // Hide skip initially
        
        // Ensure VideoPlayer exists
        if (videoPlayer == null)
        {
             GameObject vpObj = new GameObject("VideoPlayerSource");
             vpObj.transform.SetParent(transform);
             videoPlayer = vpObj.AddComponent<VideoPlayer>();
             videoPlayer.isLooping = false;
             videoPlayer.loopPointReached += AnyVideoEnd;
        }

        // SETUP RENDER TEXTURE
        if (targetRenderTexture != null)
        {
            // Use the specific one requested by user
            videoPlayer.targetTexture = targetRenderTexture;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            
            // Assign to RawImage (will be overwritten by docImage if present, then set back for video)
            if (contentDisplay != null) 
            {
                contentDisplay.texture = targetRenderTexture;
            }
        }
        else
        {
            // Fallback: Create dynamic if none assigned
            if (videoPlayer.targetTexture == null)
            {
                videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                videoPlayer.targetTexture = new RenderTexture(1920, 1080, 0);
            }
            if (contentDisplay != null) contentDisplay.texture = videoPlayer.targetTexture;
        }

        // 2. SHOW DOCUMENT (Reading Phase)
        if (docImage != null)
        {
            // Temporarily show document image
            if (contentDisplay != null)
            {
                contentDisplay.texture = docImage;
                contentDisplay.color = Color.white; // Visible
            }
            
            // Fade In (Optional or Instant)
            yield return StartCoroutine(Fade(0, 1, 0.5f)); // Fade in UI

            // Wait for reading
            float timer = 0;
            while (timer < readingTime)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)) break; // Click to continue early
                timer += Time.deltaTime;
                yield return null;
            }

            // Fade Out Document
            yield return StartCoroutine(Fade(1, 0, fadeDuration)); // Fade out content, keeps BG black
        }

        // 3. PLAY VIDEO
        if (video != null)
        {
            // Set texture back to Video Render Texture
            if (contentDisplay != null)
            {
                contentDisplay.texture = videoPlayer.targetTexture;
                contentDisplay.color = Color.white; // Visible
            }

            videoPlayer.clip = video;
            videoPlayer.Prepare();
            while (!videoPlayer.isPrepared) yield return null;
            
            videoPlayer.Play();
            
            // Fade In Video
            yield return StartCoroutine(Fade(0, 1, 0.5f));

            // Show Skip Prompt
            if (skipPrompt != null)
            {
                skipPrompt.SetActive(true);
                if (skipText != null) skipText.text = "S - SKIP"; // Specific request
            }

            while (videoPlayer.isPlaying)
            {
                if (Input.GetKeyDown(KeyCode.S))
                {
                    StopVideo();
                    yield break;
                }
                yield return null;
            }
        }
        else
        {
            StopVideo(); // No video, just end after doc
        }
    }

    IEnumerator Fade(float startAlpha, float endAlpha, float duration)
    {
        float timer = 0;
        Color c = contentDisplay.color;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
            contentDisplay.color = c;
            yield return null;
        }
        c.a = endAlpha;
        contentDisplay.color = c;
    }

    void AnyVideoEnd(VideoPlayer vp)
    {
        StopVideo();
    }

    public void StopVideo()
    {
        if (currentSequence != null) StopCoroutine(currentSequence);
        if (videoPlayer != null) videoPlayer.Stop();
        
        cinematicCanvas.SetActive(false);

        // Unlock Player
        if (playerController != null) playerController.SetInputLock(false);
    }


}
