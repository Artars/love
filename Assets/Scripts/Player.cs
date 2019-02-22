using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MLAPI;

public class Player : NetworkedBehaviour
{

    public enum Mode {
        Selecting, Observing, Playing
    }

    public enum Role
    {
        Pilot, Gunner
    }


    public Mode currentMode = Mode.Observing;
    public  Tank tankRef;
    public Cannon cannonRef;
    public Role role = Role.Pilot;


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



    


    protected void Start() {
        if(!isLocalPlayer){
            firstPersonCamera.gameObject.SetActive(false);
            observerPivot.gameObject.SetActive(false);
        }
        if(isLocalPlayer){
            Debug.Log("Is player " + networkId);
        }
        //Debug
        // possesTank(tankRef);
    }

    [ClientRPC]
    public void observePositionRPC(float x, float y, float z, float speed) {
        Debug.Log("Called");
        if(isLocalPlayer){
            Debug.Log("Will observe " +x+ " "+y+ " "+z+ " ");
            currentMode = Mode.Observing;
            firstPersonCamera.gameObject.SetActive(false);
            observerPivot.gameObject.SetActive(true);
            pointToObserve = new Vector3(x,y,z);
            rotateSpeed = speed;
        }
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

        if(tankRef != null)
            tankRef.InvokeServerRpc("setTankInputRPC", leftAxis, rightAxis);

    }

    protected void gunnerUpdate(float deltaTime) {
        float horizontalAxis = Input.GetAxis("Horizontal");
        float verticalAxis = Input.GetAxis("Vertical");

        if(tankRef != null) {
            tankRef.InvokeServerRpc("setCannonInputRPC", horizontalAxis, verticalAxis);
        }
    }

    public void possesTank(Tank tank, Role role) {
        if(!isServer) return;
        if(tank == null)
            return;

        //Posses network logic

        InvokeClientRpcOnEveryone("assignTankRPC", tank.team.Value, (int) role);

    }

    [ClientRPC]
    protected void assignTankRPC(int team, int roleToAssing){

        tankRef = GameMode.instance.getTank(team);
        if(tankRef == null) {
            Debug.Log("Player found null tank");
            return;
        }
        cannonRef = tankRef.cannon;

        if(!isLocalPlayer) return;

        observerPivot.gameObject.SetActive(false);
        firstPersonCamera.gameObject.SetActive(true);

        this.role = (Role) roleToAssing;
        if(role == Role.Pilot){
            firstPersonCamera.position = tankRef.cameraPositionDriver.position;
            firstPersonCamera.rotation = tankRef.cameraPositionDriver.rotation;
            firstPersonCamera.SetParent(tankRef.cameraPositionDriver);
        }
        else if(role == Role.Gunner){
            firstPersonCamera.position = cannonRef.gunnerCameraTransform.position;
            firstPersonCamera.rotation = cannonRef.gunnerCameraTransform.rotation;
            firstPersonCamera.SetParent(tankRef.cameraPositionGunner);

        }

        assignHUD();
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
