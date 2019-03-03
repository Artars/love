using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Bullet : NetworkBehaviour
{
    protected Rigidbody rgbd;

    void Awake(){
        rgbd = GetComponent<Rigidbody>();
    }

    [ClientRpc]
    public void RpcFireWithVelocityRpc(Vector3 position, Quaternion rotation, Vector3 velocity){
        transform.position = position;
        transform.rotation = rotation;

        rgbd.velocity = velocity;
    }

    public void fireWithVelocity(Vector3 velocity){
        rgbd.velocity = velocity;
    }
}
