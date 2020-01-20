using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Most of the processing happen on the server. There is some local processing as well, such as sound
/// </summary>
public class Tank : NetworkBehaviour
{
    /// <summary>
    /// Used to keep track of who is on the tank
    /// </summary>
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
    public GameObject cannonPrefab;
    [SyncVar]
    public NetworkIdentity cannonIdentity;
    public Cannon cannonReference;

    [Header("Team")]
    public List<Player> players;
    [SyncVar]
    public int tankId = -1;
    [SyncVar]
    public int team = -1;
    public Color color;
    public List<Assigment> playerRoles;

    [Header("Parameters")]
    
    public TankParametersObject tankParametersObject;
    [SyncVar(hook="UpdateTankParameters")]
    public TankParameters tankParameters = null;

    [Header("Cannon")]
    protected float turnCannonSpeed = 20;
    protected float nivelCannonSpeed = 20;
    protected float minCannonNivel = -30;
    protected float maxCannonNivel = 30;
    [SyncVar]
    protected float rotationAxis;
    [SyncVar]
    protected float inclinationAxis;
    protected float currentInclinationAngle = 0;
    protected float currentRotationAngle = 0;


    [Header("Shooting")]
    public GameObject bulletPrefab;
    public ParticleSystem shootParticles;
    protected bool canMoveCannon = true;
    protected float shootCooldown = 1;
    public float ShootCooldown {
        get{return shootCooldown;}
    }
    protected float bulletSpeed = 30;
    protected float bulletDamage = 20;
    protected float cannonShootCounter;
    protected ShootMode shootMode;

    [Header("Transform references")]
    public Transform cannonAttachmentPoint;
    public Transform rotationPivot;
    public Transform tankTransform;
    public Transform nivelTransform;
    public Transform bulletSpawnPosition;
    public Transform cameraPositionDriver;
    public Transform cameraPositionGunner;
    public Transform rightThreadBegining;
    public Transform rightThreadEnd;
    public Transform leftThreadBegining;
    public Transform leftThreadEnd;
    public Transform frontCollisionCheck;
    public Transform backCollisionCheck;
    public Transform centerTransform;


    [SyncVar]
    protected bool canBeMoved = true;
    [Header("Movement")]
    public bool useNavMesh = false;
    public UnityEngine.AI.NavMeshAgent navMeshAgent;
    protected float forwardSpeed = 10;
    protected float backwardSpeed = 5;
    protected float turnSpeed = 10;
    protected GearSystem m_gearSystem;
    public float distanceCheckGround = 0.01f;
    public float distanceCollisionCheck = 0.5f;

    public GearSystem gearSystem {
        get {return m_gearSystem;}
    }


    [Header("Health")]
    public float maxHeath = 100;
    [SyncVar]
    public float currentHealth;
    public UnityEngine.UI.Slider healthSlider;

    [Header("Sound")]
    public AudioSource motorSoundSource;
    public AudioSource firingSoundSource;
    public AudioSource frontCollisionSoundSource;
    public AudioSource backCollisionSoundSource;
    public AudioSource hitSoundSource;
    public AudioSource turretRotationSoundSource;
    public float pitchStopped = 0.8f;
    public float pitchForward = 1.4f;
    public float pitchRotating = 1.0f;
    public float turretMaxPitch = 1.4f;
    public float turretDefaultPitch = 0.8f;
    protected float turretDefaultVolume = 0.5f;

    [Header("Threads")]
    public float threadSpeed = 0.2f;
    public MeshRenderer leftThreadMesh;
    public int leftThreadIndex = -1;
    protected Material leftThreadMaterial;
    public MeshRenderer rightThreadMesh;
    public int rightThreadIndex = -1;
    protected Material rightThreadMaterial;

    [Header("Killing")]
    public float timeToKillFlipped = 5f;
    public float angleLimitToKill;
    protected bool isFlippedCountdownOn = false;
    protected float flippedCountdown = 0;



    //Movement variables
    // protected float leftAxis;
    // protected float rightAxis;
    [Range(-1, 1)]
    [SyncVar]
    public float leftAxis;
    [Range(-1, 1)]
    [SyncVar]
    public float rightAxis;

