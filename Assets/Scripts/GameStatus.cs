using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Clase de autoridade do servidor para mostrar status de jogo
/// </summary>
public class GameStatus : NetworkBehaviour
{
    public enum TimeFormat
    {
        Decrement,Increment,None
    }

    public enum ScoreFormat
    {
        Decrement,Increment,Self_Point_Slash_Max
    }

    public class SyncListGoal : SyncList<NetworkIdentity> {};
    public class SyncListKillPair : SyncList<GameMode.KillPair> {};

    public static GameStatus instance = null;

    [SyncVar]
    public TimeFormat timeFormat = TimeFormat.Decrement;
    [SyncVar]
    public ScoreFormat scoreFormat = ScoreFormat.Decrement;

    public readonly SyncListInt score = new SyncListInt();
    public readonly SyncListInt deaths = new SyncListInt();
    public readonly SyncListInt kills = new SyncListInt();
    public readonly SyncListKillPair killHistory = new SyncListKillPair();
    public readonly SyncListGoal goalIdentitiesTeam0 = new SyncListGoal();
    public readonly SyncListGoal goalIdentitiesTeam1 = new SyncListGoal();

    protected TMPro.TMP_Text timeText;
    protected bool timeRunning = false;
    protected float timeCounter = 0;
    protected float maxTime;


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
        killHistory.Clear();
        goalIdentitiesTeam0.Clear();
        goalIdentitiesTeam1.Clear();

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
        if(timeRunning)
        {
            timeCounter += Time.deltaTime;
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
        timeCounter = 0;
        this.maxTime = maxTime;

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
        if(maxTime == Mathf.Infinity || timeFormat == TimeFormat.None)
            return "--:--";
        
        float useTime = time;
        if(timeFormat == TimeFormat.Decrement)
            useTime = maxTime - time;

        if(useTime < 0)
            useTime = 0;

        int min = Mathf.FloorToInt(useTime / 60);
        int sec = Mathf.FloorToInt(useTime-min*60);

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
            string newText;
            if(scoreFormat == ScoreFormat.Decrement)
            {
                newText = (MatchSetting.maxPoints - score[team]).ToString("D2");

                for(int i = 0; i < score.Count; i++) {
                    if(i != team){
                        newText += "x" + (MatchSetting.maxPoints - score[i]).ToString("D2"); 
                    }
                }
            }
            else if(scoreFormat == ScoreFormat.Increment)
            {
                newText = (score[team]).ToString("D2");

                for(int i = 0; i < score.Count; i++) {
                    if(i != team){
                        newText += "x" + (score[i]).ToString("D2"); 
                    }
                }
            }
            else if(scoreFormat == ScoreFormat.Self_Point_Slash_Max)
            {
                newText = (score[team]).ToString("D2") +"/" + MatchSetting.maxPoints.ToString("D2");
            }
            else
            {
                newText = "00x00";
            }
            return newText;
        }
        
    }
    

}
