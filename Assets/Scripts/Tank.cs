using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Tank : NetworkBehaviour
{

    [System.Serializable]
    public class Assigment
    {
        public Role role;
        public Player playerRef = null;
        public bool available {
            get {
                return playerRef == null;
            }
            set {

            }
        }

        public Assigment(Role role)
        {
            this.role = role;
            playerRef = null;
        }

        public Assigment(Role role, Player player) : this(role)
        {
            playerRef = player;
        }
    }

    #region Variables

    [Header("Main references")]
    public TankOption tankOption;
    public GameObject mockPrefab;

    [Header("Team")]
    public List<Player> players;
    [SyncVar]
    public int tankId = -1;
    [SyncVar]
    public int team = -1;
    public Color color;
    public List<Assigment> playerRoles;

    [Header("Parameters")]
    public TankParameters tankParameters;

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
    protected float currentRotationAngle = 0;


    [Header("Shooting")]
    public float shootCooldown = 1;
    public GameObject bulletPrefab;
    public ParticleSystem shootParticles;
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
    [SyncVar]
    public bool canBeControlled = true;
    public float forwardSpeed = 10;
    public float backwardSpeed = 5;
    public float turnSpeed = 10;
    public float distanceCheckGround = 0.01f;



    [Header("Health")]
    public float maxHeath = 100;
    [SyncVar]
    public float currentHealth;
    public UnityEngine.UI.Slider healthSlider;

    [Header("Threads")]
    public float threadSpeed = 0.2f;
    public MeshRenderer leftThreadMesh;
    public int leftThreadIndex = -1;
    protected Material leftThreadMaterial;
    public MeshRenderer rightThreadMesh;
    public int rightThreadIndex = -1;
    protected Material rightThreadMaterial;



    //Movement variables
    // protected float leftAxis;
    // protected float rightAxis;
    [Range(-1, 1)]
    [SyncVar]
    public float leftAxis;
    [Range(-1, 1)]
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


    #endregion

    #region Messaging


    [ClientRpc]
    public void RpcSetColor(Color newColor) {
        color = newColor;
        ApplyColor();
    }

    [ClientRpc]
    public void RpcOnChangeHealth(float health) {
        if (healthSlider != null) {
            healthSlider.value = health;
        }
    }

    [ClientRpc]
    public void RpcForceCannonRotationSync(Quaternion cannon, Quaternion nivel) {
        if (isServer) return;
        cannonTransform.rotation = cannon;
        nivelTransform.rotation = nivel;
    }

    #endregion

    #region Initialization

    void Awake() {
        LoadTankParameters();
        rgbd = GetComponent<Rigidbody>();
        myTransform = transform;
        if (isServer) {
            currentHealth = maxHeath;
        }

        //Roles
        playerRoles = new List<Assigment>();
        foreach(var role in tankOption.tankRoles)
        {
            playerRoles.Add(new Assigment(role));
        }

        //Threads
        if(leftThreadIndex != -1)
            leftThreadMaterial = leftThreadMesh.materials[leftThreadIndex];
        else
            leftThreadMaterial = null;
        if(rightThreadIndex != -1)
            rightThreadMaterial = rightThreadMesh.materials[rightThreadIndex];
        else
            rightThreadMaterial = null;
        
    }

    protected void LoadTankParameters()
    {
        if (tankParameters == null) return;
        this.forwardSpeed = tankParameters.forwardSpeed;
        this.backwardSpeed = tankParameters.backwardSpeed;
        this.turnSpeed = tankParameters.turnSpeed;
        this.turnCannonSpeed = tankParameters.turnCannonSpeed;
        this.nivelCannonSpeed = tankParameters.nivelCannonSpeed;
        this.minCannonNivel = tankParameters.minCannonNivel;
        this.maxCannonNivel = tankParameters.maxCannonNivel;
        this.shootCooldown = tankParameters.shootCooldown;
        this.bulletSpeed = tankParameters.bulletSpeed;
        this.bulletDamage = tankParameters.bulletDamage;
        this.maxHeath = tankParameters.maxHeath;
    }

    void Start() {
        // ApplyColor();
        if (!isServer) {
            GetComponent<Rigidbody>().isKinematic = true;
        }
    }

    public void ApplyColor() {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) {
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

    /// <summary>
    /// Reset tank position to be the one given. Will also reset rotation of guns
    /// </summary>
    /// <param name="position">Position to be placed</param>
    public void ResetTankPosition(Vector3 position) {
        ResetTank();
        transform.position = position;
        transform.rotation = Quaternion.identity;
        rgbd.velocity = Vector3.zero;
        currentRotationAngle = 0;
        rotationPivot.localRotation = Quaternion.identity;

        cannonTransform.localRotation = Quaternion.identity;
        currentInclinationAngle = 0;
        nivelTransform.localRotation = Quaternion.Euler(0, currentInclinationAngle, 0);

        // RpcForceCannonRotationSync(cannonTransform.rotation,nivelTransform.rotation);
    }

    public void AssignPlayer(Player player, Role role)
    {
        players.Add(player);

        bool hasOnlyOnePlayer = players.Count == 1;
        if (hasOnlyOnePlayer)
        {
            player.canSwitchRoles = true;
        }
        else
        {
            foreach (var p in players)
            {
                p.canSwitchRoles = false;
            }
        }

        for (int i = 0; i < playerRoles.Count; i++) {
            if (playerRoles[i].role == role) {
                playerRoles[i].playerRef = player;
                break;
            }
        }
    }

    public void RemovePlayer(Player player, Role role)
    {
        players.Remove(player);

        bool hasOnlyOnePlayer = players.Count == 1;
        if (hasOnlyOnePlayer)
        {
            foreach (var p in players)
            {
                p.canSwitchRoles = true;
                p.RpcDisplayMessage("You can change roles", 2, 0.1f, 0.5f);
            }
        }

        for (int i = 0; i < playerRoles.Count; i++) {
            if (playerRoles[i].role == role) {
                playerRoles[i].playerRef = null;
                break;
            }
        }
    }

    public void ClearPlayerAssigments()
    {
        players.Clear();
        foreach(var assigment in playerRoles)
        {
            if(assigment.playerRef != null)
                assigment.playerRef.canSwitchRoles = false;
            assigment.playerRef = null;
        }
    }

    public void SwitchPlayerRole(Player player, Role currentRole)
    {
        if (!player.canSwitchRoles) return; //Avoid changin if the player is not available
        Role roleToSwitch = (currentRole == Role.Pilot) ? Role.Gunner : Role.Pilot;

        for (int i = 0; i < playerRoles.Count; i++) {
            //Remove current player role reference
            if (playerRoles[i].role == currentRole) {
                playerRoles[i].playerRef = null;
            }
            //Set new reference and switch player
            else if (playerRoles[i].role == roleToSwitch)
            {
                playerRoles[i].playerRef = player;
                player.role = roleToSwitch;
                player.RpcAssignPlayer(team, roleToSwitch, GetComponent<NetworkIdentity>());
            }
        }
    }

    #endregion



    #region Update

    void Update() {
        //Both
        UpdateThreadsVisual();
        //Only server
        if(isServer)
        {
            cannonShootCounter -= Time.deltaTime;
        }
        //Only client
        else
        {
            
        }

    }

    protected void UpdateThreadsVisual()
    {
        if(rightThreadMaterial != null)
        {
            rightThreadMaterial.mainTextureOffset += new Vector2(threadSpeed * rightAxis * Time.deltaTime,0);
        }
        if(leftThreadMaterial != null)
        {
            leftThreadMaterial.mainTextureOffset += new Vector2(threadSpeed * leftAxis * Time.deltaTime,0);
        }
    }


    void FixedUpdate() {
        if (isServer) {
            checkGround();
            moveTank(Time.fixedDeltaTime);
            updateCannonRotation(Time.fixedDeltaTime);
        }
    }


    public void setAxis(float left, float right) {
        float newLeft = Mathf.Clamp(left, -1, 1);
        float newRight = Mathf.Clamp(right, -1, 1);
        if (leftAxis != newLeft) leftAxis = newLeft;
        if (rightAxis != newRight) rightAxis = newRight;
    }

    public void setCannonAxis(float rotation, float nivel) {
        if (rotationAxis != rotation) rotationAxis = rotation;
        if (inclinationAxis != nivel) inclinationAxis = nivel;
    }

    public void cannonShoot() {
        if (cannonShootCounter < 0 && canBeControlled) {
            ShootCannon(team);
            cannonShootCounter = shootCooldown;
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
        bulletScript.tankId = tankId;
        bulletScript.damage = bulletDamage;
        bulletScript.fireWithVelocity(directionToUse.normalized * bulletSpeed);

        Debug.Log("Firing from: " + positionToUse);

        NetworkServer.Spawn(bullet);

        RpcShootCannon();

        // RpcForceCannonRotationSync(cannonTransform.rotation, nivelTransform.rotation);
    }

    [ClientRpc]
    public void RpcShootCannon()
    {
        shootParticles.Play();
    }

    public virtual void updateCannonRotation(float deltaTime) {
        //Check rotation from tower
        float realRightAxis = rightThreadOnGround ? rightAxis : 0;
        float realLeftAxis = leftThreadOnGround ? leftAxis : 0;

        //Rotation
        if (Mathf.Abs(realRightAxis - realLeftAxis) > float.Epsilon) {
            float dif = realLeftAxis - realRightAxis;
            dif *= turnSpeed * deltaTime * 0.5f;
            currentRotationAngle -= dif;
            cannonTransform.localRotation = Quaternion.Euler(0, currentRotationAngle, 0);
            // cannonTransform.RotateAround(transform.position,transform.up.normalized, -dif);
        }

        //Should rotate
        if (rotationAxis != 0) {
            currentRotationAngle += rotationAxis * turnCannonSpeed * deltaTime;
            cannonTransform.localRotation = Quaternion.Euler(0, currentRotationAngle, 0);

            // cannonTransform.RotateAround(transform.position, transform.up, rotationAxis * turnCannonSpeed * deltaTime);
        }

        if (inclinationAxis != 0) {
            currentInclinationAngle += inclinationAxis * nivelCannonSpeed * deltaTime;
            currentInclinationAngle = Mathf.Clamp(currentInclinationAngle, minCannonNivel, maxCannonNivel);
            if (nivelTransform != null) {
                nivelTransform.localRotation = Quaternion.Euler(-currentInclinationAngle, 0, 0); //For some reason, positive means downward
            }
        }
    }




    protected void checkGround() {

        bool begin = Physics.Raycast(leftThreadBegining.position, -leftThreadBegining.up, distanceCheckGround);
        bool end = Physics.Raycast(leftThreadEnd.position, -leftThreadBegining.up, distanceCheckGround);
        leftThreadOnGround = begin && end;

        begin = Physics.Raycast(rightThreadBegining.position, -rightThreadBegining.up, distanceCheckGround);
        end = Physics.Raycast(rightThreadEnd.position, -rightThreadBegining.up, distanceCheckGround);
        rightThreadOnGround = begin && end;
    }


    //Move the tank based on the axis inputs. Should be called from the fixed updates
    protected void moveTank(float deltaTime) {
        if(!canBeControlled) return;

        Vector3 nonControllableSpeed = Vector3.Dot(rgbd.velocity, transform.up) * transform.up;

        float realRightAxis = rightThreadOnGround ? rightAxis : 0;
        float realLeftAxis = leftThreadOnGround ? leftAxis : 0;

        bool shouldMove = ( (realRightAxis > 0) ^ (realLeftAxis > 0) );
        shouldMove = !shouldMove && realRightAxis != 0 && realLeftAxis != 0;

        //Make linear movement
        if(shouldMove) {
            float axisMin = Mathf.Min(Mathf.Abs(realLeftAxis), Mathf.Abs(realRightAxis));
            float speed = (realRightAxis > 0 ) ? forwardSpeed : -backwardSpeed; //The left axis would also work
            speed *= axisMin;

            rgbd.velocity = myTransform.forward.normalized * speed + nonControllableSpeed;
        } 
        //Break the tank
        else if(rightThreadOnGround && leftThreadOnGround){
            rgbd.velocity = Vector3.zero + nonControllableSpeed;
        }

        //Rotation
        if( Mathf.Abs(realRightAxis - realLeftAxis) > float.Epsilon){
            float dif = realLeftAxis - realRightAxis;
            dif *= turnSpeed * deltaTime * 0.5f;
            myTransform.RotateAround(myTransform.position, myTransform.up.normalized, dif);
            // rotationPivot.RotateAround(rotationPivot.position,myTransform.up.normalized, -dif);
        }
    }

    #endregion

    #region Damage


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
                    DealDamage(bullet.damage, bullet.tankId, bullet.angleFired);
                    NetworkServer.Destroy(col.gameObject);
                }
            }
        }
    }

    public void DealDamage(float damage, int otherTank, float angle) {
        Debug.Log("Tank from team " + team + " received " + damage + " damage!");
        currentHealth -= damage;
        if(currentHealth <= 0 && canBeControlled) {
            Debug.Log("Is ded. RIP team " + team);
            CreateMock();
            killTank(otherTank);
        }
        else {
            RpcOnChangeHealth(currentHealth);
            NotifyDamageToPlayers(damage,angle);
        }
    }

    protected void CreateMock()
    {
        RpcCreateMock(transform.position, transform.rotation, rotationPivot.localRotation.eulerAngles.y, nivelTransform.eulerAngles.x);
    }

    [ClientRpc]
    protected void RpcCreateMock(Vector3 position, Quaternion rotation, float turretRotation, float cannonRotation)
    {
        GameObject mock = GameObject.Instantiate(mockPrefab, position, rotation);
        TankMock mockScript = mock.GetComponent<TankMock>();
        mockScript.ApplyPosition(position, rotation, Quaternion.Euler(0,turretRotation,0), Quaternion.Euler(cannonRotation,0,0));
        mockScript.Explode();
    }

    protected void NotifyDamageToPlayers(float damage, float angle)
    {
        foreach(Player player in players)
        {
            player.RpcReceiveDamageFromDirection(damage, angle);
        }
    }

    protected void killTank(int otherTank){
        GameMode.instance.TankKilled(tankId,otherTank);
    }

    public void SetHealthSlider(UnityEngine.UI.Slider slider) {
        healthSlider = slider;
        healthSlider.maxValue = maxHeath;
        healthSlider.minValue = 0;
        healthSlider.value = currentHealth;
    }


    #endregion

    
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
