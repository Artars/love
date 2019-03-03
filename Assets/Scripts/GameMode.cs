using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class GameMode : NetworkBehaviour
{
    public int numTeams = 2;
    public int numberOfPlayersToStartGame = 2;
    public float timeToStartGame = 5;
    public GameObject tankPrefab;
    public GameObject cannonPrefab;
    public Tank[] tanks;
    public Cannon[] cannons;
    protected SpawnPoint[] spawnPoints;
    protected bool tanksSpawned;

    [SyncVar]
    public bool gameHasStarted;

    public List<Player> players;

    public static GameMode instance = null;

    protected float currentCountdown;
    protected int connectedNumberOfClients = 1;

    //Debug
    bool tankAssigned = false;

    protected void Awake(){
        if(instance == null) instance = this;
            else if(instance != this) Destroy(gameObject);
        tanks = new Tank[numTeams];
        cannons = new Cannon[numTeams];
        players = new List<Player>();
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

    protected void spawnTank(Transform position, int team){
        GameObject tank = GameObject.Instantiate(tankPrefab,position.position,Quaternion.identity);
        Tank tankRef = tank.GetComponent<Tank>();
        tankRef.team = team;
        tanks[team] = tankRef;
        tankRef.ResetTank();
        NetworkServer.Spawn(tank);

        tankRef.RpcUpdateTankReferenceRPC(team);

        GameObject cannon = GameObject.Instantiate(cannonPrefab, tankRef.rotationPivot);
        Cannon cannonScript = cannon.GetComponent<Cannon>();
        cannonScript.team = team;
        cannonScript.tankIdentity = tank.GetComponent<NetworkIdentity>();
        cannons[team] = cannonScript;

        NetworkServer.Spawn(cannon);

    }

    public void setPlayerReference(Player player) {
        players.Add(player);
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
                int id = pair.Key - 1;
                if(id < 0) id = 0; //Fix host

                Player player = pair.Value.playerController.GetComponent<Player>();
                if(player != null) {
                    int playerTeam = id/2;
                    Player.Role role = (id % 2 == 0) ? Player.Role.Pilot : Player.Role.Gunner; 

                    NetworkIdentity toPosses = role == Player.Role.Pilot ? 
                    tanks[playerTeam].GetComponent<NetworkIdentity>() : cannons[playerTeam].GetComponent<NetworkIdentity>();

                    toPosses.AssignClientAuthority(pair.Value);
                    player.RpcAssignPlayer(playerTeam, role, toPosses);
                }
            }
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
}
