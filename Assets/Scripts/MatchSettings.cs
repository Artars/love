using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public int connectedPlayers = 0;

    public List<LobbyManager.InfoTank> infoTanks;
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

        infoTanks = new List<LobbyManager.InfoTank>();
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
