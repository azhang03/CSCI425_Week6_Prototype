using System.Collections;
using UnityEngine;

public class LobbyAudioManager : MonoBehaviour
{
    public static LobbyAudioManager Instance { get; private set; }

    [Header("Audio Source")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Lobby Sounds")]
    [SerializeField] private AudioClip levelStartSound;
    [SerializeField] private AudioClip clickSound;


    [SerializeField] private AudioClip loadLevelSound;

  
    [SerializeField] private float loadSoundDuration = 2.5f;
    private Coroutine loadSoundRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void PlayLevelStart()
    {
        PlaySound(levelStartSound);
    }

    public void PlayClickSound()
    {
        PlaySound(clickSound);
    }

    public void PlayLoadLevel()
    {
        Debug.Log("L0ading");
        if (loadLevelSound == null || sfxSource == null)
            return;

        if (loadSoundRoutine != null)
            StopCoroutine(loadSoundRoutine);

        loadSoundRoutine = StartCoroutine(PlayPartialClip(loadLevelSound, loadSoundDuration));
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip);
    }

    private IEnumerator PlayPartialClip(AudioClip clip, float duration)
    {
        sfxSource.clip = clip;
        sfxSource.time = 0f;
        sfxSource.Play();

        yield return new WaitForSeconds(duration);

        sfxSource.Stop();
        sfxSource.clip = null;
        loadSoundRoutine = null;
    }
}