using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Cannon : NetworkBehaviour
{

    [Header("References")]
    [SyncVar]
    public NetworkIdentity tankIdentity;
    [SyncVar]
    public int team;

    public Tank tankScript;

    public Transform nivelTransform;
    public Transform gunnerCameraTransform;

    [Header("Movement")]
    public float turnSpeed = 20;
    public float nivelSpeed = 20;
    public float minNivel = -30;
    public float maxNivel = 30;

    [Header("Shooting")]
    public Transform bulletSpawnPosition;
    public GameObject bulletPrefab;
    public float bulletSpeed = 30;
    public float bulletDamage = 20;
    public float fireWaitTime = 1;
    protected float fireCounter = 0;

    
    //Movement variables
    [Range(-1,1)]
    public float rotationAxis;
    [Range(-1,1)]
    public float inclinationAxis;
    protected float currentInclinationAngle = 0;

    public void Start() {
        if(tankIdentity != null){
            tankScript = tankIdentity.GetComponent<Tank>();
            if(tankScript != null) {
                transform.SetParent(tankScript.rotationPivot);
            }
        }
    }


    public void setInputAxis(float rotation, float inclination) {
        rotationAxis = rotation;
        inclinationAxis = inclination;
    }

    protected void FixedUpdate(){
        if(!hasAuthority) return;
        updateRotation(Time.fixedDeltaTime);
    }

   protected void Update(){
       if(!isServer) return;
        fireCounter -= Time.deltaTime;

   }

    protected virtual void updateRotation(float deltaTime){
        //Should rotate
        if(rotationAxis != 0){
            transform.RotateAround(transform.position, transform.up, rotationAxis * turnSpeed * deltaTime);
        }

        if(inclinationAxis != 0) {
            currentInclinationAngle += inclinationAxis * nivelSpeed * deltaTime;
            currentInclinationAngle = Mathf.Clamp(currentInclinationAngle, minNivel, maxNivel);
            if(nivelTransform != null) {
                nivelTransform.localRotation = Quaternion.Euler(0,currentInclinationAngle,0);
            }
        }
    } 

    [Command]
    public void CmdShootCannon(Vector3 position, Quaternion rotation, Vector3 forward) {
        if(fireCounter <= 0){
            Vector3 positionToUse = position;
            Quaternion rotationToUse = rotation;
            Vector3 directionToUse = forward;

            GameObject bullet = GameObject.Instantiate(bulletPrefab, positionToUse, rotationToUse);

            bullet.transform.position = positionToUse;
            bullet.transform.rotation = rotationToUse;

            
            Bullet bulletScript = bullet.GetComponent<Bullet>();
            bulletScript.team = team;
            bulletScript.damage = bulletDamage;
            bulletScript.fireWithVelocity(directionToUse.normalized * bulletSpeed);

            Debug.Log("Firing from: " + positionToUse);

            NetworkServer.Spawn(bullet);

            fireCounter = fireWaitTime;
            
        }
    }

    protected void OnTriggerEnter(Collider col) {
        if(isServer){
            if(tankScript != null){
                tankScript.DealWithCollision(col, true);
            }
        }
    }
    
}
