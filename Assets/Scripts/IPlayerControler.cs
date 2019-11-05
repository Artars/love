using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlayerControler
{
    void AssignTank(int team, Role role, Tank tank);
    void RemoveOwnership();
    void PlayVictoryMusic();
    void ReceiveDamageFromDirection(float damage, float angle, Transform firstPersonCamera);
    void ForcePilotStop();
    void ShowHitmark(Vector3 position);
    void HideHUD();
    void TryToAssignCallback();
    void AddMessage(PlayerMessage newMessage);
    Player.Mode CurrentMode
    {
        get;set;
    }
    bool CanSwitchRoles
    {
        get;set;
    }
}
