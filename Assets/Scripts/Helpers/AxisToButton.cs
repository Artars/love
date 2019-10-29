using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AxisToButton
{
    public string input;
    public float threshold = 0.1f;
    public int currentValue = 0;

    public AxisToButton(string inputString, float threshold = 0.2f)
    {
        input = inputString;
        this.threshold = threshold;
    }

    public bool GetButtonDown()
    {
        int state = GetNewState();

        if(state != currentValue)
        {
            currentValue = state;
            if(state != 0)
                return true;
        }
        return false;
    }

    public bool GetButtonUp()
    {
        int state = GetNewState();
        

        if(state != currentValue)
        {
            currentValue = state;
            if(state == 0)
                return true;
        }
        return false;
    }

    public bool GetState()
    {
        int state = GetNewState();

        if(state != currentValue)
        {
            currentValue = state;
        }
        if(state != 0)
            return true;
        else
            return false;
    }

    protected int GetNewState()
    {
        float currentInputValue = Input.GetAxisRaw(input);
        int state = currentInputValue > threshold ? 1 : 0;
        state = currentInputValue < -threshold ? -1 : state;
        
        return state;
    }

}