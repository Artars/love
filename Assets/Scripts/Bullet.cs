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

    protected Transform myTransform;
    protected Vector3 lastPosition;
    protected Vector3 estimatePosition;

    protected Rigidbody rgbd;

    void Awake(){
        rgbd = GetComponent<Rigidbody>();
        myTransform = transform;
        if(isServer){
            StartCoroutine(waitDestroyTime(lifeTime));
        }
        lastPosition = myTransform.position;
    }

    protected void Update() {
        //Rotate toward velocity
        if(isServer){
            myTransform.LookAt(rgbd.velocity);
        }
        //Will estimate the velocity
        else
        {
            Vector3 currentPosition = myTransform.position;
            estimatePosition = currentPosition - lastPosition;
            lastPosition = currentPosition;

            myTransform.LookAt(estimatePosition);
        }
    }

    [ClientRpc]
    public void RpcFireWithVelocityRpc(Vector3 position, Quaternion rotation, Vector3 velocity){
        myTransform.position = position;
        myTransform.rotation = rotation;

        rgbd.velocity = velocity;
        velocityFired = velocity;
        angleFired = Mathf.Atan2(velocity.z,velocity.x) * Mathf.Rad2Deg;
    }

    public void fireWithVelocity(Vector3 velocity){
        rgbd.velocity = velocity;
        velocityFired = velocity;
        angleFired = Mathf.Atan2(velocity.z,velocity.x) * Mathf.Rad2Deg;
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
