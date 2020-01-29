using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MatchConfiguration : MonoBehaviour
{
    public static MatchConfiguration instance {
        get {
            if(m_instance == null){
                CreateInstance();
            }
            return m_instance;
        }
        set {}
    }

    protected static MatchConfiguration m_instance = null;

    [Header("Match Configuration")]
    public MapOption mapOption;
    public MatchSetting matchSetting;

    [Header("Match Assignment")]
    public List<InfoTank> infoTanks;
    public DictionaryIntPlayerInfo playersInfo;

    public int numTanks
    {
        get {
            return infoTanks.Count;
        }
    }

    public int numPlayers
    {
        get {
            return GetNumberValidPlayers();
        }
    }

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
        matchSetting = new MatchSetting();
    }

    public static void CreateInstance() {
        GameObject instanceHolder = new GameObject("MatchSettings");
        MatchConfiguration newInstance = instanceHolder.AddComponent<MatchConfiguration>();
        instanceHolder.transform.SetParent(null);
        DontDestroyOnLoad(instanceHolder);
    }

    public void ClearConfiguration() {
        infoTanks.Clear();
        playersInfo.Clear();
    }

    public int GetNumberValidPlayers()
    {
        int total = 0;
        foreach(KeyValuePair<int,PlayerInfo> player in playersInfo)
        {
            if(player.Value.role != Role.None && player.Value.tankID != -1)
            {
                total++;
            }
        }
        return total;
    }

    public List<int> GetListValidPlayers()
    {
        List<int> result = new List<int>();
        foreach(KeyValuePair<int,PlayerInfo> player in playersInfo)
        {
            if(player.Value.role != Role.None && player.Value.tankID != -1)
            {
                result.Add(player.Key);
            }
        }
        return result;
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
    public string name;
    public int skin;
    public bool showName;
    public RoleAssigment[] assigments;

    public InfoTank(int id, int team, int numPlaces, int prefabID) {
        this.id = id;
        this.team = team;
        this.name = "";
        this.skin = 0;
        this.showName = true;
        assigments = new RoleAssigment[numPlaces];
        this.prefabID = prefabID;
    }

    public InfoTank(int id, int team, TankOption tankOption) {
        this.id = id;
        this.team = team;
        this.name = tankOption.defaultNames[UnityEngine.Random.Range(0,tankOption.defaultNames.Length)];
        this.skin = 0;
        this.showName = true;
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

    public bool HasAssigment()
    {
        return tankID != -1 && role != Role.None && roleIndex != -1;
    }

}

[Serializable]
public struct MatchSetting 
{
    public int[] teamConfiguration;
    public int numTeams;
    public int maxPoints;
    public float maxTime;
    public float timeToSetup;
    public float timeToRespawn;
    public ushort serverPort;

    public MatchSetting( 
    int numTeams = 2, 
    int maxPoints = 5, 
    float maxTime = Mathf.Infinity,
    float timeToRespawn = 4f, 
    float timeToSetup = 4f,
    ushort serverPort = 7777)
    {
        this.teamConfiguration = new int[numTeams];
        for (int i = 0; i < numTeams; i++)
        {
            teamConfiguration[i] = 1;
        }
        this.numTeams = numTeams;
        this.maxPoints = maxPoints;
        this.maxTime = maxTime;
        this.timeToSetup = timeToSetup;
        this.timeToRespawn = timeToRespawn;
        this.serverPort = serverPort;
    }


}