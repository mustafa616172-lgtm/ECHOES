using UnityEngine;
using UnityEngine.Video;

public class DocumentItem : MonoBehaviour, IInteractable
{
    [Header("Document Settings")]
    public string documentName = "Secret File";
    public Texture documentImage; // The image to show before video
    public VideoClip cinematicVideo;

    public string GetInteractionPrompt()
    {
        return $"[E] Read {documentName}";
    }

    public void Interact()
    {
        Debug.Log($"[DocumentItem] Interact called on {gameObject.name}");
        
        if (CinematicManager.Instance != null)
        {
            // Now passing both image and video to the manager
            CinematicManager.Instance.StartCinematicSequence(documentImage, cinematicVideo);
        }
        else
        {
            Debug.LogError($"[DocumentItem] Cinematic Manager missing! Cannot play sequence.");
        }
    }
}
