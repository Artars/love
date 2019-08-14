using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class VolumeSettings : MonoBehaviour
{
    public Slider mainSlider;
    public Slider fxSlider;
    public Slider musicSlider;

    public AudioMixer audioMixer;

    public void Start()
    {
        float main = PlayerPrefs.GetFloat("VolumeMaster",1);
        mainSlider.value = main;
        UpdateMainVolume(main);
        float fx = PlayerPrefs.GetFloat("VolumeFx",1);
        fxSlider.value = fx;
        UpdateFxVolume(fx);
        float music = PlayerPrefs.GetFloat("VolumeMusic",1);
        musicSlider.value = music;
        UpdateMusicVolume(music);
    }

    public void UpdateMainVolume(Slider slider)
    {
        UpdateMainVolume(slider.value);
    }

    public void UpdateFxVolume(Slider slider)
    {
        UpdateFxVolume(slider.value);
    }

    public void UpdateMusicVolume(Slider slider)
    {
        UpdateMusicVolume(slider.value);
    }

    public void UpdateMainVolume(float newValue)
    {
        audioMixer.SetFloat("VolumeMaster", Mathf.Log10(newValue) * 20);
        PlayerPrefs.SetFloat("VolumeMaster", newValue);
    }

    public void UpdateFxVolume(float newValue)
    {
        audioMixer.SetFloat("VolumeFx", Mathf.Log10(newValue) * 20);
        PlayerPrefs.SetFloat("VolumeFx", newValue);
    }

    public void UpdateMusicVolume(float newValue)
    {
        audioMixer.SetFloat("VolumeMusic", Mathf.Log10(newValue) * 20);
        PlayerPrefs.SetFloat("VolumeMusic", newValue);
    }
}
