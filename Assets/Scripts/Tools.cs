using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Tools : MonoBehaviour
{
    [MenuItem("Tools/Force recompile")]
    public static void ForceRecompile() {
         AssetDatabase.Refresh();
         
    }
}
