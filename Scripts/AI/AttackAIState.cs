
using UnityEngine;

class AttackAIState : AIState
{
    public AttackAIState(Player owningCharacter, AIPlayerController aiController)
        : base(owningCharacter, aiController)
    {
    }

    public override void Activate()
    {
        AIController.NavAgent.isStopped = true;
        //AIController.NavAgent.updateRotation = false;
    }

    public override void Deactivate()
    {
        AIController.NavAgent.updateRotation = true;
        AIController.NavAgent.isStopped = false;
        AIController.UseItem = false;
    }

    public override void Update()
    {

        //If you don't have a target, wander
        if (AIController.Target == null)
        {
            return;
        }

        //If you are close enough, attack.  Otherwise get closer.
        float distFromTargetSqrd = (AIController.Target.transform.position - Owner.transform.position).sqrMagnitude;

        if (AIController.IsPlayerInFieldOfView())
        {
            if (Object.FindObjectOfType<ThirdPersonCamera>().m_IsLookingAtPlayer == false)
            {
                Object.FindObjectOfType<ThirdPersonCamera>().m_PlayerIsInFOV = true;
                Object.FindObjectOfType<ThirdPersonCamera>().SetEnemy(AIController.transform);
                Object.FindObjectOfType<ThirdPersonCamera>().m_IsLookingAtPlayer = true;
                AIController.Target.GetComponent<Player>().ResetPlayer();
            }

            AIController.UseItem = true;
        }
        else
        {
            Object.FindObjectOfType<ThirdPersonCamera>().m_PlayerIsInFOV = false;
            Object.FindObjectOfType<ThirdPersonCamera>().m_IsLookingAtPlayer = false;
            AIController.SetState(new WanderingAIState(Owner, AIController));
        }

        //Aim towards target
        UpdateAimDirection();
    }

    

    public override string GetName()
    {
        return "Attack State";
    }

    private void UpdateAimDirection()
    {
        if (AIController.Target == null)
        {
            return;
        }

        AIController.AimPosition = AIController.Target.transform.position;
        AIController.transform.LookAt(AIController.Target.transform.position);
    }
}
