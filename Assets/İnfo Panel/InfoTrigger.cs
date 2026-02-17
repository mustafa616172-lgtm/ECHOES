using UnityEngine;

public class InfoTrigger : MonoBehaviour
{
    [Header("Bilgi Mesajı")]
    [TextArea(3, 10)]
    [SerializeField] private string infoMessage = "Buraya bilgi mesajınızı yazın...";

    [Header("Trigger Ayarları")]
    [SerializeField] private bool oneTimeUse = true; // Sadece bir kez tetiklensin mi?
    [SerializeField] private float triggerDelay = 0f; // Tetikleme gecikmesi (saniye)
    [SerializeField] private string playerTag = "Player"; // Oyuncu tag'i

    private bool hasBeenTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        // Eğer bir kez kullanımlıysa ve zaten tetiklendiyse, çık
        if (oneTimeUse && hasBeenTriggered)
            return;

        // Oyuncu tag kontrolü
        if (other.CompareTag(playerTag))
        {
            hasBeenTriggered = true;

            if (triggerDelay > 0f)
            {
                Invoke(nameof(ShowInfoPanel), triggerDelay);
            }
            else
            {
                ShowInfoPanel();
            }
        }
    }

    private void ShowInfoPanel()
    {
        if (InfoPanelController.Instance != null)
        {
            InfoPanelController.Instance.ShowPanel(infoMessage);
        }
        else
        {
            Debug.LogWarning("InfoPanelController bulunamadı! Lütfen sahnede InfoPanel objesi olduğundan emin olun.");
        }
    }

    // Editor'da görseli görmek için Gizmo çiz
    private void OnDrawGizmos()
    {
        Gizmos.color = hasBeenTriggered ? Color.gray : Color.yellow;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}