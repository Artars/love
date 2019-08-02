using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class DestroyableProp : NetworkBehaviour
{
    public enum DestroyWay
    {
        Disapear,Particles,Fall,ParticlesAndFall
    }

    public DestroyWay wayToDestroy = DestroyWay.Disapear;
    public float timeToDestroy = 5;

    [Header("References to particles")]
    public GameObject particlesToInstantiate;
    public Transform positionToSpawnParticles;

    [Header("References to Falling")]
    public Transform pivotPoint;
    public float timeToFall = 1f;

    protected bool hasDestroyed = false;


    public void OnTriggerEnter(Collider col)
    {
        if(isServer && !hasDestroyed)
        {
            hasDestroyed = true;
            Vector3 dif = col.transform.position - transform.position;
            RpcDestroyTheProp(-dif);
        }
    }

    [ClientRpc]
    public void RpcDestroyTheProp(Vector3 hitDirection)
    {
        Destroy(gameObject, timeToDestroy);

        if(wayToDestroy == DestroyWay.Particles || wayToDestroy == DestroyWay.ParticlesAndFall)
        {
            GameObject particles = GameObject.Instantiate(particlesToInstantiate, positionToSpawnParticles.position, positionToSpawnParticles.rotation);
        }
        if(wayToDestroy == DestroyWay.Fall || wayToDestroy == DestroyWay.ParticlesAndFall)
        {
            StartCoroutine(FallProp(hitDirection));
        }
    }

    protected IEnumerator FallProp(Vector3 hitDirection)
    {
        float speed = 90f/timeToFall;
        float counter = 0, delta;

        GameObject auxObj = new GameObject(gameObject.name + " Rotation Aux");
        Transform auxTransform = auxObj.transform;
        auxTransform.parent = null;
        auxTransform.position = pivotPoint.position;

        Transform lastParent = transform.parent;

        transform.SetParent(auxTransform);


        Vector3 axis = Vector3.Cross(Vector3.up, hitDirection);

        while(counter < timeToFall)
        {
            delta = Time.deltaTime;
            auxTransform.Rotate(axis, speed * delta);
            counter += delta;
            yield return null;
        }


        transform.SetParent(lastParent);
        Destroy(auxObj);

    }
}
