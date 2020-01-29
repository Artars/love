using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class RaceGoalPoint : NetworkBehaviour
{
    [SyncVar]
    public int scoreIncresedTeam = -999; 

    protected void OnTriggerEnter(Collider col){
        
        GameObject colliding = col.gameObject;
        Tank collidingTank = colliding.GetComponentInParent<Tank>();
        
        if (collidingTank  != null && scoreIncresedTeam == -999)
        {
            scoreIncresedTeam = collidingTank.team;
            GameMode.instance.UpdateScore();
        }
    }
}
