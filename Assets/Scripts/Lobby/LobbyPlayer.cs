using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class LobbyPlayer : NetworkBehaviour
{
    [SyncVar]
    public string playerName = "Douce";
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

    public List<TankInfoHolder> tankInfoHolders;
    public Dictionary<int,PlayerInfoHolder> playerInfoHolders;


    protected int currentlySelectedTank = -1;

    // Start is called before the first frame update
    void Start()
    {
        if(isLocalPlayer){
            playersInfo = new DictionaryIntPlayerInfo();
            tanksInfo = new List<LobbyManager.InfoTank>();
            playerInfoHolders = new Dictionary<int,PlayerInfoHolder>();
            selectRoleContainer.SetActive(true);
            

            if(PlayerPrefs.HasKey("Name"))
                playerName = PlayerPrefs.GetString("Name");
            CmdJoinLobby(name);
        }
        else{
            canvas.SetActive(false);
        }
    }

    [Command]
    public void CmdJoinLobby(string name){
        LobbyManager.instance.PlayerJoin(this,name);
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
            UpdatePlayerInfo(index);
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
        }
    }

    [Command]
    public void CmdSelectRole(int tankId, int roleIndex) {
        LobbyManager.instance.SelectTankRole(tankId,this,roleIndex);
    }

    protected void UpdatePlayerInfo(int index){
        if(!playerInfoHolders.ContainsKey(index)){
            GameObject infoHolder = GameObject.Instantiate(playerInfoContainerPrefab,playerInfoParent);
            playerInfoHolders.Add(index, infoHolder.GetComponent<PlayerInfoHolder>());
        }

        PlayerInfoHolder holder = playerInfoHolders[index];
        holder.SetPlayerInfo(playersInfo[index]);
    }

    protected void UpdateTankInfo(int index){
        TankInfoHolder holder;
        if(tankInfoHolders.Count < index){
            GameObject infoHolder = GameObject.Instantiate(tankInfoContainerPrefab,tankInfoParent);

            holder = infoHolder.GetComponent<TankInfoHolder>();
            tankInfoHolders.Add(holder);

            holder.selectButton.onClick.AddListener(() => ClickOnTankButton(index));
            
        }

        holder = tankInfoHolders[index];

        holder.SetTankInfo(tanksInfo[index], connectionToServer.connectionId);

        //Update selection options if the player is selecting
        if(index == currentlySelectedTank){
            UpdateRoleSelectionButtons();
        }
    }

    protected void UpdateRoleSelectionButtons(){
        if(currentlySelectedTank != -1){
            LobbyManager.InfoTank infoTank = tanksInfo[currentlySelectedTank];

            textTankSelectRole.text = "Tank " + infoTank.id;

            int lowestIndex = buttonRoleSelection.Length;
            if(lowestIndex > infoTank.assigments.Length) lowestIndex = infoTank.assigments.Length;

            for(int i = 0; i < lowestIndex; i++){
                buttonRoleSelection[i].enabled = (infoTank.assigments[i].playerAssigned == -1);
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
        CmdSelectRole(currentlySelectedTank, index);
    }


    
}
