using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class Player : NetworkBehaviour
{

    public enum Mode {
        Selecting, Observing, Playing
    }

    public enum Role
    {
        Pilot, Gunner
    }


    public Mode currentMode = Mode.Observing;
    public Tank tankRef;
    public Cannon cannonRef;
    [SyncVar]
    public int team = -1;
    [SyncVar]
    public Role role = Role.Pilot;
    [SyncVar]
    public NetworkIdentity possesedObject;


    [Header("Camera")]
    public Transform observerPivot;
    protected Vector3 pointToObserve;
    protected float rotateSpeed;
    public Transform firstPersonCamera;

    [Header("HUD")]
    public GameObject canvasPilot;
    public Slider rightSlider;
    public Slider leftSlider;
    
    public GameObject canvasGunner;

    [Header("Pilot")]
    public float axisSpeed = 2;
    protected float rightAxis;
    protected float leftAxis;

    protected float fireCounter = 0;



    


    protected void Start() {
        //Update referece
        GameMode.instance.setPlayerReference(this);
        

        if(!isLocalPlayer){
            firstPersonCamera.gameObject.SetActive(false);
            observerPivot.gameObject.SetActive(false);
        }
        if(isLocalPlayer){
            // Debug.Log("Is player " + NetworkManager.singleton.);
        }
        //Debug
        // possesTank(tankRef);
    }

    [ClientRpc]
    public void RpcObservePosition(Vector3 position, float speed) {
        Debug.Log("Called");
        if(isLocalPlayer){
            Debug.Log("Will observe " + position);
            currentMode = Mode.Observing;
            firstPersonCamera.gameObject.SetActive(false);
            observerPivot.gameObject.SetActive(true);
            pointToObserve = position;
            rotateSpeed = speed;
        }
    }

    [ClientRpc]
    public void RpcAssignPlayer(int team, Role role, NetworkIdentity toAssign){
        Debug.Log("Assigning player team " + team + " with role " + role.ToString());

        if(!isLocalPlayer) return;
        

        observerPivot.gameObject.SetActive(false);
        firstPersonCamera.gameObject.SetActive(true);

        this.team = team;
        this.role = role;
        this.possesedObject = toAssign;

        if(role == Role.Pilot){
            tankRef = possesedObject.GetComponent<Tank>();

            firstPersonCamera.position = tankRef.cameraPositionDriver.position;
            firstPersonCamera.rotation = tankRef.cameraPositionDriver.rotation;
            firstPersonCamera.SetParent(tankRef.cameraPositionDriver);
        }
        else if(role == Role.Gunner){
            cannonRef = possesedObject.GetComponent<Cannon>();

            firstPersonCamera.position = cannonRef.gunnerCameraTransform.position;
            firstPersonCamera.rotation = cannonRef.gunnerCameraTransform.rotation;
            firstPersonCamera.SetParent(cannonRef.gunnerCameraTransform);

        }

        assignHUD();

    }

    


    protected void Update() {
        if(!isLocalPlayer) return;
        
        if(currentMode == Mode.Observing) {
            if(pointToObserve != null) {
                observerPivot.position = pointToObserve;
                observerPivot.RotateAround(pointToObserve, observerPivot.up, rotateSpeed * Time.deltaTime);
            }
        }
        else if(currentMode == Mode.Playing) {
            if(role == Role.Pilot){
                pilotUpdate(Time.deltaTime);
            }
            else if(role == Role.Gunner){
                gunnerUpdate(Time.deltaTime);
            }
        }
    }

    protected void pilotUpdate(float deltaTime){
        if(!Input.GetButton("Jump")){
            rightAxis += deltaTime * axisSpeed * Input.GetAxis("Vertical");
            leftAxis += deltaTime * axisSpeed * Input.GetAxis("Vertical2");
        }
        else {
            rightAxis = 0;
            leftAxis = 0;
        }
        rightAxis = Mathf.Clamp(-1,rightAxis,1);
        leftAxis = Mathf.Clamp(-1,leftAxis,1);

        if(rightSlider != null) rightSlider.value = rightAxis;
        if(leftSlider != null) leftSlider.value = leftAxis;

        if(tankRef != null){
            tankRef.setAxis(leftAxis,rightAxis);
        }
    }

    protected void gunnerUpdate(float deltaTime) {
        fireCounter -= Time.deltaTime;

        if(fireCounter < 0 && Input.GetButton("Jump")){
            cannonRef.CmdShootCannon(0);
            fireCounter = cannonRef.fireWaitTime;
        }

        float horizontalAxis = Input.GetAxis("Horizontal");
        float verticalAxis = Input.GetAxis("Vertical");

        if(cannonRef != null) {
            cannonRef.setInputAxis(horizontalAxis, verticalAxis);
        }
    }

    protected void assignHUD() {
        if(role == Role.Pilot){
            canvasGunner.SetActive(false);
            canvasPilot.SetActive(true);

            rightSlider.onValueChanged.AddListener(onUpdateRightSlider);
            rightSlider.value = rightAxis;
            rightSlider.gameObject.SetActive(true);
            leftSlider.onValueChanged.AddListener(onUpdateLeftSlider);
            leftSlider.value = leftAxis;
            leftSlider.gameObject.SetActive(true);

        }
    }

    protected void onUpdateRightSlider(float value) {
        rightAxis = Mathf.Clamp(-1,value,1);
        if(tankRef != null)
            tankRef.setAxis(leftAxis, rightAxis);
    }
    protected void onUpdateLeftSlider(float value) {
        leftAxis = Mathf.Clamp(-1,value,1);
        if(tankRef != null)
            tankRef.setAxis(leftAxis, rightAxis);
    }



}
