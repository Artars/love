using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.IO; 

public class SettingsSelector : MonoBehaviour
{
    public class SettingsChangeEvent : UnityEvent<MatchSetting>
    {
    }

    public SettingsChangeEvent OnSettingsChanged;

    public bool autoLoadSettings = false;
    public string fileName = "MatchConfig.json";
    public MatchSetting matchSetting;
    public float[] maxTimeOptions = new float[]{60, 90, 120, 150, 180, 210, 240, 300, 360, Mathf.Infinity};

    [Header("References")]
    public TMPro.TMP_Text team1Value;
    public TMPro.TMP_Text team2Value;
    public TMPro.TMP_Text maxPointsValue;
    public TMPro.TMP_Text maxTimeValue;
    public TMPro.TMP_Text respawnTimeValue;

    protected int maxTimeIndex = 0;
    
    public void Awake()
    {
        OnSettingsChanged = new SettingsChangeEvent();
    }

    public void Start()
    {
        string path = Application.persistentDataPath + fileName;
        if(!autoLoadSettings && File.Exists(path))
        {
            string jasonText = File.ReadAllText(path);
            matchSetting = JsonUtility.FromJson<MatchSetting>(jasonText);
            maxTimeIndex = FindNearestvalueIndex(maxTimeOptions, matchSetting.maxTime);

        }
        else
        {
            matchSetting = new MatchSetting(2);
            
        }

        UpdateText();


    }

    public void SaveSettings()
    {
        string path = Application.persistentDataPath + fileName;
        string jasonText = JsonUtility.ToJson(matchSetting);

        StreamWriter writer = new StreamWriter(path, false);
        writer.Write(jasonText);
        writer.Close();

    }

    protected int FindNearestvalueIndex(float[] values, float currentValue)
    {
        int index = -1;
        float lowestValue = Mathf.Infinity;

        for (int i = 0; i < values.Length; i++)
        {
            float dif = Mathf.Abs(values[i] - currentValue);
            if(dif < lowestValue)
            {
                lowestValue = dif;
                index = i;

                if(dif == 0)
                    return i;
            }
        }

        return index;
    }

    public void ModifyTeam1(bool increment)
    {
        int value = (increment) ? 1 : -1;
        value += matchSetting.teamConfiguration[0];
        if(value > 0)
        {
            matchSetting.teamConfiguration[0] = value;
            UpdateText();
        }
    }

    public void ModifyTeam2(bool increment)
    {
        int value = (increment) ? 1 : -1;
        value += matchSetting.teamConfiguration[1];
        if(value > 0)
        {
            matchSetting.teamConfiguration[1] = value;
            UpdateText();
        }
    }

    public void ModifyMaxPoints(bool increment)
    {
        int value = (increment) ? 1 : -1;
        value += matchSetting.maxPoints;
        if(value > 0)
        {
            matchSetting.maxPoints = value;
            UpdateText();
        }
    }

    public void ModifyMaxTime(bool increment)
    {
        int value = (increment) ? 1 : -1;
        value += maxTimeIndex;
        if(value < 0)
        {
            value = maxTimeOptions.Length-1;
        }
        else if(value >= maxTimeOptions.Length)
        {
            value = 0;
        }

        maxTimeIndex = value;

        matchSetting.maxTime = maxTimeOptions[maxTimeIndex];
        UpdateText();
    }

    public void ModifyRespawnTime(bool increment)
    {
        float value = (increment) ? 1 : -1;
        value += matchSetting.timeToRespawn;
        if(value > 0)
        {
            matchSetting.timeToRespawn = value;
            UpdateText();
        }
    }

    public void UpdateText()
    {
        OnSettingsChanged.Invoke(matchSetting);

        team1Value.text = matchSetting.teamConfiguration[0].ToString();
        team2Value.text = matchSetting.teamConfiguration[1].ToString();
        maxPointsValue.text = matchSetting.maxPoints.ToString();
        maxTimeValue.text = TimeToString(matchSetting.maxTime);
        respawnTimeValue.text = matchSetting.timeToRespawn.ToString("F0");
    }

    protected string TimeToString(float time)
    {
        if(time == Mathf.Infinity)
        {
            return "No time";
        }
        else{
            float minPart = Mathf.Floor(time/60);
            float secPart = Mathf.Floor(time - minPart * 60);

            return minPart.ToString("F0") + ":" + string.Format("{0:00.}", secPart);
        }
    }

    public MatchSetting GetMatchSetting()
    {
        return matchSetting;

    }

    public void SetMatchSetting(MatchSetting newSetting)
    {
        matchSetting = newSetting;
        UpdateText();
    }
}
