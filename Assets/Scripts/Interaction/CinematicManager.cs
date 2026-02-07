using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;

public class CinematicManager : MonoBehaviour
{
    public static CinematicManager Instance;

    [Header("UI Settings")]
    public GameObject cinematicCanvas;
    public RawImage videoDisplay;
    public VideoPlayer videoPlayer;
    public GameObject skipPrompt;

    private PlayerController playerController;

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
        
        playerController = FindObjectOfType<PlayerController>();
        
        if (cinematicCanvas != null) cinematicCanvas.SetActive(false);
    }

    public void PlayVideo(VideoClip clip)
    {
        if (clip == null)
        {
            Debug.LogError("No video clip provided!");
            return;
        }

        // Setup Video
        if (videoPlayer == null)
        {
             GameObject vpObj = new GameObject("VideoPlayerSource");
             vpObj.transform.SetParent(transform);
             videoPlayer = vpObj.AddComponent<VideoPlayer>();
        }

        videoPlayer.clip = clip;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = new RenderTexture(1920, 1080, 0); // Create dynamic RT
        videoDisplay.texture = videoPlayer.targetTexture;
        videoPlayer.isLooping = false;
        
        // Setup Callbacks
        videoPlayer.loopPointReached += AnyVideoEnd; // End event

        // Activate UI
        cinematicCanvas.SetActive(true);
        if (skipPrompt != null) skipPrompt.SetActive(true);

        // Lock Player
        if (playerController != null)
        {
             playerController.SetInputLock(true);
        }
        InteractionUI.Instance.HidePrompt(); // Hide interaction prompt

        videoPlayer.Play();
        StartCoroutine(CheckForSkip());
    }

    IEnumerator CheckForSkip()
    {
        while (videoPlayer.isPlaying || !videoPlayer.isPrepared)
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                StopVideo();
                yield break;
            }
            yield return null;
        }
    }

    void AnyVideoEnd(VideoPlayer vp)
    {
        StopVideo();
    }

    public void StopVideo()
    {
        if (videoPlayer != null) videoPlayer.Stop();
        if (cinematicCanvas != null) cinematicCanvas.SetActive(false);

        // Unlock Player
        if (playerController != null)
        {
             playerController.SetInputLock(false);
        }
        
        // Cleanup RT
        if (videoPlayer != null && videoPlayer.targetTexture != null)
        {
            videoPlayer.targetTexture.Release();
            videoPlayer.targetTexture = null;
        }
    }

    private void CreateCinematicUI()
    {
        GameObject canvasObj = new GameObject("CinematicCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Ontop of everything
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        cinematicCanvas = canvasObj;

        // Black Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bg = bgObj.AddComponent<Image>();
        bg.color = Color.black;
        bg.rectTransform.anchorMin = Vector2.zero;
        bg.rectTransform.anchorMax = Vector2.one;
        bg.rectTransform.sizeDelta = Vector2.zero;

        // Video Display
        GameObject rawImgObj = new GameObject("VideoDisplay");
        rawImgObj.transform.SetParent(canvasObj.transform, false);
        videoDisplay = rawImgObj.AddComponent<RawImage>();
        videoDisplay.color = Color.white;
        RectTransform rawRect = videoDisplay.rectTransform;
        rawRect.anchorMin = Vector2.zero;
        rawRect.anchorMax = Vector2.one;
        rawRect.sizeDelta = Vector2.zero;

        // Skip Prompt
        GameObject skipObj = new GameObject("SkipText");
        skipObj.transform.SetParent(canvasObj.transform, false);
        Text skipText = skipObj.AddComponent<Text>();
        skipText.text = "Press 'S' to Skip";
        skipText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        skipText.color = new Color(1, 1, 1, 0.5f);
        skipText.alignment = TextAnchor.LowerRight;
        skipText.fontSize = 20;
        
        RectTransform skipRect = skipText.rectTransform;
        skipRect.anchorMin = new Vector2(1, 0);
        skipRect.anchorMax = new Vector2(1, 0);
        skipRect.pivot = new Vector2(1, 0);
        skipRect.anchoredPosition = new Vector2(-20, 20);
        skipRect.sizeDelta = new Vector2(200, 30);
        
        skipPrompt = skipObj;
    }
}
