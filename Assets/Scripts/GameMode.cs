using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class GameMode : NetworkBehaviour
{
    [Header("Game settings")]
    [SyncVar]
    public int numTeams = 2;
    public Color[] teamColors = {new Color(1,0.3820755f,0.9357688f)};
    [SyncVar]
    public float timeOutTime = 5;
    [SyncVar]
    public int numberOfPlayersToStartGame = 2;
    [SyncVar]
    public float timeToStartGame = 5;
    [SyncVar]
    public float timeToEndGame = 5;
    [SyncVar]
    public bool returnToLobby = true;
    [SyncVar]
    public int maxScore = 5;

    public List<LobbyManager.InfoTank> infoTanks;
    public DictionaryIntPlayerInfo playersInfo;


    public bool startGameOnCommand = false;

    [Header("Prefabs")]
    public GameObject tankPrefab;

    [Header("References")]
    public Tank[] tanks;
    public List<Player> players;
    public Player localPlayer;
    public List<Player>[] teamPlayers;
    protected SpawnPoint[] spawnPoints;
    protected bool tanksSpawned;

    [Header("Game State")]
    [SyncVar]
    public string hostIP;
    [SyncVar]
    public bool gameHasStarted;
    public SyncListInt score;
    public SyncListInt deaths;
    public SyncListInt kills;
    



    protected float currentCountdown;
    protected int connectedNumberOfClients = 1;

    public static GameMode instance = null;

    protected void Awake(){
        if(instance == null) instance = this;
            else if(instance != this) Destroy(gameObject);
        tanks = new Tank[numTeams];
        players = new List<Player>();
        teamPlayers = new List<Player>[numTeams];
    }

    void Start(){
        spawnPoints = GameObject.FindObjectsOfType<SpawnPoint>();
        if(isServer) {
            for(int i = 0; i < numTeams; i++) {
                score.Insert(i,0);
                deaths.Insert(i,0);
                kills.Insert(i,0);
            }

            Debug.Log("List: " + score);
            hostIP = getIPString();

            numberOfPlayersToStartGame = MatchSettings.instance.connectedPlayers;
            playersInfo = MatchSettings.instance.playersInfo;
            infoTanks = MatchSettings.instance.infoTanks;

            numTeams = infoTanks.Count;

        }
    }

    void Update() {
        if(!isServer) return;
        if(startGameOnCommand && !gameHasStarted && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))){
            StartCountDown();
        }
        timeOutTime -= Time.deltaTime;

        if(!gameHasStarted && timeOutTime < 0) {
            StartCountDown();
        }
    }

    #region Tank

    protected void spawnTanks(){
        int index = 0;
        if(spawnPoints.Length < 1){
            Debug.LogWarning("NO SPAWN POINTS");
            return;
        }

        for(int i= 0; i < numTeams; i++) {
            spawnTank(spawnPoints[index % spawnPoints.Length].transform, i);
            index++;
        }
    }

    /// <summary>
    /// Should be called by the server to spawn a tank of a given team in the right position
    /// </summary>
    /// <param name="position"></param>
    /// <param name="team"></param>
    protected void spawnTank(Transform position, int team){
        GameObject tank = GameObject.Instantiate(tankPrefab,position.position,Quaternion.identity);
        Tank tankRef = tank.GetComponent<Tank>();
        tankRef.team = team;
        tanks[team] = tankRef;
        tankRef.color = teamColors[team % teamColors.Length];
        tankRef.ApplyColor();
        tankRef.ResetTank();
        NetworkServer.Spawn(tank);

        tankRef.RpcUpdateTankReferenceRPC(team);
        tankRef.RpcSetColor(tankRef.color);
    }

    public void tankKilled(int ownerTeam, int oposingTeam) {
        kills[oposingTeam]++;
        deaths[ownerTeam]++;
        updateScore();

        ResetTank(ownerTeam);
    }

    public void ResetTank(int team){
        Tank tankToReset = tanks[team];

        Transform positionToSpawn = spawnPoints[Random.Range(0,spawnPoints.Length)].transform;

        tankToReset.ResetTankPosition(positionToSpawn.position);

    }

    public void setTankReference(Tank tank, int team) {
        tanks[team]  = tank;
    }

    public Tank getTank(int team) {
        if(tanks == null) {
            Debug.LogWarning("Couldn't find tank array");
            return null;
        }
        return tanks[team];
    }

    #endregion

    #region Player

    public void setPlayerReference(Player player) {
        players.Add(player);
        score.Callback += player.ScoreCallBack;
        
        int id;
        
        
        if(player.isLocalPlayer) {
            localPlayer = player;
        }

        if(isServer){
            id = player.connectionToClient.connectionId;
            int playerTeam = playersInfo[id].team;
            Player.Role role = (Player.Role)((int)playersInfo[id].role - 1); //Roles from lobby start from 1

            player.team = playerTeam;
            player.role = role;

            if(teamPlayers[playerTeam] == null) teamPlayers[playerTeam] = new List<Player>();
                teamPlayers[playerTeam].Add(player);

            connectedNumberOfClients++;
            if(connectedNumberOfClients > numberOfPlayersToStartGame && !gameHasStarted){
                StartCountDown();
            }

            player.RpcObservePosition(spawnPoints[playerTeam % spawnPoints.Length].transform.position,10);
            player.RpcDisplayMessage("You are on Team " + (playerTeam+1) + " with role " + role.ToString(),timeToStartGame/2, 0.5f, 1f);
            player.RpcShowHostIp(hostIP);
        }

    }

    public void assignPlayers() {
        if(isServer){
            Debug.Log("Trying to assing");
            foreach (KeyValuePair<int, NetworkConnection> pair in NetworkServer.connections){
                Debug.Log("Connected: " + pair.Key);
                assignPlayer(pair.Value);
            }
        }
    }

    protected void assignPlayer(NetworkConnection connection) {
        int id = connection.connectionId - 1;
        if(id < 0) id = 0; //Fix host

        Player player = connection.playerController.GetComponent<Player>();
        if(player != null) {
            int playerTeam = player.team;
            Player.Role role = player.role; 

            NetworkIdentity toPosses = tanks[playerTeam].GetComponent<NetworkIdentity>();
            // NetworkIdentity toPosses = role == Player.Role.Pilot ? 
            // tanks[playerTeam].GetComponent<NetworkIdentity>() : cannons[playerTeam].GetComponent<NetworkIdentity>();

            // toPosses.AssignClientAuthority(connection);

            player.SetTankReference(tanks[playerTeam], playerTeam, role);
            player.RpcAssignPlayer(playerTeam, role, toPosses);
        }
    }

    public void makeUsersObservePoint(Vector3 position, float speed) {
        if(isServer){
            foreach(Player p in players) {
                p.RpcObservePosition(position, speed);
            }
        }

    }

    private IEnumerator waitToAssignBack(float time, NetworkConnection toAssing) {
        currentCountdown = time;
        while(currentCountdown > 0) {
            currentCountdown -= Time.deltaTime;
            yield return null;
        }

        assignPlayer(toAssing);
    }

    #endregion

    #region GameFlow

    private void StartCountDown() {
        Debug.Log("Game will start in "+ timeToStartGame + " seconds");
        StartCoroutine(doCountdown(timeToStartGame));
        spawnTanks();
        gameHasStarted = true;
        // makeUsersObservePoint();
    }

    private IEnumerator doCountdown(float time) {
        currentCountdown = time;
        while(currentCountdown > 0) {
            currentCountdown -= Time.deltaTime;
            yield return null;
        }

        startGame();
    }

    public void startGame(){
        Debug.Log("Starting game");
        assignPlayers();
    }

    public virtual void updateScore(){
        for(int i = 0; i < numTeams; i++) {
            score[i] = kills[i];
        }
        checkWinCondition();
    }

    public virtual void checkWinCondition() {
        for(int i = 0; i < numTeams; i++) {
            if(score[i] >= maxScore){
                endGame(i);
                break;
            }
        }
    }

    public void endGame(int winnerTeam){
        for(int i = 0; i < numTeams; i ++){
            if(teamPlayers[i] != null){
                foreach(Player p in teamPlayers[i])
                {
                    if(i == winnerTeam){
                        p.RpcDisplayMessage("You achieved love!", 10, 0.1f, 1);
                    }
                    else
                        p.RpcDisplayMessage("You tried your best, but the team " + winnerTeam + " achieved love and you don't"
                        , 10, 0.1f, 1);
                }
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
        // if(!returnToLobby)
        NetworkManager.singleton.StopHost();
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
