using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Flag : NetworkBehaviour
{
    [Header("References")]
    public MeshRenderer flagMesh;
    public Transform flagTransform;
    public Material flagMaterialTeam0;
    public Material flagMaterialTeam1;
    public Material flagMaterialTeamNeutral;
    public Transform flagTopPosition;
    public Transform flagBottomPosition;

    [Header("CurrentState")]
    [SyncVar(hook="SetFlagTeam")]
    public int currentTeam = -1;

    [SyncVar(hook="SetFlagVisibility")]
    public bool  isFlagVisible = false;

    public void Start()
    {
        SetFlagTeam(currentTeam);
        SetFlagVisibility(isFlagVisible);
    }

    [Server]
    public void SetFlagPosition(float porcent)
    {
        if(flagTransform == null) return;
        flagTransform.position = Vector3.Lerp(flagBottomPosition.position,flagTopPosition.position,porcent);
    }

    /// <summary>
    /// Use 0 for team 0, 1 for team 1 and -1 for no team
    /// </summary>
    /// <param name="team"></param>
    public void SetFlagTeam(int team)
    {
        if(flagMesh == null) return;
        if(isServer)
            this.currentTeam = team;
        Material toUse = (team > -1) ? (team == 0) ? flagMaterialTeam0 : flagMaterialTeam1 : flagMaterialTeamNeutral;
        flagMesh.material = toUse;
    }

    public void SetFlagVisibility(bool hide)
    {
        if(isServer)
        {
            isFlagVisible = hide;
        }

        flagTransform.gameObject.SetActive(hide);
    }

}
