using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tank : MonoBehaviour
{
    [Header("Team")]
    public List<Player> players;
    public int team;

    [Header("Transform references")]
    public Transform cannonTransform;
    public Transform rotationPivot;
    public Transform cameraPositionDriver;
    public Transform cameraPositionGunner;
    public Transform rightThreadBegining;
    public Transform rightThreadEnd;
    public Transform leftThreadBegining;
    public Transform leftThreadEnd;

    [Header("Movement")]
    public float forwardSpeed = 10;
    public float backwardSpeed = 5;
    public float turnSpeed = 10;


    [Header("Health")]
    public float maxHeath = 100;
    [HideInInspector]
    public float currentHealth;


    //Movement variables
    // protected float leftAxis;
    // protected float rightAxis;
    [Range(-1,1)]
    public float leftAxis;
    [Range(-1,1)]
    public float rightAxis;
    protected bool rightThreadOnGround = true;
    protected bool leftThreadOnGround = true;
    

    //Components references
    protected Rigidbody rgbd;
    protected Transform myTransform;

    void Awake(){
        rgbd = GetComponent<Rigidbody>();
        myTransform = transform;
    }

    void FixedUpdate(){
        moveTank(Time.fixedDeltaTime);
    }

    public void setAxis(float left, float right) {
        leftAxis = Mathf.Clamp(left,-1,1);
        rightAxis = Mathf.Clamp(right,-1,1);
    }


    //Move the tank based on the axis inputs. Should be called from the fixed updates
    protected void moveTank(float deltaTime) {
        bool shouldMove = ( (rightAxis > 0) ^ (leftAxis > 0) );
        shouldMove = !shouldMove && rightAxis != 0 && leftAxis != 0;
        shouldMove = shouldMove && leftThreadOnGround && rightThreadOnGround;

        //Make linear movement
        if(shouldMove) {
            float axisMin = Mathf.Min(Mathf.Abs(leftAxis), Mathf.Abs(rightAxis));
            float speed = (rightAxis > 0 ) ? forwardSpeed : -backwardSpeed; //The left axis would also work
            speed *= axisMin;

            rgbd.velocity = myTransform.forward.normalized * speed;
        }

        //Rotation
        if( Mathf.Abs(leftAxis - rightAxis) > float.Epsilon){
            float dif = leftAxis - rightAxis;
            dif *= turnSpeed * deltaTime;
            myTransform.RotateAround(myTransform.position, myTransform.up.normalized, dif);
            rotationPivot.RotateAround(rotationPivot.position,myTransform.up.normalized, -dif);
        }
    }

    
}
