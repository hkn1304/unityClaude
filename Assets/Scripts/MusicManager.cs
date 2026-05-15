using UnityEngine;

// Assign your audio clip in the Inspector — drag the imported MP3/OGG onto "Bgm Clip".
[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    [SerializeField] AudioClip bgmClip;
    [Range(0f, 1f)] public float volume = 0.65f;

    AudioSource src;

    void Awake()
    {
        src          = GetComponent<AudioSource>();
        src.clip     = bgmClip;
        src.loop     = true;
        src.volume   = volume;
        src.playOnAwake = false;
    }

    void Start()
    {
        if (bgmClip != null) src.Play();
    }
}
