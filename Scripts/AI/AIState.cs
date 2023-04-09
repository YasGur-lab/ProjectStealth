using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

//The base AI state.  This is an abstract class that defines the interface that all of the AI states
//should follow.  It also holds some common data and functions that should be usefull for all of its
//child classes.
public abstract class AIState
{
    public AIState(Player owningCharacter, AIPlayerController aiController)
    {
        Owner = owningCharacter;
        AIController = aiController;
    }

    public abstract void Activate();

    public abstract void Deactivate();

    public abstract void Update();

    public abstract string GetName();

    public virtual void GetDebugOutput(StringBuilder debugOutput) { }

    public virtual Vector3 GetMoveInput()
    {
        //Use the desired velocity from the nav mesh agent to determine the move direction
        Vector3 worldMoveDir = AIController.NavAgent.desiredVelocity;
        worldMoveDir.Normalize();

        Quaternion controlRotation = Quaternion.Euler(AIController.GetControlRotation());
        Quaternion invControlRotation = Quaternion.Inverse(controlRotation);

        Vector3 localMoveDir = invControlRotation * worldMoveDir;

        return localMoveDir;
    }

    public Player Owner { get; private set; }

    public AIPlayerController AIController { get; private set; }
}
