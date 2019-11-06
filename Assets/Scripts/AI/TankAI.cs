using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Panda;

public class TankAI : MonoBehaviour
{
    public Tank tank;
    public NavMeshAgent navMeshAgent;
    public BoxCollider navegableArea;
    public float reachDistance = 10;

    protected Tank lastEnemySeen;
    protected bool hasLastEnemyPosition = false;
    protected Vector3 lastPositionSeen;
    protected bool hasSetDestination = false;

    Vector3 destination;

    void Update()
    {
        //Check if last seen enemy is dead
        if(lastEnemySeen != null)
        {
            if(lastEnemySeen.currentHealth < 0)
            {
                lastEnemySeen = null;
                hasLastEnemyPosition = false;
                lastPositionSeen = Vector3.zero;
            }
        }
    }

    [Task]
    void GetRandomPosition()
    {
        Vector3 sourcePosition = AIHelper.instance.GetRandomPosition(); //PLACEHOLDER
        NavMeshHit navHit;
        if(NavMesh.SamplePosition(sourcePosition, out navHit, 2f, NavMesh.AllAreas))
        {
            destination = navHit.position;
            Task.current.Succeed();
        }
        else
        {
            Task.current.Fail();
        }
    }

    [Task]
    void SetTargetPosition(float x, float y, float z)
    {
        Vector3 sourcePosition = new Vector3(x,y,z);
        NavMeshHit navHit;
        // if(NavMesh.SamplePosition(sourcePosition, out navHit, 1, 0))
        // {
            // destination = navHit.position;
            destination = sourcePosition;
            Task.current.Succeed();
        // }
        // else
        // {
        //     Task.current.Fail();
        // }
    }

    [Task]
    void GoToTarget()
    {
        if(navMeshAgent.SetDestination(destination))
        {
            hasSetDestination = true;
            Task.current.Succeed();
        }
        else
        {
            Task.current.Fail();
        }
    }

    [Task]
    void HasReachedDestination()
    {
        if(!hasSetDestination || (tank.transform.position - destination).magnitude < reachDistance)
        {
            hasSetDestination = false;
            Task.current.Succeed();
        }
        else
        {
            Task.current.Fail();
        }
    }

    [Task]
    void Say(string toSay)
    {
        Debug.Log(toSay);
    }

    [Task]
    void FoundEnemy()
    {
        if(lastEnemySeen != null || hasLastEnemyPosition)
        {
            Task.current.Succeed();
        }
        else
        {
            Task.current.Fail();
        }
    }
}
