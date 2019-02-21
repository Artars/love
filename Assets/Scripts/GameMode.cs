using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Collections;

public class GameMode : NetworkedBehaviour
{
    public int numTeams = 2;
    public GameObject tankPrefab;
    protected Tank[] tanks;
    protected SpawnPoint[] spawnPoints;
    protected bool tanksSpawned;

    public static GameMode instance = null;


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
        NetworkedObject playerObj = NetworkingManager.singleton.ConnectedClients[client].PlayerObject;
        Player playerScript = playerObj.GetComponent<Player>();
        if(playerScript != null) {
            assingPlayer(playerScript);
        }
    }

    protected void Start() {
        if(instance == null) instance = this;
            else if(instance != this) Destroy(gameObject);

        if(isServer){

            tanks = new Tank[numTeams];
            spawnPoints = GameObject.FindObjectsOfType<SpawnPoint>();
            for(int i= 0; i < numTeams; i++) {
                spawnTank(spawnPoints[i].transform, i);
            }

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
    }

    protected void spawnTank(Transform position, int team){
        GameObject tank = GameObject.Instantiate(tankPrefab,position.position,Quaternion.identity);
        tank.GetComponent<NetworkedObject>().Spawn();
        Tank tankRef = tank.GetComponent<Tank>();
        tankRef.team.Value = team;
        tanks[team] = tankRef;
    }

    public void assingPlayer(Player player) {
        Debug.Log("Player " + player.OwnerClientId + " tried to assign");
        uint playerId = player.OwnerClientId;
        if(!tankAssigned) {
            player.possesTank(tanks[0]);
            tankAssigned = true;
        } else {
            player.possesTank(tanks[1]);
        }
        

    }


    public void test(){
        Debug.Log("Reached");
    }
}
