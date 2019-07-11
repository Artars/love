using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MapCollection", menuName = "Collection/Map Collection", order = 1)]
public class MapCollection : ScriptableObject
{
    public MapOption[] mapOptions;
}

