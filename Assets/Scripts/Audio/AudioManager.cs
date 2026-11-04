using UnityEngine;
using UnityEngine.SceneManagement;

public enum Sfx { Click, Collect, Jump, Dialogue, Powerup, Complete, Hurt, Attack, Hit }

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip[] sfxClips;
    [SerializeField] private string[] sceneNames;
    [SerializeField] private AudioClip[] sceneMusic;
    [SerializeField] private float musicVolume = 0.4f;
    [SerializeField] private float sfxVolume = 0.8f;

    private bool muted;

    public static void Play(Sfx sfx)
    {
        if (Instance != null)
        {
            Instance.PlaySfx(sfx);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        musicSource.volume = musicVolume;
        PlayMusicForScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            muted = !muted;
            musicSource.mute = muted;
            sfxSource.mute = muted;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusicForScene(scene.name);
    }

    private void PlayMusicForScene(string sceneName)
    {
        for (int i = 0; i < sceneNames.Length && i < sceneMusic.Length; i++)
        {
            if (sceneNames[i] == sceneName)
            {
                PlayMusic(sceneMusic[i]);
                return;
            }
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void PlaySfx(Sfx sfx)
    {
        int index = (int)sfx;
        if (sfxClips == null || index >= sfxClips.Length || sfxClips[index] == null) return;
        sfxSource.PlayOneShot(sfxClips[index], sfxVolume);
    }
}