    [SyncVar]
    public int leftGear;
    [SyncVar]
    public int rightGear;
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
    public void RpcOnChangeHealth(float health, bool receivedDamage) {
        if (healthSlider != null) {
            healthSlider.value = health;
        }

        if(receivedDamage)
        {
            hitSoundSource.Play();
        }
    }

    [ClientRpc]
    public void RpcForceCannonRotationSync(Quaternion cannon, Quaternion nivel) {
        if (isServer) return;
        rotationPivot.rotation = cannon;
        nivelTransform.rotation = nivel;
    }

    #endregion

    #region Initialization

    void Awake() {
        if(navMeshAgent == null)
            navMeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if(navMeshAgent == null)
        {
            useNavMesh = false;
        }
        else if(isClient)
        {
            navMeshAgent.enabled = false;
        }
        else
        {
            navMeshAgent.enabled = useNavMesh;
        }
        if(!isClient)
            tankParameters = tankParametersObject.tankParameters;
        UpdateTankParameters(tankParameters);
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

        //Sound
        motorSoundSource.loop = true;
        motorSoundSource.pitch = pitchStopped;
        motorSoundSource.Play();

        turretRotationSoundSource.loop = true;
        turretRotationSoundSource.pitch = turretDefaultPitch;
        turretDefaultVolume = turretRotationSoundSource.volume;
        turretRotationSoundSource.volume = 0;
        turretRotationSoundSource.Play();
        
    }

    protected void UpdateTankParameters(TankParameters tankParameters)
    {
        if (tankParameters == null) return;
        this.forwardSpeed = tankParameters.forwardSpeed;
        this.backwardSpeed = tankParameters.backwardSpeed;
        this.m_gearSystem = tankParameters.gearSystem;
        this.turnSpeed = tankParameters.turnSpeed;
        this.turnCannonSpeed = tankParameters.turnCannonSpeed;
        this.nivelCannonSpeed = tankParameters.nivelCannonSpeed;
        this.minCannonNivel = tankParameters.minCannonNivel;
        this.maxCannonNivel = tankParameters.maxCannonNivel;
        this.shootCooldown = tankParameters.shootCooldown;
        this.bulletSpeed = tankParameters.bulletSpeed;
        this.bulletDamage = tankParameters.bulletDamage;
        this.maxHeath = tankParameters.maxHeath;
        this.shootMode = tankParameters.shootMode;

        if(navMeshAgent != null)
        {
            navMeshAgent.speed = forwardSpeed;
            navMeshAgent.angularSpeed = turnSpeed;
        }
    }

    [Server]
    public void SetTankParameters(TankParameters tankParam)
    {
        tankParameters = tankParam;
        UpdateTankParameters(tankParameters);
    }

    public void SetCanMove(bool canMove)
    {
        this.canMoveCannon = canMove;
        SetNavMeshEnabled(canMove);
    }

    public void SetNavMeshEnabled(bool canMove)
    {
        this.canBeMoved = canMove && !useNavMesh;
        if(navMeshAgent != null)
        {
            navMeshAgent.enabled = canMove;
        }
    }

