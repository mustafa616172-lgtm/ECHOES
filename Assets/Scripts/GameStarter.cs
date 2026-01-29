using Unity.Netcode;
using UnityEngine;

public class GameStarter : NetworkBehaviour
{
    [Header("Panel Referanslari")]
    public GameObject lobbyPanel;
    public GameObject hudPanel;
    public GameObject mainMenuPanel;

    void Update()
    {
        // Host ise ve lobideyse, Space tuþu ile oyunu baþlat
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            if (lobbyPanel != null && lobbyPanel.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    StartGameClientRpc();
                }
            }
        }
    }

    [ClientRpc]
    void StartGameClientRpc()
    {
        // Tüm clientlarda lobi panelini kapat, HUD'u aç
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        
        // Fareyi kilitle
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        Debug.Log("Oyun baþladý!");
    }

    // Manuel oyun baþlatma (butondan çaðrýlabilir)
    public void StartGame()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            StartGameClientRpc();
        }
    }
}
