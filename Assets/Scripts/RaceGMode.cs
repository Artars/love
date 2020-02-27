using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class RaceGMode : GameMode
{
    [Header("Race Info")]
    public GameObject actualRaceGoal;
    public List<GoalSpawner> goalSpawners;
    [SyncVar]
    public int actualGoalSpawner =-1;
    [SyncVar]
    public int scoreByGoal = 1;


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

    public override void PrepareGoal()
    {
        SpawnNewGoal();
    }


    void Update()
    {
        if(gameStage == GameStage.Match && actualRaceGoal != null)
        {
            matchTime += Time.deltaTime;
            if(matchTime > matchSettings.maxTime)
            {
                MatchTimeEnded();
            }
        }
    }

    public void SpawnNewGoal(){
        //first Spawn
        if(actualGoalSpawner == -1 || goalSpawners.Count == 1){
        
            actualGoalSpawner = 0;
            goalSpawners[0].SpawnGameObject();
            actualRaceGoal = goalSpawners[actualGoalSpawner].instance;

            addAllTeamGoal(actualRaceGoal.transform.position);
        }else{
                        
            actualGoalSpawner++;
            actualGoalSpawner = actualGoalSpawner%goalSpawners.Count;

            goalSpawners[actualGoalSpawner].SpawnGameObject();
            actualRaceGoal = goalSpawners[actualGoalSpawner].instance;

            setAllTeamGoal(actualRaceGoal.transform.position);

        }
    }

    public override void UpdateScore(){
        
        if(gameStage != GameStage.Match) return;

        for(int i = 0; i < matchSettings.numTeams; i++){
            if (i == actualRaceGoal.GetComponentInChildren<RaceGoalPoint>().scoreIncresedTeam)
            {
                Debug.Log("if");
                if (score[i] < matchSettings.maxPoints)
                {
                    score[i]+= scoreByGoal;
                    PlayClipToTeam(i, AudioManager.SoundClips.IncrementPoint);
                }
            }
            Debug.Log("o score do time" + i + "é " + score[i]);

            GameStatus.instance.score[i] = (int)score[i];
            
        }
        
        Destroy(actualRaceGoal);
        CheckWinCondition();
        SpawnNewGoal();
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
