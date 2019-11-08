using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEditor;

public class DominationGMode : GameMode
{
    [Header("References")]
    public GameObject DominationGoal;
    public int scoreBySecond = 1;

    protected new void Awake(){
        //Initialize singleton
        if(instance == null) 
            instance = this;
        else if(instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Update()
    {
        if(gameStage == GameStage.Match)
        {
            matchTime += Time.deltaTime;
            if(matchTime > matchSettings.maxTime)
            {
                MatchTimeEnded();
            }
        }
    }

    public override void PrepareGoal()
    {
        matchSettings.maxPoints *= 10;
        GameStatus.instance.MatchSetting.maxPoints *= 10;
        ClearAllTeamGoals();//preparing to add a new goal
        addAllTeamGoal(DominationGoal.transform.position);
    }

    public override void UpdateScore(){
        for(int i = 0; i < matchSettings.numTeams; i++){
            if (i == DominationGoal.GetComponent<DominationGoalPoint>().currentTeam)
            {
                if (score[i] < matchSettings.maxPoints)
                    score[i]+= scoreBySecond;
            }
            Debug.Log("o score do time" + i + "é " + score[i]);

            GameStatus.instance.score[i] = (int)score[i];
        }

        CheckWinCondition();
    }

    #region goal
    
    public void addAllTeamGoal(Vector3 target){
        for (int i = 0; i <  matchSettings.numTeams; i++)
        {
            AddTeamGoal(i, target);   
        }
    }

    public void addAllTeamGoal(NetworkIdentity target){
        for (int i = 0; i <  matchSettings.numTeams; i++)
        {
            AddTeamGoal(i, target);   
        }
    }

    public void setAllTeamGoal(Vector3 target, int id = 0)
    {
        for (int i = 0; i < matchSettings.numTeams; i++)
        {
            SetTeamGoal(i, target, id);
        }
    }

    public void setAllTeamGoal(NetworkIdentity target, int id = 0)
    {
        for (int i = 0; i < matchSettings.numTeams; i++)
        {
            SetTeamGoal(i, target, id);
        }
    }

    #endregion
}
