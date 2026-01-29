using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

public class NetworkUI : MonoBehaviour
{
    [Header("Butonlar")]
    [SerializeField] public Button hostButton;
    [SerializeField] public Button clientButton;
    [SerializeField] public Button disconnectButton;
    [SerializeField] public Button startGameButton;

    [Header("Input")]
    [SerializeField] public TMP_InputField ipInputField;

    [Header("Metinler")]
    [SerializeField] public TextMeshProUGUI statusText;
    [SerializeField] public TextMeshProUGUI playerCountText;

    [Header("Paneller")]
    [SerializeField] public GameObject mainMenuPanel;
    [SerializeField] public GameObject lobbyPanel;
    [SerializeField] public GameObject hudPanel;

    private bool isConnected = false;
    private bool lobbyVisible = false;

    void Start()
    {
        // Buton dinleyicilerini bagla
        if (hostButton != null)
            hostButton.onClick.AddListener(StartHost);
        
        if (clientButton != null)
            clientButton.onClick.AddListener(StartClient);
        
        if (disconnectButton != null)
            disconnectButton.onClick.AddListener(Disconnect);
        
        if (startGameButton != null)
            startGameButton.onClick.AddListener(StartGame);

        ShowMainMenu();
        
        Debug.Log("NetworkUI baslatildi. Buton referanslari: Host=" + (hostButton != null) + 
                  ", Client=" + (clientButton != null) + 
                  ", Disconnect=" + (disconnectButton != null) +
                  ", StartGame=" + (startGameButton != null));
    }

    void Update()
    {
        // ESC ile lobi panelini toggle et (sadece baglandiysa)
        if (Input.GetKeyDown(KeyCode.Escape) && isConnected)
        {
            ToggleLobbyPanel();
        }

        // Oyuncu sayisini guncelle
        if (isConnected && NetworkManager.Singleton != null)
        {
            int playerCount = NetworkManager.Singleton.ConnectedClients.Count;
            if (playerCountText != null)
            {
                playerCountText.text = "AJANLAR: " + playerCount + "/4";
            }
        }
    }

    void ToggleLobbyPanel()
    {
        lobbyVisible = !lobbyVisible;
        
        if (lobbyVisible)
        {
            if (lobbyPanel != null)
                lobbyPanel.SetActive(true);
            if (hudPanel != null)
                hudPanel.SetActive(false);
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            if (lobbyPanel != null)
                lobbyPanel.SetActive(false);
            if (hudPanel != null)
                hudPanel.SetActive(true);
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void StartGame()
    {
        Debug.Log("Operasyon baslatildi!");
        
        lobbyVisible = false;
        
        if (lobbyPanel != null)
            lobbyPanel.SetActive(false);
        if (hudPanel != null)
            hudPanel.SetActive(true);
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void StartHost()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager bulunamadi!");
            return;
        }

        NetworkManager.Singleton.StartHost();
        isConnected = true;
        lobbyVisible = true;
        
        // Ana menuyu gizle, lobi goster
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
        if (hudPanel != null) hudPanel.SetActive(false);

        if (statusText != null)
            statusText.text = "> SUNUCU AKTIF...";

        Debug.Log("Host baslatildi!");
    }

    void StartClient()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager bulunamadi!");
            return;
        }

        string ip = "127.0.0.1";
        if (ipInputField != null && !string.IsNullOrEmpty(ipInputField.text))
        {
            ip = ipInputField.text;
        }

        var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        if (transport != null)
        {
            transport.ConnectionData.Address = ip;
        }

        NetworkManager.Singleton.StartClient();
        isConnected = true;
        lobbyVisible = true;
        
        // Ana menuyu gizle, lobi goster
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
        if (hudPanel != null) hudPanel.SetActive(false);

        if (statusText != null)
            statusText.text = "> BAGLANILIYOR...";

        Debug.Log("Client baslatildi! IP: " + ip);
    }

    public void Disconnect()
    {
        Debug.Log("Oturum sonlandiriliyor!");
        
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        isConnected = false;
        lobbyVisible = false;
        
        ShowMainMenu();
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Baglanti kesildi!");
    }

    void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(false);
    }
}
