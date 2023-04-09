using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.EventSystems.EventTrigger;
using Debug = UnityEngine.Debug;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AIPlayerController : MonoBehaviour, PlayerController
{
    public Transform m_StartWanderingPos;
    public Transform m_EndWanderingPos;
    public Transform m_CurrentDestination;

    //AI Settings.  These will be shared with all of the AI States
    public float MaxAttackRange = 15.0f;

    public float MaxSightRange = 50.0f;

    public float MinTimeToChangeDirection = 1.0f;
    public float MaxTimeToChangeDirection = 5.0f;

    public float ArriveAtDestinationDist = 2.0f;

    public int PreferedWeaponIndex = 0;

    public bool UseNavMeshAgentMovement = false;

    public GameObject PlayerRef;

    public void Init(Player owner)
    {
        Owner = owner;

        m_ItemToSwitchTo = PreferedWeaponIndex;

        //Set up nav mesh   
        NavAgent = GetComponent<NavMeshAgent>();

        //We want to use the actual player's movement instead of the nav mesh movement.  This will turn
        //off the nav mesh agent automatic movement.
        if (!UseNavMeshAgentMovement)
        {
            NavAgent.updatePosition = false;
        }
        else
        {
            NavAgent.updatePosition = true;
            NavAgent.updateRotation = true;
        }
        SetState(new WanderingAIState(Owner, this));
    }

    public void UpdateControls()
    {
        //Update the state if you have one
        if (m_CurrentAIState != null)
        {
            m_CurrentAIState.Update();
        }

        //Since NavMeshAgent.updatePosition is false, the AI's position will not be automatically be
        //synchronized with the internal NavMeshAgent position.  This call will update the position
        //within the NavMeshAgent
        if (!UseNavMeshAgentMovement)
        {
            NavAgent.nextPosition = transform.position;
        }

        //Update debug info
        UpdateDebugDisplay();
    }

    public AIState GetState()
    {
        return m_CurrentAIState;
    }
    public void SetState(AIState state)
    {
        if (state != null && m_CurrentAIState != null && state.GetName() == m_CurrentAIState.GetName())
            return; // needs to be removed

        //Deactivate your old state
        if (m_CurrentAIState != null)
        {
            m_CurrentAIState.Deactivate();
        }

        //switch to the new state
        m_CurrentAIState = state;

        //Activate the new state
        if (m_CurrentAIState != null)
        {
            m_CurrentAIState.Activate();
        }
    }

    void Update()
    {
    }

    public void SetFacingDirection(Vector3 direction)
    {
        Owner.transform.rotation = Quaternion.LookRotation(direction);
    }

    public void AddLedgeDir(Vector3 ledgeDir)
    {
    }

    private float GetDistanceFromPlayer()
    {
        float distance = Vector3.Distance(transform.position, PlayerRef.transform.position);
        return distance;
    }

    public bool IsPlayerInFieldOfView()
    {
        Vector3 direction = PlayerRef.transform.position - transform.position;
        float angle = Vector3.Angle(direction, transform.forward);

        if (angle < 45 && GetDistanceFromPlayer() <= 10)
        {
            return true;
        }
        return false;
    }

    public Player Owner { get; private set; }

    public GameObject Target { get; set; }

    public Vector3 AimPosition { get; set; }

    public bool UseItem { get; set; }

    public UnityEngine.AI.NavMeshAgent NavAgent { get; private set; }

    #region Input Getting Functions

    public Vector3 GetControlRotation()
    {
        Vector3 lookDirection = AimPosition - transform.position;
        lookDirection.y = 0.0f;

        if (lookDirection.sqrMagnitude > MathUtils.CompareEpsilon)
        {
            return Quaternion.LookRotation(lookDirection).eulerAngles;
        }
        else
        {
            return Vector3.zero;
        }
    }

    public Vector3 GetMoveInput()
    {
        if (m_CurrentAIState != null)
        {
            return m_CurrentAIState.GetMoveInput();
        }
        else
        {
            return Vector3.zero;
        }
    }

    public Vector3 GetLookInput()
    {
        return Vector3.zero;
    }

    public Vector3 GetAimTarget()
    {
        return AimPosition;
    }

    public bool IsJumping()
    {
        return false;
    }

    public bool IsFiring()
    {
        return UseItem;
    }

    public bool IsAiming()
    {
        return false;
    }

    public bool ToggleCrouch()
    {
        return false;
    }

    public bool SwitchToItem1()
    {
        return HandleItemSwitch(0);
    }

    public bool SwitchToItem2()
    {
        return HandleItemSwitch(1);
    }

    public bool SwitchToItem3()
    {
        return HandleItemSwitch(2);
    }

    public bool SwitchToItem4()
    {
        return HandleItemSwitch(3);
    }

    public bool SwitchToItem5()
    {
        return HandleItemSwitch(4);
    }

    public bool SwitchToItem6()
    {
        return HandleItemSwitch(5);
    }

    public bool SwitchToItem7()
    {
        return HandleItemSwitch(6);
    }

    public bool SwitchToItem8()
    {
        return HandleItemSwitch(7);
    }

    public bool SwitchToItem9()
    {
        return HandleItemSwitch(8);
    }

    public Vector3 GetLedgeDir()
    {
        return Vector3.zero;
    }

    #endregion


    #region Private Members

    [Conditional("UNITY_EDITOR")]
    void UpdateDebugDisplay()
    {
        //Display useful vectors
        UnityEngine.Debug.DrawLine(transform.position, NavAgent.destination);
        UnityEngine.Debug.DrawLine(transform.position, transform.position + NavAgent.desiredVelocity, Color.red);

        //Display to the AI debug GUI
        AIDebugGUI debugGUI = Camera.main.GetComponent<AIDebugGUI>();
        if (debugGUI != null)
        {
            //Ignore this if the object isn't selected.  Note that this won't work properly if there is more
            //than one ai entity selected.
            if (Selection.Contains(gameObject))
            {
                StringBuilder debugOutput = new StringBuilder();

                //Output state
                debugOutput.Append("CurrentState = ");

                if (m_CurrentAIState != null)
                {
                    debugOutput.Append(m_CurrentAIState.GetName());
                }
                else
                {
                    debugOutput.Append("null");
                }

                //Ouput other debug info
                if (m_CurrentAIState != null)
                {
                    debugOutput.AppendLine();

                    m_CurrentAIState.GetDebugOutput(debugOutput);
                }

                //Set Debug String
                debugGUI.AIDebugDisplayMsg = debugOutput.ToString();
            }
        }
    }

    bool HandleItemSwitch(int indexToCheck)
    {
        if (indexToCheck == m_ItemToSwitchTo)
        {
            m_ItemToSwitchTo = InvalidWeaponIndex;

            return true;
        }
        else
        {
            return false;
        }
    }

    const int InvalidWeaponIndex = -1;

    AIState m_CurrentAIState;

    int m_ItemToSwitchTo;

    #endregion
}
