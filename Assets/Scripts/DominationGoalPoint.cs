using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class DominationGoalPoint : NetworkBehaviour
{
    [Header("Domination States")]
    public int currentTeam = -404;//there is no team dominating
    public float timeToDominate = 10f;
    [Tooltip("there is a new team dominating")]
    public bool assertDomination =  false;
    public bool removeCurrentTeamDomination = false;
    private float time = 0;
    private float scoreTime = 1f;
    private float dominationTime = 0;

    [Header("References")]
    public Flag[] flags;
    
 

    public List<Tank> tanks;

    public void Awake()
    {
        tanks =new List<Tank>();
        dominationTime = 0;
        SetFlagsVisibility(false);
    }

    public void Update()
    {

        checkTankTeams();
        
        time += Time.deltaTime;
        if (time > scoreTime){// 1 second update
            if (!removeCurrentTeamDomination && currentTeam != -404)//make score
            {
                if(GameMode.instance != null){
                    GameMode.instance.UpdateScore();
                }
            }
            time = 0.0f;
        }
            

        if (assertDomination)
        {
            dominationTime += Time.deltaTime;
            if (dominationTime >= timeToDominate)
            {
                dominationTime = timeToDominate;
                assertDomination = false;
            }
        }

        if (removeCurrentTeamDomination)
        {
            dominationTime -= Time.deltaTime;
            if (dominationTime <= 0)
            {
                removeCurrentTeamDomination = false;
                //Make sound
                GameMode.instance.PlayClipToTeam(currentTeam,AudioManager.SoundClips.DecrementPoint);

                currentTeam = -404;
                SetFlagsVisibility(false);
            }
        }
        
        SetFlagsPosition(dominationTime / timeToDominate);
    }


    protected void OnTriggerEnter(Collider col){
        
        GameObject inComing = col.gameObject;
        Debug.Log(inComing);

        Tank domineering = inComing.GetComponentInParent<Tank>();
        if (domineering  != null)
        {
            Debug.Log("detectou o tank");
            if (!tanks.Contains(domineering))
                tanks.Add(domineering);
        }
    }

    protected void OnTriggerExit(Collider col) {
    
        GameObject exited = col.gameObject;
        Tank left = exited.GetComponentInParent<Tank>();

        if (left != null)
        {
            if (tanks.Contains(left))
                tanks.Remove(left);

            if (tanks.Count == 0)
            {
                assertDomination = false;
                removeCurrentTeamDomination = false;
            }
        }
    }

    public void checkTankTeams(){
        
        if (tanks.Count == 0)
        {
            removeCurrentTeamDomination = false;
            assertDomination = false;
            return;
        }

        int team = tanks[0].team;
        for (int i = 0; i < tanks.Count; i++)
        {
            if (tanks[i].team != team)
            {
                assertDomination = false;//pause time
                removeCurrentTeamDomination = true;
                return;
            }
        }

        if (team  != currentTeam)
        {
            // Change teams
            if(dominationTime <= 0)
            {
                assertDomination = true;
                currentTeam = team;
                removeCurrentTeamDomination = false;
                dominationTime = 0;
                SetFlagsVisibility(true);
                SetFlagsTeam(currentTeam);

                //Make sound
                GameMode.instance.PlayClipToTeam(team,AudioManager.SoundClips.IncrementPoint);
            }
            //Reduce current team
            else
            {
                assertDomination = false;
                removeCurrentTeamDomination = true;
            }
        }
        // Increase domination
        else if(team == currentTeam){
            assertDomination = true;
            removeCurrentTeamDomination = false;
        }
    }

    protected void SetFlagsVisibility(bool visibility)
    {
        foreach (var flag in flags)
        {
            flag.SetFlagVisibility(false, visibility);
        }
    }

    protected void SetFlagsPosition(float porcent)
    {
        foreach (var flag in flags)
        {
            flag.SetFlagPosition(porcent);
        }
    }

    protected void SetFlagsTeam(int team)
    {
        foreach (var flag in flags)
        {
            flag.SetFlagTeam(-1,team);
        }
    }
}
