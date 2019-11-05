using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class AIControler : NetworkBehaviour, IPlayerControler
{
    public Player.Mode CurrentMode { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    public bool CanSwitchRoles { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public void AddMessage(PlayerMessage newMessage)
    {
        throw new System.NotImplementedException();
    }

    public void AssignTank(int team, Role role, Tank tank)
    {
        throw new System.NotImplementedException();
    }

    public void ForcePilotStop()
    {
        throw new System.NotImplementedException();
    }

    public void HideHUD()
    {
        throw new System.NotImplementedException();
    }

    public void PlayVictoryMusic()
    {
        throw new System.NotImplementedException();
    }

    public void ReceiveDamageFromDirection(float damage, float angle, Transform firstPersonCamera)
    {
        throw new System.NotImplementedException();
    }

    public void RemoveOwnership()
    {
        throw new System.NotImplementedException();
    }

    public void ShowHitmark(Vector3 position)
    {
        throw new System.NotImplementedException();
    }

    public void TryToAssignCallback()
    {
        throw new System.NotImplementedException();
    }
}
