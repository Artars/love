using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    [Mirror.Scene]
    public string sceneToTranisition;

    public UnityEngine.UI.Slider slider;

    public bool autoLoad = true;

    protected bool isLoading = false;
    protected AsyncOperation asyncOperation;

    public void Start()
    {
        if(autoLoad)
            StartLoading();
    }

    public void StartLoading()
    {
        asyncOperation = SceneManager.LoadSceneAsync(sceneToTranisition, LoadSceneMode.Single);
        isLoading = true;
    }

    public void Update()
    {
        if(isLoading && slider != null)
        {
            slider.value = asyncOperation.progress;
        }
    }
}
