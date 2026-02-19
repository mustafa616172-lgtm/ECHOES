using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// ECHOES - Sound Recording UI
/// Lightweight HUD overlay for recording/playback system.
/// Positioned bottom-left of screen, always visible when Sound Recorder device is held.
/// 
/// Shows:
/// - Current device mode (Tuner/Recorder/Playback)
/// - Recording progress bar (during recording)
/// - Clip slots 1-4 (occupied/empty/selected status)
/// - Status text (scanning, recording, playing, etc.)
/// - Battery level indicator
/// </summary>
[DisallowMultipleComponent]
public class SoundRecordingUI : MonoBehaviour
{
    public static SoundRecordingUI Instance;

    [Header("References")]
    public SoundRecorderDevice recorderDevice;

    // UI Elements (created programmatically)
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private GameObject uiPanel;

    // Mode indicator
    private Text modeText;

    // Recording progress bar
    private GameObject progressBarContainer;
    private Image progressBarFill;
    private Text progressText;

    // Battery bar
    private Image batteryBarFill;
    private Text batteryText;

    // Clip slots
    private GameObject[] slotContainers;
    private Image[] slotBackgrounds;
    private Text[] slotTexts;
    private Image[] slotBorders;

    // Status
    private Text statusText;
    private float statusTimer = 0f;

    // Animation state
    private float targetAlpha = 0f;
    private float currentAlpha = 0f;
    private float pulseTimer = 0f;

    // FIX #10: Cached font - created once, reused everywhere
    private Font cachedFont;

    // FIX #5: Track subscribed device instance
    private SoundRecorderDevice subscribedDevice = null;
    private Coroutine subscribeCoroutine;

    // Colors
    private Color tunerColor = new Color(1f, 0.4f, 0.2f);
    private Color recorderColor = new Color(1f, 0.15f, 0.15f);
    private Color playbackColor = new Color(0.2f, 0.8f, 1f);
    private Color emptySlotColor = new Color(0.2f, 0.2f, 0.25f, 0.7f);
    private Color occupiedSlotColor = new Color(0.3f, 0.15f, 0.4f, 0.9f);
    private Color selectedSlotColor = new Color(0.5f, 0.2f, 0.7f, 1f);
    private Color progressBgColor = new Color(0.15f, 0.15f, 0.2f, 0.8f);

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // FIX #10: Create font ONCE
        cachedFont = Font.CreateDynamicFontFromOSFont("Arial", 14);

