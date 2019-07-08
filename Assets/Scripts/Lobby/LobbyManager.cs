using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

public class LobbyManager : NetworkBehaviour
{
    #region ClassDefinitions

    [Serializable]
    public struct InfoTank {
        public int prefabID;
        public int id;
        public int team;
        public RoleAssigment[] assigments;

        public InfoTank(int id, int team, int numPlaces, int prefabID) {
            this.id = id;
            this.team = team;
            assigments = new RoleAssigment[numPlaces];
            this.prefabID = prefabID;
        }

        public InfoTank(int id, int team, TankOption tankOption) {
            this.id = id;
            this.team = team;
            assigments = new RoleAssigment[tankOption.tankRoles.Length];
            this.prefabID = tankOption.prefabID;

            for (int i = 0; i < tankOption.tankRoles.Length; i++)
            {
                assigments[i].role = tankOption.tankRoles[i];
            }
        }

    }

    [Serializable]
    public struct RoleAssigment {
        public Role role;
        public int playerAssigned;
    }
    

    [Serializable]
    public struct PlayerInfo {
        public int connectionID;
        public string name;
        public int tankID;
        public int team;
        public Role role;
        public int roleIndex;
        public bool ready;

        public PlayerInfo(int connectionID, string name){
            this.connectionID = connectionID;
            this.name = name;
            tankID = -1;
            team = -1;
            role = Role.None;
            roleIndex = -1;
            ready = false;
        }

    }

    #endregion

    public static LobbyManager instance = null;

    public TankOptionCollection tankCollection;
    public int numTanks = 2;
    public int numTeams = 2;
    public List<InfoTank> infoTanks;
    public DictionaryIntPlayerInfo playersInfo;
    public List<LobbyPlayer> playersConnected;
    public string ip;
    public bool isGameStarting = false;

    public void Awake(){
        //Creates singleton
        if(instance == null){
            instance = this;
        }
        else if(instance != this){
            Destroy(gameObject);
        }

        if(MatchSettings.instance != null && MatchSettings.instance.infoTanks.Count > 0) {
            infoTanks = new List<InfoTank>(MatchSettings.instance.infoTanks);

            //Clear player choices
            for (int i = 0; i < infoTanks.Count; i++)
            {
                InfoTank info = infoTanks[i];
                for (int j = 0; j < info.assigments.Length; j++)
                {
                    info.assigments[j].playerAssigned = -1;
                }
            }
        }

        else {
            infoTanks = new  List<InfoTank>();
            for(int i = 0; i < numTanks; i++){
                InfoTank tank = new InfoTank(i,(i+1)/numTeams, tankCollection.tankOptions[i % tankCollection.tankOptions.Length]);

                infoTanks.Add(tank);
            }
        }

        playersInfo = new DictionaryIntPlayerInfo();
        playersConnected = new List<LobbyPlayer>();

        ip = getIPString();
    }


    #region UpdateInformation

    public void PlayerJoin(LobbyPlayer player, string playerName) {
        int connectionID = player.connectionToClient.connectionId;
        

        //Save reference of this player
        playersConnected.Add(player);
        player.RpcReceiveConnectionID(connectionID);

        //Send the current information
        for(int i = 0; i < infoTanks.Count; i++){
            player.RpcUpdateTankInfo(i,infoTanks[i]);
        }

        //Send the current playerInfo
        foreach(var pairValue in playersInfo){
            player.RpcUpdatePlayerInfo(pairValue.Key, pairValue.Value);
        }

        //Send information from each position to this player
        PlayerInfo info = new PlayerInfo(connectionID,playerName);
        UpdatePlayerInfo(connectionID, info);

        //Update IP text
        player.RpcReceiveIP(ip);
    }

    public void UpdateTankInfo(int tankID, InfoTank infoTank){
        //Go for each player and update
        infoTanks[tankID] = infoTank;

        foreach(var player in playersConnected){
            player.RpcUpdateTankInfo(tankID,infoTank);
        }

    }

    public void UpdatePlayerInfo(int id, PlayerInfo playerInfo){
        if(!playersInfo.ContainsKey(id))
            playersInfo.Add(id, playerInfo);
        else
            playersInfo[id] = playerInfo;

        foreach(var player in playersConnected){
            player.RpcUpdatePlayerInfo(id,playerInfo);
        }
    }

