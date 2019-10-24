using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Clase de autoridade do servidor para mostrar status de jogo
/// </summary>
public class GameStatus : NetworkBehaviour
{
    public static GameStatus instance = null;

    public SyncListInt score;
    public SyncListInt deaths;
    public SyncListInt kills;
    // public SyncList<GameMode.KillPair> killHistory;

    protected TMPro.TMP_Text timeText;
    protected bool timeRunning = false;
    protected float timeCounter = 0;


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

        // score = new SyncListInt();
        // deaths = new SyncListInt();
        // kills = new SyncListInt();
        // killHistory = new GameMode.SyncListKillPair();
    }

    public void Setup(MatchSetting MatchSetting)
    {
        this.MatchSetting = MatchSetting;
        score.Clear();
        deaths.Clear();
        kills.Clear();
        // killHistory.Clear();

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

    public void Update()
    {
        if(timeRunning && timeCounter != Mathf.Infinity)
        {
            timeCounter -= Time.deltaTime;
            if(timeText != null)
            {
                timeText.text = TimeToString(timeCounter);
            }
        }
    }

    [ClientRpc]
    public void RpcStartCounter(float maxTime)
    {
        timeRunning = true;
        timeCounter = maxTime;

        if(timeText != null)
            timeText.text = TimeToString(timeCounter);
    }

    [ClientRpc]
    public void RpcStopCounter(float finalTime)
    {
        timeRunning = false;
        timeCounter = finalTime;

        if(timeText != null)
            timeText.text = TimeToString(timeCounter);
    }


    public void SetTimeText(TMPro.TMP_Text textRef)
    {
        timeText = textRef;
        if(timeRunning)
            timeText.text = TimeToString(timeCounter);

    }

    protected string TimeToString(float time)
    {
        if(time == Mathf.Infinity)
            return "--:--";
        
        if(time < 0)
            time = 0;

        int min = Mathf.FloorToInt(time / 60);
        int sec = Mathf.FloorToInt(time-min*60);

        return min.ToString("D2") + ":" + sec.ToString("D2");
    }

    public string GetCurrentScore(int team = 0)
    {
        if(team < 0 || team >= score.Count)
        {
            return "00x00";
        }
        else
        {
            string newText = (MatchSetting.maxPoints - score[team]).ToString("D2");
            for(int i = 0; i < score.Count; i++) {
                if(i != team){
                    newText += "x" + (MatchSetting.maxPoints - score[i]).ToString("D2"); 
                }
            }
            return newText;
        }
        
    }
    

}
