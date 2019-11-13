using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Panda;

public class AIControler : NetworkBehaviour, IPlayerControler
{
    public Player.Mode CurrentMode {
        get 
        {
            return _currentMode;
        } 
        set 
        {
            _currentMode = value;
        
           if(_currentMode == Player.Mode.Playing)
           {
               tankAI.Reset();
           }
        } 
    }
    public bool CanSwitchRoles {
        get {
            return false;
        }
        set {
            
        }
    }
    
    protected Player.Mode _currentMode = Player.Mode.Selecting;

    public PandaBehaviour behaviour;
    public TankAI tankAI;

    protected Tank tankRef;
    protected int team;

    void Start()
    {
        if(behaviour == null)
        {
            behaviour = GetComponent<PandaBehaviour>();
        }
        if(tankAI == null)
        {
            tankAI = GetComponent<TankAI>();
        }
    }

    void Update()
    {
        if(_currentMode == Player.Mode.Playing && tankRef != null)
        {
            tankAI.CallUpdate(Time.deltaTime);
        }
    }

    public void AddMessage(PlayerMessage newMessage)
    {

    }

    public void AssignTank(int team, Role role, Tank tank)
    {
        if(!isServer) return;
        tankRef = tank;
        this.team = team;

        tankAI.SetTank(tankRef);
        tank.SetNavMeshEnabled(true);
    }

    public void RemoveOwnership()
    {
        if(!isServer) return;
        tankRef.SetNavMeshEnabled(false);
        tankRef = null;
        tankAI.RemoveTank();
    }

    public void ForcePilotStop()
    {

    }

    public void HideHUD()
    {

    }

    public void PlayVictoryMusic()
    {

    }

    public void ReceiveDamageFromDirection(float damage, float angle, Transform firstPersonCamera)
    {

    }


    public void ShowHitmark(Vector3 position)
    {

    }

    public void TryToAssignCallback()
    {

    }
}
