using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class GoalPoint : NetworkBehaviour
{
    public delegate void ChangeColor(Color newColor);

    [SyncVar(hook="UpdateTargetPosition")]
    public Vector3 targetPosition;
    [SyncVar(hook="UpdateTargetTransform")]
    public NetworkIdentity target;
    public Transform targetTransform = null;
    [SyncVar]
    public Color goalColor = Color.blue;

    [SyncEvent]
    public event ChangeColor EventChangeColor;
    

    public Vector3 Position
    {
        get{
            return myTransform.position;
        }
    }

    protected Transform myTransform;

    void Awake()
    {
        myTransform = transform;
        myTransform.position = targetPosition;

        if(target != null)
        {
            targetTransform = target.transform;
            myTransform.position = targetTransform.position;
        }
    }

    [Server]
    public void SetTarget(Vector3 target)
    {
        this.target = null;
        targetPosition = target;
    }

    [Server]
    public void SetTarget(NetworkIdentity target)
    {
        this.target = target;
        this.targetTransform = target.transform;
    }

    [Server]
    public void SetColor(Color color)
    {
        goalColor = color;

        EventChangeColor(color);
    }

    protected void UpdateTargetTransform(NetworkIdentity newTarget)
    {
        if(newTarget != null)
        {
            targetTransform = newTarget.transform;
        }
        else
        {
            targetTransform = null;
        }
    }

    protected void UpdateTargetPosition(Vector3 newPosition)
    {
        myTransform.position = newPosition;
    }

    public void Update()
    {
        if(targetTransform != null)
        {
            myTransform.position = targetTransform.position;
        }
    }


    
}
