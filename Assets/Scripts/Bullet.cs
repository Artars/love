using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Bullet : NetworkBehaviour
{
    [SyncVar]
    public int team = -1;
    [SyncVar]
    public float damage;
    [SyncVar]
    public Vector3 velocityFired;
    public float angleFired;

    public float lifeTime = 10;

    protected Rigidbody rgbd;

    void Awake(){
        rgbd = GetComponent<Rigidbody>();
        if(isServer){
            StartCoroutine(waitDestroyTime(lifeTime));
        }
    }

    protected void Update() {
        //Rotate toward velocity
        if(isServer){
            transform.LookAt(rgbd.velocity);
        }
    }

    [ClientRpc]
    public void RpcFireWithVelocityRpc(Vector3 position, Quaternion rotation, Vector3 velocity){
        transform.position = position;
        transform.rotation = rotation;

        rgbd.velocity = velocity;
        velocityFired = velocity;
        angleFired = Mathf.Atan2(velocity.z,velocity.x);
    }

    public void fireWithVelocity(Vector3 velocity){
        rgbd.velocity = velocity;
        velocityFired = velocity;
        angleFired = Mathf.Atan2(velocity.z,velocity.x);
    }

    protected void OnCollisionEnter(Collision col) {
        if(isServer) {
            if(col.gameObject.tag != "Tank"){
                NetworkServer.Destroy(gameObject);
            }
        }

        if(col.gameObject.tag == "Tank")
            Physics.IgnoreCollision(GetComponent<Collider>(), col.collider);

    }

    protected IEnumerator waitDestroyTime(float time) {
        float counter = time;

        while(counter > 0) {
            counter -= Time.deltaTime;
            yield return null;
        }

        if(isServer)
            NetworkServer.Destroy(gameObject);
    }
}
