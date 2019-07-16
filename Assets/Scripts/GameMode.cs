using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEditor;

public class GameMode : NetworkBehaviour
{

    public enum GameStage
    {
        Lobby,Setup,Match,End
    }

    public struct KillPair
    {
        public int killerId;
        public int killedId;
        public float matchTime;

        public KillPair(int killer, int killed, float time)
        {
            killedId = killed;
            killerId = killer;
            matchTime = time;
        }
    }
       
    [Header("Game settings")]
    [SyncVar]
    public MatchSetting matchSettings;
    public float timeToStartGame = 5;
    public float timeToEndGame = 5;
    public bool returnToLobby = true;

    public List<InfoTank> infoTanks;
    public DictionaryIntPlayerInfo playersInfo;


    [Header("References")]
    public TankOptionCollection tankColection;
    public MapCollection mapCollection;
    public Transform spectatorPosition;
    public float spectatorDistance = 10;
    public Tank[] tanks;
    public List<Player> players;
    public List<Player> spectators;
    public List<Player>[] teamPlayers;
    protected Dictionary<int,List<SpawnPoint>> spawnPoints;

    [Header("Game State")]
    public string hostIP;
    [SyncVar]
    public GameStage gameStage = GameStage.Lobby;
    public List<float> score;
    public List<int> deaths;
    public List<int> kills;
    public List<KillPair> killHistory;
    public float matchTime;
    


    protected float currentCountdown;
    protected int connectedNumberOfClients = 0;

    public static GameMode instance = null;

    protected void Awake(){
        //Initialize singleton
        if(instance == null) 
            instance = this;
        else if(instance != this)
        {
            Destroy(gameObject);
            return;
        } 
        
    }

    void Update() {
        if(gameStage == GameStage.Match)
        {
            matchTime += Time.deltaTime;
            if(matchTime > matchSettings.maxTime)
            {
                MatchTimeEnded();
            }
        }
    }

    public void BroadcastMessageToAllConnected(string message, float duration, float fadeIn = 0.5f, float fadeOut = 1f)
    {
        foreach(Player p in players)
        {
            p.RpcDisplayMessage(message, duration, fadeIn, fadeOut);
        }
        foreach(Player p in spectators)
        {
            p.RpcDisplayMessage(message, duration, fadeIn, fadeOut);
        }
    }

    public void BroadcastMessageToTeam(int team, string message, float duration, float fadeIn = 0.5f, float fadeOut = 1f)
    {
        foreach(Player p in teamPlayers[team])
        {
            p.RpcDisplayMessage(message, duration, fadeIn, fadeOut);
        }
    }

    public void BroadcastMessageToPlayers(string message, float duration, float fadeIn = 0.5f, float fadeOut = 1f)
    {
        foreach(Player p in players)
        {
            p.RpcDisplayMessage(message, duration, fadeIn, fadeOut);
        }
    }

    public void BroadcastMessageToSpectators(string message, float duration, float fadeIn = 0.5f, float fadeOut = 1f)
    {
        foreach(Player p in spectators)
        {
            p.RpcDisplayMessage(message, duration, fadeIn, fadeOut);
        }
    }

    public void TryToJoinAsSpectator(Player player)
    {
        if(gameStage != GameStage.Lobby)
        {
            AssignPlayerToSpectator(player);
        }
    }

    #region Setup

    public void StartMatchSetup()
    {
        gameStage = GameStage.Setup;
        InitializeVariables();
        SpawnTanks();
        SetupPlayers();
        StartCoroutine(WaitMatchPresentation());

    }

    protected void EndMatchSetup()
    {
        AssignPlayersToTanks();

        matchTime = 0;
        gameStage = GameStage.Match;
        
        BroadcastMessageToAllConnected("Match has started!", 2f);
    }

    protected void InitializeVariables()
    {
        MatchConfiguration matchConfiguration = MatchConfiguration.instance;
        matchSettings = matchConfiguration.matchSetting;

        score = new List<float>();
        deaths = new List<int>();
        kills = new List<int>();
        killHistory = new List<KillPair>();
        for (int i = 0; i < matchConfiguration.numTanks; i++)
        {
            deaths.Add(0);
            kills.Add(0);
        }
        for (int i = 0; i < matchSettings.numTeams; i++)
        {
            score.Add(0);
        }

        tanks = new Tank[matchConfiguration.numTanks];
        players = new List<Player>();
        spectators = new List<Player>();
        teamPlayers = new List<Player>[matchConfiguration.matchSetting.numTeams];
        spawnPoints = new Dictionary<int, List<SpawnPoint>>();
        //Can have -1 team
        for (int i = -1; i < matchSettings.numTeams; i++)
        {
            spawnPoints.Add(i, new List<SpawnPoint>());
        }

        GameStatus.instance.Setup(matchSettings);
    }

