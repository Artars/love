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
    public List<GoalPoint> goals = new List<GoalPoint>();

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

        // Avoid spectator having a goal
        if(team == -1) return;
        GameStatus.SyncListGoal goalList = (team == 1) ? GameStatus.instance.goalIdentitiesTeam1 : GameStatus.instance.goalIdentitiesTeam0;
        
        //Subscribe to changes in the goal
        goalList.Callback += (GoalCallBack);
        for (int i = 0; i < goalList.Count; i++)
        {
            goals.Add(goalList[i].GetComponent<GoalPoint>());
        }
    }

    protected void GoalCallBack(GameStatus.SyncListGoal.Operation operation, int index, NetworkIdentity oldItem, NetworkIdentity newItem) {
        // Get team
        if(team == -1) return;
        GameStatus.SyncListGoal goalList = (team == 1) ? GameStatus.instance.goalIdentitiesTeam1 : GameStatus.instance.goalIdentitiesTeam0;
        
        switch(operation)
        {
            case GameStatus.SyncListGoal.Operation.OP_ADD:
                goals.Add(newItem.GetComponent<GoalPoint>() );
                break;
            case GameStatus.SyncListGoal.Operation.OP_REMOVE:
                goals.Remove(newItem.GetComponent<GoalPoint>());
                break;
            case GameStatus.SyncListGoal.Operation.OP_REMOVEAT:
                goals.RemoveAt(index);
                break;
            case GameStatus.SyncListGoal.Operation.OP_INSERT:
                goals.Insert(index, newItem.GetComponent<GoalPoint>());
                break;
            case GameStatus.SyncListGoal.Operation.OP_CLEAR:
                goals.Clear();
                break;
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

        tankAI.SetTank(tankRef,this);
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
        if(!isServer) return;
        tankAI.StunPilot();
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
