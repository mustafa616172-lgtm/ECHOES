using UnityEngine;

public class MenuMusicManager : MonoBehaviour
{
    public static MenuMusicManager instance;

    [Header("Ses Ayarlarý")]
    public AudioSource audioSource;
    public AudioClip menuMusic;
    [Range(0f, 1f)] public float volume = 0.5f;

    void Awake()
    {
        // Singleton Yapýsý: Sahnede sadece bir tane müzik objesi olmasýný saðlar
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Sahneler arasý geçiþte bu objeyi yok etme
        }
        else
        {
            Destroy(gameObject); // Eðer zaten bir tane varsa, yeni geleni yok et
        }
    }

    void Start()
    {
        if (audioSource != null && menuMusic != null)
        {
            audioSource.clip = menuMusic;
            audioSource.loop = true;
            audioSource.volume = volume;
            audioSource.Play();
        }
    }

    // Müziði durdurmak veya sesini kýsmak için fonksiyonlar ekleyebilirsin
    public void StopMusic()
    {
        audioSource.Stop();
    }
}