using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MusicSelector : NetworkBehaviour
{
    public static MusicSelector instance;
    public AudioSource audioSource;
    public AudioClip[] musicTracks;
    public float initialVolume = 1;

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
            RpcPlayTrack(Random.Range(0,musicTracks.Length), initialVolume);
        }
    }

    [ClientRpc]
    public void RpcPlayTrack(int track, float volume)
    {
        audioSource.clip = musicTracks[track];
        audioSource.volume = volume;
        audioSource.Play();
    }

    [ClientRpc]
    public void RpcReduceVolume(float newVolume, float transitionTime)
    {
        if(volumeCoroutine != null)
            StopCoroutine(volumeCoroutine);
        volumeCoroutine = StartCoroutine(ReduceVolume(newVolume,transitionTime));
    }

    protected IEnumerator ReduceVolume(float newVolume, float transitionTime)
    {
        float deltaVol = newVolume - audioSource.volume;
        float delta, counter = 0;

        while(counter <= transitionTime)
        {
            delta = Time.deltaTime;
            counter += delta;
            audioSource.volume += deltaVol * delta;

            yield return null;
        }

        audioSource.volume = newVolume;
        volumeCoroutine = null;
    }
}
