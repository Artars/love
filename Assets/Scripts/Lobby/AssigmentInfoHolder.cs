using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AssigmentInfoHolder : MonoBehaviour
{

    public LobbyManager.InfoTank infoTank;

    [Header("References")]
    public Button button;
    public TMPro.TextMeshProUGUI playerText;
    public Image roleImage;
    public Sprite[] rolesSprites;

    [Header("Colors")]
    public Color colorAvailable = Color.gray;
    public Color colorOcupied = Color.red;
    public Color colorUserSeletion = Color.green;

    public void SetAssigmentInfo(LobbyManager.RoleAssigment info, DictionaryIntPlayerInfo playerDict, int playerConnectionId)
    {
        int roleInInt = (int) info.role;
        roleImage.sprite = rolesSprites[roleInInt];

        if(info.playerAssigned != -1)
        {
            playerText.text = playerDict[info.playerAssigned].name;
            
            if(info.playerAssigned == playerConnectionId)
            {
                roleImage.color = colorUserSeletion;
                button.interactable = true;
            }
            else
            {
                roleImage.color = colorOcupied;
                button.interactable = false;
            }
        }
        else
        {
            playerText.text = "";
            roleImage.color = colorAvailable;
            button.interactable = true;
        }

    }



}
