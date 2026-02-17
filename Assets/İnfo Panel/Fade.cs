using UnityEngine;
using System.Collections;

public class EchoesInfoPanel : MonoBehaviour
{
    public RectTransform panel;
    public float speed = 5f;
    public Vector2 hiddenPos;
    public Vector2 visiblePos;

    bool isOpen = false;

    void Start()
    {
        panel.anchoredPosition = hiddenPos;
    }

    public void TogglePanel()
    {
        StopAllCoroutines();
        if (isOpen)
            StartCoroutine(MovePanel(hiddenPos));
        else
            StartCoroutine(MovePanel(visiblePos));

        isOpen = !isOpen;
    }

    IEnumerator MovePanel(Vector2 target)
    {
        while (Vector2.Distance(panel.anchoredPosition, target) > 0.1f)
        {
            panel.anchoredPosition = Vector2.Lerp(panel.anchoredPosition, target, Time.deltaTime * speed);
            yield return null;
        }
    }
}
