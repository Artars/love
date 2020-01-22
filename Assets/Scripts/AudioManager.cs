using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class AudioManager : NetworkBehaviour
{
    public enum SoundClips
    {
        Victory, IncrementPoint, DecrementPoint
    };
    
    public static AudioManager instance;

    [Header("References")]
    public AudioSource musicAudioSource;
    public AudioSource clipAudioSource;
    public AudioClip[] musicTracks;
    public DictionaryEnumClip soundClips;

    [Header("Status")]
    [Range(0f,1f)]
    public float initialVolume = 1;
    [SyncVar]
    public float currentMusicVolume = 1;
    [SyncVar]
    public int currentTrack = -1;

    protected Coroutine volumeCoroutine = null;

    public void Awake()
    {
        if(instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);
    }

    public void Start()
    {
        if(isServer)
        {
            StartRandomTrack(initialVolume);
        }
        else if(currentTrack != -1)
        {
            if(musicAudioSource == null || musicAudioSource.gameObject == null || !musicAudioSource.gameObject.activeInHierarchy) 
                StartCoroutine(tryToPlayMusic(0.5f));
            else
                StartCoroutine(tryToPlayMusic(0));

            if(!musicAudioSource.enabled) musicAudioSource.enabled = true;

            musicAudioSource.clip = musicTracks[currentTrack];
            musicAudioSource.volume = currentMusicVolume;
            musicAudioSource.Play();
        }
    }

    protected IEnumerator tryToPlayMusic(float timeToWait)
    {
        yield return new WaitForSeconds(timeToWait);
        if(musicAudioSource != null && musicAudioSource.gameObject != null && musicAudioSource.gameObject.activeInHierarchy)
        {
            musicAudioSource.clip = musicTracks[currentTrack];
            musicAudioSource.volume = currentMusicVolume;
            musicAudioSource.Play();
        }
    }

    [Server]
    public void StartRandomTrack(float volume)
    {
        currentTrack = Random.Range(0,musicTracks.Length);
        currentMusicVolume = volume;
        RpcPlayTrack(currentTrack, currentMusicVolume);
    }


    [ClientRpc]
    public void RpcPlayTrack(int track, float volume)
    {
        if(musicAudioSource == null) return;
        musicAudioSource.clip = musicTracks[track];
        musicAudioSource.volume = volume;
        musicAudioSource.Play();
    }

    [Server]
    public void SetVolume(float newVolume, float transitionTime)
    {
        currentMusicVolume = newVolume;
        RpcReduceVolume(newVolume, transitionTime);
    }

    [ClientRpc]
    public void RpcReduceVolume(float newVolume, float transitionTime)
    {
        if(musicAudioSource == null) return;

        if(volumeCoroutine != null)
            StopCoroutine(volumeCoroutine);
        volumeCoroutine = StartCoroutine(ReduceVolume(newVolume,transitionTime));
    }

    [ClientRpc]
    public void RpcPlayClip(SoundClips clip)
    {
        if(clipAudioSource == null) return;
        if(soundClips.ContainsKey(clip))
        {
            clipAudioSource.PlayOneShot(soundClips[clip]);
        }
    }

    [TargetRpc]
    public void TargetPlayClip(NetworkConnection target,SoundClips clip)
    {
        if(clipAudioSource == null) return;
        if(soundClips.ContainsKey(clip))
        {
            clipAudioSource.PlayOneShot(soundClips[clip]);
        }
    }

    protected IEnumerator ReduceVolume(float newVolume, float transitionTime)
    {
        float deltaVol = newVolume - musicAudioSource.volume;
        float delta, counter = 0;

        while(counter <= transitionTime)
        {
            delta = Time.deltaTime;
            counter += delta;
            musicAudioSource.volume += deltaVol * delta;

            yield return null;
        }

        musicAudioSource.volume = newVolume;
        volumeCoroutine = null;
    }
}
