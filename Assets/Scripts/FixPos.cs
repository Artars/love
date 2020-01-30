using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class FixPos : NetworkBehaviour
{
    [SyncVar(hook = nameof(SetPos))]
    public Vector3 pos;

    void Start()
    {
        pos = transform.position;
        pos *= -1;
    }

    void Update() {
        
        transform.position = pos;
        
    }

    public void SetPos(Vector3 newPos){
        pos = newPos;
    }
}
