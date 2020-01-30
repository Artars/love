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

    [Header("Game Info")]
    public string[] teamAliases = new string[]{"Nationals", "Republicans"};
       
    [Header("Game settings")]
    [SyncVar]
    public MatchSetting matchSettings;
    [Tooltip("Will force all tanks to use this parameters")]
    public TankParametersObject forceTankParameters = null;
    public float timeToStartGame = 5;
    public float timeToEndGame = 10;
    public bool returnToLobby = true;
    public float volumeInMatch = 0.25f;
    public bool assingAIToMissingTanks = true;
    public Color defaultMessageColor = Color.yellow;
    public bool updateScoreInkill =  true;
    

    public List<InfoTank> infoTanks;
    public DictionaryIntPlayerInfo playersInfo;


    [Header("References")]
    public GameObject goalPointPrefab;
    public GameObject AIPlayerPrefab;
    public TankOptionCollection tankColection;
    public MapCollection mapCollection;
    public Transform spectatorPosition;
    public Transform hideTankPosition;
    public float spectatorDistance = 10;
    protected Tank[] tanks;
    protected List<Player> players;
    protected List<Player> spectators;
    protected List<Player>[] teamPlayers;
    protected Dictionary<int,List<SpawnPoint>> spawnPoints;

    [Header("Game State")]
    public string hostIP;
    [SyncVar]
    public GameStage gameStage = GameStage.Lobby;
    protected List<float> score;
    protected List<int> deaths;
    protected List<int> kills;
    protected List<KillPair> killHistory;
    public float matchTime;
    protected List<GoalPoint>[] teamGoals;
    


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

    #region InformPlayers

    public void BroadcastMessageToAllConnected(string message, float duration, Color color, float fadeIn = 0.5f, float fadeOut = 1f)
    {
        foreach(Player p in players)
        {
            p.RpcDisplayMessage(message, duration, color, fadeIn, fadeOut);
        }
        foreach(Player p in spectators)
        {
            p.RpcDisplayMessage(message, duration, color, fadeIn, fadeOut);
        }
    }

    public void BroadcastMessageToTeam(int team, string message, float duration, Color color, float fadeIn = 0.5f, float fadeOut = 1f)
    {
        foreach(Player p in teamPlayers[team])
        {
            p.RpcDisplayMessage(message, duration, color, fadeIn, fadeOut);
        }
    }

    public void BroadcastMessageToPlayers(string message, float duration, Color color, float fadeIn = 0.5f, float fadeOut = 1f)
    {
        foreach(Player p in players)
        {
            p.RpcDisplayMessage(message, duration, color, fadeIn, fadeOut);
        }
    }

    public void BroadcastMessageToSpectators(string message, float duration, Color color, float fadeIn = 0.5f, float fadeOut = 1f)
    {
        foreach(Player p in spectators)
        {
            p.RpcDisplayMessage(message, duration, color, fadeIn, fadeOut);
        }
    }

    public void PlayClipToAllPlayers(AudioManager.SoundClips clip)
    {
        AudioManager.instance.RpcPlayClip(clip);
    }

    public void PlayClipToTeam(int team, AudioManager.SoundClips clip)
    {
        foreach (var player in teamPlayers[team])
        {
            if(!player.isAI)
            {
                AudioManager.instance.TargetPlayClip(player.connectionToClient, clip);
            }
        }
    }

    #endregion

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

        if(AudioManager.instance != null)
            AudioManager.instance.RpcReduceVolume(volumeInMatch, matchSettings.timeToSetup * 0.5f);

    }

    protected void EndMatchSetup()
    {
        AssignPlayersToTanks();

        matchTime = 0;
        gameStage = GameStage.Match;
        GameStatus.instance.RpcStartCounter(matchSettings.maxTime);
        
        BroadcastMessageToAllConnected("Match has started!", 2f, defaultMessageColor);

        PrepareGoal();
    }

    public virtual void PrepareGoal(){}

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
        // Goals references for each team
        teamGoals = new List<GoalPoint>[matchConfiguration.matchSetting.numTeams];
        for (int i = 0; i < matchConfiguration.matchSetting.numTeams; i++) teamGoals[i] = new List<GoalPoint>();

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
        tankRef.SetTankNameAndSkin(tankInfo.name, tankInfo.showName, tankInfo.skin);
        
        // Set tank forced parametters
        if(forceTankParameters != null)
        {
            tankRef.SetTankParameters(forceTankParameters.tankParameters);
        }

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

                playerRef.ObservePosition(tanks[playerInfo.tankID].transform.position,10,45,10);
                playerRef.RpcDisplayMessage("You are on Team " + teamAliases[playerInfo.team] + " with role " + playerInfo.role.ToString(),
                timeToStartGame/2, defaultMessageColor, 0.5f, 1f);
            }
        }

        //First will look for tanks that don't have assignments
        if(assingAIToMissingTanks)
        {
            var infoTanks = MatchConfiguration.instance.infoTanks;
            for (int i = 0; i < infoTanks.Count; i++)
            {
                bool hasAssigments = false;
                for (int j = 0; j < infoTanks[i].assigments.Length; j++)
                {
                    if(infoTanks[i].assigments[j].playerAssigned != -1)
                    {
                        hasAssigments = true;
                        break;
                    }

                }
                // Spawn AI
                if(!hasAssigments)
                {
                    GameObject aiPlayer = GameObject.Instantiate(AIPlayerPrefab);
                    Player playerRef = aiPlayer.GetComponent<Player>();
                    NetworkServer.Spawn(aiPlayer);

                    players.Add(playerRef);

                    PlayerInfo playerInfo = new PlayerInfo(-1, "BOT");
                    playerInfo.team = infoTanks[i].team;
                    playerInfo.role = Role.Pilot;
                    playerInfo.tankID = infoTanks[i].id;
                    playerInfo.roleIndex = 0;

                    playerRef.team = playerInfo.team;
                    playerRef.role = playerInfo.role;
                    playerRef.playerInfo = playerInfo;
                    playerRef.isAI = true;


                    if(teamPlayers[playerInfo.team] == null) teamPlayers[playerInfo.team] = new List<Player>();
                        teamPlayers[playerInfo.team].Add(playerRef);
                }
            }

        }
    }

    protected void AssignPlayerToSpectator(Player player)
    {
        if(player != null)
        {
            if(!spectators.Contains(player))
                spectators.Add(player);
            player.ObservePosition(spectatorPosition.position, 0, 90f, spectatorDistance);
            player.AssignSpectator();
            player.RpcDisplayMessage("You joined as spectator!", 2f, defaultMessageColor, 0.25f, 1f);
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
            Tank toAssing = tanks[player.playerInfo.tankID];
            toAssing.AssignPlayer(player, player.playerInfo.role);
            player.AssignPlayer(player.playerInfo.team, player.playerInfo.role, toAssing.GetComponent<NetworkIdentity>());
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

    public void NotifyPlayerLeft(Player player,PlayerInfo playerInfo)
    {
        BroadcastMessageToAllConnected("Player " + playerInfo.name + " has left!", 2,defaultMessageColor);

        players.Remove(player);
        teamPlayers[playerInfo.team].Remove(player);
    }
    
    public void TankKilled(int ownerId, int enemyId) {
        bool hasSuicided = ownerId == enemyId;

        if(!hasSuicided)
        {
            kills[enemyId]++;
            GameStatus.instance.kills[enemyId]++;
        }
        else
        {
            kills[enemyId]--;
            GameStatus.instance.kills[enemyId]--;
        }

        deaths[ownerId]++;
        GameStatus.instance.deaths[ownerId]++;
        KillPair newKill = new KillPair(enemyId,ownerId,matchTime);
        killHistory.Add(newKill);
        GameStatus.instance.killHistory.Add(newKill);

        if(!hasSuicided)
        {
            BroadcastMessageToAllConnected("Tank " + tanks[ownerId].tankName + " was killed by Tank " + tanks[enemyId].tankName, 2f,defaultMessageColor);
        }
        else
        {
            BroadcastMessageToAllConnected("Tank " + tanks[ownerId].tankName + " killed itself!", 2f,defaultMessageColor);
        }

        ResetTank(ownerId);
        
        if(updateScoreInkill)
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
            if(count > score[i])
            {
                PlayClipToTeam(i, AudioManager.SoundClips.IncrementPoint);
            }
            else if(count < score[i])
            {
                PlayClipToTeam(i, AudioManager.SoundClips.DecrementPoint);
            }
            score[i] = count;
            GameStatus.instance.score[i] = count;
        }
        CheckWinCondition();
    }

    

    public void ResetTank(int tankId){
        Tank tankToReset = tanks[tankId];

        tankToReset.SetCanMove(false);

        int assigmentId = -1;
        foreach(var assigment in tankToReset.playerRoles)
        {
            assigmentId++;
            Player player = assigment.playerRef;
            if(player == null) continue;

            player.playerInfo.role = assigment.role;
            player.playerInfo.roleIndex = assigmentId;
            player.RemoveOwnership();
            player.ObservePosition(tankToReset.transform.position, 20, 45, 5);
            StartCoroutine(waitToAssignBack(matchSettings.timeToRespawn, player));
        }

        tankToReset.ClearPlayerAssigments();
        tankToReset.GetComponent<Rigidbody>().isKinematic = true;
        tankToReset.transform.position = hideTankPosition.position;
        
        StartCoroutine(WaitToRespawnTank(matchSettings.timeToRespawn-0.25f, tankToReset));

    }


    public Tank GetTank(int id) {
        if(tanks == null) {
            Debug.LogWarning("Couldn't find tank array");
            return null;
        }
        return tanks[id];
    }

    private IEnumerator WaitToRespawnTank(float time, Tank tank)
    {
        float counter = time;
        while(counter > 0) {
            counter -= Time.deltaTime;
            yield return null;
        }

        Transform positionToSpawn = GetSpawnPosition(tank.team);

        tank.GetComponent<Rigidbody>().isKinematic = false;
        tank.ResetTankPosition(positionToSpawn);
        tank.SetCanMove(true);
    }

    private IEnumerator waitToAssignBack(float time, Player toAssing) {
        float counter = time;
        while(counter > 0) {
            counter -= Time.deltaTime;
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
                highestScore = score[i];
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
        if(gameStage == GameStage.End) return;
        gameStage = GameStage.End;
        float finalTime = (matchSettings.maxTime == Mathf.Infinity) ? Mathf.Infinity : matchSettings.maxTime - matchTime;
        GameStatus.instance.RpcStopCounter(finalTime);
        
        string winningString = teamAliases[winningTeams[0]];
        for (int i = 1; i < winningTeams.Count; i++)
        {
            winningString += " & " + teamAliases[winningTeams[i]];
        }

        for(int i = 0; i < teamPlayers.Length; i ++){
            if(teamPlayers[i] != null){

                foreach(Player p in teamPlayers[i])
                {
                    // Play end music
                    // p.RpcPlayVictoryMusic();
                    PlayClipToAllPlayers(AudioManager.SoundClips.Victory);

                    if(!hasDraw)
                    {
                        if(winningTeams.Contains(i)){
                            p.RpcDisplayMessage("Outstanding performance, comrades! We have won this battle!", 10, defaultMessageColor, 0.1f, 1);
                        }
                        else
                            p.RpcDisplayMessage("A shameful display! " + winningString + " has beaten us this time!"
                            , 10f, defaultMessageColor, 0.1f, 1);
                    }
                    else
                    {
                        p.RpcDisplayMessage("I can't believe it, a Draw! Nice work teams " + winningString + "!", 10, defaultMessageColor, 0.1f, 1);
                    }
                }
            }
        }

        //Spectators
        for (int i = 0; i < spectators.Count; i++)
        {
            if(spectators[i] != null)
            {
                spectators[i].RpcDisplayMessage("Nice work teams " + winningString + "!", 10, defaultMessageColor, 0.1f, 1);
                spectators[i].RpcPlayVictoryMusic();
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
            NetworkManager.singleton.ServerChangeScene(MatchConfiguration.instance.mapOption.scene);
        else
            NetworkManager.singleton.StopHost();
    }


    #endregion

    #region Goal

    public void ClearAllTeamGoals()
    {
        for (int i = 0; i < matchSettings.numTeams; i++)
        {
            ClearTeamGoals(i);
        }
    }

    public void ClearTeamGoals(int team)
    {
        for (int i = teamGoals[team].Count-1; i > -1; i--)
        {
            NetworkServer.Destroy(teamGoals[team][i].gameObject);
            teamGoals[team].RemoveAt(i);

        }
        //Update game status
        if(team == 0)
            GameStatus.instance.goalIdentitiesTeam0.Clear();
        else
            GameStatus.instance.goalIdentitiesTeam1.Clear();

    }

    public void AddTeamGoal(int team, Vector3 target)
    {
        GameObject newGoal = GameObject.Instantiate(goalPointPrefab);
        GoalPoint goalScript = newGoal.GetComponent<GoalPoint>();
        goalScript.SetTarget(target);
        NetworkServer.Spawn(newGoal);

        teamGoals[team].Add(goalScript);

        //Update Game Status
        NetworkIdentity ni = newGoal.GetComponent<NetworkIdentity>();
        if(team == 0)
            GameStatus.instance.goalIdentitiesTeam0.Add(ni);
        else if(team == 1)
            GameStatus.instance.goalIdentitiesTeam1.Add(ni);

    }

    public void AddTeamGoal(int team, NetworkIdentity target)
    {
        GameObject newGoal = GameObject.Instantiate(goalPointPrefab);
        GoalPoint goalScript = newGoal.GetComponent<GoalPoint>();
        goalScript.SetTarget(target);
        NetworkServer.Spawn(newGoal);

        teamGoals[team].Add(goalScript);
        
        //Update Game Status
        NetworkIdentity ni = newGoal.GetComponent<NetworkIdentity>();
        if(team == 0)
            GameStatus.instance.goalIdentitiesTeam0.Add(ni);
        else if(team == 1)
            GameStatus.instance.goalIdentitiesTeam1.Add(ni);
    }

    public void SetTeamGoal(int team, Vector3 target, int id = 0)
    {
        // Verify bounds
        if(team >= teamGoals.Length)
        {
            Debug.LogError("Team " + team + " is not in the array!");
            return;    
        }

        if(id < teamGoals[team].Count)
        {
            teamGoals[team][id].SetTarget(target);
        }
        else
        {
            Debug.LogError("Target " + id + " does not exist on team " + team);
        }
    }

    public void SetTeamGoal(int team, NetworkIdentity target,int id = 0)
    {
        // Verify bounds
        if(team >= teamGoals.Length)
        {
            Debug.LogError("Team " + team + " is not in the array!");
            return;    
        }
        
        if(id < teamGoals[team].Count)
        {
            teamGoals[team][id].SetTarget(target);
        }
        else
        {
            Debug.LogError("Target " + id + " does not exist on team " + team);
        }
    }

    public void SetTeamGoalColor(int team, Color newColor, int id=0)
    {
        // Verify bounds
        if(team >= teamGoals.Length)
        {
            Debug.LogError("Team " + team + " is not in the array!");
            return;    
        }
        
        if(id < teamGoals[team].Count)
        {
            teamGoals[team][id].SetColor(newColor);
        }
        else
        {
            Debug.LogError("Target " + id + " does not exist on team " + team);
        }
    }

    #endregion


    #region DEBUG

    #if UNITY_EDITOR

    [MenuItem("Debug/Kill tank 0")]
    public static void KillTank0()
    {
        instance.TankKilled(0,1);
    }

    [MenuItem("Debug/Change Team")]
    public static void ChangePlayerTeam()
    {
        Player p = instance.players[0];
        int otherTeam = (p.team == 0) ? 1 : 0;
        p.RemoveOwnership();
        Tank toSet = instance.GetTank(otherTeam);
        p.AssignPlayer(otherTeam,Role.Pilot,toSet.GetComponent<NetworkIdentity>());
    }

    [MenuItem("Debug/Kill tank 1")]
    public static void KillTank1()
    {
        instance.TankKilled(1,0);
    }

    [MenuItem("Debug/Set goal to 0")]
    public static void SetGoalTo0()
    {
        instance.AddTeamGoal(0, Vector3.zero);
        instance.SetTeamGoalColor(0,Color.blue,0);
    }

    [MenuItem("Debug/Set goal to Tank1")]
    public static void SetGoalToTank1()
    {
        instance.AddTeamGoal(0, instance.GetTank(1).GetComponent<NetworkIdentity>());
        instance.SetTeamGoalColor(0,Color.red,0);
    }

    [MenuItem("Debug/Start game")]
    public static void StartGame()
    {
        instance.StartMatchSetup();
        if(LobbyManager.instance != null)
        {
            LobbyManager.instance.HideLobbyForAllPlayers();
        }
    }
    

    #endif

    #endregion
}
