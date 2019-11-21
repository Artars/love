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
    protected Mode currentMode = Mode.Selecting;
    public Tank tankRef;
    public Cannon cannonRef;
    [SyncVar]
    public int team = -1;
    [SyncVar]
    public Role role = Role.Pilot;
    [SyncVar]
    public NetworkIdentity possesedObject;
    [SyncVar]
    protected bool canSwitchRoles = false;
    [SyncVar]
    public bool isAI = false;


    [Header("Camera")]
    public Transform firstPersonCamera;

    [Header("Observe references")]
    public Transform observerTransform;
    public Transform observerPivot;
    public Transform observerCameraTransform;
    protected Vector3 pointToObserve;
    protected float rotateSpeed;

    protected IPlayerControler playerController;

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
        playerController = GetComponent<IPlayerControler>();

        //Update referece
        if(isLocalPlayer){
            observerTransform.gameObject.SetActive(true);
            localPlayer = this;
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

    [Server]
    public void AssignSpectator()
    {
        SetCurrentMode(Mode.Spectator);
        RpcAssignSpectator();
    }

    [Server]
    public void ObservePosition(Vector3 position, float speed, float angle, float distance)
    {
        SetCurrentMode(Mode.Observing);
        RpcObservePosition(position, speed, angle, distance);
    }

    [Server]
    public void AssignPlayer(int team, Role role, NetworkIdentity toAssign)
    {
        SetCurrentMode(Mode.Playing);
        this.team = team;
        this.role = role;
        this.tankRef = toAssign.GetComponent<Tank>();
        if(playerController != null) playerController.AssignTank(team,role,tankRef);
        RpcAssignPlayer(team, role, toAssign);
    }

    [Server]
    public void RemoveOwnership()
    {
        SetCurrentMode(Mode.Observing);
        RpcRemoveOwnership();
    }

    [Server]
    public void SetCurrentMode(Mode newMode)
    {
        currentMode = newMode;
        if(playerController != null)
        {   
            playerController.CurrentMode = newMode;
        }
    }

    [Server]
    public void SetCanSwitchRoles(bool canSwitchRoles)
    {
        this.canSwitchRoles = canSwitchRoles;
        if(playerController != null)
        {
            playerController.CanSwitchRoles = canSwitchRoles;
        }
    }

    public bool GetCanSwitchRoles()
    {
        return canSwitchRoles;
    }


    #endregion

    #region Networking

    [ClientRpc]
    protected void RpcObservePosition(Vector3 position, float speed, float angle, float distance) {
        if(isLocalPlayer){
            Debug.Log("Will observe " + position);
            currentMode = Mode.Observing;
            if(playerController != null)
            {
                playerController.HideHUD();
            }
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
    protected void RpcAssignSpectator()
    {
        if(playerController != null)
        {
            playerController.TryToAssignCallback();
        }
        if(isLocalPlayer)
        {
            RenderSettings.fog = false;
        }
    }

    [ClientRpc]
    protected void RpcAssignPlayer(int team, Role role, NetworkIdentity toAssign){
        Debug.Log("Assigning player team " + team + " with role " + role.ToString());

        tankRef = toAssign.GetComponent<Tank>();
        if(playerController != null)
            playerController.AssignTank(team, role, tankRef);

        if(!isLocalPlayer) return;


        observerTransform.gameObject.SetActive(false);
        firstPersonCamera.gameObject.SetActive(true);

        this.team = team;
        this.role = role;
        this.possesedObject = toAssign;
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
    }

    [ClientRpc]
    protected void RpcRemoveOwnership(){
        // currentMode = Mode.Observing;
        if(playerController != null)
        {
            playerController.RemoveOwnership();
        }
    }

    [ClientRpc]
    public void RpcDisplayMessage(string message, float duration, Color color, float fadeIn, float fadeOut){
        if(isLocalPlayer){
            if(playerController != null)
            {
                playerController.AddMessage(new PlayerMessage(message, duration, fadeIn, fadeOut, color));
            }
        }
    }


    [ClientRpc]
    public void RpcPlayVictoryMusic()
    {
        if(isLocalPlayer)
        {
            if(playerController != null)
            {
                playerController.PlayVictoryMusic();
            }
        }
    }

    [ClientRpc]
    public void RpcReceiveDamageFromDirection(float damage, float angle)
    {
        if(isLocalPlayer)
        {
            if(playerController != null)
            {
                playerController.ReceiveDamageFromDirection(damage,angle,firstPersonCamera);
            }
        }
    }

    [ClientRpc]
    public void RpcForcePilotStop()
    {
        if(isLocalPlayer)
        {
            if(playerController != null)
            {
                playerController.ForcePilotStop();
            }
        }
    }

    [ClientRpc]
    public void RpcShowHitmark(Vector3 position)
    {
        if(isLocalPlayer)
        {
            if(playerController != null)
            {
                playerController.ShowHitmark(position);

            }
        }
    }

    #endregion


    protected void Update() {
        if(!isLocalPlayer) return;
        
        if(currentMode == Mode.Observing) {
            if(pointToObserve != null) {
                observerTransform.position = pointToObserve;
                observerTransform.RotateAround(pointToObserve, observerTransform.up, rotateSpeed * Time.deltaTime);
            }
        }
    }


}
