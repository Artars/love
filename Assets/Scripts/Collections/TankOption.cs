﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Option", menuName = "Options/Tank Option", order = 2)]
public class TankOption : ScriptableObject
{
    public string tankName;
    public Sprite tankSprite;
    public Role[] tankRoles;
    public int prefabID;
    public GameObject tankPrefab;
}

