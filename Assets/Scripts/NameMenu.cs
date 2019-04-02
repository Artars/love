﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NameMenu : MonoBehaviour
{
    public int mininumNameSize = 1;
    public int maximumNameSize = 10;

    [Header("References")]
    public GameObject panel;
    public InputField inputField;
    public Button cancelButton;
    public Button confirmButton;

    protected bool hasKey = true;

    public void Start() {
        if(!PlayerPrefs.HasKey("Name")){
            panel.SetActive(true);
            hasKey = false;
            cancelButton.interactable = false;
            confirmButton.interactable = false;
        }
        else {
            panel.SetActive(false);
        }

        inputField.characterLimit = maximumNameSize;
    }

    public void ValidateName() {
        //Verify if meets the minimun text size
        if(inputField.text.Length < mininumNameSize) {
            confirmButton.interactable = false;
        }
        else {
            confirmButton.interactable = true;
        }

        //Clamp the text size if it's too long
        if(inputField.text.Length > maximumNameSize){
            inputField.text.Remove(maximumNameSize);
        }
    }

    public void OnCancelButtonClick() {
        inputField.text = "";
        panel.SetActive(false);
    }

    public void OnConfirmButtonClick() {
        PlayerPrefs.SetString("Name", inputField.text);
        cancelButton.interactable = true;
        
        inputField.text = "";
        panel.SetActive(false);

    }
}