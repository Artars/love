using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Panda;

public class TankAI : MonoBehaviour
{
    [Header("Parameters")]
    public float reachDistance = 10;

    [Header("References")]
    public TankVision[] visions;

    protected Tank tank;
    protected NavMeshAgent navMeshAgent;

    protected PandaBehaviour behaviour;

    protected Tank lastEnemySeen;
    protected bool hasLastEnemyPosition = false;
    protected bool canEnemyBeSeen = false;
    public Vector3 lastPositionSeen;

    protected bool shouldAimCannon = false;
    protected Vector3 cannonTargetPosition;

    protected bool hasSetDestination = false;
    Vector3 destination;

    void Awake()
    {
        behaviour = GetComponent<PandaBehaviour>();
        Reset();
    }

    public void SetTank(Tank tank)
    {
        if(tank == null)
        {
            RemoveTank();
            return;
        }

        Reset();

        this.tank = tank;
        this.navMeshAgent = tank.navMeshAgent;

        visions[0].SetToFollow(tank.cameraPositionDriver, tank);
        visions[1].SetToFollow(tank.rotationPivot, tank);
    }

    public void RemoveTank()
    {
        this.tank = null;
        this.navMeshAgent = null;
        foreach (var vision in visions)
        {
            vision.StopFollowing();
        }
        Reset();
    }

    public void Reset()
    {
        hasSetDestination = false;
        hasLastEnemyPosition = false;
        lastEnemySeen = null;
        behaviour.Reset();
    }


    public void CallUpdate(float timeDelta)
    {
        //Check if last enemy position is still valid
        if(hasLastEnemyPosition)
        {
            if(!canEnemyBeSeen)
            {
                Vector3 dist = lastPositionSeen - tank.transform.position;
                if(dist.magnitude < reachDistance)
                {
                    lastEnemySeen = null;
                    hasLastEnemyPosition = false;
                }
            }
        }

        // Update behaviour ticks
        if(behaviour != null)
        {
            behaviour.Tick();
        }

        SenseEnemy();
        AimCannon();

    }

    protected void SenseEnemy()
    {
        if(tank != null)
        {
            canEnemyBeSeen = false;
            for (int i = visions.Length-1; i > -1; i--)
            {
                if(DetectEnemy(visions[i]))
                    break;
            }
        }
    }

    protected bool DetectEnemy(TankVision vision)
    {
        if(vision.Visible.Count > 0)
        {
            hasLastEnemyPosition = true;
            lastEnemySeen = vision.Visible[0];
            lastPositionSeen = vision.SeenPosition[0];
            canEnemyBeSeen = true;

            return true;
        }
        return false;
    }

    protected void AimCannon()
    {
        if(tank != null && shouldAimCannon)
        {
            float direction = 0;

            Vector3 turretDiference = cannonTargetPosition - tank.rotationPivot.position;
            float dot = Vector3.Dot(turretDiference.normalized, tank.rotationPivot.right);
            if(Mathf.Abs(dot) > 0.1f)
                direction = (dot > 0) ? 1 : -1;
            
            tank.setCannonAxis(direction,0);

            if(direction == 0)
            {
                shouldAimCannon = false;
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
    void SetTargetPosition(Vector3 position)
    {
        NavMeshHit navHit;
        if(NavMesh.SamplePosition(position, out navHit, 1, NavMesh.AllAreas))
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
    void SetPositionToEnemy()
    {
        Vector3 sourcePosition = lastPositionSeen; //PLACEHOLDER
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
    void GoToTarget()
    {
        navMeshAgent.isStopped = false;
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
    void Stop()
    {
        hasSetDestination = false;
        navMeshAgent.isStopped = true;
        Task.current.Succeed();
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

    [Task]
    void CanEnemyBeSeen()
    {
        if(canEnemyBeSeen) Task.current.Succeed();
        else Task.current.Fail();
    }

    [Task]
    void AimAtEnemy()
    {
        if(hasLastEnemyPosition)
        {
            cannonTargetPosition = lastPositionSeen;
            shouldAimCannon = true;

            // Task.current.Succeed();
        }
        else
        {
            // Task.current.Fail();
        }

        Task.current.Succeed();

    }
}
