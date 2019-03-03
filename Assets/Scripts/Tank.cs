using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Tank : NetworkBehaviour
{
    [Header("Team")]
    public List<Player> players;
    [SyncVar]
    public int team = -1;

    [Header("Transform references")]
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
    [SyncVar]
    public float currentHealth;
    public UnityEngine.UI.Slider healthSlider;


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


    [ClientRpc]
    public void RpcUpdateTankReferenceRPC(int team){
        GameMode.instance.setTankReference(this, team);
    }

    

    void Awake(){
        rgbd = GetComponent<Rigidbody>();
        myTransform = transform;
        if(team != -1) {
            GameMode.instance.setTankReference(this,team);
        }
        if(isServer) {
            currentHealth = maxHeath;
        }
    }

    public void ResetTank() {
        currentHealth = maxHeath;
    }

    void FixedUpdate(){
        if(hasAuthority) {
            checkGround();
            moveTank(Time.fixedDeltaTime);
        }
    }


    public void setAxis(float left, float right) {
        leftAxis = Mathf.Clamp(left,-1,1);
        rightAxis = Mathf.Clamp(right,-1,1);
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

    protected void OnTriggerEnter(Collider col) {
        if(isServer){
            Debug.Log("Enter trigger with " + col.gameObject);
            DealWithCollision(col, false);
        }
    }

    public void DealWithCollision(Collider col, bool fromCannon) {
        if(isServer){
            Bullet bullet = col.GetComponent<Bullet>();
            if(bullet != null) {
                Debug.Log("Bullet of team " + bullet.team + " , with " + bullet.damage + "  damage");
                if(bullet.team != team) {
                    DealDamage(bullet.damage);
                    NetworkServer.Destroy(col.gameObject);
                }
            }
        }
    }

    public void DealDamage(float damage) {
        Debug.Log("Tank from team " + team + " received " + damage + " damage!");
        currentHealth -= damage;
        if(currentHealth <= 0) {
            Debug.Log("Is ded. RIP team " + team);
        }
        else {
            RpcOnChangeHealth(currentHealth);
        }
    }

    public void SetHealthSlider(UnityEngine.UI.Slider slider) {
        healthSlider = slider;
        healthSlider.maxValue = maxHeath;
        healthSlider.minValue = 0;
        healthSlider.value = currentHealth;
    }


    [ClientRpc]
    public void RpcOnChangeHealth(float health) {
        if(healthSlider != null) {
            healthSlider.value = health;
        }
    }
    
}
