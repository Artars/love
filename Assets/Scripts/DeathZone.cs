using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class DeathZone : NetworkBehaviour
{
    public Dictionary<Tank,float> debounceForTank = new Dictionary<Tank, float>();
    public float debounceTime = 1;

    public void Update()
    {
        foreach (KeyValuePair<Tank,float> debounce in debounceForTank)
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
                    if(!debounceForTank.ContainsKey(tankScript))
                        debounceForTank.Add(tankScript, 0);

                    if(debounceForTank[tankScript] <= 0)
                    {
                        tankScript.KillTank(tankScript.tankId);
                        debounceForTank[tankScript] = debounceTime;
                    }
                }
            }
        }
    }
}
