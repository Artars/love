using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class GoalSpawner : NetworkBehaviour{
    public Vector3 size;
    public Vector3 center;
    public GameObject prefab;
    public GameObject instance;

    public void SpawnGameObject(){
        Vector3 pos = center+ new Vector3(Random.Range(-size.x/2, size.x/2),Random.Range(-size.y/2, size.y/2),Random.Range(-size.z/2, size.z/2));
       instance =  Instantiate(prefab, pos, Quaternion.identity);
    }
    public  void OnDrawGizmosSelected() {
        Gizmos.color = new Color(1,0,0,0.5f);
        Gizmos.DrawCube(transform.localPosition + center,size);
    }
}
