using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapSelector : MonoBehaviour
{
    public MapCollection mapCollection;
    public TMPro.TMP_Dropdown dropdown;

    void Start()
    {
        int currentValue = PlayerPrefs.GetInt("LastMap", 0);


        dropdown.ClearOptions();
        List<TMPro.TMP_Dropdown.OptionData> options = new List<TMPro.TMP_Dropdown.OptionData>();

        foreach(var map in mapCollection.mapOptions)
        {
            TMPro.TMP_Dropdown.OptionData option = new TMPro.TMP_Dropdown.OptionData(map.mapName,map.mapThumbnail);

            options.Add(option);
             
        }

        dropdown.AddOptions(options);

        if(currentValue < 0 || currentValue >= options.Count)
            currentValue = 0;
        dropdown.value = currentValue;

        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }

    public void MakeDropdownActive(bool state)
    {
        dropdown.gameObject.SetActive(state);
    }

    public MapOption GetSelectedMapOption()
    {
        return mapCollection.mapOptions[dropdown.value];
    }

    public void OnDropdownValueChanged(int newValue)
    {
        PlayerPrefs.SetInt("LastMap", newValue);
    }


}
