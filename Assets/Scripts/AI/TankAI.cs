using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Panda;

public class TankAI : MonoBehaviour
{
    [Header("Parameters")]
    public float reachDistance = 2;
    public float turnTankThreshold = 0.1f;
    public float cannonTurnThreshold = 0.1f;
    public float turretInclinationThreshold = 0.5f;

    [Header("References")]
    public TankVision[] visions;

    protected Tank tank;

    protected PandaBehaviour behaviour;

    // Enemy detection
    protected Tank lastEnemySeen;
    protected bool hasLastEnemyPosition = false;
    protected bool canEnemyBeSeen = false;
    public Vector3 lastPositionSeen;

    // Aim
    protected bool isAimCorrect = false;
    protected bool shouldAimCannon = false;
    protected Vector3 cannonTargetPosition;

    // Movement
    protected bool hasSetPath = false;
    protected NavMeshPath path;
    protected Vector3 waypointDestination;
    protected Vector3 finalDestination;
    protected int currentWaypoint = -1;

    void Awake()
    {
        behaviour = GetComponent<PandaBehaviour>();
        path = new NavMeshPath();
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

        visions[0].SetToFollow(tank.cameraPositionDriver, tank);
        visions[1].SetToFollow(tank.rotationPivot, tank);
    }

    public void RemoveTank()
    {
        this.tank = null;
        foreach (var vision in visions)
        {
            vision.StopFollowing();
        }
        Reset();
    }

    public void Reset()
    {
        hasSetPath = false;
        hasLastEnemyPosition = false;
        lastEnemySeen = null;
        behaviour.Reset();
    }


