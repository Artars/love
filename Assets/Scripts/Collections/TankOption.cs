using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Option", menuName = "Options/Tank Option", order = 2)]
public class TankOption : ScriptableObject
{
    public string tankName;
    public Sprite[] tankSprites;
    public Role[] tankRoles;
    public int prefabID;
    public GameObject tankPrefab;

    public string[] defaultNames;
}


