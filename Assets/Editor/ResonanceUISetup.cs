using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class ResonanceUISetup : EditorWindow
{
    [MenuItem("Tools/ECHOES/Setup Resonance UI")]
    public static void SetupUI()
    {
        // 1. Canvas
        GameObject canvasObj = GameObject.Find("ResonanceCanvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("ResonanceCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create ResonanceCanvas");
        }

        // 2. Root (always active, holds script)
        GameObject rootObj = FindOrCreate("ResonanceUIRoot", canvasObj.transform, true);
        StretchFull(rootObj);

        ResonanceUI uiScript = rootObj.GetComponent<ResonanceUI>();
        if (uiScript == null) uiScript = rootObj.AddComponent<ResonanceUI>();

        // 3. Panel (toggled child)
        GameObject panelObj = FindOrCreate("ResonancePanel", rootObj.transform, false);
        StretchFull(panelObj);
        EnsureImage(panelObj, new Color(0, 0, 0, 0.85f));

        // ========== LEFT SIDE: FREQUENCY SLIDER ==========

        // Slider Container (left side)
        GameObject sliderContainer = FindOrCreate("SliderContainer", panelObj.transform, false);
        RectTransform sliderContainerRect = EnsureRect(sliderContainer);
        sliderContainerRect.anchorMin = new Vector2(0, 0.1f);
        sliderContainerRect.anchorMax = new Vector2(0.12f, 0.9f);
        sliderContainerRect.offsetMin = new Vector2(40, 0);
        sliderContainerRect.offsetMax = new Vector2(-10, 0);

        // Slider Label
        GameObject sliderLabel = FindOrCreate("SliderLabel", sliderContainer.transform, false);
        TextMeshProUGUI labelTmp = EnsureTMP(sliderLabel, "FREQUENCY", 18, Color.white);
        RectTransform labelRect = sliderLabel.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 1);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.pivot = new Vector2(0.5f, 1);
        labelRect.anchoredPosition = new Vector2(0, 20);
        labelRect.sizeDelta = new Vector2(0, 30);

        // Slider Track (the vertical bar background)
        GameObject trackObj = FindOrCreate("SliderTrack", sliderContainer.transform, false);
        RectTransform trackRect = EnsureRect(trackObj);
        trackRect.anchorMin = new Vector2(0.3f, 0.05f);
        trackRect.anchorMax = new Vector2(0.7f, 0.90f);
        trackRect.offsetMin = Vector2.zero;
        trackRect.offsetMax = Vector2.zero;
        EnsureImage(trackObj, new Color(0.15f, 0.15f, 0.15f, 1f));

        // Slider Fill (fills from bottom up)
        GameObject fillObj = FindOrCreate("SliderFill", trackObj.transform, false);
        RectTransform fillRect = EnsureRect(fillObj);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImg = EnsureImage(fillObj, new Color(0.2f, 0.5f, 1f, 0.4f));
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Vertical;
        fillImg.fillOrigin = 0; // Bottom
        fillImg.fillAmount = 0.5f;

        // Target Marker (shows where the target frequency is)
        GameObject targetMarkerObj = FindOrCreate("TargetMarker", trackObj.transform, false);
        RectTransform targetMarkerRect = EnsureRect(targetMarkerObj);
        targetMarkerRect.anchorMin = new Vector2(0, 0.5f);
        targetMarkerRect.anchorMax = new Vector2(1, 0.5f);
        targetMarkerRect.sizeDelta = new Vector2(0, 6);
        targetMarkerRect.anchoredPosition = Vector2.zero;
        EnsureImage(targetMarkerObj, new Color(1f, 0.3f, 0.3f, 0.9f));

        // Slider Handle (moves with frequency)
        GameObject handleObj = FindOrCreate("SliderHandle", trackObj.transform, false);
        RectTransform handleRect = EnsureRect(handleObj);
        handleRect.anchorMin = new Vector2(-0.3f, 0.5f);
        handleRect.anchorMax = new Vector2(1.3f, 0.5f);
        handleRect.sizeDelta = new Vector2(0, 12);
        handleRect.anchoredPosition = Vector2.zero;
        EnsureImage(handleObj, Color.green);

        // Freq Value Text (below slider)
        GameObject freqTextObj = FindOrCreate("FreqValueText", sliderContainer.transform, false);
        TextMeshProUGUI freqTmp = EnsureTMP(freqTextObj, "440 Hz", 22, Color.cyan);
        RectTransform freqRect = freqTextObj.GetComponent<RectTransform>();
        freqRect.anchorMin = new Vector2(0, 0);
        freqRect.anchorMax = new Vector2(1, 0);
        freqRect.pivot = new Vector2(0.5f, 0);
        freqRect.anchoredPosition = new Vector2(0, -10);
        freqRect.sizeDelta = new Vector2(0, 30);

        // ========== CENTER: WAVEFORMS ==========

        // Title
        GameObject titleObj = FindOrCreate("ResonanceTitle", panelObj.transform, false);
        TextMeshProUGUI titleTmp = EnsureTMP(titleObj, "RESONANCE PUZZLE", 32, Color.white);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.15f, 0.88f);
        titleRect.anchorMax = new Vector2(0.85f, 0.96f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        // Target Label
        GameObject targetLabelObj = FindOrCreate("TargetLabel", panelObj.transform, false);
        EnsureTMP(targetLabelObj, "TARGET WAVE", 20, new Color(1f, 0.3f, 0.3f));
        RectTransform targetLabelRect = targetLabelObj.GetComponent<RectTransform>();
        targetLabelRect.anchorMin = new Vector2(0.15f, 0.78f);
        targetLabelRect.anchorMax = new Vector2(0.5f, 0.85f);
        targetLabelRect.offsetMin = Vector2.zero;
        targetLabelRect.offsetMax = Vector2.zero;

        // Target Wave Image
        GameObject targetWaveObj = FindOrCreate("TargetWave", panelObj.transform, false);
        RawImage targetRaw = targetWaveObj.GetComponent<RawImage>();
        if (targetRaw == null) targetRaw = targetWaveObj.AddComponent<RawImage>();
        RectTransform targetWaveRect = EnsureRect(targetWaveObj);
        targetWaveRect.anchorMin = new Vector2(0.15f, 0.52f);
        targetWaveRect.anchorMax = new Vector2(0.85f, 0.78f);
        targetWaveRect.offsetMin = Vector2.zero;
        targetWaveRect.offsetMax = Vector2.zero;

        // Player Label
        GameObject playerLabelObj = FindOrCreate("PlayerLabel", panelObj.transform, false);
        EnsureTMP(playerLabelObj, "YOUR WAVE", 20, new Color(0.3f, 1f, 0.3f));
        RectTransform playerLabelRect = playerLabelObj.GetComponent<RectTransform>();
        playerLabelRect.anchorMin = new Vector2(0.15f, 0.42f);
        playerLabelRect.anchorMax = new Vector2(0.5f, 0.49f);
        playerLabelRect.offsetMin = Vector2.zero;
        playerLabelRect.offsetMax = Vector2.zero;

        // Player Wave Image
        GameObject playerWaveObj = FindOrCreate("PlayerWave", panelObj.transform, false);
        RawImage playerRaw = playerWaveObj.GetComponent<RawImage>();
        if (playerRaw == null) playerRaw = playerWaveObj.AddComponent<RawImage>();
        RectTransform playerWaveRect = EnsureRect(playerWaveObj);
        playerWaveRect.anchorMin = new Vector2(0.15f, 0.18f);
        playerWaveRect.anchorMax = new Vector2(0.85f, 0.42f);
        playerWaveRect.offsetMin = Vector2.zero;
        playerWaveRect.offsetMax = Vector2.zero;

        // Status Text
        GameObject statusObj = FindOrCreate("StatusText", panelObj.transform, false);
        TextMeshProUGUI statusTmp = EnsureTMP(statusObj, "ADJUST FREQUENCY", 36, Color.red);
        RectTransform statusRect = statusObj.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.15f, 0.05f);
        statusRect.anchorMax = new Vector2(0.85f, 0.15f);
        statusRect.offsetMin = Vector2.zero;
        statusRect.offsetMax = Vector2.zero;

        // Hint Text
        GameObject hintObj = FindOrCreate("HintText", panelObj.transform, false);
        EnsureTMP(hintObj, "[Mouse Scroll] Adjust Frequency", 16, new Color(0.5f, 0.5f, 0.5f));
        RectTransform hintRect = hintObj.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.15f, 0.01f);
        hintRect.anchorMax = new Vector2(0.85f, 0.05f);
        hintRect.offsetMin = Vector2.zero;
        hintRect.offsetMax = Vector2.zero;

        // ========== ASSIGN REFERENCES ==========
        uiScript.uiPanel = panelObj;
        uiScript.staticWaveImage = targetRaw;
        uiScript.playerWaveImage = playerRaw;
        uiScript.statusText = statusTmp;
        uiScript.sliderTrack = trackRect;
        uiScript.sliderHandle = handleRect;
        uiScript.sliderFill = fillImg;
        uiScript.freqValueText = freqTmp;
        uiScript.targetMarker = targetMarkerRect;

        // Hide panel initially
        panelObj.SetActive(false);
        rootObj.SetActive(true);

        EditorUtility.SetDirty(uiScript);
        Debug.Log("[ResonanceUISetup] Complete! Check ResonanceCanvas in hierarchy.");
        Selection.activeGameObject = rootObj;
    }

    // --- Helper Methods ---

    private static GameObject FindOrCreate(string name, Transform parent, bool stretch)
    {
        Transform found = parent.Find(name);
        if (found != null) return found.gameObject;

        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        if (stretch) StretchFull(obj);
        return obj;
    }

    private static void StretchFull(GameObject obj)
    {
        RectTransform rt = EnsureRect(obj);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static RectTransform EnsureRect(GameObject obj)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt == null) rt = obj.AddComponent<RectTransform>();
        return rt;
    }

    private static Image EnsureImage(GameObject obj, Color color)
    {
        Image img = obj.GetComponent<Image>();
        if (img == null) img = obj.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private static TextMeshProUGUI EnsureTMP(GameObject obj, string text, int fontSize, Color color)
    {
        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        if (tmp == null)
        {
            tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
        }
        return tmp;
    }
}
