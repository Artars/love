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


    public Mode currentMode = Mode.Playing;
    public  Tank tankRef;
    public Cannon cannonRef;
    public Role role = Role.Pilot;


    [Header("Camera")]
    public Transform observerPivot;
    protected Transform pointToObserve;
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


    protected void Update() {
        if(isLocalPlayer) {
            if(currentMode == Mode.Playing) {
                if(role == Role.Pilot){
                    pilotUpdate(Time.deltaTime);
                }
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
            tankRef.setAxis(leftAxis, rightAxis);

    }

    public void possesTank(Tank tank) {
        if(!isServer) return;
        if(tank == null)
            return;
        //Posses network logic
        tankRef = tank;
        NetworkedObject tankNO = tank.GetComponent<NetworkedObject>();
        if(tankNO != null) {
            tankNO.ChangeOwnership(OwnerClientId);
        }

        InvokeClientRpcOnClient("assignTankRPC", OwnerClientId);

    }

    [ClientRPC]
    protected void assignTankRPC(int param){
        observerPivot.gameObject.SetActive(false);
        firstPersonCamera.gameObject.SetActive(true);
        if(role == Role.Pilot){
            firstPersonCamera.position = tankRef.cameraPositionDriver.position;
            firstPersonCamera.rotation = tankRef.cameraPositionDriver.rotation;
            firstPersonCamera.SetParent(tankRef.cameraPositionDriver);
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
