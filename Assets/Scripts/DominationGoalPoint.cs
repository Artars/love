using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class DominationGoalPoint : NetworkBehaviour
{
    [Header("Domination States")]
    public int currentTeam = -404;//there is no team dominating
    public bool isDominating = false;
    [Tooltip("there is a new team dominating")]
    public bool inChange =  false;
    public int scoreBySecond = 1;
    private int time = 0;

    public List<Tank> tanks;

    public void Awake()
    {
        tanks =new List<Tank>();
    }

    public void Update()
    {

        checkTankTeams();
        
        if(inChange){//a team is trying to dominate
            time += Time.deltaTime;

            if (time == 10)//full Domination
            {
                inChange = false;
                currentTeam = tanks[0].Team;
                time = 0; 
                isDominating = true;
            }
        }
    }

    protected void OnCollisionEnter(Collision col){

        GameObject inComing = col.gameObject;
        if (inComing.GetComponent(typeof(Tank))  != null)
        {
            Tank domineering = inComing.GetComponent<Tank>();
            if (tanks.Count == 0)
            {
                inChange = true;
            }
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
            }
        }
    }

    public void checkTankTeams(){
        
        if (tanks.Count == 0)
        {
            inChange = false;
        }
        for (int i = 0; i < tanks.Count; i++)
        {
            
        }
    }
}