    protected void SpawnTanks()
    {
        SpawnPoint[] allSpawnPoints = GameObject.FindObjectsOfType<SpawnPoint>();
        foreach(SpawnPoint spawn in allSpawnPoints)
        {
            if(spawn.team == -1)
            {
                for (int i = 0; i < matchSettings.numTeams; i++)
                {
                    spawnPoints[i].Add(spawn);
                }
            }
            spawnPoints[spawn.team].Add(spawn);
            
        }

        foreach(InfoTank tankInfo in MatchConfiguration.instance.infoTanks)
        {
            SpawnTank(tankInfo);
        }


    }

    protected void SpawnTank(InfoTank tankInfo)
    {
        Transform spawnPosition = GetSpawnPosition(tankInfo.team);
        GameObject tankPrefab = tankColection.tankOptions[tankInfo.prefabID].tankPrefab;

        GameObject tank = GameObject.Instantiate(tankPrefab, spawnPosition.position, spawnPosition.rotation);
        Tank tankRef = tank.GetComponent<Tank>();
        tankRef.team = tankInfo.team;
        tankRef.tankId = tankInfo.id;
        tanks[tankInfo.id] = tankRef;
        tankRef.ResetTank();
        NetworkServer.Spawn(tank);
    }

    public Transform GetSpawnPosition (int team)
    {
        if(!spawnPoints.ContainsKey(team))
        {
            return null;
        }

        return spawnPoints[team][Random.Range(0,spawnPoints[team].Count)].transform;

    }

    protected void SetupPlayers()
    {
        foreach(var connection in NetworkServer.connections)
        {
            int playerID = connection.Value.connectionId;
            NetworkIdentity identity = connection.Value.playerController;
            Player playerRef = identity.GetComponent<Player>();
    
            if(!MatchConfiguration.instance.playersInfo.ContainsKey(playerID)
            || !MatchConfiguration.instance.playersInfo[playerID].HasAssigment())
            {
                AssignPlayerToSpectator(playerRef);
            }
            else
            {
                players.Add(playerRef);

                PlayerInfo playerInfo = MatchConfiguration.instance.playersInfo[playerID];

                playerRef.team = playerInfo.team;
                playerRef.role = playerInfo.role;
                playerRef.playerInfo = playerInfo;


                if(teamPlayers[playerInfo.team] == null) teamPlayers[playerInfo.team] = new List<Player>();
                    teamPlayers[playerInfo.team].Add(playerRef);

                playerRef.RpcObservePosition(tanks[playerInfo.tankID].transform.position,10,45,10);
                playerRef.RpcDisplayMessage("You are on Team " + (playerInfo.team+1) + " with role " + playerInfo.role.ToString(),timeToStartGame/2, 0.5f, 1f);
            }
        }
    }

    protected void AssignPlayerToSpectator(Player player)
    {
        if(player != null)
        {
            if(!spectators.Contains(player))
                spectators.Add(player);
            player.RpcObservePosition(spectatorPosition.position, 0, 90f, spectatorDistance);
            player.currentMode = Player.Mode.Spectator;
        }
    }

    protected void AssignPlayersToTanks()
    {
        foreach(Player player in players)
        {
            {
                AssignPlayerToTank(player);
            }
        }
    }

    protected void AssignPlayerToTank(Player player)
    {
        if(player != null)
        {
            player.SetTankReference(tanks[player.playerInfo.tankID], player.playerInfo.team, player.playerInfo.role);
            Tank toAssing = tanks[player.playerInfo.tankID];
            toAssing.AssignPlayer(player, player.playerInfo.role);
            player.RpcAssignPlayer(player.playerInfo.team, player.playerInfo.role, toAssing.GetComponent<NetworkIdentity>());
        }
        else
        {
            Debug.LogWarning("Tried to assign null player!");
        }
    }

    protected IEnumerator WaitMatchPresentation()
    {
        float counter = 0;
        float endTime = matchSettings.timeToSetup;

        while(counter < endTime)
        {
            counter += Time.unscaledDeltaTime;
            yield return null;
        }

        EndMatchSetup();
    }

    #endregion



    #region Match
    
