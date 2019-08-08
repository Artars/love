using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class VersionText : MonoBehaviour
{
    public TMPro.TextMeshProUGUI textRef;

    public void Start()
    {
        textRef.text = Application.version;
    } 
}