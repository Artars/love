using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class LobbyPlayer : NetworkBehaviour
{
    [SyncVar]
    public string playerName = "Douce";
    [SyncVar]
    public int connectionID = -1;
    [SyncVar]
    public bool isReady = false;
    public List<LobbyManager.InfoTank> tanksInfo;
    public DictionaryIntPlayerInfo playersInfo;

    [Header("CanvasReferences")]
    public GameObject canvas;
    public GameObject playerInfoContainerPrefab;
    public GameObject tankInfoContainerPrefab;
    public Transform playerInfoParent;
    public Transform tankInfoParent;

    public GameObject selectRoleContainer;
    public TMPro.TextMeshProUGUI textTankSelectRole;
    public UnityEngine.UI.Button[] buttonRoleSelection;

    public UnityEngine.UI.Button buttonReady;

    public List<TankInfoHolder> tankInfoHolders;
    public Dictionary<int,PlayerInfoHolder> playerInfoHolders;

    public TMPro.TextMeshProUGUI textIP;


    protected int currentlySelectedTank = -1;

    public override void OnNetworkDestroy () {
        if(isServer){
            if(LobbyManager.instance != null) {
                LobbyManager.instance.PlayerDeselect(this);
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        if(isLocalPlayer){
            canvas.SetActive(true);
            playersInfo = new DictionaryIntPlayerInfo();
            tanksInfo = new List<LobbyManager.InfoTank>();
            playerInfoHolders = new Dictionary<int,PlayerInfoHolder>();
            selectRoleContainer.SetActive(false);
            

            if(PlayerPrefs.HasKey("Name"))
                playerName = PlayerPrefs.GetString("Name");
            CmdJoinLobby(playerName);
        }
        else{
            canvas.SetActive(false);
        }
    }

    [Command]
    public void CmdJoinLobby(string playerName){
        if(LobbyManager.instance != null) {
            LobbyManager.instance.PlayerJoin(this,playerName);
        }
        else {
            RpcDeactivateThis();
            canvas.SetActive(false);
        }
    }

    [ClientRpc]
    public void RpcReceiveConnectionID(int id) {
        connectionID = id;
    }

    [ClientRpc]
    public void RpcDeactivateThis(){
        canvas.SetActive(false);
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
    public void RpcReceiveIP(string ip) {
        textIP.text = "Lobby: " + ip;
    }

    [Command]
    public void CmdSelectRole(int tankId, int roleIndex) {
        LobbyManager.instance.SelectTankRole(tankId,this,roleIndex);
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
            GameObject infoHolder = GameObject.Instantiate(tankInfoContainerPrefab,tankInfoParent);
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
            selectRoleContainer.SetActive(true);
            
            LobbyManager.InfoTank infoTank = tanksInfo[currentlySelectedTank];

            textTankSelectRole.text = "Tank " + infoTank.id + "\nSelect your role:";

            int lowestIndex = buttonRoleSelection.Length;
            if(lowestIndex > infoTank.assigments.Length) lowestIndex = infoTank.assigments.Length;

            for(int i = 0; i < lowestIndex; i++){
                buttonRoleSelection[i].interactable = (infoTank.assigments[i].playerAssigned == -1);
            }
        }
    }

    public void ClickOnTankButton(int index){
        if(currentlySelectedTank != index){
            currentlySelectedTank = index;
            UpdateRoleSelectionButtons();
        }
    }

    public void ClickOnRoleSelection(int index) {
        if(currentlySelectedTank != -1){
            CmdSelectRole(currentlySelectedTank, index);
        }
    }

    public void ClickToggleReadyButton(){
        isReady = !isReady;
        CmdSetReady(isReady);

        foreach(var holder in tankInfoHolders){
            holder.selectButton.interactable = !isReady;
        }

        if(isReady) {
            foreach(var button in buttonRoleSelection){
                button.interactable = false;
            }
        }
        else {
            UpdateRoleSelectionButtons();
        }
    }

    
}
