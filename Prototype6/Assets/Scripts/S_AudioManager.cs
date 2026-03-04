using UnityEngine;

public class S_AudioManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public static S_AudioManager Instance;


    [Header("Sources")]
    public AudioSource musicSource;
    public AudioSource weaponSource;
    public AudioSource sfxSource;

    [Header("Sfx")]
    public AudioClip bullet; //dn
    public AudioClip laser; //dn
    public AudioClip area; //dn


    public AudioClip playerDmg; //dn
    public AudioClip playerDie; //dn
    public AudioClip enemyDmg; //dn
    public AudioClip enemyDeath; //dn
    public AudioClip enemyTp; // in teleport
    public AudioClip enemySpawn; //dn

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void PlayPlayerHurt()
    {
        Debug.Log("Player hurt");
        PlaySFX(playerDmg);
    }

    public void PlayPlayerDie()
    {
        Debug.Log("Player die");
        PlaySFX(playerDie);
    }
    public void PlayEnemyHurt()
    {
        Debug.Log("enemy dmg");

        PlaySFX(enemyDmg);
    }
    public void PlayEnemyDie()
    {
        Debug.Log("enemy die");

        PlaySFX(enemyDeath);
    }
    public void PlayEnemySpawn()
    {
        Debug.Log("enemy spwn");

        PlaySFX(enemySpawn);
    }
    public void PlayEnemyTp()
    {
        Debug.Log("enemy tp");
        PlaySFX(enemyTp);
    }
    public void PlaySFX(AudioClip clip)
    {
        if(sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    public void PlayWeapon(AudioClip clip)
    {
        if (weaponSource != null && clip != null)
        {
            weaponSource.PlayOneShot(clip);
        }
    }

    public void PlayBullet()
    {
       Debug.Log("bullet snd");
       PlayWeapon(bullet);
    }

    public void PlayLaser()
    {
        Debug.Log("laser snd");
        PlayWeapon(laser);
    }

    public void PlayAreaWeapon()
    {
        Debug.Log("area snd");
        PlayWeapon(area);
    }

    public void PlayMusic()
    {
        if(musicSource != null)
        {
            musicSource.loop = true;
            musicSource.Play();
        }
        
    }

}
