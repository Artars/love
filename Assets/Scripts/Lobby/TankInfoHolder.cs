using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class TankInfoHolder : MonoBehaviour
{
    public  InfoTank infoTank;

    [Header("Refences")]
    public TMPro.TextMeshProUGUI textTank;
    public Image[] rolesImages;
    public Sprite[] rolesSprites;
    public Button selectButton;

    [Header("Colors")]
    public Color colorAvailable = Color.gray;
    public Color colorOcupied = Color.red;
    public Color colorUserSeletion = Color.green;


    public void SetTankInfo( InfoTank info, int playerConnectionId){
        infoTank = info;

        textTank.text = "Tank " + info.name;

        for(int i = 0; i < rolesImages.Length; i++) {
            if(i < info.assigments.Length){
                Sprite toUse = rolesSprites[0];
                int roleToInt = (int) info.assigments[i].role;
                if(roleToInt < rolesSprites.Length) toUse = rolesSprites[roleToInt];
                rolesImages[i].sprite = toUse;
                rolesImages[i].gameObject.SetActive(true);
                if(info.assigments[i].playerAssigned == -1) {
                    rolesImages[i].color = colorAvailable;
                }
                else if (info.assigments[i].playerAssigned == playerConnectionId){
                    rolesImages[i].color = colorUserSeletion;
                }
                else {
                    rolesImages[i].color = colorOcupied;
                }
            }
            // Deactivate if have more images than roles
            else {
                rolesImages[i].gameObject.SetActive(false);
            }
        } 
    }


}
