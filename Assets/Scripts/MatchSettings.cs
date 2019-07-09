using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MatchSettings : MonoBehaviour
{
    public static MatchSettings instance {
        get {
            if(m_instance == null){
                CreateInstance();
            }
            return m_instance;
        }
        set {}
    }

    protected static MatchSettings m_instance = null;


    public int numtanks = 2;
    public int numTeams = 2;
    public int connectedPlayers = 16;

    public List<InfoTank> infoTanks;
    public DictionaryIntPlayerInfo playersInfo;

    void Awake() {
        if(m_instance == null) {
            m_instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else if(m_instance != this) {
            Destroy(gameObject);
        }

        infoTanks = new List<InfoTank>();
        playersInfo = new DictionaryIntPlayerInfo();
    }

    public static void CreateInstance() {
        GameObject instanceHolder = new GameObject("MatchSettings");
        MatchSettings newInstance = instanceHolder.AddComponent<MatchSettings>();
        instanceHolder.transform.SetParent(null);
        DontDestroyOnLoad(instanceHolder);
    }

    public void ClearSettings() {
        infoTanks.Clear();
        playersInfo.Clear();
    }

}

/// <summary>
/// Class containing all necessary tank informations to setup a match
/// </summary>
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
            assigments[i].playerAssigned = -1;
        }
    }

    public List<int> GetValidPlayersID()
    {
        List<int> result = new List<int>();

        foreach (var assigment in assigments)
        {
            if(assigment.playerAssigned != -1)
            {
                result.Add(assigment.playerAssigned);
            }
        }

        return result;
    }

    public int GetNumberOfValidPlayers()
    {
        int sum = 0;

        foreach (var assigment in assigments)
        {
            if(assigment.playerAssigned != -1)
            {
                sum++;
            }
        }

        return sum;
    }

}

/// <summary>
/// Contains information how each role was assigned in the tank
/// </summary>
[Serializable]
public struct RoleAssigment {
    public Role role;
    public int playerAssigned;

}

/// <summary>
/// Contains information about a player connected and how he should be connected
/// </summary>
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