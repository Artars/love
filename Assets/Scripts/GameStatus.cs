using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class GameStatus : NetworkBehaviour
{
    public static GameStatus instance = null;

    public SyncListInt score;
    public SyncListInt deaths;
    public SyncListInt kills;
    public SyncList<GameMode.KillPair> killHistory;


    [Header("Game settings")]
    [SyncVar]
    public MatchSetting MatchSetting;

    public void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else if(instance != this)
        {
            Destroy(this);
        }

        score = new SyncListInt();
        deaths = new SyncListInt();
        kills = new SyncListInt();
        killHistory = new SyncListSTRUCT<GameMode.KillPair>();
    }

    public void Setup(MatchSetting MatchSetting)
    {
        this.MatchSetting = MatchSetting;
        score.Clear();
        deaths.Clear();
        kills.Clear();
        killHistory.Clear();

        for (int i = 0; i < MatchSetting.numTeams; i++)
        {
            score.Add(0);
        }
        for (int i = 0; i < MatchConfiguration.instance.numTanks; i++)
        {
            deaths.Add(0);
            kills.Add(0);
        }
    }
    

}