    void Start() {
        if (!isServer) {
            GetComponent<Rigidbody>().isKinematic = true;
        }
        else if (cannonPrefab != null)
        {
            //Spawn turret
            GameObject cannonInstance = GameObject.Instantiate(cannonPrefab);
            cannonIdentity = cannonInstance.GetComponent<NetworkIdentity>();
            cannonReference = cannonInstance.GetComponent<Cannon>();
            cannonReference.tankIdentity = GetComponent<NetworkIdentity>();
            cannonReference.SetTankReference(cannonReference.tankIdentity);

            rotationPivot.rotation = cannonAttachmentPoint.rotation;

            NetworkServer.Spawn(cannonInstance);

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
        RpcOnChangeHealth(currentHealth, false);

        leftGear = rightGear = 0;
        rightAxis = leftAxis = 0;
    }

    /// <summary>
    /// Reset tank position to be the one given. Will also reset rotation of guns
    /// </summary>
    /// <param name="position">Position to be placed</param>
    public void ResetTankPosition(Transform toSpawn) {
        ResetTank();
        transform.position = toSpawn.position;
        transform.rotation = toSpawn.rotation;
        rgbd.velocity = Vector3.zero;
        currentRotationAngle = 0;
        cannonReference.ForceUpdate();
        rotationPivot.rotation = Quaternion.Euler(0,cannonAttachmentPoint.rotation.eulerAngles.y,0);

        currentInclinationAngle = 0;
        nivelTransform.localRotation = Quaternion.Euler(currentInclinationAngle, 0, 0);

        // RpcForceCannonRotationSync(cannonTransform.rotation,nivelTransform.rotation);
    }

    #endregion

    #region Players

    public void AssignPlayer(Player player, Role role)
    {
        players.Add(player);

        bool hasOnlyOnePlayer = players.Count == 1;
        if (hasOnlyOnePlayer)
        {
            player.SetCanSwitchRoles(true);
        }
        else
        {
            foreach (var p in players)
            {
                p.SetCanSwitchRoles(false);
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
                p.SetCanSwitchRoles(true);
                p.RpcDisplayMessage("You can change roles", 2, GameMode.instance.defaultMessageColor, 0.1f, 0.5f);
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
                assigment.playerRef.SetCanSwitchRoles(false);
            assigment.playerRef = null;
        }
    }

    public void SwitchPlayerRole(Player player, Role currentRole)
    {
        if (!player.GetCanSwitchRoles()) return; //Avoid changin if the player is not available
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
                player.AssignPlayer(team, roleToSwitch, GetComponent<NetworkIdentity>());
            }
        }
    }

    public Player GetPlayerOfRole(Role searchRole)
    {
        for (int i = 0; i < playerRoles.Count; i++) {
            if(playerRoles[i].role == searchRole && playerRoles[i].playerRef != null)
            {
                return playerRoles[i].playerRef;
            }
        }

        return null;
    }

    #endregion

    #region Input

    // public void setAxis(float left, float right) {
    //     float newLeft = Mathf.Clamp(left, -1, 1);
    //     float newRight = Mathf.Clamp(right, -1, 1);
    //     if (leftAxis != newLeft) leftAxis = newLeft;
    //     if (rightAxis != newRight) rightAxis = newRight;
    // }
    public void SetGear(int left, int right)
    {
        leftGear = m_gearSystem.ClampGear(left);
        rightGear = m_gearSystem.ClampGear(right);

        leftAxis = m_gearSystem.GetGearValue(leftGear);
        rightAxis = m_gearSystem.GetGearValue(rightGear);
    }

    public void setCannonAxis(float rotation, float nivel) {
        if (rotationAxis != rotation) rotationAxis = rotation;
        if (inclinationAxis != nivel) inclinationAxis = nivel;
    }

    public void cannonShoot() {
        if (cannonShootCounter < 0 && canMoveCannon) {
            ShootCannon(team);
            cannonShootCounter = shootCooldown;
        }
    }

    public bool CanShootCannon()
    {
        return cannonShootCounter < 0 && canMoveCannon;
    }

    #endregion


    #region Update

