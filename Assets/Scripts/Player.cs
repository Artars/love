using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class Player : NetworkBehaviour
{

    public enum Mode {
        Selecting, Observing, Playing, Spectator
    }

    public static Player localPlayer = null;

    [Header("DO NOT MESS WITH THESE VARIABLES!")]
    [SyncVar]
    public PlayerInfo playerInfo;
    [SyncVar]
    public Mode currentMode = Mode.Selecting;
    public Tank tankRef;
    public Cannon cannonRef;
    [SyncVar]
    public int team = -1;
    [SyncVar]
    public Role role = Role.Pilot;
    [SyncVar]
    public NetworkIdentity possesedObject;
    [SyncVar]
    public bool canSwitchRoles = false;

    [Header("Sound")]
    public AudioSource musicAudioSource;


    [Header("Control")]
    public float doubleClickTimeDelay = 0.2f;
    protected float lastTimeClicked = 0;


    [Header("Camera")]
    public Transform firstPersonCamera;

    [Header("Hitmark")]
    public GameObject hitmarkPrefab;

    [Header("Observe references")]
    public Transform observerTransform;
    public Transform observerPivot;
    public Transform observerCameraTransform;
    protected Vector3 pointToObserve;
    protected float rotateSpeed;

    [Header("Pilot")]
    public GearSystem gearSystem;
    protected int rightGear;
    protected int leftGear;
    protected int old_rightGear = -2; //Force first sync
    protected int old_leftGear = -2;

    protected AxisToButton leftGearInput;
    protected AxisToButton rightGearInput;

    
    //Gunner propeties
    protected float old_horizontal = -2;
    protected float old_vertical = -2;
    protected float fireCounter = 0;

    //Other
    protected bool assignedCallback = false;
    protected Coroutine messageCoroutine = null;

    protected PlayerHUD playerHUD;

    #region Initialization

    //Remove player from the game
    public override void OnNetworkDestroy () {
        if(isServer){
            if(tankRef != null)
            {
                tankRef.RemovePlayer(this, role);
                GameMode.instance.NotifyPlayerLeft(this,playerInfo);
            }
        }
        base.OnNetworkDestroy();
    }

    protected void Start() {
        //Set Player HUD
        playerHUD = GetComponent<PlayerHUD>();

        //Update referece
        if(isLocalPlayer){
            //Set player HUD
            playerHUD.AddPilotCallback(onUpdateLeftSlider,onUpdateRightSlider);

            observerTransform.gameObject.SetActive(true);
            localPlayer = this;

            //Create inputs
            rightGearInput = new AxisToButton("Vertical");
            leftGearInput = new AxisToButton("Vertical2");

            //Set time reference
        }

        if(GameMode.instance != null) {
            if(isServer)
            {
                GameMode.instance.TryToJoinAsSpectator(this);
            }
        }
        //Wil disable comands for HUD
        else if(isServer) {
            currentMode = Mode.Selecting;
        }
    }

    public void SetTankReference(Tank tank, int team, Role role){
        tankRef = tank;
        this.team = team;
        this.role = role;
    }

    #endregion

    #region Networking

    [ClientRpc]
    public void RpcObservePosition(Vector3 position, float speed, float angle, float distance) {
        if(isLocalPlayer){
            Debug.Log("Will observe " + position);
            currentMode = Mode.Observing;
            playerHUD.HideHUD();
            firstPersonCamera.gameObject.SetActive(false);
            observerTransform.gameObject.SetActive(true);

            pointToObserve = position;
            rotateSpeed = speed;

            observerTransform.position = pointToObserve;
            observerPivot.rotation = Quaternion.Euler(0,0,angle);
            observerCameraTransform.localPosition = new Vector3(distance,0,0);
        }
    }

    [ClientRpc]
    public void RpcAssignSpectator()
    {
        playerHUD.TryToAssignCallback();
    }

    [ClientRpc]
    public void RpcAssignPlayer(int team, Role role, NetworkIdentity toAssign){
        Debug.Log("Assigning player team " + team + " with role " + role.ToString());

        if(!isLocalPlayer) return;


        observerTransform.gameObject.SetActive(false);
        firstPersonCamera.gameObject.SetActive(true);

        this.team = team;
        this.role = role;
        this.possesedObject = toAssign;
        tankRef = possesedObject.GetComponent<Tank>();
        currentMode = Mode.Playing;
        
        if(role == Role.Pilot){
            firstPersonCamera.position = tankRef.cameraPositionDriver.position;
            firstPersonCamera.rotation = tankRef.cameraPositionDriver.rotation;
            firstPersonCamera.SetParent(tankRef.cameraPositionDriver);
        }
        else if(role == Role.Gunner){
            firstPersonCamera.position = tankRef.cameraPositionGunner.position;
            firstPersonCamera.rotation = tankRef.cameraPositionGunner.rotation;
            firstPersonCamera.SetParent(tankRef.cameraPositionGunner);
        }


        //Assign HUD
        playerHUD.AssignTank(team, role, tankRef);

        
        //Update control values
        //Update axis
        rightGear = tankRef.rightGear;
        leftGear = tankRef.leftGear;
        playerHUD.SetPilotValue(leftGear,rightGear);

    }

    [ClientRpc]
    public void RpcRemoveOwnership(){
        if(!isLocalPlayer) return;

        currentMode = Mode.Observing;

        //Reset control variables
        rightGear = 0;
        leftGear = 0;
        fireCounter = 0;

        //Remove tank references
        tankRef = null;
        possesedObject = null;

        playerHUD.ResetInput();

        playerHUD.HideHUD();
    }

    [ClientRpc]
    public void RpcDisplayMessage(string message, float duration, float fadeIn, float fadeOut){
        if(isLocalPlayer){
            playerHUD.AddMessage(new PlayerHUD.PlayerMessage(playerHUD.defaultMessageText, 
            message,playerHUD.defaultMessageColor, duration, fadeIn, fadeOut));
        }
    }


    [ClientRpc]
    public void RpcPlayVictoryMusic()
    {
        if(isLocalPlayer)
        {
            musicAudioSource.gameObject.SetActive(true);
            musicAudioSource.Play();
        }
    }

    [ClientRpc]
    public void RpcReceiveDamageFromDirection(float damage, float angle)
    {
        if(isLocalPlayer)
        {
            Debug.Log("Received " + damage + " damage from direction " + angle);
            playerHUD.CreateHitpoint(angle,firstPersonCamera);
        }
    }

    [ClientRpc]
    public void RpcForcePilotStop()
    {
        if(isLocalPlayer)
        {
            leftGear = old_leftGear = 0;
            rightGear = old_rightGear = 0;

            playerHUD.SetPilotValue(leftGear,rightGear);
        }
    }

    [ClientRpc]
    public void RpcShowHitmark(Vector3 position)
    {
        if(isLocalPlayer)
        {
            GameObject.Instantiate(hitmarkPrefab, position, Quaternion.identity);
        }
    }

    [Command]
    public void CmdSwitchRole(Role currentRole)
    {
        if(tankRef != null)
        {
            tankRef.SwitchPlayerRole(this, currentRole);
        }
    }


    //Input update on server
    [Command]
    public void CmdUpdateGearPilot(int left, int right){
        if(role == Role.Pilot){
            if(tankRef != null) {
                tankRef.SetGear(left, right);
            }
        }
    }

    [Command]
    public void CmdUpdateAxisGunner(float horizontal, float vertical) {
        if(role == Role.Gunner){
            if(tankRef != null) {
                tankRef.setCannonAxis(horizontal,vertical);
            }
        }
    }
    
    [Command]
    public void CmdShootCannon(){
        if(role == Role.Gunner){
            if(tankRef != null) {
                tankRef.cannonShoot();
            }
        }
    }

    #endregion

    #region Input

    protected void Update() {
        if(!isLocalPlayer) return;
        
        if(currentMode == Mode.Observing) {
            if(pointToObserve != null) {
                observerTransform.position = pointToObserve;
                observerTransform.RotateAround(pointToObserve, observerTransform.up, rotateSpeed * Time.deltaTime);
            }
        }
        else if(currentMode == Mode.Playing) {
            UpdateHUD(Time.deltaTime);
            if(role == Role.Pilot){
                pilotUpdate(Time.deltaTime);
            }
            else if(role == Role.Gunner){
                gunnerUpdate(Time.deltaTime);
            }

            if(canSwitchRoles)
            {
                TrySwitchRole();
            }
        }
    }

    protected void pilotUpdate(float deltaTime){
        if(!Input.GetButton("Stop")){
            if(leftGearInput.GetButtonDown())
            {
                leftGear += leftGearInput.currentValue;
            }
            if(rightGearInput.GetButtonDown())
            {
                rightGear += rightGearInput.currentValue;
            }
        }
        else {
            rightGear = 0;
            leftGear = 0;
        }
        rightGear = gearSystem.ClampGear(rightGear);
        leftGear = gearSystem.ClampGear(leftGear);

        playerHUD.SetPilotValue(leftGear,rightGear);

        if(old_leftGear != leftGear || old_rightGear != rightGear) {
            old_leftGear = leftGear;
            old_rightGear = rightGear;
            CmdUpdateGearPilot(leftGear, rightGear);
        }
    }

    protected void gunnerUpdate(float deltaTime) {
        UpdateGunnerCooldown(deltaTime);

        bool isPressing = Input.GetButton("Fire") || playerHUD.buttonState;

        if(fireCounter <= 0 && isPressing){
            Debug.Log("Tried to shoot from " + tankRef.bulletSpawnPosition.position);
            CmdShootCannon();
            fireCounter = tankRef.ShootCooldown;
        }

        float horizontalAxis = Input.GetAxis("Horizontal");
        float verticalAxis = Input.GetAxis("Vertical");

        if(playerHUD.IsJoystickValid()){
            Vector2 joyStickInput = playerHUD.GetJoystickInput();
            horizontalAxis = joyStickInput.x;
            verticalAxis = joyStickInput.y;
        }
        else if(!Input.GetButton("SprintCannon"))
        {
            horizontalAxis *= 0.5f;
            verticalAxis *= 0.5f;
        }

        if(old_horizontal != horizontalAxis || old_vertical != verticalAxis){
            old_horizontal = horizontalAxis;
            old_vertical = verticalAxis;
            CmdUpdateAxisGunner(horizontalAxis, verticalAxis);
        }

    }

    protected void TrySwitchRole()
    {
        bool hasClickInput = false;

        if(Input.GetMouseButtonDown(0))
        {
            float currentTime = Time.time;
            if(currentTime - lastTimeClicked < doubleClickTimeDelay)
            {
                hasClickInput = true;
            }
            lastTimeClicked = currentTime;
        }

        if(hasClickInput || Input.GetKeyDown(KeyCode.P))
        {
            CmdSwitchRole(role);
        }
    }

    #endregion

    #region HUD


    protected void onUpdateRightSlider(float value) {
        if(currentMode == Mode.Playing){
            rightGear = gearSystem.ClampGear(Mathf.FloorToInt(value));
        }
    }
    protected void onUpdateLeftSlider(float value) {
        if(currentMode == Mode.Playing){
            leftGear = gearSystem.ClampGear(Mathf.FloorToInt(value));
        }
    }

    void UpdateHUD(float deltaTime)
    {
        playerHUD.UpdateHUD(deltaTime);
    }

    void UpdateGunnerCooldown(float deltaTime)
    {
        fireCounter -= deltaTime;
        playerHUD.SetShootCooldown(fireCounter);
    }

    #endregion

}
