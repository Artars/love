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


    [Header("Control")]
    public float doubleClickTimeDelay = 0.2f;
    protected float lastTimeClicked = 0;


    [Header("Camera")]
    public Transform firstPersonCamera;

    [Header("Observe references")]
    public Transform observerTransform;
    public Transform observerPivot;
    public Transform observerCameraTransform;
    protected Vector3 pointToObserve;
    protected float rotateSpeed;

    [Header("HUD")]
    public GameObject canvasPilot;
    public GameObject canvasGunner;
    public GameObject hitPointer;
    public Slider rightSlider;
    public Slider leftSlider;
    public FixedJoystick joyStick;
    public bool buttonState = false;
    public GameObject[] mobileHUD;

    public GameObject canvasShared;
    public Slider healthSlider;
    public TMPro.TextMeshProUGUI scoreText;

    public GameObject informationCanvas;
    public TMPro.TextMeshProUGUI ipText;
    public TMPro.TextMeshProUGUI messageText;

    public GameObject compassTank;
    public GameObject compassCannon;

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

    //Other
    protected bool assignedCallback = false;
    protected Coroutine messageCoroutine = null;


    //Remove player from the game
    public override void OnNetworkDestroy () {
        if(isServer){
            if(tankRef != null)
            {
                tankRef.RemovePlayer(this, role);
            }
        }
    }

    protected void Start() {
        //Update referece
        if(isLocalPlayer){
            observerTransform.gameObject.SetActive(true);
            informationCanvas.SetActive(true);
        }

        if(GameMode.instance != null) {
            if(isServer)
            {
                GameMode.instance.TryToJoinAsSpectator(this);
            }
        }

        //Wil disable comands for HUD
        else {
            currentMode = Mode.Selecting;
        }
    }


    [ClientRpc]
    public void RpcObservePosition(Vector3 position, float speed, float angle, float distance) {
        if(isLocalPlayer){
            Debug.Log("Will observe " + position);
            currentMode = Mode.Observing;
            HideHUD();
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
        TryToAssignCallback();
    }

    [ClientRpc]
    public void RpcAssignPlayer(int team, Role role, NetworkIdentity toAssign){
        Debug.Log("Assigning player team " + team + " with role " + role.ToString());

        if(!isLocalPlayer) return;

        TryToAssignCallback();

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

        assignHUD();

    }

    [ClientRpc]
    public void RpcRemoveOwnership(){
        if(!isLocalPlayer) return;

        currentMode = Mode.Observing;

        //Reset control variables
        rightAxis = 0;
        leftAxis = 0;
        rightSlider.value = 0;
        leftSlider.value = 0;
        fireCounter = 0;

        //Remove tank references
        tankRef = null;
        possesedObject = null;


        HideHUD();
    }

    [ClientRpc]
    public void RpcDisplayMessage(string message, float duration, float fadeIn, float fadeOut){
        if(isLocalPlayer){
            if(messageCoroutine != null)
                StopCoroutine(messageCoroutine);
            messageCoroutine = StartCoroutine(showMessage(messageText, message, messageText.color, duration, fadeIn, fadeOut));
        }
    }

    [ClientRpc]
    public void RpcShowHostIp(string ip){
        if(isLocalPlayer)
        {
            ipText.gameObject.SetActive(true);
            ipText.text = ip;

        }
    }

    [ClientRpc]
    public void RpcReceiveDamageFromDirection(float damage, float angle)
    {
        if(isLocalPlayer)
        {
            Debug.Log("Received " + damage + " damage from direction " + angle);
            GameObject pointer = Instantiate(hitPointer, Vector3.zero, Quaternion.identity);
            pointer.transform.parent = canvasShared.transform;
            pointer.GetComponent<HitPointer>().hitAngle = angle;
            pointer.GetComponent<HitPointer>().cameraTransform = firstPersonCamera;
        }
    }

    public void SetTankReference(Tank tank, int team, Role role){
        tankRef = tank;
        this.team = team;
        this.role = role;
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
                observerTransform.position = pointToObserve;
                observerTransform.RotateAround(pointToObserve, observerTransform.up, rotateSpeed * Time.deltaTime);
            }
        }
        else if(currentMode == Mode.Playing) {
            UpdateHUD();
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
        if(!Input.GetButton("Jump")){
            rightAxis += deltaTime * axisSpeed * Input.GetAxisRaw("Vertical");
            leftAxis += deltaTime * axisSpeed * Input.GetAxisRaw("Vertical2");
        }
        else {
            rightAxis = 0;
            leftAxis = 0;
        }
        rightAxis = Mathf.Clamp(rightAxis,-1,1);
        leftAxis = Mathf.Clamp(leftAxis,-1,1);

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

        bool isPressing = Input.GetButton("Jump") || buttonState;

        if(fireCounter <= 0 && isPressing){
            Debug.Log("Tried to shoot from " + tankRef.bulletSpawnPosition.position);
            CmdShootCannon();
            fireCounter = tankRef.shootCooldown;
        }

        float horizontalAxis = Input.GetAxis("Horizontal");
        float verticalAxis = Input.GetAxis("Vertical");

        if(joyStick != null && joyStick.gameObject.activeInHierarchy){
            horizontalAxis = joyStick.Horizontal;
            verticalAxis = joyStick.Vertical;
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

    protected void assignHUD() {
        canvasShared.SetActive(true);
        healthSlider.gameObject.SetActive(true);
        tankRef.SetHealthSlider(healthSlider);
        TryToAssignCallback();

        ipText.gameObject.SetActive(false);

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
        else if(role == Role.Gunner){
            canvasGunner.SetActive(true);
            buttonState = false;
            canvasPilot.SetActive(false);
        }

        //Define a bússola
        if(role == Role.Gunner)
        {
            compassTank.GetComponent<Image>().color = Color.white;
            compassCannon.GetComponent<Image>().color = Color.red;
        }else
        {
            compassTank.GetComponent<Image>().color = Color.red;
            compassCannon.GetComponent<Image>().color = Color.white;
        }

        //Set mobile HUD
        bool isMobile = false;
        #if UNITY_ANDROID
            isMobile = true;
        #endif

        for(int i = 0 ; i < mobileHUD.Length; i++)
        {
            mobileHUD[i].SetActive(isMobile);
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
        if(currentMode == Mode.Playing){
            rightAxis = Mathf.Clamp(-1,value,1);
            if(tankRef != null)
                tankRef.setAxis(leftAxis, rightAxis);
        }
    }
    protected void onUpdateLeftSlider(float value) {
        if(currentMode == Mode.Playing){
            leftAxis = Mathf.Clamp(-1,value,1);
            if(tankRef != null)
                tankRef.setAxis(leftAxis, rightAxis);
        }
    }

    void UpdateHUD()
    {
        if(tankRef != null)
        {
            compassTank.transform.eulerAngles = new Vector3(0, 0, -tankRef.tankTransform.eulerAngles.y - 180);
            compassCannon.transform.eulerAngles = new Vector3(0, 0, -tankRef.cannonTransform.eulerAngles.y);
        }
    }

    public void TryToAssignCallback()
    {
        if(!assignedCallback)
        {
            if(GameStatus.instance != null)
            {
                assignedCallback = true;
                GameStatus.instance.score.Callback += ScoreCallBack;
            }
        }
        if(assignedCallback)
        {
            ScoreCallBack(SyncListInt.Operation.OP_DIRTY,0,0);
        }
    }

    public void ScoreCallBack(SyncListInt.Operation operation, int index, int item) {
        Debug.Log("Callbacked");
        int useTeam = (team != -1) ? team : 0;
        if(scoreText != null && useTeam != -1){
            SyncListInt syncList = GameStatus.instance.score;

            if(syncList == null || syncList.Count < 1) return;

            string newText = syncList[useTeam].ToString();
            for(int i = 0; i < syncList.Count; i++) {
                if(i != useTeam){
                    newText += " x " + syncList[i]; 
                }
            }

            scoreText.text = newText;
        }
    }


    protected IEnumerator showMessage(TMPro.TextMeshProUGUI textRef, string message, Color colorToUse, float duration, float fadeIn, float fadeOut){
        textRef.text = message;
        Color colorToChange = colorToUse;
        float counter = 0;
        
        
        //Do fade in
        if(fadeIn != 0) {
            colorToChange.a = 0;
            textRef.color = colorToChange;

            while(counter < fadeIn){
                counter += Time.deltaTime;
                
                colorToChange.a = counter/fadeIn;
                textRef.color = colorToChange;

                yield return null;        
            }
        }

        colorToChange.a = 1;
        textRef.color = colorToChange;

        //Wait duration of message
        counter = 0;
        while(counter < duration){
            counter += Time.deltaTime;

            yield return null;
        }

        //Do fade out
        counter = 0;
        if(fadeOut != 0) {
            while(counter < fadeOut){
                counter += Time.deltaTime;

                colorToChange.a = (fadeOut - counter)/fadeOut;
                textRef.color = colorToChange;

                yield return null;
            }
        }

        colorToChange.a = 0;
        textRef.color = colorToChange;

        messageCoroutine = null;

    }

    public void OnShootButtonDown(){
        buttonState = true;
    }

    public void OnShootButtonUp(){
        buttonState = false;
    }



}
