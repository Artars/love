using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

public class Tank : NetworkedBehaviour
{
    [Header("Team")]
    public List<Player> players;
    public NetworkedVar<int> team = new NetworkedVar<int>(-1);
    public Cannon cannon;

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
    public float distanceCheckGround = 0.5f;


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
    [SerializeField]
    protected bool rightThreadOnGround = true;
    [SerializeField]
    protected bool leftThreadOnGround = true;
    

    //Components references
    protected Rigidbody rgbd;
    protected Transform myTransform;


    [ClientRPC]
    public void updateTankReferenceRPC(int team){
        GameMode.instance.setTankReference(this, team);
    }

    [ServerRPC(RequireOwnership = false)]
    public void setTankInputRPC(float left, float right){
        setAxis(left,right);
    }

    [ServerRPC(RequireOwnership = false)]
    public void setCannonInputRPC(float rotation, float inclination){
        cannon.setInputAxis(rotation,inclination);
    }

    void Awake(){
        rgbd = GetComponent<Rigidbody>();
        myTransform = transform;
        if(team.Value != -1) {
            GameMode.instance.setTankReference(this,team.Value);
        }
    }


    void FixedUpdate(){
        if(isOwner) {
            checkGround();
            moveTank(Time.fixedDeltaTime);
            cannon.UpdateLoop(Time.fixedDeltaTime);
        }
    }

    public void attachCannonToTank(Cannon cannonRef) {
        cannon = cannonRef;
        cannon.transform.SetParent(cannonTransform);
        cameraPositionGunner = cannon.gunnerCameraTransform;
    }

    public void setAxis(float left, float right) {
        leftAxis = Mathf.Clamp(left,-1,1);
        rightAxis = Mathf.Clamp(right,-1,1);
    }

    public void setCannonAxis(float horizontal, float inclination) {
        cannon.setInputAxis(horizontal, inclination);
    }


    protected void checkGround(){

        bool begin = Physics.Raycast(leftThreadBegining.position, -leftThreadBegining.up, distanceCheckGround);
        bool end = Physics.Raycast(leftThreadEnd.position, -leftThreadBegining.up, distanceCheckGround);
        leftThreadOnGround = begin && end;

        begin = Physics.Raycast(rightThreadBegining.position, -rightThreadBegining.up, distanceCheckGround);
        end = Physics.Raycast(rightThreadEnd.position, -rightThreadBegining.up, distanceCheckGround);
        rightThreadOnGround = begin && end;
    }


    //Move the tank based on the axis inputs. Should be called from the fixed updates
    protected void moveTank(float deltaTime) {
        float realRightAxis = rightThreadOnGround ? rightAxis : 0;
        float realLeftAxis = leftThreadOnGround ? leftAxis : 0;

        bool shouldMove = ( (realRightAxis > 0) ^ (realLeftAxis > 0) );
        shouldMove = !shouldMove && realRightAxis != 0 && realLeftAxis != 0;

        //Make linear movement
        if(shouldMove) {
            float axisMin = Mathf.Min(Mathf.Abs(realLeftAxis), Mathf.Abs(realRightAxis));
            float speed = (realRightAxis > 0 ) ? forwardSpeed : -backwardSpeed; //The left axis would also work
            speed *= axisMin;

            rgbd.velocity = myTransform.forward.normalized * speed;
        }

        //Rotation
        if( Mathf.Abs(realRightAxis - realLeftAxis) > float.Epsilon){
            float dif = realLeftAxis - realRightAxis;
            dif *= turnSpeed * deltaTime * 0.5f;
            myTransform.RotateAround(myTransform.position, myTransform.up.normalized, dif);
            rotationPivot.RotateAround(rotationPivot.position,myTransform.up.normalized, -dif);
        }
    }


    protected void OnDrawGizmos() {
        Gizmos.color = Color.red;
        if(leftThreadBegining != null && leftThreadEnd != null) {
            Gizmos.DrawRay(leftThreadBegining.position,-leftThreadBegining.up * distanceCheckGround);
            Gizmos.DrawRay(leftThreadEnd.position,-leftThreadEnd.up * distanceCheckGround);
        }
        if(rightThreadBegining != null && rightThreadEnd != null) {
            Gizmos.DrawRay(rightThreadBegining.position,-rightThreadBegining.up * distanceCheckGround);
            Gizmos.DrawRay(rightThreadEnd.position,-rightThreadEnd.up * distanceCheckGround);
        }
    }
    
}
