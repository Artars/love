using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class GoalSpawner : NetworkBehaviour{
    public Vector3 size;
    public Vector3 center;
    public GameObject prefab;
    public GameObject instance;
    [SyncVar]
    private Vector3 pos;

    
    public void SpawnGameObject(){
        //spawn at random position in the area
        pos = center+ new Vector3(Random.Range(-size.x/2, size.x/2),Random.Range(-size.y/2, size.y/2),Random.Range(-size.z/2, size.z/2));
        pos += this.gameObject.transform.position;
        // pos.y += (10.15f - 7.48f);//fix the y position

        instance = Instantiate(prefab, pos, Quaternion.identity);
        NetworkServer.Spawn(instance);
        instance.GetComponent<FixPos>().pos = pos;
    }
  

    public  void OnDrawGizmosSelected() {
        Gizmos.color = new Color(1,0,0,0.5f);//draw the spawning area
        Gizmos.DrawCube(transform.localPosition + center,size);
    }
}
