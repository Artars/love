using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class RaceGoalPoint : NetworkBehaviour
{
    public int scoreIncresedTeam = -999;


    protected void OnTriggerEnter(Collider col){
        
        GameObject colliding = col.gameObject;
        Debug.Log(colliding);

        Tank collidingTank = colliding.GetComponentInParent<Tank>();
        if (collidingTank  != null)
        {
            scoreIncresedTeam = collidingTank.team;
            GameMode.instance.UpdateScore();
            Destroy(gameObject);
        }
    }
}
