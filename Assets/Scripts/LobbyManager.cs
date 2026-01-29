using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class LobbyManager : NetworkBehaviour
{
    public int maxPlayers = 4;
    public Color[] playerColors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow };
    private Dictionary<ulong, PlayerData> connectedPlayers = new Dictionary<ulong, PlayerData>();

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Oyuncu baglandi: {clientId}");
        if (IsServer)
        {
            if (NetworkManager.Singleton.ConnectedClientsList.Count > maxPlayers)
            {
                Debug.LogWarning("Lobi dolu!");
                NetworkManager.Singleton.DisconnectClient(clientId);
                return;
            }
            int playerIndex = (int)clientId % playerColors.Length;
            PlayerData newPlayer = new PlayerData { clientId = clientId, playerName = $"Oyuncu {clientId}", playerColor = playerColors[playerIndex] };
            connectedPlayers[clientId] = newPlayer;
            Debug.Log($"Oyuncu eklendi: {newPlayer.playerName}");
        }
    }

    void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Oyuncu ayrildi: {clientId}");
        if (IsServer && connectedPlayers.ContainsKey(clientId)) connectedPlayers.Remove(clientId);
    }

    public PlayerData GetPlayerData(ulong clientId)
    {
        if (connectedPlayers.ContainsKey(clientId)) return connectedPlayers[clientId];
        return null;
    }

    public Dictionary<ulong, PlayerData> GetAllPlayers() { return connectedPlayers; }
}

[System.Serializable]
public class PlayerData
{
    public ulong clientId;
    public string playerName;
    public Color playerColor;
}