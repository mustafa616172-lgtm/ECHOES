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
        if (CinematicManager.Instance != null && cinematicVideo != null)
        {
            CinematicManager.Instance.PlayVideo(cinematicVideo);
        }
        else
        {
            Debug.LogWarning("Cinematic Manager missing or Video Clip null!");
            if (cinematicVideo == null) Debug.LogError("Please assign a Video Clip to this Document Item!");
        }
    }
}
