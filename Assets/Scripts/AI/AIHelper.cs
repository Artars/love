using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIHelper : MonoBehaviour
{
    public static AIHelper instance;

    public Transform bottomLeftTransform;
    public Transform topRightTransform;

    public float topY = 10;

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
        Ray ray = new Ray();
        RaycastHit hit;
        ray.direction = Vector3.down;
        do
        {

            Vector3 diference = topRightTransform.position - bottomLeftTransform.position;

            Vector3 randomDirection = new Vector3(Random.Range(0f,1f) * diference.x,0,Random.Range(0,1f) * diference.z);
            Vector3 randomPosition = bottomLeftTransform.position + randomDirection;
            randomPosition.y = topY;
            ray.origin = randomPosition;
        }
        while(!Physics.Raycast(ray, out hit, topY * 1.1f, LayerMask.GetMask("Default")));
       

       return hit.point;
    }

    public void OnDrawGizmosSelected()
    {
        if(bottomLeftTransform != null && topRightTransform != null)
        {
            Vector3 diference = topRightTransform.position - bottomLeftTransform.position;

            Vector3 center = bottomLeftTransform.position + diference*0.5f;
            center.y = topY/2;
            diference.y = topY;
            Gizmos.DrawWireCube(center, diference);

        }
    }
}
