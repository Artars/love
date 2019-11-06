using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIHelper : MonoBehaviour
{
    public static AIHelper instance;

    public BoxCollider mapBounds;
    public float baseY = 0;

    public void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public Vector3 GetRandomPosition()
    {
       Vector3 randomPosition = mapBounds.center + Random.Range(-1f,1f) * mapBounds.size * 0.5f;
       randomPosition.y = baseY;

       return randomPosition;
    }
}
