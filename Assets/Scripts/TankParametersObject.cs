using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TankParameters", menuName = "Game Parameters/Tank parameters", order = 1)]
[System.Serializable]
public class TankParametersObject : ScriptableObject
{
    public TankParameters tankParameters;
}

[System.Serializable]
public class TankParameters
{
    [Header("Driver")]
    
    public float forwardSpeed = 10;
    public float backwardSpeed = 5;
    public float turnSpeed = 10;
    public GearSystem gearSystem = new GearSystem();


    [Header("Gunner")]
    public ShootMode shootMode = ShootMode.Damage;
    public float turnCannonSpeed = 20;
    public float nivelCannonSpeed = 20;
    public float minCannonNivel = -30;
    public float maxCannonNivel = 30;
    public float shootCooldown = 1;
    public float bulletSpeed = 30;
    public float bulletDamage = 20;

    [Header("Health")]
    public float maxHeath = 100;



}

[System.Serializable]
public enum ShootMode
{
    Damage,Stop
}

[System.Serializable]
public class GearSystem
{
    [Range(-5,0)]
    public int lowestGear = -2;
    [Range(0,5)]
    public int highestGear = 3;
    public float[] values;

    public GearSystem()
    {
        values = new float[Mathf.Abs(lowestGear) + Mathf.Abs(highestGear) + 1];
    }

    public float GetGearValue(int gear)
    {

        //Get out of bounds
        if(gear < lowestGear)
        {
            return -1;
        }

        if(gear > highestGear)
        {
            return 1;
        }

        return values[Mathf.Abs(lowestGear)+gear];

    }

    public int FindGear(float value)
    {
        int closestIndex = -1;
        float closestValue = Mathf.Infinity;

        for (int i = 0; i < values.Length; i++)
        {
            float dif = Mathf.Abs(value - values[i]);
            if(dif < closestValue)
            {
                closestValue = dif;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    public int ClampGear(int gear)
    {
        //Get out of bounds
        if(gear < lowestGear)
        {
            return lowestGear;
        }

        if(gear > highestGear)
        {
            return highestGear;
        }

        return gear;
    }

    public void FixValueArray()
    {
        values = new float[Mathf.Abs(lowestGear) + Mathf.Abs(highestGear) + 1];

        int negativeCount = Mathf.Abs(lowestGear);

        for (int i = 0; i < negativeCount; i++)
        {
            values[i] = -1.0f + (1.0f/negativeCount) * i;
        }
        for(int i = 0; i < highestGear + 1; i++)
        {
            values[negativeCount + i] = (1.0f/highestGear)*i;
        }

    }
}
