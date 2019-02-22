using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

public class Bullet : NetworkedBehaviour
{
    protected Rigidbody rgbd;

    void Awake(){
        rgbd = GetComponent<Rigidbody>();
    }

    [ClientRPC]
    public void fireWithVelocityRPC(Vector3 position, Quaternion rotation, Vector3 velocity){
        transform.position = position;
        transform.rotation = rotation;

        rgbd.velocity = velocity;
    }

    public void fireWithVelocity(Vector3 velocity){

        rgbd.velocity = velocity;
    }
}