        CreateUI();
    }

    void Start()
    {
        if (recorderDevice == null)
            recorderDevice = SoundRecorderDevice.Instance;

        // FIX #4: Subscribe with timeout
        subscribeCoroutine = StartCoroutine(SubscribeToEvents());
    }

    // FIX #4: Coroutine with timeout (30 seconds max)
    private IEnumerator SubscribeToEvents()
    {
        float elapsed = 0f;
        float timeout = 30f;

        while (SoundRecorderDevice.Instance == null && elapsed < timeout)
        {
            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

        if (SoundRecorderDevice.Instance != null)
        {
            SubscribeToDevice(SoundRecorderDevice.Instance);
        }
#if UNITY_EDITOR
        else
        {
            Debug.LogWarning("[SoundRecordingUI] Timeout waiting for SoundRecorderDevice. Will retry in Update.");
        }
#endif
        subscribeCoroutine = null;
    }

    private void SubscribeToDevice(SoundRecorderDevice device)
    {
        // Unsubscribe from old device
        if (subscribedDevice != null && subscribedDevice != device)
        {
            subscribedDevice.OnModeChanged -= OnModeChanged;
            subscribedDevice.OnRecordingStateChanged -= OnRecordingStateChanged;
            subscribedDevice.OnClipRecorded -= OnClipRecorded;
        }

        recorderDevice = device;
        subscribedDevice = device;
        subscribedDevice.OnModeChanged += OnModeChanged;
        subscribedDevice.OnRecordingStateChanged += OnRecordingStateChanged;
        subscribedDevice.OnClipRecorded += OnClipRecorded;
    }

    void Update()
    {
        if (recorderDevice == null)
        {
            recorderDevice = SoundRecorderDevice.Instance;
            if (recorderDevice == null) return;
        }

        // FIX #5: Re-subscribe if device instance changed
        if (SoundRecorderDevice.Instance != null && SoundRecorderDevice.Instance != subscribedDevice)
        {
            SubscribeToDevice(SoundRecorderDevice.Instance);
        }

        // Show/hide based on device state
        bool shouldShow = recorderDevice.IsActive &&
                          recorderDevice.CurrentMode != SoundRecorderDevice.DeviceMode.FrequencyTuner;
        targetAlpha = shouldShow ? 1f : 0f;

        // Smooth fade
        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * 8f);
        if (canvasGroup != null)
            canvasGroup.alpha = currentAlpha;

        if (currentAlpha < 0.01f) return;

        // Update all UI elements
        UpdateModeIndicator();
        UpdateProgressBar();
        UpdateClipSlots();
        UpdateStatusText();
        UpdateBatteryBar();

        pulseTimer += Time.deltaTime;
    }

    // ==========================================
    // UI CREATION (Programmatic)
    // ==========================================

    private void CreateUI()
    {
        // Canvas
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        gameObject.AddComponent<GraphicRaycaster>();

        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // Main panel (bottom-left)
        uiPanel = CreatePanel("SoundRecUI_Panel", transform,
            new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(20f, 20f), new Vector2(280f, 200f),
            new Color(0.05f, 0.05f, 0.08f, 0.85f));

        // Mode indicator text (top of panel)
        modeText = CreateText("ModeText", uiPanel.transform,
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(10f, -8f), new Vector2(260f, 25f),
            "SES KAYIT", 14, TextAnchor.MiddleLeft, recorderColor);

        // Separator line
        CreatePanel("Separator", uiPanel.transform,
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(10f, -35f), new Vector2(260f, 1f),
            new Color(0.4f, 0.4f, 0.5f, 0.5f));

        // Progress bar container
        progressBarContainer = CreatePanel("ProgressBar_BG", uiPanel.transform,
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(10f, -50f), new Vector2(260f, 14f),
            progressBgColor);

        // Progress bar fill
        GameObject fillObj = CreatePanel("ProgressBar_Fill", progressBarContainer.transform,
            new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(1f, 1f), new Vector2(0f, -1f),
            recorderColor);
        progressBarFill = fillObj.GetComponent<Image>();
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.offsetMin = new Vector2(1f, 1f);
        fillRect.offsetMax = new Vector2(1f, -1f);
        fillRect.sizeDelta = new Vector2(0f, -2f);

        // Progress text
        progressText = CreateText("ProgressText", progressBarContainer.transform,
            new Vector2(0f, 0f), new Vector2(1f, 1f),
            Vector2.zero, Vector2.zero,
            "", 10, TextAnchor.MiddleCenter, Color.white);
        RectTransform ptRect = progressText.GetComponent<RectTransform>();
        ptRect.anchorMin = Vector2.zero;
        ptRect.anchorMax = Vector2.one;
        ptRect.offsetMin = Vector2.zero;
        ptRect.offsetMax = Vector2.zero;

        progressBarContainer.SetActive(false);

        // Clip slots (4 slots in a row)
        CreateClipSlots(uiPanel.transform);

        // Battery bar
        CreateBatteryBar(uiPanel.transform);

        // Status text (bottom)
        statusText = CreateText("StatusText", uiPanel.transform,
            new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(10f, 10f), new Vector2(260f, 20f),
            "", 11, TextAnchor.MiddleLeft, new Color(0.7f, 0.7f, 0.8f));
    }

    private void CreateClipSlots(Transform parent)
    {
        int maxSlots = 4;
        slotContainers = new GameObject[maxSlots];
        slotBackgrounds = new Image[maxSlots];
        slotTexts = new Text[maxSlots];
        slotBorders = new Image[maxSlots];

        float slotWidth = 60f;
        float slotHeight = 50f;
        float spacing = 5f;
        float startX = 10f;
        float startY = -75f;

        for (int i = 0; i < maxSlots; i++)
        {
            // Slot container
            slotContainers[i] = CreatePanel("Slot_" + i, parent,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(startX + i * (slotWidth + spacing), startY),
                new Vector2(slotWidth, slotHeight),
                emptySlotColor);

            slotBackgrounds[i] = slotContainers[i].GetComponent<Image>();

            // Slot number
            CreateText("SlotNum_" + i, slotContainers[i].transform,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(3f, -2f), new Vector2(15f, 14f),
                (i + 1).ToString(), 9, TextAnchor.UpperLeft, new Color(0.5f, 0.5f, 0.6f));

            // Slot text (clip name or empty)
            slotTexts[i] = CreateText("SlotText_" + i, slotContainers[i].transform,
                new Vector2(0f, 0f), new Vector2(1f, 0.7f),
                new Vector2(2f, 2f), new Vector2(-2f, -2f),
                "[Bos]", 8, TextAnchor.MiddleCenter, new Color(0.4f, 0.4f, 0.5f));
            RectTransform stRect = slotTexts[i].GetComponent<RectTransform>();
            stRect.anchorMin = new Vector2(0f, 0f);
            stRect.anchorMax = new Vector2(1f, 0.75f);
            stRect.offsetMin = new Vector2(2f, 2f);
            stRect.offsetMax = new Vector2(-2f, 0f);

            // Border (for selected state)
            GameObject borderObj = new GameObject("Border_" + i);
            borderObj.transform.SetParent(slotContainers[i].transform, false);
            slotBorders[i] = borderObj.AddComponent<Image>();
            slotBorders[i].color = Color.clear;
            Outline outl = borderObj.AddComponent<Outline>();
            outl.effectColor = selectedSlotColor;
            outl.effectDistance = new Vector2(2, 2);
            outl.enabled = false;

            RectTransform bRect = borderObj.GetComponent<RectTransform>();
            bRect.anchorMin = Vector2.zero;
            bRect.anchorMax = Vector2.one;
            bRect.offsetMin = Vector2.zero;
            bRect.offsetMax = Vector2.zero;
        }
    }

    private void CreateBatteryBar(Transform parent)
    {
        // Battery label
        CreateText("BatteryLabel", parent,
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(10f, -135f), new Vector2(40f, 14f),
            "PIL:", 9, TextAnchor.MiddleLeft, new Color(0.5f, 0.5f, 0.6f));

        // Battery bar background
        GameObject batteryBg = CreatePanel("Battery_BG", parent,
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(50f, -135f), new Vector2(170f, 12f),
            new Color(0.15f, 0.15f, 0.2f, 0.8f));

        // Battery bar fill
        GameObject battFillObj = CreatePanel("Battery_Fill", batteryBg.transform,
            new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(1f, 1f), new Vector2(168f, -1f),
            Color.green);
        batteryBarFill = battFillObj.GetComponent<Image>();
        RectTransform bfRect = battFillObj.GetComponent<RectTransform>();
        bfRect.anchorMin = new Vector2(0f, 0f);
        bfRect.anchorMax = new Vector2(0f, 1f);
        bfRect.offsetMin = new Vector2(1f, 1f);
        bfRect.offsetMax = new Vector2(169f, -1f);

        // Battery percentage text
        batteryText = CreateText("BatteryText", batteryBg.transform,
            new Vector2(0f, 0f), new Vector2(1f, 1f),
            Vector2.zero, Vector2.zero,
            "100%", 9, TextAnchor.MiddleCenter, Color.white);
        RectTransform btRect = batteryText.GetComponent<RectTransform>();
        btRect.anchorMin = Vector2.zero;
        btRect.anchorMax = Vector2.one;
        btRect.offsetMin = Vector2.zero;
        btRect.offsetMax = Vector2.zero;
    }

    // ==========================================
    // UI UPDATES
    // ==========================================

    private void UpdateModeIndicator()
    {
        if (modeText == null || recorderDevice == null) return;

        switch (recorderDevice.CurrentMode)
        {
            case SoundRecorderDevice.DeviceMode.Recorder:
                string recIcon = (recorderDevice.CurrentRecordingState == SoundRecorderDevice.RecordingState.Recording)
                    ? "[ REC ]" : "[ KAYIT ]";
                modeText.text = recIcon + " SES KAYIT";
                modeText.color = recorderColor;
                break;
            case SoundRecorderDevice.DeviceMode.Playback:
                string playIcon = recorderDevice.IsPlayingClip ? ">> " : "> ";
                modeText.text = playIcon + "OYNATMA";
                modeText.color = playbackColor;
                break;
            default:
                modeText.text = "~ FREKANS";
                modeText.color = tunerColor;
                break;
        }
    }

    private void UpdateProgressBar()
    {
        if (progressBarContainer == null || recorderDevice == null) return;

        bool showProgress = recorderDevice.CurrentMode == SoundRecorderDevice.DeviceMode.Recorder &&
                            recorderDevice.CurrentRecordingState != SoundRecorderDevice.RecordingState.Idle;

        progressBarContainer.SetActive(showProgress);

        if (showProgress)
        {
            float progress = recorderDevice.RecordingProgress;
            RectTransform fillRect = progressBarFill.GetComponent<RectTransform>();
            RectTransform containerRect = progressBarContainer.GetComponent<RectTransform>();
            float maxWidth = containerRect.sizeDelta.x - 2f;
            fillRect.sizeDelta = new Vector2(maxWidth * progress, fillRect.sizeDelta.y);

            switch (recorderDevice.CurrentRecordingState)
            {
                case SoundRecorderDevice.RecordingState.Scanning:
                    float scanPulse = (Mathf.Sin(pulseTimer * 8f) + 1f) * 0.5f;
                    progressBarFill.color = Color.Lerp(new Color(0.2f, 0.2f, 0.8f), new Color(0.4f, 0.4f, 1f), scanPulse);
                    progressText.text = "TARANIYOR...";
                    break;
                case SoundRecorderDevice.RecordingState.Recording:
                    float recPulse = (Mathf.Sin(pulseTimer * 4f) + 1f) * 0.5f;
                    progressBarFill.color = Color.Lerp(recorderColor, new Color(1f, 0.4f, 0.2f), recPulse);
                    progressText.text = "KAYDEDILIYOR " + Mathf.FloorToInt(progress * 100f) + "%";
                    break;
                case SoundRecorderDevice.RecordingState.Done:
                    progressBarFill.color = Color.green;
                    progressText.text = "KAYDEDILDI!";
                    break;
            }
        }
    }

    private void UpdateClipSlots()
    {
        if (recorderDevice == null || slotContainers == null) return;

        for (int i = 0; i < slotContainers.Length; i++)
        {
            RecordedClipData clip = recorderDevice.GetClipAtSlot(i);
            bool isSelected = (recorderDevice.CurrentMode == SoundRecorderDevice.DeviceMode.Playback &&
                              recorderDevice.SelectedClipSlot == i);

            if (clip.hasClip)
            {
                slotBackgrounds[i].color = isSelected ? selectedSlotColor : occupiedSlotColor;
                slotTexts[i].text = TruncateString(clip.clipName, 8);
                slotTexts[i].color = isSelected ? Color.white : new Color(0.8f, 0.7f, 0.9f);

                Outline outline = slotBorders[i].GetComponent<Outline>();
                if (outline != null)
                {
                    outline.enabled = isSelected;
                    if (isSelected && recorderDevice.IsPlayingClip)
                    {
                        float glow = (Mathf.Sin(pulseTimer * 6f) + 1f) * 0.5f;
                        outline.effectColor = Color.Lerp(playbackColor, Color.white, glow);
                    }
                }
            }
            else
            {
                slotBackgrounds[i].color = isSelected ?
                    new Color(0.25f, 0.25f, 0.3f, 0.8f) : emptySlotColor;
                slotTexts[i].text = "[Bos]";
                slotTexts[i].color = new Color(0.4f, 0.4f, 0.5f);

                Outline outline = slotBorders[i].GetComponent<Outline>();
                if (outline != null) outline.enabled = false;
            }
        }
    }

    private void UpdateStatusText()
    {
        if (statusText == null || recorderDevice == null) return;

        if (statusTimer > 0f)
        {
            statusTimer -= Time.deltaTime;
            return;
        }

        switch (recorderDevice.CurrentMode)
        {
            case SoundRecorderDevice.DeviceMode.Recorder:
                switch (recorderDevice.CurrentRecordingState)
                {
                    case SoundRecorderDevice.RecordingState.Idle:
                        statusText.text = "[Sol Tik] Kayit baslat";
                        statusText.color = new Color(0.6f, 0.6f, 0.7f);
                        break;
                    case SoundRecorderDevice.RecordingState.Scanning:
                        statusText.text = "Ses kaynaklari taraniyor...";
                        statusText.color = new Color(0.4f, 0.4f, 1f);
                        break;
                    case SoundRecorderDevice.RecordingState.Recording:
                        statusText.text = "Kaydediliyor... [Sag Tik] iptal";
                        statusText.color = recorderColor;
                        break;
                    case SoundRecorderDevice.RecordingState.Done:
                        statusText.text = "Kayit tamamlandi!";
                        statusText.color = Color.green;
                        break;
                }
                break;

            case SoundRecorderDevice.DeviceMode.Playback:
                if (recorderDevice.IsPlayingClip)
                {
                    statusText.text = "Calinyor... [Sol Tik] durdur";
                    statusText.color = playbackColor;
                }
                else if (recorderDevice.GetRecordedClipCount() > 0)
                {
                    statusText.text = "[1-4] Slot sec, [Sol Tik] oynat";
                    statusText.color = new Color(0.6f, 0.6f, 0.7f);
                }
                else
                {
                    statusText.text = "Kayit yok. Once bir ses kaydet.";
                    statusText.color = new Color(0.5f, 0.5f, 0.5f);
                }
                break;

            default:
                statusText.text = "[F] Mod degistir";
                statusText.color = new Color(0.5f, 0.5f, 0.6f);
                break;
        }
    }

    private void UpdateBatteryBar()
    {
        if (batteryBarFill == null || recorderDevice == null) return;

        float battery = recorderDevice.BatteryNormalized;
        int percent = Mathf.FloorToInt(battery * 100f);

        // Update fill width
        RectTransform bfRect = batteryBarFill.GetComponent<RectTransform>();
        bfRect.offsetMax = new Vector2(1f + 168f * battery, bfRect.offsetMax.y);

        // Color based on battery level
        Color battColor;
        if (battery > 0.5f)
            battColor = Color.Lerp(Color.yellow, Color.green, (battery - 0.5f) * 2f);
        else if (battery > 0.2f)
            battColor = Color.Lerp(new Color(1f, 0.5f, 0f), Color.yellow, (battery - 0.2f) / 0.3f);
        else
        {
            float flash = Mathf.Sin(pulseTimer * 8f) * 0.5f + 0.5f;
            battColor = Color.Lerp(Color.red, new Color(1f, 0.3f, 0f), flash);
        }

        batteryBarFill.color = battColor;

        if (batteryText != null)
            batteryText.text = percent + "%";
    }

    // ==========================================
    // EVENT HANDLERS
    // ==========================================

    private void OnModeChanged(SoundRecorderDevice.DeviceMode newMode)
    {
        pulseTimer = 0f;
    }

    private void OnRecordingStateChanged(SoundRecorderDevice.RecordingState newState, float progress)
    {
        if (newState == SoundRecorderDevice.RecordingState.Done)
        {
            statusTimer = 2f;
            if (statusText != null)
            {
                statusText.text = "KAYIT TAMAMLANDI!";
                statusText.color = Color.green;
            }
        }
    }

    private void OnClipRecorded(int slotIndex, RecordedClipData clipData)
    {
#if UNITY_EDITOR
        Debug.Log("[SoundRecordingUI] New clip in slot " + (slotIndex + 1) + ": " + clipData.clipName);
#endif
    }

    // ==========================================
    // HELPER METHODS
    // ==========================================

    private GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        Image img = obj.AddComponent<Image>();
        img.color = color;

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = position + size * 0.5f;
        rect.sizeDelta = size;

        if (anchorMin == Vector2.zero && anchorMax == Vector2.zero)
        {
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = position;
        }

        return obj;
    }

    // FIX #10: Uses cached font instead of creating new per-call
    private Text CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax, string text, int fontSize, TextAnchor alignment, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        Text txt = obj.AddComponent<Text>();
        txt.text = text;
        txt.fontSize = fontSize;
        txt.alignment = alignment;
        txt.color = color;
        txt.font = cachedFont; // FIX #10: Reuse cached font
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Truncate;

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;

        if (anchorMin == anchorMax)
        {
            rect.pivot = anchorMin;
            rect.anchoredPosition = offsetMin;
            rect.sizeDelta = offsetMax;
        }
        else
        {
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        return txt;
    }

    private string TruncateString(string s, int maxLen)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.Length <= maxLen) return s;
        return s.Substring(0, maxLen - 1) + ".";
    }

    // ==========================================
    // CLEANUP
    // ==========================================

    void OnDisable()
    {
        if (subscribeCoroutine != null)
        {
            StopCoroutine(subscribeCoroutine);
            subscribeCoroutine = null;
        }
    }

    void OnDestroy()
    {
        if (subscribedDevice != null)
        {
            subscribedDevice.OnModeChanged -= OnModeChanged;
            subscribedDevice.OnRecordingStateChanged -= OnRecordingStateChanged;
            subscribedDevice.OnClipRecorded -= OnClipRecorded;
            subscribedDevice = null;
        }

        if (Instance == this)
            Instance = null;
    }
}