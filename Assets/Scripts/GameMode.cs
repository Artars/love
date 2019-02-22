using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Collections;

public class GameMode : NetworkedBehaviour
{
    public int numTeams = 2;
    public int numberOfPlayersToStartGame = 2;
    public float timeToStartGame = 5;
    public GameObject tankPrefab;
    public Tank[] tanks;
    protected SpawnPoint[] spawnPoints;
    protected bool tanksSpawned;

    public static GameMode instance = null;

    protected float currentCountdown;
    protected int connectedNumberOfClients = 1;

    //Debug
    bool tankAssigned = false;

    public override void OnEnabled() {
        if(isServer)
            NetworkingManager.singleton.OnClientConnectedCallback += OnClientConnect;

        base.OnEnabled();
    }

    public override void OnDestroyed() {
        if(isServer)
            NetworkingManager.singleton.OnClientConnectedCallback -= OnClientConnect;

        base.OnDestroyed();
    }


    protected void OnClientConnect(uint client) {
        if(isHost) {
            Debug.Log("Logged :" + client);
            connectedNumberOfClients += 1;
            if(connectedNumberOfClients >= numberOfPlayersToStartGame){
                StartCountDown();
            }
            // NetworkedObject playerObj = NetworkingManager.singleton.ConnectedClients[client].PlayerObject;
            // Player playerScript = playerObj.GetComponent<Player>();
            // if(playerScript != null) {
            //     playerScript.InvokeClientRpcOnOwner("observeTransform",0,0,0, "Reliable", MLAPI.Data.SecuritySendFlags.None);
            // }
        }
    }

    protected void Awake(){
        if(instance == null) instance = this;
            else if(instance != this) Destroy(gameObject);
        tanks = new Tank[numTeams];
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
        NetworkedObject noTank = tank.GetComponent<NetworkedObject>();
        noTank.Spawn();
        Tank tankRef = tank.GetComponent<Tank>();
        tankRef.team.Value = team;
        tanks[team] = tankRef;

        tankRef.InvokeClientRpcOnEveryone("updateTankReferenceRPC", team);
    }

    public void assingPlayer(Player player) {
        Debug.Log("Player " + player.OwnerClientId + " tried to assign");
        uint playerId = player.OwnerClientId;
        if(!tankAssigned) {
            player.possesTank(tanks[0], Player.Role.Pilot);
            tankAssigned = true;
        } else {
            player.possesTank(tanks[0], Player.Role.Gunner);
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

        //Assing all players
        foreach(MLAPI.Data.NetworkedClient client in  NetworkingManager.singleton.ConnectedClientsList) {
            NetworkedObject networkedObj = client.PlayerObject;
            if(networkedObj != null) {
                Player script = networkedObj.GetComponent<Player>();

                if(script != null) {
                    assingPlayer(script);
                } else Debug.Log("Player script is null");

            } else Debug.Log("Couldn't get player obj");
        }
    }


    public void updateAllTankReferences() {
        foreach(Tank t in tanks) {
            t.InvokeClientRpcOnEveryone("updateTankReferenceRPC", t.team.Value, "Reliable", MLAPI.Data.SecuritySendFlags.None);
        }
    }

    public void makeUsersObservePoint() {
        foreach(MLAPI.Data.NetworkedClient client in  NetworkingManager.singleton.ConnectedClientsList) {
            NetworkedObject networkedObj = client.PlayerObject;
            if(networkedObj != null) {
                Player script = networkedObj.GetComponent<Player>();
                script.InvokeClientRpcOnEveryone("observePositionRPC",0f,0f,0f);
            }
        }

    }

    private void StartCountDown() {
        Debug.Log("Game will start in "+ timeToStartGame + " seconds");
        StartCoroutine(doCountdown(timeToStartGame));
        spawnTanks();
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
