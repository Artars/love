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
        public int id;
        public int team;
        public RoleAssigment[] assigments;

        public InfoTank(int id, int team, int numPlaces) {
            this.id = id;
            this.team = team;
            assigments = new RoleAssigment[numPlaces];
        }
    }

    [Serializable]
    public struct RoleAssigment {
        public Role role;
        public int playerAssigned;
    }
    public enum Role {
        None,Pilot,Gunner
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

    public int numTanks = 2;
    public int numTeams = 2;
    public List<InfoTank> infoTanks;
    public DictionaryIntPlayerInfo playersInfo;
    public List<LobbyPlayer> playersConnected;
    public string ip;

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
        }

        else {
            infoTanks = new  List<InfoTank>();
            for(int i = 0; i < numTanks; i++){
                InfoTank tank = new InfoTank(i,(i+1)/numTeams,2);

                //For assigning diferent places
                tank.assigments[0].role = Role.Pilot;
                tank.assigments[0].playerAssigned = -1;

                tank.assigments[1].role = Role.Gunner;
                tank.assigments[1].playerAssigned = -1;

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
    
    #endregion

    #region SelectionFunctions

    public void SelectTankRole(int tankID, LobbyPlayer player, int roleIndex){
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
