using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInfoHolder : MonoBehaviour
{
    public LobbyManager.PlayerInfo playerInfo;

    public TMPro.TextMeshProUGUI textPlayerName;
    public TMPro.TextMeshProUGUI textTank;
    public TMPro.TextMeshProUGUI textTeam;
    public Image imageRole;

    public Sprite[] rolesSprites;

    public void SetPlayerInfo(LobbyManager.PlayerInfo info){
        playerInfo = info;
        textPlayerName.text = info.name;
        textTank.text = (info.tankID + 1).ToString();
        textTeam.text = info.team.ToString();

        Sprite toUse = rolesSprites[0];
        int roleToInt = (int) info.role;
        if(roleToInt < rolesSprites.Length) toUse = rolesSprites[roleToInt];
        imageRole.sprite = toUse;
    }
}
