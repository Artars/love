﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class DominationGoalPoint : NetworkBehaviour
{
    [Header("Domination States")]
    public int currentTeam = -404;//there is no team dominating
    public int tryingTeam =  -404;
    public bool isDominating = false;
    [Tooltip("there is a new team dominating")]
    public bool inChange =  false;
    public bool restoreTime = false;
    private float time = 0;
    private int dominationTime = 0;

    public List<Tank> tanks;

    public void Awake()
    {
        tanks =new List<Tank>();
    }

    public void Update()
    {

        checkTankTeams();
        
        time += Time.deltaTime;
        if (time >= 1){// 1 second update
            time =0;

            if (inChange)
            {
                dominationTime++;
                if (dominationTime == 10)
                {
                    inChange = false;
                    currentTeam = tryingTeam;
                }
            }

            if (restoreTime)
            {
                dominationTime--;
                if (dominationTime == 0)
                {
                    restoreTime = false;
                    tryingTeam = currentTeam;
                }
            }
        }

    }

    protected void OnCollisionEnter(Collision col){

        GameObject inComing = col.gameObject;
        if (inComing.GetComponent(typeof(Tank))  != null)
        {
            Tank domineering = inComing.GetComponent<Tank>();
            tanks.Add(domineering);
        }
    }

    protected void OnCollisionExit(Collision col) {
    
        GameObject exited = col.gameObject;

        if (exited.GetComponent(typeof(Tank)) != null)
        {
            Tank left = exited.GetComponent<Tank>();
            tanks.Remove(left);

            if (tanks.Count == 0 && inChange)
            {
                inChange = false;
                if ( dominationTime != 0 )
                {
                    restoreTime = true;
                }
            }
        }
    }

    public void checkTankTeams(){
        
        if (tanks.Count == 0)
        {
            inChange = false;
            return;
        }

        int team = tanks[0].team;
        for (int i = 0; i < tanks.Count; i++)
        {
            if (tanks[i].team != team)
            {
                inChange = false;//pause time
                return;
            }
        }
        if (team  != currentTeam && team != tryingTeam)
        {
            tryingTeam = team;
            inChange = true;// time ++
            restoreTime = false;
        }
        else if(team == currentTeam && dominationTime != 0){
            restoreTime = true;// time--
        }
    }
}