    public void CallUpdate(float timeDelta)
    {

        VerifyCurrentEnemyValid();

        MoveTank();
        SenseEnemy();
        AimCannon();

        // Update behaviour ticks
        if(behaviour != null)
        {
            behaviour.Tick();
        }

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

    protected void VerifyCurrentEnemyValid()
    {
        //Check if last enemy position is still valid
        if(hasLastEnemyPosition)
        {
            if(!canEnemyBeSeen)
            {
                Vector3 dist = lastPositionSeen - tank.transform.position;
                if(dist.magnitude < reachDistance || lastEnemySeen.currentHealth <= 0)
                {
                    lastEnemySeen = null;
                    hasLastEnemyPosition = false;
                }
            }
        }
    }

    protected void AimCannon()
    {
        isAimCorrect = false;
        if(tank != null && shouldAimCannon)
        {
            // Calculate turning
            float turnInput = 0;

            Vector3 turretDiference = cannonTargetPosition - tank.rotationPivot.position;
            float dot = Vector3.Dot(turretDiference.normalized, tank.rotationPivot.right);
            if(Mathf.Abs(dot) > cannonTurnThreshold)
                turnInput = (dot > 0) ? 1 : -1;
            

            // Nivel calculation
            Vector3 nivelDiference = cannonTargetPosition - tank.nivelTransform.position;
            float dy = nivelDiference.y;
            float dxz = Mathf.Sqrt(nivelDiference.x * nivelDiference.x + nivelDiference.z * nivelDiference.z);

            // Use the equation provided by https://en.wikipedia.org/wiki/Projectile_motion
            float alpha = Mathf.Atan(dy/dxz) * Mathf.Rad2Deg;
            // float theta = 90 - 0.5f*(90-alpha);
            float theta = alpha;
            // float theta =  Mathf.Atan(dy/dxz + Mathf.Sqrt((dy*dy)/(dxz*dxz) + 1)) * Mathf.Rad2Deg;

            float currentY = tank.nivelTransform.forward.y;
            float currentXZ = Mathf.Sqrt(tank.nivelTransform.forward.x * tank.nivelTransform.forward.x + 
                                         tank.nivelTransform.forward.z * tank.nivelTransform.forward.z);

            float currentNivel = Mathf.Atan(currentY/currentXZ) * Mathf.Rad2Deg;
            float nivelInput = 0;

            //Clamp theta to available angles
            theta = Mathf.Clamp(theta, tank.tankParameters.minCannonNivel, tank.tankParameters.maxCannonNivel);

            if(Mathf.Abs(theta - currentNivel) > turretInclinationThreshold)
            {
                nivelInput = (theta - currentNivel) > 0 ? 1 : -1;
            }


            tank.setCannonAxis(turnInput,nivelInput);

            if(turnInput == 0 && nivelInput == 0)
            {
                isAimCorrect = true;
            }
        }
    }

    //Should face destination and then moving to it
    protected void MoveTank()
    {
        //Find direction
        Vector3 targetDestination = waypointDestination - tank.transform.position;

        //Check if reached target
        if(targetDestination.magnitude < reachDistance)
        {
            UpdateDestination();

            // Has no more destination
            if(!hasSetPath) return;
        }


        //Check if direction is the same
        int turnInput = 0;

        float dot = Vector3.Dot(targetDestination.normalized, tank.transform.right);
        float dotForward = Vector3.Dot(targetDestination.normalized, tank.transform.forward);
        if(Mathf.Abs(dot) > turnTankThreshold)
            turnInput = (dot > 0) ? 1 : -1;
        //Facing backwards
        else if (dotForward < 0)
        {
            turnInput = 1;
        }

        //If it doesn't, face the direction
        if(turnInput != 0)
        {
            if(turnInput == 1)
            {
                tank.SetGear(tank.gearSystem.highestGear, tank.gearSystem.lowestGear);
            }
            else
            {
                tank.SetGear(tank.gearSystem.lowestGear, tank.gearSystem.highestGear);
            }
        }
        //Else will go to the destination
        else
        {
            tank.SetGear(tank.gearSystem.highestGear,tank.gearSystem.highestGear);
        }

    }

    //Will get the next waypoint from the path
    protected void UpdateDestination()
    {
        if(path != null)
        {
            currentWaypoint ++;
            if(currentWaypoint < path.corners.Length)
            {
                waypointDestination = path.corners[currentWaypoint];
            }
            else
            {
                currentWaypoint = -1;
                hasSetPath = false;
            }
        }
    }

    [Task]
    void SetRandomPosition()
    {
        Vector3 sourcePosition = AIHelper.instance.GetRandomPosition(); //PLACEHOLDER
        SetTargetPosition(sourcePosition);
    }

    [Task]
    void SetPositionToEnemy()
    {
        if(hasLastEnemyPosition)
        {
            Vector3 sourcePosition = lastPositionSeen; //PLACEHOLDER
            SetTargetPosition(sourcePosition);
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
        int navArea = NavMesh.AllAreas;
        // Will try to find valid position, starting with smallest distance
        for(int i = 0; i < 10; i++)
        {
            if(NavMesh.SamplePosition(position, out navHit, i, navArea))
            {
                finalDestination = navHit.position;
                if(NavMesh.CalculatePath(tank.transform.position, finalDestination, navArea, path))
                {
                    // Configurate path
                    hasSetPath = true;
                    currentWaypoint = 0;
                    UpdateDestination();

                    Task.current.Succeed();
                    return;
                }
            }
        }

        // If can't find position, failed
        Task.current.Fail();
    }


    [Task]
    void HasReachedDestination()
    {
        if(!hasSetPath)
        {
            Task.current.Succeed();
        }
        else
        {
            Task.current.Fail();
        }
    }

    [Task]
    void CloseToTarget(float distance)
    {
        if( (finalDestination - tank.transform.position).magnitude <= distance)
        {
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
        hasSetPath = false;

        if(tank != null)
        {
            tank.SetGear(0,0);
        }

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

            Task.current.Succeed();
        }
        else
        {
            Task.current.Fail();
        }

        // Task.current.Succeed();

    }

    [Task]
    void AimAtRandomDirection()
    {
        Vector2 direction = Random.insideUnitCircle.normalized;
        Vector3 offset = new Vector3(direction.x,0,direction.y);

        cannonTargetPosition = tank.centerTransform.position + offset*20;

        shouldAimCannon = true;

        Task.current.Succeed();
    }

    [Task]
    void Shoot()
    {
        if(tank != null && tank.CanShootCannon())
        {
            tank.cannonShoot();
            Task.current.Succeed();
        }
        else
        {
            Task.current.Fail();
        }
    }

    [Task]
    bool AimCorrect()
    {
        return isAimCorrect;
    }

    [Task]
    bool StopAim()
    {
        shouldAimCannon = false;
        return true;
    }

    public void OnDrawGizmos()
    {
        if(hasSetPath)
        {
            for(int i = 1; i < path.corners.Length; i++)
            {
                Gizmos.color = (i != currentWaypoint) ? Color.red : Color.green;
                Gizmos.DrawLine(path.corners[i-1], path.corners[i]);
            }
        }

        if(hasLastEnemyPosition)
        {
            Gizmos.color = Color.red;

            Gizmos.DrawSphere(lastPositionSeen, 1);
        }
    }
}