    void Update() {
        //Both
        UpdateThreadsVisual();
        UpdateMotorPitch();
        UpdateTurretPitch();
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

    protected void UpdateMotorPitch()
    {
        bool isGoingForward = (rightAxis > 0 && leftAxis > 0);
        bool isRotatingInPlace = (rightAxis > 0 ^ leftAxis > 0);

        float pitch = pitchStopped;
        if(!isRotatingInPlace)
        {
            if(isGoingForward)
            {
                pitch += (pitchForward - pitchStopped) * Mathf.Min(rightAxis,leftAxis);
            }
            else
            {
                pitch += (pitchForward - pitchStopped) * -Mathf.Max(rightAxis,leftAxis);
            }
        }
        else
        {
            pitch += (pitchRotating - pitchStopped) * Mathf.Abs(rightAxis-leftAxis) * 0.5f;
        }

        motorSoundSource.pitch = pitch;
    }

    protected void UpdateTurretPitch()
    {
        float realRightAxis = rightThreadOnGround ? rightAxis : 0;
        float realLeftAxis = leftThreadOnGround ? leftAxis : 0;

        //Rotation compensation
        
        float dif = 0;
        if (Mathf.Abs(realRightAxis - realLeftAxis) > float.Epsilon) {
            dif = realRightAxis - realLeftAxis;
            dif *= 0.5f;
        }

        //Rotation from tank moving
        float totalRotation = dif * turnSpeed;
        //Rotation from turret itself
        totalRotation += rotationAxis * turnCannonSpeed;

        float maxRotation = (turnSpeed+turnCannonSpeed);

        float porcent = Mathf.InverseLerp(0,maxRotation, Mathf.Abs(totalRotation));
        float pitch = Mathf.Lerp(turretDefaultPitch, turretMaxPitch, porcent);

        turretRotationSoundSource.pitch = pitch;
        turretRotationSoundSource.volume = (porcent > Mathf.Epsilon) ? turretDefaultVolume : 0;
    }


    void FixedUpdate() {
        if (isServer) {
            CheckGround(Time.fixedDeltaTime);
            CheckCollision(Time.fixedDeltaTime);
            CheckTankRotation(Time.fixedDeltaTime);
            moveTank(Time.fixedDeltaTime);
            updateCannonRotation(Time.fixedDeltaTime);
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
        bulletScript.tankWhoShot = this;
        bulletScript.shootMode = shootMode;

        Debug.Log("Firing from: " + positionToUse);

        NetworkServer.Spawn(bullet);

        RpcShootCannon();

        // RpcForceCannonRotationSync(cannonTransform.rotation, nivelTransform.rotation);
    }

    [ClientRpc]
    public void RpcShootCannon()
    {
        shootParticles.Play();
        firingSoundSource.Play();
    }

    public virtual void updateCannonRotation(float deltaTime) {
        //Check rotation from tower
        float realRightAxis = rightThreadOnGround ? rightAxis : 0;
        float realLeftAxis = leftThreadOnGround ? leftAxis : 0;

        //Rotation compensation
        // if (Mathf.Abs(realRightAxis - realLeftAxis) > float.Epsilon) {
        //     float dif = realLeftAxis - realRightAxis;
        //     dif *= turnSpeed * deltaTime * 0.5f;
        //     currentRotationAngle -= dif;
        //     rotationPivot.localRotation = Quaternion.Euler(0, currentRotationAngle, 0);
        //     // cannonTransform.RotateAround(transform.position,transform.up.normalized, -dif);
        // }

        //Should rotate
        if (rotationAxis != 0) {
            rotationPivot.Rotate(0f, rotationAxis * turnCannonSpeed * deltaTime,0f);
            // currentRotationAngle += rotationAxis * turnCannonSpeed * deltaTime;
            // rotationPivot.localRotation = Quaternion.Euler(0, currentRotationAngle, 0);

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




    protected void CheckGround(float deltaTime) {
        // Raycast check
        bool begin = Physics.Raycast(leftThreadBegining.position, -leftThreadBegining.up, distanceCheckGround);
        bool end = Physics.Raycast(leftThreadEnd.position, -leftThreadBegining.up, distanceCheckGround);
        leftThreadOnGround = begin || end;

        begin = Physics.Raycast(rightThreadBegining.position, -rightThreadBegining.up, distanceCheckGround);
        end = Physics.Raycast(rightThreadEnd.position, -rightThreadBegining.up, distanceCheckGround);
        rightThreadOnGround = begin || end;

        // Box check
        // Collider[] result = new Collider[10];

        // Vector3 leftDif = leftThreadEnd.position - leftThreadBegining.position;
        // Vector3 leftPos = leftThreadBegining.position + leftDif*0.5f;
        // leftDif.y = distanceCheckGround;
        // leftPos.y -= distanceCheckGround*0.5f;
        // leftThreadOnGround = Physics.OverlapBoxNonAlloc(leftDif, leftDif * 0.5f, result, leftThreadBegining.rotation, LayerMask.GetMask("Default")) > 0; 

        // Vector3 rightDif = rightThreadEnd.position - rightThreadBegining.position;
        // Vector3 rightPos = rightThreadBegining.position + rightDif*0.5f;
        // rightDif.y = distanceCheckGround;
        // rightPos.y -= distanceCheckGround*0.5f;
        // rightThreadOnGround = Physics.OverlapBoxNonAlloc(rightDif, rightDif * 0.5f, result, rightThreadBegining.rotation, LayerMask.GetMask("Default")) > 0; 
        

        //Verify if it's currently flipped
        bool isFlipped = !(leftThreadOnGround || rightThreadOnGround);
        
        //Check if there's a diference between the check and the current state
        if(isFlippedCountdownOn ^ isFlipped)
        {
            //Will stop counter
            if(isFlippedCountdownOn)
            {
                isFlippedCountdownOn = false;
                flippedCountdown = 0;
            }
            //Will start counter
            else
            {
                isFlippedCountdownOn = true;
                //Make it start in 0
                flippedCountdown = -deltaTime;
            }
        }

        //Make the countdown
        if(isFlippedCountdownOn)
        {
            flippedCountdown += deltaTime;
            if(flippedCountdown >= timeToKillFlipped)
            {
                isFlippedCountdownOn = false;
                KillTank(tankId);
            }
        }
    }

    public void CheckCollision(float timeDelta)
    {
        //Make check only if moving
        if(rightGear == 0 && leftGear == 0) return;

        LayerMask layer = LayerMask.GetMask("Default");

        //Forward check
        Ray forwardRay = new Ray(frontCollisionCheck.position, frontCollisionCheck.forward);
        RaycastHit forwardResult;
        Physics.Raycast(forwardRay, out forwardResult,distanceCollisionCheck, layer);
        if(forwardResult.collider != null && !forwardResult.collider.isTrigger && rightGear > 0 && leftGear > 0)
        {
            CauseCollision(true);
            return;
        }

        //Back check
        Ray backRay = new Ray(backCollisionCheck.position, backCollisionCheck.forward);
        RaycastHit backResult;
        Physics.Raycast(backRay, out backResult,distanceCollisionCheck, layer);
        if(backResult.collider != null && !backResult.collider.isTrigger && rightGear < 0 && leftGear < 0)
        {
            CauseCollision(false);
            return;
        }

    }

    protected void CauseCollision(bool inFront)
    {
        // Call collision for everyone
        RpcHadCollision(inFront);

        ForceStop();

    }

    [ClientRpc]
    protected void RpcHadCollision(bool inFront)
    {
        if(inFront)
            frontCollisionSoundSource.Play();
        else
            backCollisionSoundSource.Play();

    }


    //Move the tank based on the axis inputs. Should be called from the fixed updates
    // protected void moveTank(float deltaTime) {
    //     if(!canBeControlled) return;

    //     forwardSpeed = 10;
    //     turnSpeed = 40 / Mathf.PI;

    //     rgbd.AddForce(myTransform.forward * forwardSpeed * (rightAxis + leftAxis), ForceMode.Acceleration);
    //     rgbd.AddRelativeTorque(myTransform.up * turnSpeed * -(rightAxis-leftAxis), ForceMode.Acceleration);

    // }
    protected void moveTank(float deltaTime) {
        if(!canBeMoved) return;

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

            // rgbd.velocity = myTransform.forward.normalized * speed + nonControllableSpeed;
            rgbd.MovePosition(myTransform.position + myTransform.forward * speed * deltaTime);
        } 
        //Break the tank
        else if(rightThreadOnGround && leftThreadOnGround){
            // rgbd.velocity = Vector3.zero + nonControllableSpeed;
        }

        //Rotation
        if( Mathf.Abs(realRightAxis - realLeftAxis) > float.Epsilon){
            float dif = realLeftAxis - realRightAxis;
            dif *= turnSpeed * deltaTime * 0.5f;
            myTransform.RotateAround(myTransform.position, myTransform.up.normalized, dif);
            // rotationPivot.RotateAround(rotationPivot.position,myTransform.up.normalized, -dif);
        }
    }

    public void CheckTankRotation(float deltaTime)
    {
        //Avoid killing and uncontrollable tank
        if(!canBeMoved)
            return;

        //Verify if it's currently flipped
        bool isFlipped = false;
        if(Mathf.Abs(myTransform.rotation.eulerAngles.x) > angleLimitToKill)
            isFlipped = true;
        if(Mathf.Abs(myTransform.rotation.eulerAngles.z) > angleLimitToKill)
            isFlipped = true;
        
        //Check if there's a diference between the check and the current state
        if(isFlippedCountdownOn ^ isFlipped)
        {
            //Will stop counter
            if(isFlippedCountdownOn)
            {
                isFlippedCountdownOn = false;
                flippedCountdown = 0;
            }
            //Will start counter
            else
            {
                isFlippedCountdownOn = true;
                //Make it start in 0
                flippedCountdown = -deltaTime;
            }
        }

        //Make the countdown
        if(isFlippedCountdownOn)
        {
            flippedCountdown += deltaTime;
            if(flippedCountdown >= timeToKillFlipped)
            {
                isFlippedCountdownOn = false;
                KillTank(tankId);
            }
        }
    }

    #endregion

    #region Damage


    public void DealWithCollision(Collider otherCollider, Collider selfCollider) {
        if(isServer){
            Bullet bullet = otherCollider.GetComponent<Bullet>();
            if(bullet != null) {
                Debug.Log("Bullet of team " + bullet.team + " , with " + bullet.damage + "  damage");
                if(bullet.team != team) {

                    // Reaction to different shoot modes
                    switch (bullet.shootMode)
                    {
                        case ShootMode.Damage:
                            DealDamage(bullet.damage, bullet.tankId, bullet.angleFired);
                            break;
                        
                        case ShootMode.Stop:
                            ForceStop();
                            break;

                        default:
                            DealDamage(bullet.damage, bullet.tankId, bullet.angleFired);
                            break;

                    }

                    // Make callback to shooter
                    if(bullet.tankWhoShot != null)
                        bullet.tankWhoShot.NotifyHitToGunner(otherCollider.transform.position);
                    // Destroy bullet
                    NetworkServer.Destroy(otherCollider.gameObject);
                }
            }
        }
    }

    public void DealDamage(float damage, int otherTank, float angle) {
        Debug.Log("Tank from team " + team + " received " + damage + " damage!");
        currentHealth -= damage;
        if(currentHealth <= 0 && canBeMoved) {
            Debug.Log("Is ded. RIP team " + team);
            KillTank(otherTank);
        }
        else {
            RpcOnChangeHealth(currentHealth, true);
            NotifyDamageToPlayers(damage,angle);
        }
    }

    public void ForceStop()
    {
        // Stop tank
        leftGear = rightGear = 0;
        leftAxis = rightAxis = 0;
        // Make pilot stop 
        foreach(var player in playerRoles)
        {
            if(player.role == Role.Pilot && player.playerRef != null)
            {
                player.playerRef.RpcForcePilotStop();
                break;
            }
        }
    }

    protected void CreateMock()
    {
        RpcCreateMock(transform.position, transform.rotation, rotationPivot.rotation, nivelTransform.eulerAngles.x);
    }

    [ClientRpc]
    protected void RpcCreateMock(Vector3 position, Quaternion rotation, Quaternion turretRotation, float cannonRotation)
    {
        GameObject mock = GameObject.Instantiate(mockPrefab, position, rotation);
        TankMock mockScript = mock.GetComponent<TankMock>();
        mockScript.ApplyPosition(position, rotation, turretRotation, Quaternion.Euler(cannonRotation,0,0));
        mockScript.Explode();
    }

    protected void NotifyDamageToPlayers(float damage, float angle)
    {
        foreach(Player player in players)
        {
            player.RpcReceiveDamageFromDirection(damage, angle);
        }
    }

    protected void NotifyHitToGunner(Vector3 position)
    {
        Player gunner = GetPlayerOfRole(Role.Gunner);
        if(gunner != null)
            gunner.RpcShowHitmark(position);
    }

    public void KillTank(int otherTank, bool explodeTank = true){
        if(!isServer) return;
        
        if(explodeTank)
            CreateMock();

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
            Gizmos.DrawRay(leftThreadBegining.position, -leftThreadBegining.up * distanceCheckGround);
            Gizmos.DrawRay(leftThreadEnd.position, -leftThreadBegining.up * distanceCheckGround);
        }
        if(rightThreadBegining != null && rightThreadEnd != null) {
            Gizmos.DrawRay(rightThreadBegining.position, -rightThreadBegining.up * distanceCheckGround);
            Gizmos.DrawRay(rightThreadEnd.position, -rightThreadBegining.up * distanceCheckGround);
        }
        if(frontCollisionCheck != null)
        {
            Gizmos.DrawRay(frontCollisionCheck.position, frontCollisionCheck.forward * distanceCollisionCheck);
        }
        if(backCollisionCheck != null)
        {
            Gizmos.DrawRay(backCollisionCheck.position, backCollisionCheck.forward * distanceCollisionCheck);
        }
    }
}