    public void TankKilled(int ownerId, int enemyId) {
        kills[enemyId]++;
        GameStatus.instance.kills[enemyId]++;
        deaths[ownerId]++;
        GameStatus.instance.deaths[ownerId]++;
        KillPair newKill = new KillPair(enemyId,ownerId,matchTime);
        killHistory.Add(newKill);
        // GameStatus.instance.killHistory.Add(newKill);

        BroadcastMessageToAllConnected("Tank " + ownerId + " was killed by Tank " + enemyId, 2f);

        ResetTank(ownerId);
        
        UpdateScore();
    }

    public virtual void UpdateScore(){
        for(int i = 0; i < matchSettings.numTeams; i++) {
            int count = 0;
            for (int j = 0; j < kills.Count; j++)
            {
                int team = MatchConfiguration.instance.infoTanks[j].team;
                if(team == i)
                    count+=kills[j];
            }
            score[i] = count;
            GameStatus.instance.score[i] = count;
        }
        CheckWinCondition();
    }

    

    public void ResetTank(int tankId){
        Tank tankToReset = tanks[tankId];

        Transform positionToSpawn = GetSpawnPosition(tankToReset.team);

        tankToReset.ResetTankPosition(positionToSpawn.position);

    }


    public Tank GetTank(int id) {
        if(tanks == null) {
            Debug.LogWarning("Couldn't find tank array");
            return null;
        }
        return tanks[id];
    }

    private IEnumerator waitToAssignBack(float time, Player toAssing) {
        currentCountdown = time;
        while(currentCountdown > 0) {
            currentCountdown -= Time.deltaTime;
            yield return null;
        }

        AssignPlayerToTank(toAssing);
    }

    #endregion

    #region End

    public virtual void CheckWinCondition() {
        List<int> winner = new List<int>();
        for(int i = 0; i < score.Count; i++) {
            if(score[i] >= matchSettings.maxPoints){
                winner.Add(i);
            }
        }
        if(winner.Count > 0)
        {
            EndGame(winner, winner.Count > 1);
        }
    }

    public virtual void MatchTimeEnded()
    {
        List<int> winningTeams = new List<int>();
        float highestScore = Mathf.NegativeInfinity;
        bool hasDraw = false;
        for (int i = 0; i < score.Count; i++)
        {
            if(score[i] > highestScore)
            {
                winningTeams.Clear();
                winningTeams.Add(i);
                hasDraw = false;
            }
            else if(score[i] == highestScore)
            {
                winningTeams.Add(i);
                hasDraw = true;
            }
        }

        EndGame(winningTeams, hasDraw);
        
    }

    public virtual void EndGame(List<int> winningTeams, bool hasDraw = false){
        gameStage = GameStage.End;
        
        string winningString = winningTeams[0].ToString();
        for (int i = 1; i < winningTeams.Count; i++)
        {
            winningString += " & " + winningTeams[i];
        }

        for(int i = 0; i < teamPlayers.Length; i ++){
            if(teamPlayers[i] != null){

                if(!hasDraw)
                {
                    foreach(Player p in teamPlayers[i])
                    {
                        if(winningTeams.Contains(i)){
                            p.RpcDisplayMessage("Outstanding performance, comrades! We have won this battle!", 10, 0.1f, 1);
                        }
                        else
                            p.RpcDisplayMessage("A shameful display! " + winningString + " has beaten us this time!"
                            , 10, 0.1f, 1);
                    }
                }
                else
                {
                    foreach(Player p in teamPlayers[i])
                    {
                            p.RpcDisplayMessage("I can't believe it, a Draw! Nice work teams " + winningString + "!", 10, 0.1f, 1);
                    }
                }
            }
        }

        //Spectators
        for (int i = 0; i < spectators.Count; i++)
        {
            if(spectators[i] != null)
            {
                spectators[i].RpcDisplayMessage("Nice work teams " + winningString + "!", 10, 0.1f, 1);
            }
        }

        StartCoroutine(waitTimeToEndGame(timeToEndGame));
    }

    private IEnumerator waitTimeToEndGame(float time) {
        float counter = time;

        while (counter > 0){
            counter -= Time.deltaTime;
            yield return null;
        }

        shutdownGame();
    }

    public void shutdownGame(){

        if(returnToLobby)
            NetworkManager.singleton.ServerChangeScene(mapCollection.mapOptions[matchSettings.mapIndex].scene);
        else
            NetworkManager.singleton.StopHost();
    }


    #endregion


    #region DEBUG

    #if UNITY_EDITOR

    [MenuItem("Debug/Kill tank 0")]
    public static void KillTank0()
    {
        instance.TankKilled(0,1);
    }

    #endif

    #endregion
}
