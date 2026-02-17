using UnityEngine;

[CreateAssetMenu(fileName = "NewInfoData", menuName = "InfoPanel/Info Data")]
public class InfoPanelData : ScriptableObject
{
    [Header("Metin Ýçeriði")]
    [TextArea(3, 10)]
    public string title = "Baþlýk";

    [TextArea(5, 15)]
    public string description = "Açýklama metni buraya gelecek...";

    [Header("Görsel")]
    public Sprite icon; // Opsiyonel ikon

    [Header("Animasyon Ayarlarý")]
    public float displayDuration = 0f; // 0 = manuel kapatma, >0 = otomatik kapanma süresi

    [Header("Opsiyonel")]
    public AudioClip openSound;
    public Color panelColor = Color.white;
}