using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class GameMode : NetworkBehaviour
{
    [Header("Game settings")]
    public int numTeams = 2;
    public Color[] teamColors = {new Color(1,0.3820755f,0.9357688f)};
    public int numberOfPlayersToStartGame = 2;
    public float timeToStartGame = 5;

    [Header("Prefabs")]
    public GameObject tankPrefab;

    [Header("References")]
    public Tank[] tanks;
    public List<Player> players;
    protected SpawnPoint[] spawnPoints;
    protected bool tanksSpawned;

    [Header("Game State")]
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
    }

    void Start(){
        if(isServer) {
            for(int i = 0; i < numTeams; i++) {
                score.Insert(i,0);
                deaths.Insert(i,0);
                kills.Insert(i,0);
            }

            Debug.Log("List: " + score);

        }
    }

    protected void spawnTanks(){
        spawnPoints = GameObject.FindObjectsOfType<SpawnPoint>();
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

    public virtual void updateScore(){
        for(int i = 0; i < numTeams; i++) {
            score[i] = kills[i];
        }
    }

    public void ResetTank(int team){
        Tank tankToReset = tanks[team];

        Transform positionToSpawn = spawnPoints[Random.Range(0,spawnPoints.Length)].transform;

        tankToReset.ResetTankPosition(positionToSpawn.position);

    }

    public void setPlayerReference(Player player) {
        players.Add(player);
        score.Callback += player.ScoreCallBack;

        if(isServer){
            connectedNumberOfClients++;
            if(connectedNumberOfClients > numberOfPlayersToStartGame && !gameHasStarted){
                StartCountDown();
            }
            player.RpcObservePosition(Vector3.zero,10);
        }

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

    public void startGame(){
        Debug.Log("Starting game");
        assignPlayers();
        
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
            int playerTeam = id/2;
            Player.Role role = (id % 2 == 0) ? Player.Role.Pilot : Player.Role.Gunner; 

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

    private IEnumerator waitToAssignBack(float time, NetworkConnection toAssing) {
        currentCountdown = time;
        while(currentCountdown > 0) {
            currentCountdown -= Time.deltaTime;
            yield return null;
        }

        assignPlayer(toAssing);
    }
}
