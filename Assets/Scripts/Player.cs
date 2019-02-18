using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{

    public enum Role
    {
        Pilot, Gunner
    }


    public Tank tankRef;
    public Role role = Role.Pilot;


    protected float rightAxis;
    protected float leftAxis;


}
