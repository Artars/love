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
    public Color color;

    [Header("Cannon")]
    public float turnCannonSpeed = 20;
    public float nivelCannonSpeed = 20;
    public float minCannonNivel = -30;
    public float maxCannonNivel = 30;
    [SyncVar]
    protected float rotationAxis;
    [SyncVar]
    protected float inclinationAxis;
    protected float currentInclinationAngle = 0;

    [Header("Shooting")]
    public float timeToShoot = 1;
    public GameObject bulletPrefab;
    public float bulletSpeed = 30;
    public float bulletDamage = 20;
    protected float cannonShootCounter;

    [Header("Transform references")]
    public Transform rotationPivot;
    public Transform cannonTransform;
    public Transform tankTransform;
    public Transform nivelTransform;
    public Transform bulletSpawnPosition;
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
    [SyncVar]
    public float leftAxis;
    [Range(-1,1)]
    [SyncVar]
    public float rightAxis;
    [SerializeField]
    [SyncVar]
    protected bool rightThreadOnGround = true;
    [SerializeField]
    [SyncVar]
    protected bool leftThreadOnGround = true;
    

    //Components references
    protected Rigidbody rgbd;
    protected Transform myTransform;


    [ClientRpc]
    public void RpcUpdateTankReferenceRPC(int team){
        GameMode.instance.setTankReference(this, team);
    }

    [ClientRpc]
    public void RpcSetColor(Color newColor) {
        color = newColor;
        ApplyColor();
    }

    public void ApplyColor(){
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach(Renderer r in renderers) {
            r.material.color = color;
        }
    }

    /// <summary>
    /// Should be called by the server
    /// </summary>
    public void ResetTank() {
        currentHealth = maxHeath;
        RpcOnChangeHealth(currentHealth);
    }

    public void ResetTankPosition(Vector3 position) {
        ResetTank();
        transform.position = position;
        transform.rotation = Quaternion.identity;
        rgbd.velocity = Vector3.zero;
        rotationPivot.rotation = Quaternion.identity;

        cannonTransform.rotation = Quaternion.identity;
        currentInclinationAngle = 0;
        nivelTransform.localRotation = Quaternion.Euler(0,currentInclinationAngle,0);

        RpcForceCannonRotationSync(cannonTransform.rotation,nivelTransform.rotation);
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

    void Start(){
        ApplyColor();
        if(!isServer){
            GetComponent<Rigidbody>().isKinematic = true;
        }
    }

    void Update(){
        if(!isServer) return;

        cannonShootCounter -= Time.deltaTime;

        if(Input.GetKeyDown(KeyCode.K) && team == 0){
            int otherTeam = (team == 1) ? 0 : 1;
            killTank(otherTeam);
        }
    }


    void FixedUpdate(){
        if(isServer) {
            checkGround();
            moveTank(Time.fixedDeltaTime);
        }
        updateCannonRotation(Time.fixedDeltaTime);
    }


    public void setAxis(float left, float right) {
        float newLeft = Mathf.Clamp(left,-1,1);
        float newRight = Mathf.Clamp(right,-1,1);
        if(leftAxis - newLeft > Mathf.Epsilon) leftAxis = newLeft;
        if(rightAxis - newRight > Mathf.Epsilon) leftAxis = newLeft;
    }

    public void setCannonAxis(float rotation, float nivel) {
        if(rotationAxis != rotation) rotationAxis = rotation;
        if(inclinationAxis != nivel) inclinationAxis = nivel;
    }

    public void cannonShoot() {
        if(cannonShootCounter < 0){
            ShootCannon(team);
            cannonShootCounter = timeToShoot;
        }
    }


    public void ShootCannon(int team) {
        Vector3 positionToUse = bulletSpawnPosition.position;
        Quaternion rotationToUse = bulletSpawnPosition.rotation;
        Vector3 directionToUse = bulletSpawnPosition.forward;

        GameObject bullet = GameObject.Instantiate(bulletPrefab, positionToUse, rotationToUse);

        bullet.transform.position = positionToUse;
        bullet.transform.rotation = rotationToUse;

        
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        bulletScript.team = team;
        bulletScript.damage = bulletDamage;
        bulletScript.fireWithVelocity(directionToUse.normalized * bulletSpeed);

        Debug.Log("Firing from: " + positionToUse);

        NetworkServer.Spawn(bullet);

        RpcForceCannonRotationSync(cannonTransform.rotation, nivelTransform.rotation);
    }

    public virtual void updateCannonRotation(float deltaTime){
        //Check rotation from tower
        float realRightAxis = rightThreadOnGround ? rightAxis : 0;
        float realLeftAxis = leftThreadOnGround ? leftAxis : 0;

        //Rotation
        if( Mathf.Abs(realRightAxis - realLeftAxis) > float.Epsilon){
            float dif = realLeftAxis - realRightAxis;
            dif *= turnSpeed * deltaTime * 0.5f;
            cannonTransform.RotateAround(transform.position,transform.up.normalized, -dif);
        }

        //Should rotate
        if(rotationAxis != 0){
            cannonTransform.RotateAround(transform.position, transform.up, rotationAxis * turnCannonSpeed * deltaTime);
        }

        if(inclinationAxis != 0) {
            currentInclinationAngle += inclinationAxis * nivelCannonSpeed * deltaTime;
            currentInclinationAngle = Mathf.Clamp(currentInclinationAngle, minCannonNivel, maxCannonNivel);
            if(nivelTransform != null) {
                nivelTransform.localRotation = Quaternion.Euler(0,currentInclinationAngle,0);
            }
        }
    } 

    [ClientRpc]
    public void RpcForceCannonRotationSync(Quaternion cannon, Quaternion nivel){
        if(isServer) return;
        cannonTransform.rotation = cannon;
        nivelTransform.rotation = nivel;
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
        //Break the tank
        else if(rightThreadOnGround && leftThreadOnGround){
            rgbd.velocity = Vector3.zero;
        }

        //Rotation
        if( Mathf.Abs(realRightAxis - realLeftAxis) > float.Epsilon){
            float dif = realLeftAxis - realRightAxis;
            dif *= turnSpeed * deltaTime * 0.5f;
            myTransform.RotateAround(myTransform.position, myTransform.up.normalized, dif);
            // rotationPivot.RotateAround(rotationPivot.position,myTransform.up.normalized, -dif);
        }
    }



    protected void OnTriggerEnter(Collider col) {
        if(isServer){
            Debug.Log("Enter trigger with " + col.gameObject);
            DealWithCollision(col);
        }
    }

    public void DealWithCollision(Collider col) {
        if(isServer){
            Bullet bullet = col.GetComponent<Bullet>();
            if(bullet != null) {
                Debug.Log("Bullet of team " + bullet.team + " , with " + bullet.damage + "  damage");
                if(bullet.team != team) {
                    DealDamage(bullet.damage, bullet.team);
                    NetworkServer.Destroy(col.gameObject);
                }
            }
        }
    }

    public void DealDamage(float damage, int otherTeam) {
        Debug.Log("Tank from team " + team + " received " + damage + " damage!");
        currentHealth -= damage;
        if(currentHealth <= 0) {
            Debug.Log("Is ded. RIP team " + team);
            killTank(otherTeam);
        }
        else {
            RpcOnChangeHealth(currentHealth);
        }
    }

    protected void killTank(int oposingTeam){
        GameMode.instance.tankKilled(team,oposingTeam);
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
