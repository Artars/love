using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class PlayerController : NetworkBehaviour, IPlayerControler
{

    #region Variables

    [Header("Control")]
    [SyncVar]
    protected Player.Mode currentMode = Player.Mode.Selecting;
    [SyncVar]
    public bool canSwitchRoles = false;

    [Header("References")]
    public AudioSource musicAudioSource;
    public GameObject hitmarkPrefab;


    [Header("Control")]
    public float doubleClickTimeDelay = 0.2f;
    protected float lastTimeClicked = 0;

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
    public TMPro.TextMeshProUGUI messageText;

    [Header("Compass")]
    public GameObject compassTank;
    public GameObject compassCannon;
    public GameObject goalCompassPrefab;
    public Transform goalCompassParent;

    protected List<Image> goalCompassReference;
    protected List<GoalPoint> goalReferences;
    protected Tank tankRef;
    private bool assignedCallback;

    //Player information

    private int team;
    private Role role;

    //Message system
    public TMPro.TextMeshProUGUI defaultMessageText
    {
        get
        {
            return messageText;
        }
    }
    public Color defaultMessageColor
    {
        get
        {
            return Color.yellow;
        }
    }

    public Player.Mode CurrentMode { 
        get {
            return currentMode;
        } 
        set {
            currentMode = value;
        } 
    }

    public bool CanSwitchRoles { 
        get {
            return canSwitchRoles;            
        } 
        set {
            canSwitchRoles = value;
        } 
    }

    protected Queue<PlayerMessage> messageQueue = new Queue<PlayerMessage>();
    protected Coroutine messageCoroutine;

    #endregion

    public void Start()
    {
        if(isLocalPlayer){
            informationCanvas.SetActive(true);
            GameStatus.instance.SetTimeText(timeText);
            //Set player HUD
            AddPilotCallback(onUpdateLeftSlider,onUpdateRightSlider);

            //Create inputs
            rightGearInput = new AxisToButton("Vertical");
            leftGearInput = new AxisToButton("Vertical2");
        }
        //Update referece
        if(isLocalPlayer){

            //Set time reference
        }
    }

    protected void Update() {
        if(!isLocalPlayer) return;
        

        if(currentMode == Player.Mode.Playing) {
            UpdateHUD(Time.deltaTime);
            UpdateGunnerCooldown(Time.deltaTime);

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

    public void AssignTank(int team, Role role, Tank tank)
    {
        tankRef = tank;
        this.role = role;
        this.team = team;
        if(!isLocalPlayer) return;

        //Update gears
        gearSystem = tankRef.gearSystem;
        leftSlider.minValue = gearSystem.lowestGear;
        leftSlider.maxValue = gearSystem.highestGear;
        rightSlider.minValue = gearSystem.lowestGear;
        rightSlider.maxValue = gearSystem.highestGear;

        //Update axis
        rightGear = tankRef.rightGear;
        leftGear = tankRef.leftGear;
        SetPilotValue(leftGear, rightGear);

        //Update gunner
        loadingSlider.minValue = 0;
        loadingSlider.maxValue = tankRef.ShootCooldown;


        // Previous assign HUD
        healthSlider.gameObject.SetActive(true);
        tankRef.SetHealthSlider(healthSlider);
        TryToAssignCallback();

        if(role == Role.Pilot){
            canvasGunner.SetActive(false);
            canvasPilot.SetActive(true);

            rightSlider.gameObject.SetActive(true);
            leftSlider.gameObject.SetActive(true);
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

    #region Control

    #region Commands

    [Command]
    public void CmdSwitchRole(Role currentRole)
    {
        if(tankRef != null)
        {
            tankRef.SwitchPlayerRole(GetComponent<Player>(), currentRole);
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

    #region ResponseToRPC

    public void RemoveOwnership()
    {
        if(!isLocalPlayer) return;
        //Reset control variables
        rightGear = 0;
        leftGear = 0;
        fireCounter = 0;

        //Remove tank references
        tankRef = null;

        ResetInput();

        HideHUD();
    }

    public void PlayVictoryMusic()
    {
        musicAudioSource.gameObject.SetActive(true);
        musicAudioSource.Play();
    }

    public void ReceiveDamageFromDirection(float damage, float angle, Transform firstPersonCamera)
    {
        Debug.Log("Received " + damage + " damage from direction " + angle);
        CreateHitpoint(angle,firstPersonCamera);
    }

    public void ForcePilotStop()
    {
        if(isLocalPlayer)
        {
            leftGear = old_leftGear = 0;
            rightGear = old_rightGear = 0;

            SetPilotValue(leftGear,rightGear);
        }
    }

    public void ShowHitmark(Vector3 position)
    {
        if(isLocalPlayer)
        {
            GameObject.Instantiate(hitmarkPrefab, position, Quaternion.identity);
        }
    }

    #endregion

    void UpdateGunnerCooldown(float deltaTime)
    {
        fireCounter -= deltaTime;
        SetShootCooldown(fireCounter);
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

        SetPilotValue(leftGear,rightGear);

        if(old_leftGear != leftGear || old_rightGear != rightGear) {
            old_leftGear = leftGear;
            old_rightGear = rightGear;
            CmdUpdateGearPilot(leftGear, rightGear);
        }
    }

    protected void gunnerUpdate(float deltaTime) {
        bool isPressing = Input.GetButton("Fire") || buttonState;

        if(fireCounter <= 0 && isPressing){
            Debug.Log("Tried to shoot from " + tankRef.bulletSpawnPosition.position);
            CmdShootCannon();
            fireCounter = tankRef.ShootCooldown;
        }

        float horizontalAxis = Input.GetAxis("Horizontal");
        float verticalAxis = Input.GetAxis("Vertical");

        if(IsJoystickValid()){
            Vector2 joyStickInput = GetJoystickInput();
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

    protected void onUpdateRightSlider(float value) {
        if(currentMode == Player.Mode.Playing){
            rightGear = gearSystem.ClampGear(Mathf.FloorToInt(value));
        }
    }
    protected void onUpdateLeftSlider(float value) {
        if(currentMode == Player.Mode.Playing){
            leftGear = gearSystem.ClampGear(Mathf.FloorToInt(value));
        }
    }

    #endregion

    #region HUD

    public void HideHUD()
    {
        healthSlider.gameObject.SetActive(false);

        // if(role == Role.Pilot){
        //     rightSlider.onValueChanged.RemoveListener(onUpdateRightSlider);
        //     leftSlider.onValueChanged.RemoveListener(onUpdateLeftSlider);
        // }
        canvasGunner.SetActive(false);
        canvasPilot.SetActive(false);
    }

    public void UpdateHUD(float deltaTime)
    {
        //Compass update
        if(tankRef != null)
        {
            compassTank.transform.eulerAngles = new Vector3(0, 0, -tankRef.tankTransform.eulerAngles.y - 180);
            compassCannon.transform.eulerAngles = new Vector3(0, 0, -tankRef.rotationPivot.eulerAngles.y);
            
            //Update goal compass
            if(goalCompassReference.Count > 0)
            {
                UpdateGoalCompassHUD(deltaTime);
            }
        }
    }

    public void CreateHitpoint(float angle, Transform cameraTransform)
    {
        GameObject pointer = Instantiate(hitPointer, Vector3.zero, Quaternion.identity);
        pointer.transform.parent = informationCanvas.transform;
        pointer.GetComponent<HitPointer>().hitAngle = angle;
        pointer.GetComponent<HitPointer>().cameraTransform = cameraTransform;
    }

    public void OnClickExit()
    {
        // NetworkDiscovery.instance.StopDiscovery();
        // NetworkManager.Shutdown();
        if(isServer)
        {
            NetworkManager.singleton.StopHost();

        }
        else
        {
            NetworkManager.singleton.StopClient();
        }
    }

    #region Pilot

    public void SetPilotValue(float left, float right)
    {
        rightSlider.value = right;
        leftSlider.value = left;
    }

    public Vector2 GetPilotValue()
    {
        return new Vector2(leftSlider.value,rightSlider.value);
    }

    public void ResetInput()
    {
        //Reset player sliders
        rightSlider.value = 0;
        leftSlider.value = 0;

        //Reset Shooter sliders
        loadingSlider.value = 0;
    }


    public void AddPilotCallback(UnityEngine.Events.UnityAction<float> leftUpdate, UnityEngine.Events.UnityAction<float> rightUpdate)
    {
        rightSlider.onValueChanged.AddListener(rightUpdate);
        leftSlider.onValueChanged.AddListener(leftUpdate);
    }

    #endregion

    #region Gunner

    public void SetShootCooldown(float fireCounter)
    {
        loadingSlider.value = loadingSlider.maxValue - fireCounter;
    }

    public bool IsJoystickValid()
    {
        return joyStick != null && joyStick.gameObject.activeInHierarchy;
    }

    public Vector2 GetJoystickInput()
    {
        if(IsJoystickValid())
        {
            return new Vector2(joyStick.Horizontal,joyStick.Vertical);
        }
        else
        {
            return Vector2.zero;

        }
    }

    public void OnShootButtonDown(){
        buttonState = true;
    }

    public void OnShootButtonUp(){
        buttonState = false;
    }

    #endregion

    #region Score

    public void TryToAssignCallback()
    {
        if(!assignedCallback)
        {
            if(GameStatus.instance != null)
            {
                assignedCallback = true;
                GameStatus.instance.score.Callback += ScoreCallBack;
                InitializeGoalCompass();
            }
        }
        if(assignedCallback)
        {
            ScoreCallBack(SyncListInt.Operation.OP_DIRTY,0,0);
        }
    }

    public void ScoreCallBack(SyncListInt.Operation operation, int index, int item) {
        int useTeam = (team != -1) ? team : 0;
        if(scoreText != null && useTeam != -1){
            scoreText.text = GameStatus.instance.GetCurrentScore(useTeam);
        }
    }

    #endregion

    #region Goal

    // Goal processing
    protected void InitializeGoalCompass()
    {
        if(goalCompassReference == null)
            goalCompassReference = new List<Image>();
        if(goalReferences == null)
            goalReferences = new List<GoalPoint>();

        // Avoid spectator having a goal
        if(team == -1) return;
        GameStatus.SyncListGoal goalList = (team == 1) ? GameStatus.instance.goalIdentitiesTeam1 : GameStatus.instance.goalIdentitiesTeam0;
        
        //Subscribe to changes in the goal
        goalList.Callback += (GoalCallBack);
        
        GoalCallBack(GameStatus.SyncListGoal.Operation.OP_DIRTY, 0, null);
    }

    protected void UpdateGoalCompassHUD(float delta)
    {
        if(tankRef == null) return;
        for (int i = 0; i < goalReferences.Count; i++)
        {
            goalCompassReference[i].color = goalReferences[i].goalColor;
            Vector3 distance = goalReferences[i].Position - tankRef.transform.position;
            float angle = Mathf.Atan2(distance.z,distance.x) * Mathf.Rad2Deg;
            angle -= 90; //Fix rotation
            goalCompassReference[i].transform.eulerAngles = new Vector3(0,0,angle);
        }
    }

    protected void GoalCallBack(GameStatus.SyncListGoal.Operation operation, int index, NetworkIdentity item) {
        // Get team
        if(team == -1) return;
        GameStatus.SyncListGoal goalList = (team == 1) ? GameStatus.instance.goalIdentitiesTeam1 : GameStatus.instance.goalIdentitiesTeam0;
        
        //Add targets
        goalReferences.Clear();
        foreach (var goal in goalList)
        {
            GoalPoint goalScript = goal.GetComponent<GoalPoint>();
            goalReferences.Add(goalScript);
        }
        
        //Add necessary compasses
        while (goalCompassReference.Count < goalReferences.Count)
        {
            GameObject newCompass = GameObject.Instantiate(goalCompassPrefab, goalCompassParent);
            newCompass.SetActive(true);
            Image compassImage = newCompass.GetComponent<Image>();
            goalCompassReference.Add(compassImage);
        }
        //Remove unecessaries compasses
        while (goalCompassReference.Count > goalReferences.Count)
        {
            Image toRemove = goalCompassReference[goalCompassReference.Count-1];
            goalCompassReference.RemoveAt(goalCompassReference.Count-1);
            Destroy(toRemove.gameObject);
        }

        // Update HUD
        UpdateGoalCompassHUD(Time.deltaTime);

    }

    #endregion

    #region Message

    public void AddMessage(PlayerMessage newMessage)
    {
        messageQueue.Enqueue(newMessage);
        if(messageCoroutine == null)
        {
            messageCoroutine = StartCoroutine(showMessage(messageQueue.Dequeue()));
        }

    }

    protected IEnumerator showMessage(PlayerMessage displayMessage){
        messageText.text = displayMessage.message;
        Color colorToChange = displayMessage.colorToUse;
        float counter = 0;
        
        
        //Do fade in
        if(displayMessage.fadeIn != 0) {
            colorToChange.a = 0;
            messageText.color = colorToChange;

            while(counter < displayMessage.fadeIn){
                counter += Time.deltaTime;
                
                colorToChange.a = counter/displayMessage.fadeIn;
                messageText.color = colorToChange;

                yield return null;        
            }
        }

        colorToChange.a = 1;
        messageText.color = colorToChange;

        //Wait duration of message
        counter = 0;
        while(counter < displayMessage.duration){
            counter += Time.deltaTime;

            yield return null;
        }

        //Do fade out
        counter = 0;
        if(displayMessage.fadeOut != 0) {
            while(counter < displayMessage.fadeOut){
                counter += Time.deltaTime;

                colorToChange.a = (displayMessage.fadeOut - counter)/displayMessage.fadeOut;
                messageText.color = colorToChange;

                yield return null;
            }
        }

        colorToChange.a = 0;
        messageText.color = colorToChange;

        if(messageQueue.Count > 0)
        {
            messageCoroutine = StartCoroutine(showMessage(messageQueue.Dequeue()));
        }
        else
        {
            messageCoroutine = null;
        }
    }
    
    #endregion

    #endregion
}