    public void RemoveTankInfo(int tankID)
    {
        if(infoTanks.Count < tankID)
        {
            infoTanks.RemoveAt(tankID);
            
            foreach(var player in playersConnected){
                player.RpcRemoveTankInfo(infoTanks.Count);
            }

            for(int i = tankID; i < infoTanks.Count; i++)
            {
                InfoTank toChange = infoTanks[i];
                toChange.id = i-1;


                foreach(RoleAssigment r in toChange.assigments)
                {
                    if(r.playerAssigned != -1)
                    {
                        if(playersInfo.ContainsKey(r.playerAssigned))
                        {
                            PlayerInfo playerInfo = playersInfo[r.playerAssigned];
                            playerInfo.tankID = i;
                            UpdatePlayerInfo(playerInfo.connectionID, playerInfo);
                        }
                    }
                }
            }
        }
    }

    public void RemovePlayer(LobbyPlayer removedPlayer)
    {
        //Deselect if selefted before
        PlayerDeselect(removedPlayer);

        //Remove from lists
        playersConnected.Remove(removedPlayer);
        playersInfo.Remove(removedPlayer.connectionID);

        //Call everyone to remove that player from the list
        foreach (var player in playersConnected)
        {
            if(player != removedPlayer)
            {
                player.RpcRemovePlayerInfo(removedPlayer.connectionID);
            }
        }
    }
    
    #endregion

    #region SelectionFunctions

    public void SelectTankRole(int tankID, LobbyPlayer player, int roleIndex){
        if(isGameStarting) return; // Won't change if game is starting

        InfoTank tankInfo = infoTanks[tankID];
        int playerConnectionId = player.connectionToClient.connectionId;
        
        PlayerInfo playerInfo = playersInfo[playerConnectionId];

        //Try to set the previous selection to free
        int previousTankId = playerInfo.tankID;
        if(previousTankId != -1){
            InfoTank previousTankInfo = infoTanks[previousTankId];

            previousTankInfo.assigments[playerInfo.roleIndex].playerAssigned = -1;
            UpdateTankInfo(previousTankId,previousTankInfo);
        }

        //Update the new tank
        tankInfo.assigments[roleIndex].playerAssigned = playerConnectionId;

        UpdateTankInfo(tankID, tankInfo);


        //Update player info
        playerInfo.roleIndex = roleIndex;
        playerInfo.tankID = tankID;
        playerInfo.team = tankInfo.team;
        playerInfo.role = tankInfo.assigments[roleIndex].role;

        UpdatePlayerInfo(playerConnectionId, playerInfo);
    }

    public void PlayerDeselect(LobbyPlayer player) {
        if(isGameStarting) return; // Won't change if game is starting

        int playerConnectionId = player.connectionToClient.connectionId;
        
        PlayerInfo playerInfo = playersInfo[playerConnectionId];

        //Try to set the previous selection to free
        int previousTankId = playerInfo.tankID;
        if(previousTankId != -1){
            InfoTank previousTankInfo = infoTanks[previousTankId];

            previousTankInfo.assigments[playerInfo.roleIndex].playerAssigned = -1;
            UpdateTankInfo(previousTankId,previousTankInfo);
        }
    }

    public void PlayerSetReady(LobbyPlayer player, bool isReady) {
        int playerConnectionId = player.connectionToClient.connectionId;

        PlayerInfo playerInfo = playersInfo[playerConnectionId];

        playerInfo.ready = isReady;

        UpdatePlayerInfo(playerConnectionId,playerInfo);

        if(isGameReady()){
            StartGame();
        }
    }

    #endregion

    #region StartGame

    public void StartGame(){
        isGameStarting = true;
        MatchSettings.instance.infoTanks = infoTanks;
        MatchSettings.instance.playersInfo = playersInfo;
        MatchSettings.instance.connectedPlayers = playersConnected.Count;
        NetworkManager.singleton.ServerChangeScene("Scenes/MapTest");
    }

    public bool isGameReady() {
        bool everyoneIsReady = true;

        foreach(var player in playersInfo){
            if(!player.Value.ready){
                everyoneIsReady = false;
                break;
            }
        }

        return everyoneIsReady;
    }

    #endregion

    protected string getIPString(){
        string s;
        // NetworkManager.singleton.transport.GetConnectionInfo(0, out s);
        s = NetworkManager.singleton.networkAddress;
        foreach (var ip in System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList){//[0].ToString());
            if(ip.ToString().Length < 17)
                s = ip.ToString();
        }
        return s;
    }
}
