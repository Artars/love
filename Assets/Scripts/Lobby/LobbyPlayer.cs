using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class LobbyPlayer : NetworkBehaviour
{
    [Header("Game Info")]
    [SyncVar]
    public string playerName = "Deuce";
    [SyncVar]
    public int connectionID = -1;
    [SyncVar]
    public bool isReady = false;
    public List< InfoTank> tanksInfo;
    public DictionaryIntPlayerInfo playersInfo;

    [Space]
    [Header("CanvasReferences")]
    public GameObject rootCanvas;
    public GameObject mainCanvas;
    public GameObject roleAssigmentCanvas;
    

    [Header("Prefabs")]
    public GameObject playerInfoContainerPrefab;
    public GameObject tankInfoContainerPrefab;
    public GameObject assigmentContainerPrefab;

    [Header("Containers")]
    public Transform playerInfoParent;
    public Transform tankInfoParentTeam1;
    public Transform tankInfoParentTeam2;
    public Transform assigmentInfoParent;

    [Header("Holders reference")]
    public List<TankInfoHolder> tankInfoHolders;
    public Dictionary<int,PlayerInfoHolder> playerInfoHolders;
    public List<AssigmentInfoHolder> assigmentInfoHolder;

    [Header("Other References")]
    public SettingsSelector settingsSelector;
    public TankOptionCollection tankCollection;
    public Image tankImage;
    public TMPro.TextMeshProUGUI textIP; 
    public UnityEngine.UI.Button buttonReady;

    [Header("Tank Assigment Window")]
    public TMPro.TextMeshProUGUI assigmentTankText;
    public Button nameChangeButton;
    public Button incrementSkinButton;
    public Button decrementSkinButton;
    public GameObject renameWindow;
    public TMPro.TMP_InputField nameField;
    public Toggle showNameToggle;


    protected int currentlySelectedTank = -1;
    protected bool hasTankAutority = false;
    protected bool assigmentWindowOpen = false;

    public override void OnNetworkDestroy () {
        if(isServer){
            if(LobbyManager.instance != null) {
                LobbyManager.instance.RemovePlayer(this);
            }
        }
        base.OnNetworkDestroy();
    }

    // Start is called before the first frame update
    void Start()
    {
        if(isLocalPlayer){
            rootCanvas.SetActive(true);
            playersInfo = new DictionaryIntPlayerInfo();
            tanksInfo = new List< InfoTank>();
            playerInfoHolders = new Dictionary<int,PlayerInfoHolder>();
            assigmentInfoHolder = new List<AssigmentInfoHolder>();

            roleAssigmentCanvas.SetActive(false);
            

            playerName = PlayerPrefs.GetString("Name", "Deuce");
            CmdJoinLobby(playerName);
        }
        else{
            rootCanvas.SetActive(false);
        }
    }

    [Command]
    public void CmdJoinLobby(string playerName){
        if(LobbyManager.instance != null) {
            LobbyManager.instance.PlayerJoin(this,playerName);
        }
        else {
            RpcDeactivateThis();
            rootCanvas.SetActive(false);
        }
    }

    [ClientRpc]
    public void RpcReceiveConnectionID(int id) {
        connectionID = id;
    }

    [ClientRpc]
    public void RpcDeactivateThis(){
        rootCanvas.SetActive(false);
    }

    [ClientRpc]
    public void RpcUpdateTankInfo(int index,  InfoTank infoTank){
        if(isLocalPlayer){
            if(index < tanksInfo.Count){
                tanksInfo[index] = infoTank;
            }
            else{
                tanksInfo.Add(infoTank);
            }
            UpdateTankInfo(index);
        }
    }

    [ClientRpc]
    public void RpcRemoveTankInfo(int newCount)
    {
        if(isLocalPlayer)
        {
            while(tanksInfo.Count > newCount)
            {
                tanksInfo.RemoveAt(tanksInfo.Count-1);
            }
        }
    }

    [ClientRpc]
    public void RpcUpdatePlayerInfo(int index,  PlayerInfo playerInfo){
        if(isLocalPlayer){
            if(playersInfo.ContainsKey(index)){
                playersInfo[index] = playerInfo;
            }
            else{
                playersInfo.Add(index,playerInfo);
            }
            UpdatePlayerInfo(index);
        }
    }

    [ClientRpc]
    public void RpcRemovePlayerInfo(int removedPlayerID)
    {
        Destroy(playerInfoHolders[removedPlayerID].gameObject);
        playerInfoHolders.Remove(removedPlayerID);
    }

    [ClientRpc]
    public void RpcReceiveIP(string ip) {
        textIP.text = "Lobby: " + ip;
    }

    [ClientRpc]
    public void RpcReceiveSettings(MatchSetting matchSetting)
    {
        if(!isLocalPlayer)
            return;

        if(settingsSelector != null)
        {
            settingsSelector.SetMatchSetting(matchSetting);
        }
    }

    [Command]
    public void CmdSelectRole(int tankId, int roleIndex) {
        LobbyManager.instance.SelectTankRole(tankId,this,roleIndex);
    }

    [Command]
    public void CmdDeselectRole()
    {
        LobbyManager.instance.PlayerDeselect(this);
    }

    [Command]
    public void CmdSetTankSkin(int tankId, int skin)
    {
        LobbyManager.instance.SelectTankSkin(tankId,this,skin);
    }

    [Command]
    public void CmdSetTankName(int tankId, string newName, bool showName)
    {
        LobbyManager.instance.SelectTankName(tankId,this,newName, showName);
    }

    [Command]
    public void CmdSetReady(bool isReady){
        LobbyManager.instance.PlayerSetReady(this, isReady);
    }

    protected void UpdatePlayerInfo(int index){
        if(!playerInfoHolders.ContainsKey(index)){
            GameObject infoHolder = GameObject.Instantiate(playerInfoContainerPrefab,playerInfoParent);
            infoHolder.SetActive(true);
            playerInfoHolders.Add(index, infoHolder.GetComponent<PlayerInfoHolder>());
        }

        PlayerInfoHolder holder = playerInfoHolders[index];
        holder.SetPlayerInfo(playersInfo[index]);

        if(playersInfo[index].connectionID == connectionID){
            buttonReady.interactable = (playersInfo[index].tankID != -1);
        }
    }

    protected void UpdateTankInfo(int index){
        TankInfoHolder holder;

        while(tankInfoHolders.Count <= index){
            Transform parentContainer;
            parentContainer = (tanksInfo[index].team == 0) ? tankInfoParentTeam1 : tankInfoParentTeam2;
            GameObject infoHolder = GameObject.Instantiate(tankInfoContainerPrefab, parentContainer);
            infoHolder.SetActive(true);

            holder = infoHolder.GetComponent<TankInfoHolder>();
            tankInfoHolders.Add(holder);

            holder.selectButton.onClick.AddListener(() => ClickOnTankButton(index));
            
        }

        holder = tankInfoHolders[index];

        holder.SetTankInfo(tanksInfo[index], connectionID);

        //Update selection options if the player is selecting
        if(index == currentlySelectedTank){
            UpdateRoleSelectionButtons();
        }
    }

    protected void UpdateRoleSelectionButtons(){
        if(currentlySelectedTank != -1){
            
            InfoTank infoTank = tanksInfo[currentlySelectedTank];

            //Update skin and name
            assigmentTankText.text = "Tank " + tanksInfo[currentlySelectedTank].name;
            tankImage.sprite = tankCollection.tankOptions[tanksInfo[currentlySelectedTank].prefabID].tankSprites[tanksInfo[currentlySelectedTank].skin];
            
            //Test if has autority
            hasTankAutority = false;
            for (int i = 0; i < tanksInfo[currentlySelectedTank].assigments.Length; i++)
            {
                if(tanksInfo[currentlySelectedTank].assigments[i].playerAssigned == connectionID)
                {
                    hasTankAutority = true;
                    break;
                }
            }

            nameChangeButton.interactable = hasTankAutority;
            incrementSkinButton.interactable = hasTankAutority;
            decrementSkinButton.interactable = hasTankAutority;

            showNameToggle.isOn = tanksInfo[currentlySelectedTank].showName;
            if(renameWindow.activeInHierarchy)
            {
                renameWindow.SetActive(hasTankAutority);
                if(hasTankAutority)
                    nameField.text = tanksInfo[currentlySelectedTank].name;
            }

            while(assigmentInfoHolder.Count <= infoTank.assigments.Length){
                int index = assigmentInfoHolder.Count;
                GameObject infoHolder = GameObject.Instantiate(assigmentContainerPrefab, assigmentInfoParent);
                infoHolder.SetActive(true);

                AssigmentInfoHolder holder = infoHolder.GetComponent<AssigmentInfoHolder>();
                assigmentInfoHolder.Add(holder);

                holder.button.onClick.AddListener(() => ClickOnRoleSelection(index));
            }

            for (int i = 0; i < assigmentInfoHolder.Count; i++)
            {
                if(i < infoTank.assigments.Length)
                {
                    assigmentInfoHolder[i].gameObject.SetActive(true);
                    assigmentInfoHolder[i].SetAssigmentInfo(infoTank.assigments[i],playersInfo, connectionID);
                    if(isReady)
                    {
                        assigmentInfoHolder[i].button.interactable = false;
                    }
                }
                else
                {
                    assigmentInfoHolder[i].gameObject.SetActive(false);
                }
            }

        }
    }

    public void ClickOnTankButton(int index){
        assigmentWindowOpen = true;
        roleAssigmentCanvas.SetActive(true);

        currentlySelectedTank = index;
        
        UpdateRoleSelectionButtons();
    }

    public void ClickCloseAssigmentWindow()
    {
        assigmentWindowOpen = false;
        roleAssigmentCanvas.SetActive(false);

        currentlySelectedTank = -1;
    }


    public void ClickOnRoleSelection(int index) {
        if(currentlySelectedTank != -1){
            if(tanksInfo[currentlySelectedTank].assigments[index].playerAssigned == -1)
            {
                CmdSelectRole(currentlySelectedTank, index);
            }
            else if(tanksInfo[currentlySelectedTank].assigments[index].playerAssigned == connectionID)
            {
                CmdDeselectRole();
            }
        }
    }

    public void ClickIncrementSkin()
    {
        if(currentlySelectedTank != -1)
        {
            int newSkin = tanksInfo[currentlySelectedTank].skin;
            newSkin++;
            if(newSkin >= tankCollection.tankOptions[tanksInfo[currentlySelectedTank].prefabID].tankSprites.Length)
                newSkin = 0;
            CmdSetTankSkin(currentlySelectedTank,newSkin);
        }
    }

    public void ClickDecrementSkin()
    {
        if(currentlySelectedTank != -1)
        {
            int newSkin = tanksInfo[currentlySelectedTank].skin;
            newSkin--;
            if(newSkin < 0)
                newSkin = tankCollection.tankOptions[tanksInfo[currentlySelectedTank].prefabID].tankSprites.Length - 1;
            CmdSetTankSkin(currentlySelectedTank,newSkin);
        }
    }

    public void ClickRenameWindow()
    {
        if(currentlySelectedTank == -1) return;
        renameWindow.SetActive(true);
        nameField.text = tanksInfo[currentlySelectedTank].name;
        showNameToggle.isOn = tanksInfo[currentlySelectedTank].showName;
    }

    public void ClickNameCancel()
    {
        renameWindow.SetActive(false);
    }

    public void ClickNameSubmit()
    {
        CmdSetTankName(currentlySelectedTank, nameField.text, showNameToggle.isOn);
        renameWindow.SetActive(false);
    }


    public void ClickToggleReadyButton(){
        isReady = !isReady;
        CmdSetReady(isReady);

        foreach(var holder in tankInfoHolders){
            holder.selectButton.interactable = !isReady;
        }

        if(isReady) {
            if(assigmentWindowOpen)
            {
                foreach(AssigmentInfoHolder info in assigmentInfoHolder)
                {
                    info.button.interactable = false;
                }
            }
        }
        else {
            UpdateRoleSelectionButtons();
        }
    }

    public void ClickExitButton()
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

    
}
