using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TankCollection", menuName = "Collection/Tank Collection", order = 1)]
public class TankOptionCollection : ScriptableObject
{
    public TankOption[] tankOptions;

}
