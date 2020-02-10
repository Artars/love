using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class DeathZone : NetworkBehaviour
{
    public Dictionary<int,bool> debounceForTank = new Dictionary<int, bool>();
    public float debounceTime = 1;

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
                        debounceForTank.Add(tankScript.tankId, true);

                    if(debounceForTank[tankScript.tankId])
                    {
                        tankScript.KillTank(tankScript.tankId);
                        debounceForTank[tankScript.tankId] = false;
                        StartCoroutine(WaitDebounce(tankScript.tankId));
                    }
                }
            }
        }
    }

    protected IEnumerator WaitDebounce(int id)
    {
        yield return new WaitForSeconds(debounceTime);
        debounceForTank[id] = true;
    }
}
