﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class PlayerHUD : NetworkBehaviour
{

    public struct PlayerMessage
    {
        public TMPro.TextMeshProUGUI textRef;
        public string message; 
        public Color colorToUse;
        public float duration;
        public float fadeIn;
        public float fadeOut;

        public PlayerMessage(TextMeshProUGUI textRef, string message, Color colorToUse, float duration, float fadeIn, float fadeOut)
        {
            this.textRef = textRef;
            this.message = message;
            this.colorToUse = colorToUse;
            this.duration = duration;
            this.fadeIn = fadeIn;
            this.fadeOut = fadeOut;
        }
    }

    #region Variables

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
    protected GearSystem gearSystem;
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
    protected Queue<PlayerMessage> messageQueue = new Queue<PlayerMessage>();
    protected Coroutine messageCoroutine;

    #endregion

    public void Start()
    {
        if(isLocalPlayer){
            informationCanvas.SetActive(true);
            GameStatus.instance.SetTimeText(timeText);
        }
    }

    public void AssignTank(int team, Role role, Tank tank)
    {
        tankRef = tank;
        this.role = role;
        this.team = team;

        //Update gears
        gearSystem = tankRef.gearSystem;
        leftSlider.minValue = gearSystem.lowestGear;
        leftSlider.maxValue = gearSystem.highestGear;
        rightSlider.minValue = gearSystem.lowestGear;
        rightSlider.maxValue = gearSystem.highestGear;

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
        displayMessage.textRef.text = displayMessage.message;
        Color colorToChange = displayMessage.colorToUse;
        float counter = 0;
        
        
        //Do fade in
        if(displayMessage.fadeIn != 0) {
            colorToChange.a = 0;
            displayMessage.textRef.color = colorToChange;

            while(counter < displayMessage.fadeIn){
                counter += Time.deltaTime;
                
                colorToChange.a = counter/displayMessage.fadeIn;
                displayMessage.textRef.color = colorToChange;

                yield return null;        
            }
        }

        colorToChange.a = 1;
        displayMessage.textRef.color = colorToChange;

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
                displayMessage.textRef.color = colorToChange;

                yield return null;
            }
        }

        colorToChange.a = 0;
        displayMessage.textRef.color = colorToChange;

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
}
