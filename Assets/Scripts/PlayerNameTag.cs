using Unity.Netcode;
using UnityEngine;
using TMPro;

public class PlayerNameTag : NetworkBehaviour
{
    public TextMeshProUGUI nameText;
    public Transform nameTagTransform;
    private Camera mainCamera;
    private MeshRenderer playerMesh;
    private LobbyManager lobbyManager;
    private string playerNameString = "Oyuncu";

    private void Start()
    {
        mainCamera = Camera.main;
        playerMesh = GetComponent<MeshRenderer>();
        lobbyManager = FindObjectOfType<LobbyManager>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        playerNameString = $"Oyuncu {OwnerClientId}";
        if (nameText != null) nameText.text = playerNameString;
        if (lobbyManager != null && IsServer)
        {
            PlayerData data = lobbyManager.GetPlayerData(OwnerClientId);
            if (data != null) SetPlayerColorClientRpc(data.playerColor);
        }
        if (IsOwner && nameTagTransform != null) nameTagTransform.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (nameTagTransform != null && mainCamera != null && !IsOwner)
        {
            nameTagTransform.LookAt(mainCamera.transform);
            nameTagTransform.Rotate(0, 180, 0);
        }
    }

    [ClientRpc]
    void SetPlayerColorClientRpc(Color color)
    {
        if (playerMesh != null && playerMesh.material != null) playerMesh.material.color = color;
    }

    public void SetPlayerName(string newName)
    {
        playerNameString = newName;
        if (nameText != null) nameText.text = newName;
    }
}