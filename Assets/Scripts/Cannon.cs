using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cannon : MonoBehaviour
{

    [Header("References")]
    public Transform nivelTransform;

    [Header("Movement")]
    public float turnSpeed = 20;
    public float nivelSpeed = 20;
    public float minNivel = -30;
    public float maxNivel = 30;

    
    //Movement variables
    [Range(-1,1)]
    public float rotationAxis;
    [Range(-1,1)]
    public float inclinationAxis;
    protected float currentInclinationAngle;

    protected void FixedUpdate(){
        updateRotation(Time.fixedDeltaTime);
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
    
}
