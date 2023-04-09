using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using System.Text;


class WanderingAIState : AIState
{
    //private Vector3 Destination;
    private float m_Timer = 2.0f;

    private bool m_Start;
    //private BoxCollider m_Boundries;
    public WanderingAIState(Player owningCharacter, AIPlayerController aiController)
        : base(owningCharacter, aiController)
    {
    }

    public override void Activate()
    {
        m_Timer = 6.0f;

        if(AIController.m_CurrentDestination == null)
            AIController.m_CurrentDestination = AIController.m_StartWanderingPos;

        AIController.NavAgent.updateRotation = true;
    }

    public override void Deactivate()
    {
    }

    public override void Update()
    {
        if (DistanceFromLocation(Owner.transform.position) <= 10.0f && AIController.IsPlayerInFieldOfView())
        {
            if (AIController.Target == null)
            {
                AIController.Target = AIUtils.FindClosestObjectInRadius(
                    Owner.transform.position,
                    AIController.MaxSightRange,
                    (obj) => (obj.tag == "Player" && obj != Owner.gameObject)
                );
            }

            if (AIController.Target != null)
                AIController.SetState(new AttackAIState(Owner, AIController));
        }

        if (AIController.IsPlayerInFieldOfView() == false)
        {
            AIController.Target = null;
        }
        
        if (AIController.Target == null && DistanceFromLocation(AIController.m_CurrentDestination.position) >= 1.0f)
        {
            AIController.SetState(this);

            AIController.NavAgent.SetDestination(AIController.m_CurrentDestination.position);
        }
        else if (AIController.Target == null && DistanceFromLocation(AIController.m_CurrentDestination.position) < 2.0f)
        {
            AIController.SetState(this);

            if (AIController.m_CurrentDestination == AIController.m_StartWanderingPos)
                AIController.m_CurrentDestination = AIController.m_EndWanderingPos;
            else if(AIController.m_CurrentDestination == AIController.m_EndWanderingPos)
                AIController.m_CurrentDestination = AIController.m_StartWanderingPos;
            AIController.NavAgent.SetDestination(AIController.m_CurrentDestination.position);
        }
    }

    float DistanceFromLocation(Vector3 destination)
    {
        float des = (AIController.transform.position - destination).sqrMagnitude;
        return des;
    }

    public override string GetName()
    {
        return "Move to Wandering State";
    }

    public override void GetDebugOutput(StringBuilder debugOutput)
    {
        debugOutput.AppendLine("Off mesh link info:");
        debugOutput.AppendFormat("   On off mesh link:  {0}\n", AIController.NavAgent.isOnOffMeshLink);

        debugOutput.AppendFormat("   current link type: {0}\n", AIController.NavAgent.currentOffMeshLinkData.linkType.ToString());

        debugOutput.AppendFormat("   next link type: {0}\n", AIController.NavAgent.nextOffMeshLinkData.linkType.ToString());
    }
}

