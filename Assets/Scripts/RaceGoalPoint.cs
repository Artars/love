using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class RaceGoalPoint : NetworkBehaviour
{
    public int scoreIncresedTeam = -999;
    public int securitValue = -1;


    protected void OnTriggerEnter(Collider col){
        
        GameObject colliding = col.gameObject;
        
        Tank collidingTank = colliding.GetComponentInParent<Tank>();
        
        if (collidingTank  != null && securitValue == -1)
        {
            securitValue = 0;
            scoreIncresedTeam = collidingTank.team;
            GameMode.instance.UpdateScore();
        }
    }
}
