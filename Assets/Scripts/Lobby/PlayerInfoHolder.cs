using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInfoHolder : MonoBehaviour
{
    public  PlayerInfo playerInfo;

    public TMPro.TextMeshProUGUI textPlayerName;
    public TMPro.TextMeshProUGUI textTank;
    public TMPro.TextMeshProUGUI textTeam;
    public Image imageRole;
    public Image imageReady;

    public Sprite[] rolesSprites;
    public Sprite[] readySprites;

    public void SetPlayerInfo( PlayerInfo info){
        playerInfo = info;
        textPlayerName.text = info.name;
        textTank.text = (info.tankID + 1).ToString();
        textTeam.text = info.team.ToString();

        Sprite toUse = rolesSprites[0];
        int roleToInt = (int) info.role;
        if(roleToInt < rolesSprites.Length) toUse = rolesSprites[roleToInt];
        imageRole.sprite = toUse;

        toUse = (info.ready) ? readySprites[1] : readySprites[0];
        imageReady.sprite = toUse;
    }
}
