using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceTimeScale : MonoBehaviour
{
    [Range(0,2)]
    public float timeScale = 1;

    void Start()
    {
        Time.timeScale = timeScale;
    }

}
