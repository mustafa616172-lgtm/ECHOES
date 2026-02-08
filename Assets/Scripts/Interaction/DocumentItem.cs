using UnityEngine;
using UnityEngine.Video;

public class DocumentItem : MonoBehaviour, IInteractable
{
    [Header("Document Settings")]
    public string documentName = "Secret File";
    public VideoClip cinematicVideo;

    public string GetInteractionPrompt()
    {
        return $"[E] Read {documentName}";
    }

    public void Interact()
    {
        Debug.Log($"[DocumentItem] Interact called on {gameObject.name}");
        if (CinematicManager.Instance != null && cinematicVideo != null)
        {
            Debug.Log($"[DocumentItem] Playing video: {cinematicVideo.name}");
            CinematicManager.Instance.PlayVideo(cinematicVideo);
        }
        else
        {
            Debug.LogWarning($"[DocumentItem] Cinematic Manager missing ({CinematicManager.Instance != null}) or Video Clip null ({cinematicVideo != null})!");
            if (cinematicVideo == null) Debug.LogError("Please assign a Video Clip to this Document Item!");
        }
    }
}
