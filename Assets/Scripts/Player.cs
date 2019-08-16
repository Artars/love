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

    public Slider healthSlider;
    public Slider loadingSlider;
    public TMPro.TextMeshProUGUI scoreText;
    public TMPro.TextMeshProUGUI timeText;

    public GameObject informationCanvas;
    public TMPro.TextMeshProUGUI ipText;
    public TMPro.TextMeshProUGUI messageText;

    public GameObject compassTank;
    public GameObject compassCannon;

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
            localPlayer = this;

            //Create inputs
            rightGearInput = new AxisToButton("Vertical");
            leftGearInput = new AxisToButton("Vertical2");

            //Set time reference
            GameStatus.instance.SetTimeText(timeText);
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

        //Update control values
        //Update axis
        rightGear = tankRef.rightGear;
        leftGear = tankRef.leftGear;

        //Update gears
        gearSystem = tankRef.gearSystem;
        leftSlider.minValue = gearSystem.lowestGear;
        leftSlider.maxValue = gearSystem.highestGear;
        rightSlider.minValue = gearSystem.lowestGear;
        rightSlider.maxValue = gearSystem.highestGear;

        //Update gunner
        loadingSlider.minValue = 0;
        loadingSlider.maxValue = tankRef.ShootCooldown;


        assignHUD();

    }

    [ClientRpc]
    public void RpcRemoveOwnership(){
        if(!isLocalPlayer) return;

        currentMode = Mode.Observing;

        //Reset control variables
        rightGear = 0;
        leftGear = 0;
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
            pointer.transform.parent = informationCanvas.transform;
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

        if(rightSlider != null) rightSlider.value = rightGear;
        if(leftSlider != null) leftSlider.value = leftGear;

        if(old_leftGear != leftGear || old_rightGear != rightGear) {
            old_leftGear = leftGear;
            old_rightGear = rightGear;
            CmdUpdateGearPilot(leftGear, rightGear);
        }
    }

    protected void gunnerUpdate(float deltaTime) {
        UpdateGunnerCooldown(deltaTime);

        bool isPressing = Input.GetButton("Fire") || buttonState;

        if(fireCounter <= 0 && isPressing){
            Debug.Log("Tried to shoot from " + tankRef.bulletSpawnPosition.position);
            CmdShootCannon();
            fireCounter = tankRef.ShootCooldown;
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
        healthSlider.gameObject.SetActive(true);
        tankRef.SetHealthSlider(healthSlider);
        TryToAssignCallback();

        ipText.gameObject.SetActive(false);

        if(role == Role.Pilot){
            canvasGunner.SetActive(false);
            canvasPilot.SetActive(true);

            rightSlider.gameObject.SetActive(true);
            rightSlider.onValueChanged.AddListener(onUpdateRightSlider);
            rightSlider.value = rightGear;
            leftSlider.gameObject.SetActive(true);
            leftSlider.onValueChanged.AddListener(onUpdateLeftSlider);
            leftSlider.value = leftGear;

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
            rightGear = gearSystem.ClampGear(Mathf.FloorToInt(value));
        }
    }
    protected void onUpdateLeftSlider(float value) {
        if(currentMode == Mode.Playing){
            leftGear = gearSystem.ClampGear(Mathf.FloorToInt(value));
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

    void UpdateGunnerCooldown(float deltaTime)
    {
        fireCounter -= deltaTime;
        loadingSlider.value = loadingSlider.maxValue - fireCounter;
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
            scoreText.text = GameStatus.instance.GetCurrentScore(useTeam);

            // SyncListInt syncList = GameStatus.instance.score;

            // if(syncList == null || syncList.Count < 1) return;

            // string newText = syncList[useTeam].ToString();
            // for(int i = 0; i < syncList.Count; i++) {
            //     if(i != useTeam){
            //         newText += " x " + syncList[i]; 
            //     }
            // }

            // scoreText.text = newText;
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

    public void OnClickExit()
    {
        if(isServer)
            NetworkManager.singleton.StopHost();
        else
            NetworkManager.singleton.StopClient();
    }



}


public class AxisToButton
{
    public string input;
    public float threshold = 0.1f;
    public int currentValue = 0;

    public AxisToButton(string inputString, float threshold = 0.2f)
    {
        input = inputString;
        this.threshold = threshold;
    }

    public bool GetButtonDown()
    {
        int state = GetNewState();

        if(state != currentValue)
        {
            currentValue = state;
            if(state != 0)
                return true;
        }
        return false;
    }

    public bool GetButtonUp()
    {
        int state = GetNewState();
        

        if(state != currentValue)
        {
            currentValue = state;
            if(state == 0)
                return true;
        }
        return false;
    }

    public bool GetState()
    {
        int state = GetNewState();

        if(state != currentValue)
        {
            currentValue = state;
        }
        if(state != 0)
            return true;
        else
            return false;
    }

    protected int GetNewState()
    {
        float currentInputValue = Input.GetAxisRaw(input);
        int state = currentInputValue > threshold ? 1 : 0;
        state = currentInputValue < -threshold ? -1 : state;
        
        return state;
    }

}