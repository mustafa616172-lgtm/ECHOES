using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour 
{
    public float speed = 5f;

    // Bu metod, obje ağda oluştuğu an otomatik çalışır
    public override void OnNetworkSpawn()
    {
        // Eğer bu karakter bana aitse (IsOwner)
        if (IsOwner)
        {
            // İçindeki kamerayı ve kulaklığı aktif et
            GetComponentInChildren<Camera>().enabled = true;
            GetComponentInChildren<AudioListener>().enabled = true;
        }
        else
        {
            // Bana ait olmayanların kamerasını benim ekranımda kapat
            GetComponentInChildren<Camera>().enabled = false;
            GetComponentInChildren<AudioListener>().enabled = false;
        }
    }

    void Update()
    {
        // Sadece kendi karakterimi hareket ettirebilirim
        if (!IsOwner) return;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        transform.position += move * speed * Time.deltaTime;
    }
}