using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

[CreateAssetMenu(fileName = "Option", menuName = "Options/Map Option", order = 2)]
public class MapOption : ScriptableObject
{
    int id;
    [Scene]
    public string scene;
    public string mapName;
    public Sprite mapThumbnail;
}

