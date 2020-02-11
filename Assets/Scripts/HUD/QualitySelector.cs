using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QualitySelector : MonoBehaviour
{
    public Dropdown dropdown;
    public bool applyExpensiveChanges = false;
    protected string[] qualityNames;
    protected int currentSetting;
    protected List<Dropdown.OptionData> dropdownOptions;
    


    void Start()
    {
        qualityNames = QualitySettings.names;
        currentSetting = QualitySettings.GetQualityLevel();
        
        GenerateOptions();
        dropdown.AddOptions(dropdownOptions);
        dropdown.value = currentSetting;
        dropdown.onValueChanged.AddListener(OnValueChanged);

    }

    void GenerateOptions()
    {
        dropdownOptions = new List<Dropdown.OptionData>();
        foreach (var option in qualityNames)
        {
            dropdownOptions.Add(new Dropdown.OptionData(option));
        }
    }

    public void OnValueChanged(int newIndex)
    {
        QualitySettings.SetQualityLevel(newIndex, applyExpensiveChanges);
    }


}
