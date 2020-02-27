using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankVision : MonoBehaviour
{
    public float raycastOffset = 0.5f;
    protected List<Tank> detectedTanks;
    protected List<Tank> visibleTanks;
    protected List<Vector3> seenPosition;
    protected Collider myCollider;

    public List<Tank> Detected
    {
        get
        {
            return detectedTanks;
        }
        set
        {

        }
    }

    public List<Tank> Visible
    {
        get
        {
            return visibleTanks;
        }
        set
        {

        }
    }

    public List<Vector3> SeenPosition { 
        get 
        {
            return seenPosition;
        } 
        set 
        {

        }
    }

    protected Transform toFollow;
    
    protected Tank toIgnore;
    protected int teamToIgnore = -1;

    protected bool shouldTrack = false;
    protected Transform myTransform;

    void Awake()
    {
        detectedTanks = new List<Tank>();
        visibleTanks = new List<Tank>();
        seenPosition = new List<Vector3>();
        myTransform = transform;


        myCollider = GetComponent<Collider>();
        myCollider.enabled = false;
    }

    public void SetToFollow(Transform target, Tank ignore = null)
    {
        detectedTanks.Clear();

        this.toFollow = target;
        toIgnore = ignore;
        if(ignore != null)
        {
            teamToIgnore = ignore.team;
        }
        shouldTrack = true;

        myCollider.enabled = true;
    }

    public void StopFollowing()
    {
        this.toFollow = null;
        toIgnore = null;
        teamToIgnore = -1;
        shouldTrack = false;
        myCollider.enabled = false;
    }

    public void Update()
    {
        if(shouldTrack && toFollow != null)
        {
            myTransform.position = toFollow.position;
            myTransform.rotation = toFollow.rotation;

            UpdateVisibility();
        }
    }

    protected void UpdateVisibility()
    {
        visibleTanks.Clear();
        seenPosition.Clear();

        foreach (var detected in detectedTanks)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector3 position;
                if(i == 0)
                    position = detected.centerTransform.position;
                else if(i == 1)
                    position = detected.frontCollisionCheck.position;
                else
                    position = detected.backCollisionCheck.position;
                
                Vector3 diference = position - myTransform.position;
                Ray ray = new Ray(myTransform.position + diference.normalized * raycastOffset,diference.normalized);
                int layerMask = LayerMask.GetMask("Default", "Tank");
                RaycastHit hit;

                if(Physics.Raycast(ray, out hit, diference.magnitude * 1.2f, layerMask))
                {
                    if(hit.collider.gameObject.tag == "Tank")
                    {
                        Tank found = hit.collider.GetComponentInParent<Tank>();
                        if(found == detected)
                        {
                            visibleTanks.Add(detected);
                            seenPosition.Add(hit.point);
                            break;
                        }
                    }
                }
            }
        }
    }

    public void OnTriggerEnter(Collider col)
    {
        if(!shouldTrack) return;
        if(col.gameObject.tag == "Tank")
        {
            Tank t = col.GetComponentInParent<Tank>();

            if(t != null && t != toIgnore && t.team != teamToIgnore)
            {
                detectedTanks.Add(t);
            }

        }
    }

    public void OnTriggerExit(Collider col)
    {
        if(!shouldTrack) return;
        if(col.gameObject.tag == "Tank")
        {
            Tank t = col.GetComponentInParent<Tank>();

            if(t != null && t.team != teamToIgnore && detectedTanks.Contains(t))
            {
                detectedTanks.Remove(t);
            }
        }
    }


}
