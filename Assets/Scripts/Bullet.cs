using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Bullet : NetworkBehaviour
{
    [SyncVar]
    public int team = -1;
    [SyncVar]
    public int tankId = -1;
    [SyncVar]
    public float damage;
    [SyncVar]
    public Vector3 velocityFired;
    [SyncVar]
    public bool canColide = false;

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
            transform.rotation = Quaternion.LookRotation(rgbd.velocity);
            // myTransform.LookAt(rgbd.velocity);
        }
        //Will estimate the velocity
        else
        {
            Vector3 currentPosition = myTransform.position;
            estimatePosition = currentPosition - lastPosition;
            lastPosition = currentPosition;

            //myTransform.LookAt(estimatePosition);
            transform.rotation = Quaternion.LookRotation(estimatePosition);
        }
    }

    public void fireWithVelocity(Vector3 velocity){
        rgbd.velocity = velocity;
        velocityFired = velocity;
        angleFired = Mathf.Atan2(velocity.z,velocity.x) * Mathf.Rad2Deg;

        canColide =true;
    }

    public void OnTriggerEnter(Collider col) {
        Debug.Log("Hit obj: " + col.gameObject);

        if(!canColide) return;

        if(isServer) {
            if(col.gameObject.tag == "Tank"){
                Tank tankScript = col.gameObject.GetComponentInParent<Tank>();
                if(tankScript != null)
                {
                    tankScript.DealWithCollision(this.GetComponent<Collider>(), col);
                }
            }
            else
            {
                NetworkServer.Destroy(gameObject);
            }
        }
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
