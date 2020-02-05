using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

/// <summary>
/// Singleton server only class used to manage the status of lobby
/// </summary>
public class LobbyManager : NetworkBehaviour
{
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

        if(MatchConfiguration.instance != null && MatchConfiguration.instance.infoTanks.Count > 0) {
            infoTanks = new List<InfoTank>(MatchConfiguration.instance.infoTanks);

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

    /// <summary>
    /// Adds the player to the current server information
    /// </summary>
    /// <param name="player">The reference to the player</param>
    /// <param name="playerName">The player name</param>
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

        //Update IP text
        player.RpcReceiveIP(ip);

        //Send match settings
        player.RpcReceiveSettings(MatchConfiguration.instance.matchSetting);

        //Do not update other fields if game already started, hide hud
        if(isGameStarting)
        {
            player.RpcDeactivateThis();
            return;
        }

        //Send information from each position to this player
        PlayerInfo info = new PlayerInfo(connectionID,playerName);
        UpdatePlayerInfo(connectionID, info);

        
    }

    /// <summary>
    /// Updates the tank info for every player
    /// </summary>
    /// <param name="tankID">The id of the tank that was changed</param>
    /// <param name="infoTank">The new tank information</param>
    public void UpdateTankInfo(int tankID, InfoTank infoTank){
        //Go for each player and update
        infoTanks[tankID] = infoTank;

        foreach(var player in playersConnected){
            player.RpcUpdateTankInfo(tankID,infoTank);
        }

    }

    /// <summary>
    /// Update the player information for every player
    /// </summary>
    /// <param name="id">The id of the player</param>
    /// <param name="playerInfo">The information of that player</param>
    public void UpdatePlayerInfo(int id, PlayerInfo playerInfo){
        if(!playersInfo.ContainsKey(id))
            playersInfo.Add(id, playerInfo);
        else
            playersInfo[id] = playerInfo;

        foreach(var player in playersConnected){
            player.RpcUpdatePlayerInfo(id,playerInfo);
        }
    }

    /// <summary>
    /// Remove the information of a given tank
    /// </summary>
    /// <param name="tankID">The ID of the tank to remove</param>
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

