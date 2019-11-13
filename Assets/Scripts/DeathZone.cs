using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class DeathZone : NetworkBehaviour
{
    public Dictionary<int,float> debounceForTank = new Dictionary<int, float>();
    public float debounceTime = 1;

    public void Update()
    {
        foreach (KeyValuePair<int,float> debounce in debounceForTank)
        {
            debounceForTank[debounce.Key] -= Time.deltaTime;
        }
    }

    public void OnTriggerEnter(Collider col)
    {
        if(isServer)
        {
            if(col.gameObject.tag == "Tank")
            {
                Tank tankScript = col.GetComponentInParent<Tank>();
                if(tankScript != null)
                {
                    if(!debounceForTank.ContainsKey(tankScript.tankId))
                        debounceForTank.Add(tankScript.tankId, 0);

                    if(debounceForTank[tankScript.tankId] <= 0)
                    {
                        tankScript.KillTank(tankScript.tankId);
                        debounceForTank[tankScript.tankId] = debounceTime;
                    }
                }
            }
        }
    }
}
