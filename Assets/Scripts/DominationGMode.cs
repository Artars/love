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

    protected void awake(){
        //Initialize singleton
        if(instance == null) 
            instance = this;
        else if(instance != this)
        {
            Destroy(gameObject);
            return;
        }

        ClearAllTeamGoals();//preparing to add a new goal
        addAllTeamGoal(DominationGoal.transform.position);
    }

    void Update()
    {
        for(int i = 0; i < matchSettings.numTeams; i++){
            if (i == DominationGoal.GetComponent<DominationGoalPoint>().currentTeam)
            {
                score[i]+= scoreBySecond;
            }
        }
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
