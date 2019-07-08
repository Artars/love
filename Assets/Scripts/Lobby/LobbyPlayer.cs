using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class LobbyPlayer : NetworkBehaviour
{
    [Header("Game Info")]
    [SyncVar]
    public string playerName = "Douce";
    [SyncVar]
    public int connectionID = -1;
    [SyncVar]
    public bool isReady = false;
    public List<LobbyManager.InfoTank> tanksInfo;
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
    public TMPro.TextMeshProUGUI assigmentTankText;
    public TMPro.TextMeshProUGUI textIP; 
    public UnityEngine.UI.Button buttonReady;


    protected int currentlySelectedTank = -1;
    protected bool assigmentWindowOpen = false;

    public override void OnNetworkDestroy () {
        if(isServer){
            if(LobbyManager.instance != null) {
                LobbyManager.instance.RemovePlayer(this);
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        if(isLocalPlayer){
            rootCanvas.SetActive(true);
            playersInfo = new DictionaryIntPlayerInfo();
            tanksInfo = new List<LobbyManager.InfoTank>();
            playerInfoHolders = new Dictionary<int,PlayerInfoHolder>();
            assigmentInfoHolder = new List<AssigmentInfoHolder>();

            roleAssigmentCanvas.SetActive(false);
            

            if(PlayerPrefs.HasKey("Name"))
                playerName = PlayerPrefs.GetString("Name");
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
    public void RpcUpdateTankInfo(int index, LobbyManager.InfoTank infoTank){
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
    public void RpcUpdatePlayerInfo(int index, LobbyManager.PlayerInfo playerInfo){
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
            parentContainer = (tanksInfo[index].team == 1) ? tankInfoParentTeam1 : tankInfoParentTeam2;
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
            
            LobbyManager.InfoTank infoTank = tanksInfo[currentlySelectedTank];

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
        assigmentTankText.text = "Tank " + index;
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
        NetworkManager.Shutdown();
    }

    
}
