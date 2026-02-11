using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// ECHOES Inventory System v2
/// Features: Item management, Hotbar (1-4), Quick-use (R=Battery, H=Heal),
/// Tab inventory panel, Note reading, Pickup notifications.
/// </summary>
public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }
    
    // Item types
    public enum ItemType { Key, Battery, Note, HealthKit, Misc }
    
    [System.Serializable]
    public class InventoryItem
    {
        public string id;
        public string displayName;
        public ItemType type;
        public string description;
        public int quantity;
        
        public InventoryItem(string id, string name, ItemType type, string desc = "", int qty = 1)
        {
            this.id = id;
            this.displayName = name;
            this.type = type;
            this.description = desc;
            this.quantity = qty;
        }
    }
    
    // Inventory storage
    private List<InventoryItem> items = new List<InventoryItem>();
    
    // ============================================
    // HOTBAR
    // ============================================
    private const int HOTBAR_SLOTS = 4;
    private int selectedHotbarSlot = -1;
    private float hotbarHighlightTimer = 0f;
    
    // Hotbar UI
    private GameObject hotbarPanel;
    private Image[] hotbarSlotBgs;
    private Text[] hotbarSlotTexts;
    private Text[] hotbarSlotKeys;
    private Text hotbarHintText;
    
    // Item use cooldown
    private float lastUseTime = 0f;
    private float useCooldown = 0.5f;
    
    // Use feedback
    private GameObject useFeedbackPanel;
    private Text useFeedbackText;
    private float feedbackTimer = 0f;
    
    // ============================================
    // GRID INVENTORY UI
    // ============================================
    private const int GRID_COLS = 4;
    private const int GRID_ROWS = 4;
    private const int GRID_TOTAL = GRID_COLS * GRID_ROWS;
    
    private GameObject inventoryUI;
    private GameObject gridPanel;
    private GameObject[] gridCells;
    private Image[] gridCellBgs;
    private Text[] gridCellIcons;
    private Text[] gridCellQtys;
    private int hoveredCell = -1;
    
    // Tooltip
    private GameObject tooltipPanel;
    private Text tooltipTitle;
    private Text tooltipDesc;
    private Text tooltipHint;
    
    // Inventory header
    private Text inventoryTitle;
    private Text itemCountText;
    
    // Pickup notification
    private GameObject pickupNotification;
    private Text notificationText;
    private float notificationTimer;
    private bool isInventoryOpen = false;
    private float savedTimeScale = 1f;
    
    // Note reading
    private GameObject notePanel;
    private Text noteContentText;
    private bool isReadingNote = false;
    
    // Cached references to avoid FindObjectOfType every frame/use
    private FlashlightController cachedFlashlight;
    private PlayerHealth cachedPlayerHealth;
    
    // Journal / Lore System
    private struct JournalEntry
    {
        public string title;
        public string content;
        public string timestamp;
    }
    private List<JournalEntry> journalEntries = new List<JournalEntry>();
    private GameObject journalPanel;
    private Text journalTitleText;
    private Text journalContentText;
    private Text journalPageText;
    private Text journalCounterHUD;
    private int journalCurrentPage = 0;
    private bool isJournalOpen = false;
    private int totalNotesInGame = 12; // Designer can set this
    
    // Battery indicator
    private Text batteryIndicatorText;
    private Image batteryBarFill;
    
    // Events
    public delegate void ItemCollected(InventoryItem item);
    public event ItemCollected OnItemCollected;
    
    public delegate void ItemUsedEvent(InventoryItem item);
    public event ItemUsedEvent OnItemUsed;
    
    /// <summary>Is player currently reading a note?</summary>
    public bool IsReadingNote => isReadingNote;
    /// <summary>Is inventory panel open?</summary>
    public bool IsInventoryOpen => isInventoryOpen;
    
    void Awake()
    {
        Instance = this;
    }
    
    void Start()
    {
        CreateInventoryUI();
        CreateHotbarUI();
        CreateUseFeedback();
        CreateBatteryIndicator();
        CreateJournalPanel();
        CreateJournalCounterHUD();
        
        // Cache references
        cachedFlashlight = FindObjectOfType<FlashlightController>();
        cachedPlayerHealth = FindObjectOfType<PlayerHealth>();
        
        Debug.Log("[Inventory] System v2 initialized - Hotbar, Grid UI, Journal, Battery HUD");
    }
    
    void Update()
    {
        if (isReadingNote)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
                CloseNote();
            return; // Block all other input while reading
        }
        
        // Journal navigation
        if (isJournalOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.J))
                CloseJournal();
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                JournalNextPage();
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                JournalPrevPage();
            return; // Block other input while reading journal
        }
        
        // Pickup notification fade
        if (notificationTimer > 0f)
        {
            notificationTimer -= Time.unscaledDeltaTime;
            if (notificationTimer <= 0f && pickupNotification != null)
                pickupNotification.SetActive(false);
        }
        
        // Use feedback fade
        if (feedbackTimer > 0f)
        {
            feedbackTimer -= Time.unscaledDeltaTime;
            if (feedbackTimer <= 0f && useFeedbackPanel != null)
                useFeedbackPanel.SetActive(false);
        }
        
        // Hotbar highlight fade
        if (hotbarHighlightTimer > 0f)
        {
            hotbarHighlightTimer -= Time.unscaledDeltaTime;
            if (hotbarHighlightTimer <= 0f)
            {
                selectedHotbarSlot = -1;
                UpdateHotbarUI();
            }
        }
        
        // Toggle inventory with Tab or Escape to close
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // Block inventory if Map is open
            if (MapSystem.Instance != null && MapSystem.Instance.IsBigMapOpen) return;
            
            ToggleInventoryDisplay();
        }
        if (isInventoryOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseInventory();
        }
        
        // Grid hover & click (only when inventory open)
        if (isInventoryOpen)
        {
            HandleGridMouseInput();
        }
        
        // === JOURNAL (J key) ===
        if (!isInventoryOpen && Input.GetKeyDown(KeyCode.J))
        {
            OpenJournal();
        }
        
        // === HOTBAR INPUT (1-4) - only when inventory closed ===
        if (!isInventoryOpen)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SelectHotbarSlot(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SelectHotbarSlot(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SelectHotbarSlot(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SelectHotbarSlot(3);
            
            // === QUICK-USE KEYS ===
            if (Input.GetKeyDown(KeyCode.R)) QuickUseBattery();
            if (Input.GetKeyDown(KeyCode.H)) QuickUseHealthKit();
        }
        
        // Update battery indicator
        UpdateBatteryIndicator();
    }
    
    // ============================================
    // ITEM MANAGEMENT
    // ============================================
    
    /// <summary>Add an item to inventory</summary>
    public void AddItem(string id, string displayName, ItemType type, string description = "", int quantity = 1)
    {
        // Check if stackable item already exists
        InventoryItem existing = items.Find(i => i.id == id);
        if (existing != null && (type == ItemType.Battery || type == ItemType.HealthKit))
        {
            existing.quantity += quantity;
        }
        else
        {
            items.Add(new InventoryItem(id, displayName, type, description, quantity));
        }
        
        // Also add to KeyInventory for backward compatibility
        if (type == ItemType.Key && KeyInventory.Instance != null)
        {
            KeyInventory.Instance.AddKey(id);
        }
        
        ShowPickupNotification(displayName, type);
        UpdateItemCount();
        UpdateHotbarUI();
        OnItemCollected?.Invoke(items.Find(i => i.id == id));
        
        Debug.Log($"[Inventory] Added: {displayName} ({type})");
        
        // If it's a note, show it immediately
        if (type == ItemType.Note)
        {
            ShowNote(displayName, description);
        }
    }
    
    /// <summary>Use/consume an item by ID</summary>
    public bool UseItem(string id)
    {
        InventoryItem item = items.Find(i => i.id == id);
        if (item == null) return false;
        
        // Apply item effect based on type
        bool used = ApplyItemEffect(item);
        if (!used) return false;
        
        item.quantity--;
        OnItemUsed?.Invoke(item);
        
        if (item.quantity <= 0)
            items.Remove(item);
        
        UpdateItemCount();
        UpdateHotbarUI();
        Debug.Log($"[Inventory] Used: {item.displayName}");
        return true;
    }
    
    /// <summary>Use first item of given type</summary>
    public bool UseItemByType(ItemType type)
    {
        InventoryItem item = items.Find(i => i.type == type && i.quantity > 0);
        if (item == null) return false;
        return UseItem(item.id);
    }
    
    /// <summary>Check if player has item</summary>
    public bool HasItem(string id)
    {
        return items.Exists(i => i.id == id && i.quantity > 0);
    }
    
    /// <summary>Check if player has any item of type</summary>
    public bool HasItemOfType(ItemType type)
    {
        return items.Exists(i => i.type == type && i.quantity > 0);
    }
    
    /// <summary>Get item count</summary>
    public int GetItemCount(string id)
    {
        InventoryItem item = items.Find(i => i.id == id);
        return item != null ? item.quantity : 0;
    }
    
    /// <summary>Get total count of item type</summary>
    public int GetTypeCount(ItemType type)
    {
        int count = 0;
        foreach (var item in items)
            if (item.type == type) count += item.quantity;
        return count;
    }
    
    /// <summary>Get all items of a type</summary>
    public List<InventoryItem> GetItemsByType(ItemType type)
    {
        return items.FindAll(i => i.type == type);
    }
    
    // ============================================
    // ITEM EFFECTS
    // ============================================
    
    bool ApplyItemEffect(InventoryItem item)
    {
        switch (item.type)
        {
            case ItemType.Battery:
                return UseBattery();
            case ItemType.HealthKit:
                return UseHealthKit();
            case ItemType.Note:
                ShowNote(item.displayName, item.description);
                return false; // Don't consume notes
            default:
                return true; // Generic use
        }
    }
    
    bool UseBattery()
    {
        // Use cached reference instead of FindObjectOfType
        FlashlightController flashlight = cachedFlashlight;
        if (flashlight == null) flashlight = cachedFlashlight = FindObjectOfType<FlashlightController>();
        if (flashlight == null)
        {
            ShowUseFeedback("Fener bulunamadi!", new Color(1f, 0.3f, 0.3f));
            return false;
        }
        
        if (flashlight.GetBatteryPercentage() >= 0.95f)
        {
            ShowUseFeedback("Fener zaten dolu!", new Color(1f, 0.8f, 0.3f));
            return false;
        }
        
        flashlight.RechargeBattery(40f); // Each battery gives 40% charge
        ShowUseFeedback("Pil kullanildi! Fener sarj edildi", new Color(0.3f, 0.9f, 1f));
        Debug.Log("[Inventory] Battery used - Flashlight recharged +40%");
        return true;
    }
    
    bool UseHealthKit()
    {
        // Use cached reference instead of FindObjectOfType
        PlayerHealth health = cachedPlayerHealth;
        if (health == null) health = cachedPlayerHealth = FindObjectOfType<PlayerHealth>();
        if (health == null)
        {
            ShowUseFeedback("Saglik sistemi bulunamadi!", new Color(1f, 0.3f, 0.3f));
            return false;
        }
        
        if (health.HealthPercentage >= 0.95f)
        {
            ShowUseFeedback("Canin zaten dolu!", new Color(1f, 0.8f, 0.3f));
            return false;
        }
        
        health.Heal(35f); // Each kit heals 35 HP
        ShowUseFeedback("Saglik kiti kullanildi! +35 Can", new Color(0.2f, 1f, 0.2f));
        Debug.Log("[Inventory] Health kit used - Healed +35");
        return true;
    }
    
    // ============================================
    // HOTBAR SYSTEM
    // ============================================
    
    void SelectHotbarSlot(int slot)
    {
        selectedHotbarSlot = slot;
        hotbarHighlightTimer = 1.5f;
        UpdateHotbarUI();
        
        // Get the item type for this slot
        ItemType? slotType = GetHotbarSlotType(slot);
        if (slotType == null) return;
        
        // Auto-use on selection
        if (Time.time - lastUseTime < useCooldown) return;
        
        if (HasItemOfType(slotType.Value))
        {
            UseItemByType(slotType.Value);
            lastUseTime = Time.time;
        }
        else
        {
            ShowUseFeedback(GetEmptySlotMessage(slotType.Value), new Color(1f, 0.4f, 0.4f));
        }
    }
    
    ItemType? GetHotbarSlotType(int slot)
    {
        switch (slot)
        {
            case 0: return ItemType.Key;
            case 1: return ItemType.Battery;
            case 2: return ItemType.HealthKit;
            case 3: return ItemType.Note;
            default: return null;
        }
    }
    
    string GetEmptySlotMessage(ItemType type)
    {
        switch (type)
        {
            case ItemType.Key: return "Anahtar yok!";
            case ItemType.Battery: return "Pil yok!";
            case ItemType.HealthKit: return "Saglik kiti yok!";
            case ItemType.Note: return "Not yok!";
            default: return "Bos slot!";
        }
    }
    
    void QuickUseBattery()
    {
        if (Time.time - lastUseTime < useCooldown) return;
        
        if (HasItemOfType(ItemType.Battery))
        {
            UseItemByType(ItemType.Battery);
            lastUseTime = Time.time;
            selectedHotbarSlot = 1;
            hotbarHighlightTimer = 1f;
            UpdateHotbarUI();
        }
        else
        {
            ShowUseFeedback("Pil yok! Objeleri arayarak pil bul", new Color(1f, 0.4f, 0.4f));
        }
    }
    
    void QuickUseHealthKit()
    {
        if (Time.time - lastUseTime < useCooldown) return;
        
        if (HasItemOfType(ItemType.HealthKit))
        {
            UseItemByType(ItemType.HealthKit);
            lastUseTime = Time.time;
            selectedHotbarSlot = 2;
            hotbarHighlightTimer = 1f;
            UpdateHotbarUI();
        }
        else
        {
            ShowUseFeedback("Saglik kiti yok!", new Color(1f, 0.4f, 0.4f));
        }
    }
    
    // ============================================
    // NOTE & JOURNAL SYSTEM
    // ============================================
    
    void ShowNote(string title, string content)
    {
        if (notePanel == null) CreateNotePanel();
        
        // Save to journal if not already saved
        bool alreadySaved = journalEntries.Exists(e => e.title == title);
        if (!alreadySaved)
        {
            journalEntries.Add(new JournalEntry
            {
                title = title,
                content = content,
                timestamp = System.DateTime.Now.ToString("HH:mm")
            });
            UpdateJournalCounterHUD();
            Debug.Log($"[Journal] New entry added: {title} ({journalEntries.Count}/{totalNotesInGame})");
        }
        
        noteContentText.text = $"<b>{title}</b>\n\n{content}\n\n<i>[E ile kapat]</i>";
        notePanel.SetActive(true);
        isReadingNote = true;
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    void CloseNote()
    {
        if (notePanel != null)
            notePanel.SetActive(false);
        isReadingNote = false;
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    // === JOURNAL ===
    
    void OpenJournal()
    {
        if (journalPanel == null) return;
        if (journalEntries.Count == 0)
        {
            ShowUseFeedback("Henuz not bulunamadi!", new Color(0.7f, 0.7f, 0.7f));
            return;
        }
        
        isJournalOpen = true;
        journalPanel.SetActive(true);
        journalCurrentPage = journalEntries.Count - 1; // Start with latest
        RefreshJournalPage();
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        Debug.Log("[Journal] Opened");
    }
    
    void CloseJournal()
    {
        isJournalOpen = false;
        if (journalPanel != null)
            journalPanel.SetActive(false);
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        Debug.Log("[Journal] Closed");
    }
    
    void JournalNextPage()
    {
        if (journalEntries.Count == 0) return;
        journalCurrentPage = (journalCurrentPage + 1) % journalEntries.Count;
        RefreshJournalPage();
    }
    
    void JournalPrevPage()
    {
        if (journalEntries.Count == 0) return;
        journalCurrentPage--;
        if (journalCurrentPage < 0) journalCurrentPage = journalEntries.Count - 1;
        RefreshJournalPage();
    }
    
    void RefreshJournalPage()
    {
        if (journalTitleText == null || journalContentText == null) return;
        
        JournalEntry entry = journalEntries[journalCurrentPage];
        
        journalTitleText.text = $"<color=#C8B896>{entry.title}</color>";
        journalContentText.text = entry.content;
        
        if (journalPageText != null)
            journalPageText.text = $"Sayfa {journalCurrentPage + 1} / {journalEntries.Count}   |   [A] Onceki  [D] Sonraki  [J] Kapat";
    }
    
    void CreateJournalPanel()
    {
        GameObject canvasObj = GameObject.Find("PlayerHUDCanvas");
        if (canvasObj == null) return;
        
        journalPanel = new GameObject("JournalPanel");
        journalPanel.transform.SetParent(canvasObj.transform, false);
        
        // Full-screen dim
        Image dimBg = journalPanel.AddComponent<Image>();
        dimBg.color = new Color(0, 0, 0, 0.7f);
        dimBg.raycastTarget = true;
        RectTransform dimRect = dimBg.rectTransform;
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;
        
        // Journal frame (old paper look)
        GameObject frame = new GameObject("JournalFrame");
        frame.transform.SetParent(journalPanel.transform, false);
        Image frameBg = frame.AddComponent<Image>();
        frameBg.color = new Color(0.08f, 0.06f, 0.04f, 0.95f);
        frameBg.raycastTarget = false;
        RectTransform frameRect = frameBg.rectTransform;
        frameRect.anchorMin = new Vector2(0.5f, 0.5f);
        frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.pivot = new Vector2(0.5f, 0.5f);
        frameRect.sizeDelta = new Vector2(500, 400);
        
        // Border
        Outline frameOutline = frame.AddComponent<Outline>();
        frameOutline.effectColor = new Color(0.4f, 0.3f, 0.15f, 0.7f);
        frameOutline.effectDistance = new Vector2(2, -2);
        
        // Header: "GUNLUK"
        GameObject headerObj = new GameObject("Header");
        headerObj.transform.SetParent(frame.transform, false);
        Text headerText = headerObj.AddComponent<Text>();
        headerText.text = "GUNLUK";
        headerText.fontSize = 20;
        headerText.fontStyle = FontStyle.BoldAndItalic;
        headerText.alignment = TextAnchor.MiddleCenter;
        headerText.color = new Color(0.8f, 0.7f, 0.5f, 1f);
        headerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform hRect = headerText.rectTransform;
        hRect.anchorMin = new Vector2(0, 1);
        hRect.anchorMax = new Vector2(1, 1);
        hRect.pivot = new Vector2(0.5f, 1);
        hRect.anchoredPosition = new Vector2(0, -10);
        hRect.sizeDelta = new Vector2(0, 30);
        
        // Divider line under header
        GameObject divider = new GameObject("Divider");
        divider.transform.SetParent(frame.transform, false);
        Image divImg = divider.AddComponent<Image>();
        divImg.color = new Color(0.4f, 0.3f, 0.15f, 0.5f);
        divImg.raycastTarget = false;
        RectTransform divRect = divImg.rectTransform;
        divRect.anchorMin = new Vector2(0.1f, 1);
        divRect.anchorMax = new Vector2(0.9f, 1);
        divRect.pivot = new Vector2(0.5f, 1);
        divRect.anchoredPosition = new Vector2(0, -42);
        divRect.sizeDelta = new Vector2(0, 2);
        
        // Note title
        GameObject titleObj = new GameObject("NoteTitle");
        titleObj.transform.SetParent(frame.transform, false);
        journalTitleText = titleObj.AddComponent<Text>();
        journalTitleText.text = "";
        journalTitleText.fontSize = 16;
        journalTitleText.fontStyle = FontStyle.Bold;
        journalTitleText.alignment = TextAnchor.UpperLeft;
        journalTitleText.color = new Color(0.85f, 0.75f, 0.55f);
        journalTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        journalTitleText.supportRichText = true;
        RectTransform tRect = journalTitleText.rectTransform;
        tRect.anchorMin = new Vector2(0, 1);
        tRect.anchorMax = new Vector2(1, 1);
        tRect.pivot = new Vector2(0, 1);
        tRect.anchoredPosition = new Vector2(25, -50);
        tRect.sizeDelta = new Vector2(-50, 25);
        
        // Note content
        GameObject contentObj = new GameObject("NoteContent");
        contentObj.transform.SetParent(frame.transform, false);
        journalContentText = contentObj.AddComponent<Text>();
        journalContentText.text = "";
        journalContentText.fontSize = 14;
        journalContentText.alignment = TextAnchor.UpperLeft;
        journalContentText.color = new Color(0.75f, 0.7f, 0.6f);
        journalContentText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        journalContentText.lineSpacing = 1.4f;
        journalContentText.supportRichText = true;
        RectTransform cRect = journalContentText.rectTransform;
        cRect.anchorMin = new Vector2(0, 0);
        cRect.anchorMax = new Vector2(1, 1);
        cRect.offsetMin = new Vector2(25, 40);
        cRect.offsetMax = new Vector2(-25, -80);
        
        // Footer (page navigation)
        GameObject footerObj = new GameObject("Footer");
        footerObj.transform.SetParent(frame.transform, false);
        journalPageText = footerObj.AddComponent<Text>();
        journalPageText.text = "";
        journalPageText.fontSize = 11;
        journalPageText.alignment = TextAnchor.MiddleCenter;
        journalPageText.color = new Color(0.5f, 0.45f, 0.35f, 0.8f);
        journalPageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform fRect = journalPageText.rectTransform;
        fRect.anchorMin = new Vector2(0, 0);
        fRect.anchorMax = new Vector2(1, 0);
        fRect.pivot = new Vector2(0.5f, 0);
        fRect.anchoredPosition = new Vector2(0, 10);
        fRect.sizeDelta = new Vector2(0, 22);
        
        journalPanel.SetActive(false);
    }
    
    void CreateJournalCounterHUD()
    {
        GameObject canvasObj = GameObject.Find("PlayerHUDCanvas");
        if (canvasObj == null) return;
        
        GameObject counterObj = new GameObject("JournalCounter");
        counterObj.transform.SetParent(canvasObj.transform, false);
        journalCounterHUD = counterObj.AddComponent<Text>();
        journalCounterHUD.text = "";
        journalCounterHUD.fontSize = 11;
        journalCounterHUD.fontStyle = FontStyle.Bold;
        journalCounterHUD.alignment = TextAnchor.MiddleLeft;
        journalCounterHUD.color = new Color(0.7f, 0.65f, 0.5f, 0.7f);
        journalCounterHUD.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        
        Outline cOutline = counterObj.AddComponent<Outline>();
        cOutline.effectColor = Color.black;
        cOutline.effectDistance = new Vector2(1, -1);
        
        RectTransform cRect = journalCounterHUD.rectTransform;
        cRect.anchorMin = new Vector2(0, 0);
        cRect.anchorMax = new Vector2(0, 0);
        cRect.pivot = new Vector2(0, 0);
        cRect.anchoredPosition = new Vector2(15, 75);
        cRect.sizeDelta = new Vector2(200, 16);
        
        UpdateJournalCounterHUD();
    }
    
    void UpdateJournalCounterHUD()
    {
        if (journalCounterHUD == null) return;
        
        if (journalEntries.Count > 0)
        {
            journalCounterHUD.text = $"Notlar: {journalEntries.Count}/{totalNotesInGame} [J]";
            
            // Color based on progress
            float progress = (float)journalEntries.Count / totalNotesInGame;
            if (progress >= 1f)
                journalCounterHUD.color = new Color(1f, 0.85f, 0.2f, 0.9f); // Gold - complete
            else if (progress >= 0.5f)
                journalCounterHUD.color = new Color(0.7f, 0.65f, 0.5f, 0.8f); // Warm
            else
                journalCounterHUD.color = new Color(0.5f, 0.5f, 0.5f, 0.6f); // Dim
        }
        else
        {
            journalCounterHUD.text = "";
        }
    }
    
    // ============================================
    // GRID INVENTORY DISPLAY
    // ============================================
    
    void ToggleInventoryDisplay()
    {
        if (isInventoryOpen)
            CloseInventory();
        else
            OpenInventory();
    }
    
    void OpenInventory()
    {
        if (gridPanel == null) return;
        
        isInventoryOpen = true;
        gridPanel.SetActive(true);
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
        
        // Show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        RefreshGrid();
        Debug.Log("[Inventory] Grid opened");
    }
    
    void CloseInventory()
    {
        isInventoryOpen = false;
        if (gridPanel != null) gridPanel.SetActive(false);
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
        hoveredCell = -1;
        
        // Hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        Debug.Log("[Inventory] Grid closed");
    }
    
    void RefreshGrid()
    {
        if (gridCellBgs == null) return;
        
        // Update title
        if (inventoryTitle != null)
            inventoryTitle.text = $"ENVANTER ({items.Count}/{GRID_TOTAL})";
        
        for (int i = 0; i < GRID_TOTAL; i++)
        {
            if (i < items.Count)
            {
                InventoryItem item = items[i];
                Color typeColor = GetTypeColor(item.type);
                
                // Cell has item
                gridCellBgs[i].color = new Color(typeColor.r * 0.15f, typeColor.g * 0.15f, typeColor.b * 0.15f, 0.85f);
                gridCellIcons[i].text = GetTypeIcon(item.type);
                gridCellIcons[i].color = typeColor;
                gridCellQtys[i].text = item.quantity > 1 ? $"x{item.quantity}" : "";
                gridCellQtys[i].color = Color.white;
            }
            else
            {
                // Empty cell
                gridCellBgs[i].color = new Color(0.06f, 0.06f, 0.1f, 0.5f);
                gridCellIcons[i].text = "";
                gridCellQtys[i].text = "";
            }
        }
    }
    
    void HandleGridMouseInput()
    {
        if (gridCells == null) return;
        
        Vector2 mousePos = Input.mousePosition;
        int newHovered = -1;
        
        // Check which cell the mouse is over
        for (int i = 0; i < GRID_TOTAL; i++)
        {
            if (gridCells[i] == null) continue;
            RectTransform rt = gridCells[i].GetComponent<RectTransform>();
            if (rt == null) continue;
            
            // Convert mouse position to check against rect
            Canvas canvas = gridPanel.GetComponentInParent<Canvas>();
            if (canvas == null) continue;
            
            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (RectTransformUtility.RectangleContainsScreenPoint(rt, mousePos, cam))
            {
                newHovered = i;
                break;
            }
        }
        
        // Update hover state
        if (newHovered != hoveredCell)
        {
            // Unhighlight old
            if (hoveredCell >= 0 && hoveredCell < GRID_TOTAL)
            {
                RefreshSingleCell(hoveredCell, false);
            }
            
            hoveredCell = newHovered;
            
            // Highlight new
            if (hoveredCell >= 0 && hoveredCell < GRID_TOTAL)
            {
                RefreshSingleCell(hoveredCell, true);
                ShowTooltip(hoveredCell);
            }
            else
            {
                HideTooltip();
            }
        }
        
        // Right-click to use item
        if (Input.GetMouseButtonDown(1) && hoveredCell >= 0 && hoveredCell < items.Count)
        {
            InventoryItem item = items[hoveredCell];
            UseItem(item.id);
            RefreshGrid();
            
            // Re-check hover after item was used
            if (hoveredCell < items.Count)
                ShowTooltip(hoveredCell);
            else
                HideTooltip();
        }
    }
    
    void RefreshSingleCell(int index, bool highlighted)
    {
        if (index < 0 || index >= GRID_TOTAL) return;
        
        if (index < items.Count)
        {
            Color typeColor = GetTypeColor(items[index].type);
            float brightness = highlighted ? 0.35f : 0.15f;
            gridCellBgs[index].color = new Color(
                typeColor.r * brightness, typeColor.g * brightness, typeColor.b * brightness, 
                highlighted ? 0.95f : 0.85f);
        }
        else
        {
            gridCellBgs[index].color = new Color(0.06f, 0.06f, 0.1f, highlighted ? 0.65f : 0.5f);
        }
    }
    
    void ShowTooltip(int cellIndex)
    {
        if (tooltipPanel == null || tooltipTitle == null) return;
        
        if (cellIndex < 0 || cellIndex >= items.Count)
        {
            HideTooltip();
            return;
        }
        
        InventoryItem item = items[cellIndex];
        Color typeColor = GetTypeColor(item.type);
        
        tooltipTitle.text = item.displayName;
        tooltipTitle.color = typeColor;
        
        string desc = string.IsNullOrEmpty(item.description) ? GetDefaultDesc(item.type) : item.description;
        tooltipDesc.text = desc;
        
        tooltipHint.text = GetUseHint(item.type);
        
        tooltipPanel.SetActive(true);
        
        // Position tooltip near mouse
        RectTransform ttRect = tooltipPanel.GetComponent<RectTransform>();
        Vector2 mousePos = Input.mousePosition;
        ttRect.position = new Vector3(mousePos.x + 15, mousePos.y + 10, 0);
    }
    
    void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }
    
    string GetDefaultDesc(ItemType type)
    {
        switch (type)
        {
            case ItemType.Key: return "Kapali bir kapiyi acar.";
            case ItemType.Battery: return "Fenerin bataryasini sarj eder.";
            case ItemType.HealthKit: return "Kayip canini yeniler.";
            case ItemType.Note: return "Okunayi bekleyen bir yazi.";
            default: return "Bilinmeyen obje.";
        }
    }
    
    string GetUseHint(ItemType type)
    {
        switch (type)
        {
            case ItemType.Battery: return "[Sag Tikla] veya [R] Kullan";
            case ItemType.HealthKit: return "[Sag Tikla] veya [H] Kullan";
            case ItemType.Note: return "[Sag Tikla] Oku";
            case ItemType.Key: return "Kapida otomatik kullanilir";
            default: return "[Sag Tikla] Kullan";
        }
    }
    
    // ============================================
    // UI CREATION
    // ============================================
    
    void CreateInventoryUI()
    {
        GameObject canvasObj = GameObject.Find("PlayerHUDCanvas");
        if (canvasObj == null) return;
        
        // === Item count display (bottom-left, above MIC) ===
        GameObject countObj = new GameObject("ItemCount");
        countObj.transform.SetParent(canvasObj.transform, false);
        itemCountText = countObj.AddComponent<Text>();
        itemCountText.text = "";
        itemCountText.fontSize = 13;
        itemCountText.fontStyle = FontStyle.Bold;
        itemCountText.alignment = TextAnchor.MiddleLeft;
        itemCountText.color = new Color(0.8f, 0.8f, 0.8f, 0.9f);
        itemCountText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        
        Outline countOutline = countObj.AddComponent<Outline>();
        countOutline.effectColor = Color.black;
        countOutline.effectDistance = new Vector2(1, -1);
        
        RectTransform countRect = itemCountText.rectTransform;
        countRect.anchorMin = new Vector2(0, 0);
        countRect.anchorMax = new Vector2(0, 0);
        countRect.pivot = new Vector2(0, 0);
        countRect.anchoredPosition = new Vector2(15, 115);
        countRect.sizeDelta = new Vector2(250, 20);
        
        // === Pickup notification (center-bottom) ===
        pickupNotification = new GameObject("PickupNotif");
        pickupNotification.transform.SetParent(canvasObj.transform, false);
        
        Image notifBg = pickupNotification.AddComponent<Image>();
        notifBg.color = new Color(0, 0, 0, 0.75f);
        notifBg.raycastTarget = false;
        RectTransform notifRect = notifBg.rectTransform;
        notifRect.anchorMin = new Vector2(0.5f, 0);
        notifRect.anchorMax = new Vector2(0.5f, 0);
        notifRect.pivot = new Vector2(0.5f, 0);
        notifRect.anchoredPosition = new Vector2(0, 30);
        notifRect.sizeDelta = new Vector2(350, 40);
        
        GameObject notifTextObj = new GameObject("Text");
        notifTextObj.transform.SetParent(pickupNotification.transform, false);
        notificationText = notifTextObj.AddComponent<Text>();
        notificationText.fontSize = 14;
        notificationText.fontStyle = FontStyle.Bold;
        notificationText.alignment = TextAnchor.MiddleCenter;
        notificationText.color = Color.white;
        notificationText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform textRect = notificationText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        pickupNotification.SetActive(false);
        
        // === GRID INVENTORY PANEL (center screen) ===
        CreateGridPanel(canvasObj);
        CreateTooltipPanel(canvasObj);
    }
    
    void CreateHotbarUI()
    {
        GameObject canvasObj = GameObject.Find("PlayerHUDCanvas");
        if (canvasObj == null) return;
        
        // Hotbar container (bottom-center)
        hotbarPanel = new GameObject("HotbarPanel");
        hotbarPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform hotbarRect = hotbarPanel.AddComponent<RectTransform>();
        hotbarRect.anchorMin = new Vector2(0.5f, 0);
        hotbarRect.anchorMax = new Vector2(0.5f, 0);
        hotbarRect.pivot = new Vector2(0.5f, 0);
        hotbarRect.anchoredPosition = new Vector2(0, 80);
        hotbarRect.sizeDelta = new Vector2(260, 50);
        
        hotbarSlotBgs = new Image[HOTBAR_SLOTS];
        hotbarSlotTexts = new Text[HOTBAR_SLOTS];
        hotbarSlotKeys = new Text[HOTBAR_SLOTS];
        
        string[] slotIcons = { "K", "P", "+", "N" }; // Key, Pil, Health, Not
        string[] slotNames = { "1", "2", "3", "4" };
        Color[] slotColors = {
            new Color(1f, 0.85f, 0.2f),   // Gold - Key
            new Color(0.3f, 0.9f, 1f),    // Cyan - Battery
            new Color(0.2f, 1f, 0.2f),    // Green - Health
            new Color(0.9f, 0.85f, 0.7f)  // Parchment - Note
        };
        
        float slotSize = 48f;
        float spacing = 6f;
        float totalWidth = HOTBAR_SLOTS * slotSize + (HOTBAR_SLOTS - 1) * spacing;
        float startX = -totalWidth / 2f + slotSize / 2f;
        
        for (int i = 0; i < HOTBAR_SLOTS; i++)
        {
            // Slot background
            GameObject slotObj = new GameObject($"Slot_{i}");
            slotObj.transform.SetParent(hotbarPanel.transform, false);
            
            Image slotBg = slotObj.AddComponent<Image>();
            slotBg.color = new Color(0.08f, 0.08f, 0.12f, 0.7f);
            slotBg.raycastTarget = false;
            hotbarSlotBgs[i] = slotBg;
            
            RectTransform slotRect = slotBg.rectTransform;
            slotRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = new Vector2(startX + i * (slotSize + spacing), 0);
            slotRect.sizeDelta = new Vector2(slotSize, slotSize);
            
            Outline slotOutline = slotObj.AddComponent<Outline>();
            slotOutline.effectColor = new Color(slotColors[i].r, slotColors[i].g, slotColors[i].b, 0.3f);
            slotOutline.effectDistance = new Vector2(1, -1);
            
            // Item icon text
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slotObj.transform, false);
            Text iconText = iconObj.AddComponent<Text>();
            iconText.text = slotIcons[i];
            iconText.fontSize = 18;
            iconText.fontStyle = FontStyle.Bold;
            iconText.alignment = TextAnchor.MiddleCenter;
            iconText.color = new Color(slotColors[i].r, slotColors[i].g, slotColors[i].b, 0.6f);
            iconText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hotbarSlotTexts[i] = iconText;
            
            RectTransform iconRect = iconText.rectTransform;
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(0, 4);
            iconRect.offsetMax = new Vector2(0, 0);
            
            // Key number label (top-left corner)
            GameObject keyObj = new GameObject("KeyLabel");
            keyObj.transform.SetParent(slotObj.transform, false);
            Text keyText = keyObj.AddComponent<Text>();
            keyText.text = slotNames[i];
            keyText.fontSize = 10;
            keyText.alignment = TextAnchor.UpperLeft;
            keyText.color = new Color(0.5f, 0.5f, 0.6f, 0.8f);
            keyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hotbarSlotKeys[i] = keyText;
            
            RectTransform keyRect = keyText.rectTransform;
            keyRect.anchorMin = Vector2.zero;
            keyRect.anchorMax = Vector2.one;
            keyRect.offsetMin = new Vector2(3, 0);
            keyRect.offsetMax = new Vector2(0, -1);
        }
        
        // Hotbar hint text (below)
        GameObject hintObj = new GameObject("HotbarHint");
        hintObj.transform.SetParent(hotbarPanel.transform, false);
        hotbarHintText = hintObj.AddComponent<Text>();
        hotbarHintText.text = "[R] Pil  [H] Iyiles  [Tab] Envanter";
        hotbarHintText.fontSize = 10;
        hotbarHintText.alignment = TextAnchor.UpperCenter;
        hotbarHintText.color = new Color(0.4f, 0.4f, 0.5f, 0.6f);
        hotbarHintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        
        RectTransform hintRect = hotbarHintText.rectTransform;
        hintRect.anchorMin = new Vector2(0.5f, 0);
        hintRect.anchorMax = new Vector2(0.5f, 0);
        hintRect.pivot = new Vector2(0.5f, 1);
        hintRect.anchoredPosition = new Vector2(0, -5);
        hintRect.sizeDelta = new Vector2(300, 15);
        
        UpdateHotbarUI();
    }
    
    void CreateUseFeedback()
    {
        GameObject canvasObj = GameObject.Find("PlayerHUDCanvas");
        if (canvasObj == null) return;
        
        useFeedbackPanel = new GameObject("UseFeedback");
        useFeedbackPanel.transform.SetParent(canvasObj.transform, false);
        
        Image bg = useFeedbackPanel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.7f);
        bg.raycastTarget = false;
        RectTransform bgRect = bg.rectTransform;
        bgRect.anchorMin = new Vector2(0.5f, 0);
        bgRect.anchorMax = new Vector2(0.5f, 0);
        bgRect.pivot = new Vector2(0.5f, 0);
        bgRect.anchoredPosition = new Vector2(0, 135);
        bgRect.sizeDelta = new Vector2(300, 30);
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(useFeedbackPanel.transform, false);
        useFeedbackText = textObj.AddComponent<Text>();
        useFeedbackText.fontSize = 13;
        useFeedbackText.fontStyle = FontStyle.Bold;
        useFeedbackText.alignment = TextAnchor.MiddleCenter;
        useFeedbackText.color = Color.white;
        useFeedbackText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        
        RectTransform textRect = useFeedbackText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        useFeedbackPanel.SetActive(false);
    }
    
    void CreateBatteryIndicator()
    {
        GameObject canvasObj = GameObject.Find("PlayerHUDCanvas");
        if (canvasObj == null) return;
        
        // Battery bar container (bottom-left, below item count)
        GameObject batteryObj = new GameObject("BatteryIndicator");
        batteryObj.transform.SetParent(canvasObj.transform, false);
        
        // Label
        batteryIndicatorText = batteryObj.AddComponent<Text>();
        batteryIndicatorText.text = "";
        batteryIndicatorText.fontSize = 11;
        batteryIndicatorText.alignment = TextAnchor.MiddleLeft;
        batteryIndicatorText.color = new Color(0.3f, 0.8f, 1f, 0.8f);
        batteryIndicatorText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        
        Outline battOutline = batteryObj.AddComponent<Outline>();
        battOutline.effectColor = Color.black;
        battOutline.effectDistance = new Vector2(1, -1);
        
        RectTransform battRect = batteryIndicatorText.rectTransform;
        battRect.anchorMin = new Vector2(0, 0);
        battRect.anchorMax = new Vector2(0, 0);
        battRect.pivot = new Vector2(0, 0);
        battRect.anchoredPosition = new Vector2(15, 95);
        battRect.sizeDelta = new Vector2(200, 16);
    }
    
    void CreateNotePanel()
    {
        GameObject canvasObj = GameObject.Find("PlayerHUDCanvas");
        if (canvasObj == null) return;
        
        notePanel = new GameObject("NotePanel");
        notePanel.transform.SetParent(canvasObj.transform, false);
        
        Image bg = notePanel.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.02f, 0.04f, 0.95f);
        bg.raycastTarget = false;
        RectTransform bgRect = bg.rectTransform;
        bgRect.anchorMin = new Vector2(0.2f, 0.15f);
        bgRect.anchorMax = new Vector2(0.8f, 0.85f);
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        // Inner border
        GameObject border = new GameObject("Border");
        border.transform.SetParent(notePanel.transform, false);
        Image borderImg = border.AddComponent<Image>();
        borderImg.color = new Color(0.4f, 0.3f, 0.2f, 0.6f);
        borderImg.raycastTarget = false;
        RectTransform borderRect = borderImg.rectTransform;
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(3, 3);
        borderRect.offsetMax = new Vector2(-3, -3);
        
        // Note content
        GameObject textObj = new GameObject("NoteContent");
        textObj.transform.SetParent(notePanel.transform, false);
        noteContentText = textObj.AddComponent<Text>();
        noteContentText.fontSize = 16;
        noteContentText.alignment = TextAnchor.UpperLeft;
        noteContentText.color = new Color(0.85f, 0.8f, 0.7f);
        noteContentText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        noteContentText.supportRichText = true;
        noteContentText.lineSpacing = 1.3f;
        RectTransform ntRect = noteContentText.rectTransform;
        ntRect.anchorMin = Vector2.zero;
        ntRect.anchorMax = Vector2.one;
        ntRect.offsetMin = new Vector2(25, 20);
        ntRect.offsetMax = new Vector2(-25, -20);
        
        notePanel.SetActive(false);
    }
    
    // ============================================
    // UI UPDATES
    // ============================================
    
    void UpdateHotbarUI()
    {
        if (hotbarSlotBgs == null) return;
        
        ItemType[] slotTypes = { ItemType.Key, ItemType.Battery, ItemType.HealthKit, ItemType.Note };
        Color[] slotColors = {
            new Color(1f, 0.85f, 0.2f),
            new Color(0.3f, 0.9f, 1f),
            new Color(0.2f, 1f, 0.2f),
            new Color(0.9f, 0.85f, 0.7f)
        };
        
        for (int i = 0; i < HOTBAR_SLOTS; i++)
        {
            int count = GetTypeCount(slotTypes[i]);
            bool isSelected = (selectedHotbarSlot == i);
            bool hasItems = count > 0;
            
            // Background color
            if (isSelected)
                hotbarSlotBgs[i].color = new Color(slotColors[i].r * 0.3f, slotColors[i].g * 0.3f, slotColors[i].b * 0.3f, 0.9f);
            else
                hotbarSlotBgs[i].color = new Color(0.08f, 0.08f, 0.12f, hasItems ? 0.8f : 0.5f);
            
            // Icon text with count
            string icon = GetSlotIcon(slotTypes[i]);
            string countStr = count > 0 ? $"\n<size=10>{count}</size>" : "";
            hotbarSlotTexts[i].text = icon + countStr;
            hotbarSlotTexts[i].color = new Color(
                slotColors[i].r, slotColors[i].g, slotColors[i].b,
                hasItems ? 1f : 0.3f
            );
        }
    }
    
    string GetSlotIcon(ItemType type)
    {
        switch (type)
        {
            case ItemType.Key: return "K";
            case ItemType.Battery: return "P";
            case ItemType.HealthKit: return "+";
            case ItemType.Note: return "N";
            default: return "?";
        }
    }
    
    void ShowPickupNotification(string itemName, ItemType type)
    {
        if (pickupNotification == null || notificationText == null) return;
        
        string icon = GetTypeIcon(type);
        notificationText.text = $"{icon}  {itemName} alindi";
        notificationText.color = GetTypeColor(type);
        pickupNotification.SetActive(true);
        notificationTimer = 2.5f;
    }
    
    void ShowUseFeedback(string message, Color color)
    {
        if (useFeedbackPanel == null || useFeedbackText == null) return;
        
        useFeedbackText.text = message;
        useFeedbackText.color = color;
        useFeedbackPanel.SetActive(true);
        feedbackTimer = 2f;
    }
    
    void UpdateItemCount()
    {
        if (itemCountText == null) return;
        
        int keys = GetTypeCount(ItemType.Key);
        int batteries = GetTypeCount(ItemType.Battery);
        int notes = GetTypeCount(ItemType.Note);
        int kits = GetTypeCount(ItemType.HealthKit);
        
        string text = "";
        if (keys > 0) text += $"Anahtar:{keys}  ";
        if (batteries > 0) text += $"Pil:{batteries}  ";
        if (kits > 0) text += $"Kit:{kits}  ";
        if (notes > 0) text += $"Not:{notes}";
        
        itemCountText.text = text;
    }
    
    void UpdateBatteryIndicator()
    {
        if (batteryIndicatorText == null) return;
        
        // Use cached reference instead of FindObjectOfType every frame
        FlashlightController flashlight = cachedFlashlight;
        if (flashlight == null) flashlight = cachedFlashlight = FindObjectOfType<FlashlightController>();
        if (flashlight == null)
        {
            batteryIndicatorText.text = "";
            return;
        }
        
        float pct = flashlight.GetBatteryPercentage();
        int pctInt = Mathf.RoundToInt(pct * 100f);
        
        // Battery bar using text characters
        int barFilled = Mathf.RoundToInt(pct * 10);
        string bar = "";
        for (int i = 0; i < 10; i++)
            bar += i < barFilled ? "|" : ".";
        
        // Color based on level
        Color color;
        if (pct > 0.5f) color = new Color(0.3f, 0.9f, 1f, 0.8f);
        else if (pct > 0.2f) color = new Color(1f, 0.8f, 0.3f, 0.9f);
        else color = new Color(1f, 0.3f, 0.3f, 1f);
        
        // Flash if critical
        if (pct <= 0.2f && pct > 0f)
        {
            float flash = Mathf.Sin(Time.time * 5f) > 0 ? 1f : 0.3f;
            color.a = flash;
        }
        
        batteryIndicatorText.text = $"Fener [{bar}] {pctInt}%";
        batteryIndicatorText.color = color;
        
        // Auto-hint
        if (pct <= 0.15f && pct > 0f && HasItemOfType(ItemType.Battery))
        {
            batteryIndicatorText.text += " [R] Sarj et!";
        }
    }
    
    void CreateGridPanel(GameObject canvasObj)
    {
        // Grid container (center screen)
        gridPanel = new GameObject("GridInventory");
        gridPanel.transform.SetParent(canvasObj.transform, false);
        
        // Semi-transparent dark background (full screen)
        Image dimBg = gridPanel.AddComponent<Image>();
        dimBg.color = new Color(0, 0, 0, 0.6f);
        dimBg.raycastTarget = true;
        RectTransform dimRect = dimBg.rectTransform;
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;
        
        // Grid frame (center panel)
        GameObject frame = new GameObject("GridFrame");
        frame.transform.SetParent(gridPanel.transform, false);
        Image frameBg = frame.AddComponent<Image>();
        frameBg.color = new Color(0.04f, 0.04f, 0.08f, 0.95f);
        frameBg.raycastTarget = false;
        RectTransform frameRect = frameBg.rectTransform;
        frameRect.anchorMin = new Vector2(0.5f, 0.5f);
        frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.pivot = new Vector2(0.5f, 0.5f);
        frameRect.sizeDelta = new Vector2(340, 380);
        
        // Add border glow
        Outline frameOutline = frame.AddComponent<Outline>();
        frameOutline.effectColor = new Color(0.3f, 0.3f, 0.5f, 0.6f);
        frameOutline.effectDistance = new Vector2(2, -2);
        
        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(frame.transform, false);
        inventoryTitle = titleObj.AddComponent<Text>();
        inventoryTitle.text = "ENVANTER (0/16)";
        inventoryTitle.fontSize = 16;
        inventoryTitle.fontStyle = FontStyle.Bold;
        inventoryTitle.alignment = TextAnchor.MiddleCenter;
        inventoryTitle.color = new Color(0.7f, 0.7f, 0.85f, 1f);
        inventoryTitle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform titleRect = inventoryTitle.rectTransform;
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -8);
        titleRect.sizeDelta = new Vector2(0, 30);
        
        // Close hint
        GameObject closeObj = new GameObject("CloseHint");
        closeObj.transform.SetParent(frame.transform, false);
        Text closeText = closeObj.AddComponent<Text>();
        closeText.text = "[Tab] Kapat    [Sag Tikla] Kullan";
        closeText.fontSize = 11;
        closeText.alignment = TextAnchor.MiddleCenter;
        closeText.color = new Color(0.4f, 0.4f, 0.5f, 0.7f);
        closeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform closeRect = closeText.rectTransform;
        closeRect.anchorMin = new Vector2(0, 0);
        closeRect.anchorMax = new Vector2(1, 0);
        closeRect.pivot = new Vector2(0.5f, 0);
        closeRect.anchoredPosition = new Vector2(0, 8);
        closeRect.sizeDelta = new Vector2(0, 20);
        
        // Create grid cells
        float cellSize = 68f;
        float cellSpacing = 6f;
        float gridWidth = GRID_COLS * cellSize + (GRID_COLS - 1) * cellSpacing;
        float gridHeight = GRID_ROWS * cellSize + (GRID_ROWS - 1) * cellSpacing;
        float gridStartX = -gridWidth / 2f + cellSize / 2f;
        float gridStartY = gridHeight / 2f - cellSize / 2f - 10f; // Offset for title
        
        gridCells = new GameObject[GRID_TOTAL];
        gridCellBgs = new Image[GRID_TOTAL];
        gridCellIcons = new Text[GRID_TOTAL];
        gridCellQtys = new Text[GRID_TOTAL];
        
        for (int i = 0; i < GRID_TOTAL; i++)
        {
            int col = i % GRID_COLS;
            int row = i / GRID_COLS;
            
            // Cell container
            GameObject cell = new GameObject($"Cell_{i}");
            cell.transform.SetParent(frame.transform, false);
            gridCells[i] = cell;
            
            // Cell background
            Image cellBg = cell.AddComponent<Image>();
            cellBg.color = new Color(0.06f, 0.06f, 0.1f, 0.5f);
            cellBg.raycastTarget = true;
            gridCellBgs[i] = cellBg;
            
            RectTransform cellRect = cellBg.rectTransform;
            cellRect.anchorMin = new Vector2(0.5f, 0.5f);
            cellRect.anchorMax = new Vector2(0.5f, 0.5f);
            cellRect.pivot = new Vector2(0.5f, 0.5f);
            float xPos = gridStartX + col * (cellSize + cellSpacing);
            float yPos = gridStartY - row * (cellSize + cellSpacing);
            cellRect.anchoredPosition = new Vector2(xPos, yPos);
            cellRect.sizeDelta = new Vector2(cellSize, cellSize);
            
            // Cell border
            Outline cellOutline = cell.AddComponent<Outline>();
            cellOutline.effectColor = new Color(0.2f, 0.2f, 0.3f, 0.4f);
            cellOutline.effectDistance = new Vector2(1, -1);
            
            // Icon text (center)
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(cell.transform, false);
            Text iconText = iconObj.AddComponent<Text>();
            iconText.text = "";
            iconText.fontSize = 22;
            iconText.fontStyle = FontStyle.Bold;
            iconText.alignment = TextAnchor.MiddleCenter;
            iconText.color = Color.white;
            iconText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            iconText.raycastTarget = false;
            gridCellIcons[i] = iconText;
            RectTransform iconRect = iconText.rectTransform;
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(0, 8);
            iconRect.offsetMax = Vector2.zero;
            
            // Quantity text (bottom-right)
            GameObject qtyObj = new GameObject("Qty");
            qtyObj.transform.SetParent(cell.transform, false);
            Text qtyText = qtyObj.AddComponent<Text>();
            qtyText.text = "";
            qtyText.fontSize = 11;
            qtyText.fontStyle = FontStyle.Bold;
            qtyText.alignment = TextAnchor.LowerRight;
            qtyText.color = Color.white;
            qtyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            qtyText.raycastTarget = false;
            gridCellQtys[i] = qtyText;
            RectTransform qtyRect = qtyText.rectTransform;
            qtyRect.anchorMin = Vector2.zero;
            qtyRect.anchorMax = Vector2.one;
            qtyRect.offsetMin = new Vector2(2, 2);
            qtyRect.offsetMax = new Vector2(-4, -2);
        }
        
        gridPanel.SetActive(false);
    }
    
    void CreateTooltipPanel(GameObject canvasObj)
    {
        tooltipPanel = new GameObject("Tooltip");
        tooltipPanel.transform.SetParent(canvasObj.transform, false);
        
        Image bg = tooltipPanel.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.02f, 0.05f, 0.95f);
        bg.raycastTarget = false;
        RectTransform bgRect = bg.rectTransform;
        bgRect.pivot = new Vector2(0, 1);
        bgRect.sizeDelta = new Vector2(220, 90);
        
        Outline outline = tooltipPanel.AddComponent<Outline>();
        outline.effectColor = new Color(0.4f, 0.4f, 0.6f, 0.5f);
        outline.effectDistance = new Vector2(1, -1);
        
        // Title
        GameObject ttTitleObj = new GameObject("TTTitle");
        ttTitleObj.transform.SetParent(tooltipPanel.transform, false);
        tooltipTitle = ttTitleObj.AddComponent<Text>();
        tooltipTitle.text = "";
        tooltipTitle.fontSize = 14;
        tooltipTitle.fontStyle = FontStyle.Bold;
        tooltipTitle.alignment = TextAnchor.UpperLeft;
        tooltipTitle.color = Color.white;
        tooltipTitle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform ttTitleRect = tooltipTitle.rectTransform;
        ttTitleRect.anchorMin = new Vector2(0, 1);
        ttTitleRect.anchorMax = new Vector2(1, 1);
        ttTitleRect.pivot = new Vector2(0, 1);
        ttTitleRect.anchoredPosition = new Vector2(8, -6);
        ttTitleRect.sizeDelta = new Vector2(-16, 20);
        
        // Description
        GameObject ttDescObj = new GameObject("TTDesc");
        ttDescObj.transform.SetParent(tooltipPanel.transform, false);
        tooltipDesc = ttDescObj.AddComponent<Text>();
        tooltipDesc.text = "";
        tooltipDesc.fontSize = 11;
        tooltipDesc.alignment = TextAnchor.UpperLeft;
        tooltipDesc.color = new Color(0.7f, 0.7f, 0.75f);
        tooltipDesc.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform ttDescRect = tooltipDesc.rectTransform;
        ttDescRect.anchorMin = new Vector2(0, 1);
        ttDescRect.anchorMax = new Vector2(1, 1);
        ttDescRect.pivot = new Vector2(0, 1);
        ttDescRect.anchoredPosition = new Vector2(8, -28);
        ttDescRect.sizeDelta = new Vector2(-16, 30);
        
        // Usage hint
        GameObject ttHintObj = new GameObject("TTHint");
        ttHintObj.transform.SetParent(tooltipPanel.transform, false);
        tooltipHint = ttHintObj.AddComponent<Text>();
        tooltipHint.text = "";
        tooltipHint.fontSize = 10;
        tooltipHint.fontStyle = FontStyle.Italic;
        tooltipHint.alignment = TextAnchor.LowerLeft;
        tooltipHint.color = new Color(0.5f, 0.5f, 0.6f);
        tooltipHint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform ttHintRect = tooltipHint.rectTransform;
        ttHintRect.anchorMin = new Vector2(0, 0);
        ttHintRect.anchorMax = new Vector2(1, 0);
        ttHintRect.pivot = new Vector2(0, 0);
        ttHintRect.anchoredPosition = new Vector2(8, 6);
        ttHintRect.sizeDelta = new Vector2(-16, 18);
        
        tooltipPanel.SetActive(false);
    }
    
    string GetTypeIcon(ItemType type)
    {
        switch (type)
        {
            case ItemType.Key: return "[K]";
            case ItemType.Battery: return "[P]";
            case ItemType.Note: return "[N]";
            case ItemType.HealthKit: return "[+]";
            default: return "[*]";
        }
    }
    
    Color GetTypeColor(ItemType type)
    {
        switch (type)
        {
            case ItemType.Key: return new Color(1f, 0.85f, 0.2f);
            case ItemType.Battery: return new Color(0.3f, 0.9f, 1f);
            case ItemType.Note: return new Color(0.9f, 0.85f, 0.7f);
            case ItemType.HealthKit: return new Color(0.2f, 1f, 0.2f);
            default: return Color.white;
        }
    }
    
    void OnDestroy()
    {
        // Restore time if destroyed while inventory open
        if (isInventoryOpen)
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        if (inventoryUI != null) Destroy(inventoryUI);
    }
}
