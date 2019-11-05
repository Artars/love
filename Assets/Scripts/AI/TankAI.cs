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

    Vector3 destination;

    [Task]
    void GetRandomPosition()
    {
        Vector3 sourcePosition = Vector3.zero; //PLACEHOLDER
        NavMeshHit navHit;
        if(NavMesh.SamplePosition(sourcePosition, out navHit, Mathf.Infinity, 0))
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
        if((transform.position - destination).magnitude < 1)
        {
            Task.current.Succeed();
        }
        else
        {
            Task.current.Fail();
        }
    }
}
