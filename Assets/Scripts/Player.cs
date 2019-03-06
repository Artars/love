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

    public GameObject canvasShared;
    public Slider healthSlider;
    public TMPro.TextMeshProUGUI scoreText;

    [Header("Pilot")]
    public float axisSpeed = 2;
    protected float rightAxis;
    protected float leftAxis;
    protected float old_rightAxis = -2; //Force first sync
    protected float old_leftAxis = -2;

    
    //Gunner propeties
    protected float old_horizontal = -2;
    protected float old_vertical = -2;
    protected float fireCounter = 0;



    


    protected void Start() {
        //Update referece
        GameMode.instance.setPlayerReference(this);
        

        if(!isLocalPlayer){
            firstPersonCamera.gameObject.SetActive(false);
            observerPivot.gameObject.SetActive(false);
        }
        if(isLocalPlayer){
            observerPivot.gameObject.SetActive(true);
        }
    }

    [ClientRpc]
    public void RpcObservePosition(Vector3 position, float speed) {
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

        assignHUD();

    }

    [ClientRpc]
    public void RpcRemoveOwnership(){
        if(!isLocalPlayer) return;

        currentMode = Mode.Observing;

        if(role == Role.Pilot)
        {
            rightAxis = 0;
            leftAxis = 0;
            rightSlider.value = 0;
            leftSlider.value = 0;
        }

        HideHUD();
    }

    public void SetTankReference(Tank tank, int team, Role role){
        tankRef = tank;
        this.team = team;
        this.role = role;
    }

    //Input update on server
    [Command]
    public void CmdUpdateAxisPilot(float leftAxis, float rightAxis){
        if(role == Role.Pilot){
            if(tankRef != null) {
                tankRef.setAxis(leftAxis, rightAxis);
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

        if(old_leftAxis != leftAxis || old_rightAxis != rightAxis) {
            old_leftAxis = leftAxis;
            old_rightAxis = rightAxis;
            CmdUpdateAxisPilot(leftAxis, rightAxis);
        }
    }

    protected void gunnerUpdate(float deltaTime) {
        fireCounter -= deltaTime;

        if(fireCounter <= 0 && Input.GetButton("Jump")){
            Debug.Log("Tried to shoot from " + tankRef.bulletSpawnPosition.position);
            CmdShootCannon();
            fireCounter = tankRef.timeToShoot;
        }

        float horizontalAxis = Input.GetAxis("Horizontal");
        float verticalAxis = Input.GetAxis("Vertical");

        if(old_horizontal != horizontalAxis || old_vertical != verticalAxis){
            old_horizontal = horizontalAxis;
            old_vertical = verticalAxis;
            CmdUpdateAxisGunner(horizontalAxis, verticalAxis);
        }

    }

    protected void assignHUD() {
        canvasShared.SetActive(true);
        healthSlider.gameObject.SetActive(true);
        tankRef.SetHealthSlider(healthSlider);
        ScoreCallBack(SyncListInt.Operation.OP_DIRTY,0,0);

        if(role == Role.Pilot){
            canvasGunner.SetActive(false);
            canvasPilot.SetActive(true);

            rightSlider.gameObject.SetActive(true);
            rightSlider.onValueChanged.AddListener(onUpdateRightSlider);
            rightSlider.value = rightAxis;
            leftSlider.gameObject.SetActive(true);
            leftSlider.onValueChanged.AddListener(onUpdateLeftSlider);
            leftSlider.value = leftAxis;

        }
    }

    protected void HideHUD(){
        healthSlider.gameObject.SetActive(false);

        if(role == Role.Pilot){
            rightSlider.onValueChanged.RemoveListener(onUpdateRightSlider);
            leftSlider.onValueChanged.RemoveListener(onUpdateLeftSlider);
        }
        canvasGunner.SetActive(false);
        canvasPilot.SetActive(false);
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


    public void ScoreCallBack(SyncListInt.Operation operation, int index, int item) {
        Debug.Log("Callbacked");
        if(scoreText != null && team != -1){
            SyncListInt syncList = GameMode.instance.score;

            if(syncList == null || syncList.Count < 1) return;
            
            string newText = syncList[team].ToString();
            for(int i = 0; i < syncList.Count; i++) {
                if(i != team){
                    newText += " x " + syncList[i]; 
                }
            }

            scoreText.text = newText;
        }
    }



}