    /// <summary>
    /// Remove for one player in the match
    /// </summary>
    /// <param name="removedPlayer">The reference of the player that must me removed</param>
    public void RemovePlayer(LobbyPlayer removedPlayer)
    {
        //Avoid double removal
        if(!playersInfo.ContainsKey(removedPlayer.connectionID)) return;

        
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

    /// <summary>
    /// Make a role selection for a given player
    /// </summary>
    /// <param name="tankID">The ID of the tank selected</param>
    /// <param name="player">The reference to the player that made the selection</param>
    /// <param name="roleIndex">The ID of the role selected on the tank</param>
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

    /// <summary>
    /// Change the skin of a given tank
    /// </summary>
    /// <param name="tankID">The ID of the tank selected</param>
    /// <param name="player">The reference to the player that made the selection</param>
    /// <param name="skin">The ID of the skin selected to the tank</param>
    public void SelectTankSkin(int tankID, LobbyPlayer player, int skin){
        if(isGameStarting) return; // Won't change if game is starting

        InfoTank tankInfo = infoTanks[tankID];
        int playerConnectionId = player.connectionToClient.connectionId;
        
        bool isCorrect = false;
        //Verify if player has autority
        for (int i = 0; i < tankInfo.assigments.Length; i++)
        {
            if(tankInfo.assigments[i].playerAssigned == playerConnectionId)
            {
                isCorrect = true;
                break;
            }
        }

        //Avoid changing skin if the player doesn't have permission
        if(!isCorrect) return;

        //Update the new tank
        //Verify range
        tankInfo.skin = Mathf.Clamp(skin,0,tankCollection.tankOptions[tankInfo.prefabID].tankSprites.Length-1);

        UpdateTankInfo(tankID, tankInfo);
    }

    /// <summary>
    /// Change the name of a given tank
    /// </summary>
    /// <param name="tankID">The ID of the tank selected</param>
    /// <param name="player">The reference to the player that made the selection</param>
    /// <param name="newName">The new name of the tank</param>
    public void SelectTankName(int tankID, LobbyPlayer player, string newName, bool showName){
        if(isGameStarting) return; // Won't change if game is starting

        InfoTank tankInfo = infoTanks[tankID];
        int playerConnectionId = player.connectionToClient.connectionId;
        
        bool isCorrect = false;
        //Verify if player has autority
        for (int i = 0; i < tankInfo.assigments.Length; i++)
        {
            if(tankInfo.assigments[i].playerAssigned == playerConnectionId)
            {
                isCorrect = true;
                break;
            }
        }

        //Avoid changing skin if the player doesn't have permission
        if(!isCorrect) return;

        //Update the new tank
        tankInfo.name = newName;
        tankInfo.showName = showName;

        UpdateTankInfo(tankID, tankInfo);
    }

    /// <summary>
    /// Remove a selection for that given player
    /// </summary>
    /// <param name="player"></param>
    public void PlayerDeselect(LobbyPlayer player) {
        if(isGameStarting) return; // Won't change if game is starting

        int playerConnectionId = player.connectionToClient.connectionId;

        //Avoid null
        if(!playersInfo.ContainsKey(playerConnectionId)) return;
        
        PlayerInfo playerInfo = playersInfo[playerConnectionId];

        //Try to set the previous selection to free
        int previousTankId = playerInfo.tankID;
        if(previousTankId != -1){
            InfoTank previousTankInfo = infoTanks[previousTankId];

            previousTankInfo.assigments[playerInfo.roleIndex].playerAssigned = -1;
            UpdateTankInfo(previousTankId,previousTankInfo);
        }
        playerInfo.role = Role.None;
        playerInfo.roleIndex = -1;
        playerInfo.tankID = -1;
        playerInfo.ready = false;
        playerInfo.team = -1;
        UpdatePlayerInfo(playerInfo.connectionID, playerInfo);
    }

    /// <summary>
    /// Set a player ready status. May trigger the start of the match
    /// </summary>
    /// <param name="player">The player reference of the player that connected to the match</param>
    /// <param name="isReady">Whatever the player is ready or not</param>
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

    /// <summary>
    /// Start the transition to the match
    /// </summary>
    public void StartGame(){
        isGameStarting = true;
        MatchConfiguration.instance.infoTanks = infoTanks;
        MatchConfiguration.instance.playersInfo = playersInfo;
        GameMode.instance.StartMatchSetup();
        HideLobbyForAllPlayers();
        // MatchSettings.instance.connectedPlayers = playersConnected.Count;
        // NetworkManager.singleton.ServerChangeScene("Scenes/Burgsgrad");
    }

    /// <summary>
    /// Will return if everyone that has choosen a role is ready
    /// </summary>
    /// <returns>Is everybody ready</returns>
    public bool isGameReady() {
        bool everyoneIsReady = true;
        int count = 0;

        foreach (var tank in infoTanks)
        {
            foreach (var assigment in tank.assigments)
            {
                if(assigment.playerAssigned != -1 && playersInfo.ContainsKey(assigment.playerAssigned))
                {
                    if(!playersInfo[assigment.playerAssigned].ready)
                    {
                        everyoneIsReady = false;
                        break;
                    }
                    else
                    {
                        count++;
                    }
                }
            }
        }

        return everyoneIsReady && count > 0;
    }

    public void HideLobbyForAllPlayers()
    {
        foreach(LobbyPlayer player in playersConnected)
        {
            player.RpcDeactivateThis();
        }
    }

    #endregion

    /// <summary>
    /// Get the IP of the server by using the DNS table
    /// </summary>
    /// <returns>The IP of the host as a string</returns>
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
