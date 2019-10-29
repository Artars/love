using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class RopePlacer : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public GameObject prefab;

    public Transform toStore;

    public Transform ropeStart;
    public Transform ropeEnd;

    [Range(0,5)]
    public float distanceBetweenElements;


    public void PlaceObjects() {
        //Check if any reference is null
        if(ropeStart == null || ropeEnd == null || lineRenderer == null || prefab == null) return;

        //Get own transform if don't find one
        if(toStore == null)
            toStore = transform;

        //Clear previous child
        for(int i = toStore.childCount - 1; i > -1; i--) {
            Transform child = toStore.GetChild(i);
            DestroyImmediate(child.gameObject); 
        }

        //Instanciate objects
        Vector3 dir = ropeEnd.position - ropeStart.position;
        int numPrefabs = (int) (dir.magnitude / distanceBetweenElements);
        Vector3 startPos = ropeStart.position;
        dir.Normalize();

        for(int i = 0; i < numPrefabs; i ++) {
            GameObject.Instantiate(prefab, startPos + dir * i * distanceBetweenElements, Quaternion.identity, toStore);
        }

        //Fix line
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, ropeStart.position);
        lineRenderer.SetPosition(1, ropeEnd.position);
    }

    public void OnDrawGizmos() {
        if(ropeStart == null || ropeEnd == null)
        {
            Gizmos.DrawLine(ropeStart.position,ropeEnd.position);
        }
    }

}


#if UNITY_EDITOR

[CustomEditor(typeof(RopePlacer))]
public class ObjectBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        RopePlacer myScript = (RopePlacer)target;
        if(GUILayout.Button("Update Ropes"))
        {
            myScript.PlaceObjects();
        }
    }
}

#endif