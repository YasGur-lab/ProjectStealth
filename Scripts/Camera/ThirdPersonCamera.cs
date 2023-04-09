using UnityEngine;
using System.Collections;
using System.Runtime.Serialization;
using System.IO;
using TMPro;

public class ThirdPersonCamera : MonoBehaviour, Saveable
{
    public FollowCameraBehaviour FollowCameraBehaviour;
    public LedgeCameraBehaviour LedgeCameraBehaviour;
    public bool m_PlayerIsInFOV;
    private Transform m_AITransform;
    public bool m_IsLookingAtPlayer;

    public GameObject m_DeadText;
    public GameObject m_GGText;
    void Awake()
    {
    }

  void Update()
  {
        if(m_LedgeDir.sqrMagnitude > MathUtils.CompareEpsilon)
        {
            SetCameraBehaviour(LedgeCameraBehaviour);
        }
        else if (m_PlayerIsInFOV)
        {

        }
        else
        {
            SetCameraBehaviour(FollowCameraBehaviour);
        }
  }

  void LateUpdate()
  {
      //Check if the camera was initialized
      if (m_Player == null)
      {
          return;
      }

      if (m_PlayerIsInFOV == false)
      {
          //If control rotation is locked don't unlock it till the player stops pressing a direction
          if (m_LockControlRotation)
          {
              Vector3 moveInput = m_Player.Controller.GetMoveInput();
              m_LockControlRotation = moveInput.sqrMagnitude > MathUtils.CompareEpsilon;
          }

          if (m_CurrentBehaviour != null)
          {
              m_CurrentBehaviour.UpdateCamera();

              if (!m_LockControlRotation)
              {
                  ControlRotation = m_CurrentBehaviour.GetControlRotation();
              }
          }
      }
  }

  public void SetEnemy(Transform aiTransform)
  {
      if (m_IsLookingAtPlayer == false)
      {
          m_DeadText.SetActive(true);
          Camera.main.transform.position = aiTransform.position + new Vector3(0, 0, -1);
          Camera.main.transform.forward = aiTransform.forward;
      }
  }

  public void SetPlayer(Player player)
  {
      m_Player = player;

      //Get Follow and look objects
      if (m_Player != null)
      {
          LookPos = m_Player.transform.position;
      }

      //Setup camera behaviours
      FollowCameraBehaviour.Init(this, m_Player);
      LedgeCameraBehaviour.Init(this, m_Player);
      //Set initial behaviour
      SetCameraBehaviour(FollowCameraBehaviour);
  }

  public void UpdateRotation(float yawAmount, float pitchAmount)
  {
      if (m_CurrentBehaviour != null)
      {
          m_CurrentBehaviour.UpdateRotation(yawAmount, pitchAmount);
      }
  }

  public void SetFacingDirection(Vector3 direction)
  {
      if (m_CurrentBehaviour != null)
      {
          m_CurrentBehaviour.SetFacingDirection(direction);
      }
  }

    public void AddLedgeDir(Vector3 ledgeDir)
    {
        m_LedgeDir += ledgeDir;
    }

    public Vector3 LedgeDir
    {
        get
        {
           return m_LedgeDir.normalized;
        }
    }

  public Vector3 ControlRotation { get; private set; }

  public Vector3 LookPos { get; set; }

  public Vector3 PivotRotation { get; set; }

  void SetCameraBehaviour(CameraBehaviour behaviour)
  {
     //Ignore the behaviour if it's the same as before
     if (m_CurrentBehaviour == behaviour)
     {
         return;
     }

        //Init values used for control rotation locking
        bool oldUsesStandardControlRotation = true;
        bool newUsesStandardControlRotation = true;

        //Deactivate old behaviour
     if (m_CurrentBehaviour != null)
     {
         oldUsesStandardControlRotation = m_CurrentBehaviour.UsesStandardControlRotation();
         m_CurrentBehaviour.Deactivate();
     }

     //Set new behaviour
     m_CurrentBehaviour = behaviour;

     //Activate new behaviour
     if (m_CurrentBehaviour != null)
     {
         m_CurrentBehaviour.Activate();
         newUsesStandardControlRotation = m_CurrentBehaviour.UsesStandardControlRotation();
     }

        //Set control rotation lock if needed.  If either behaviour uses a non standard
        //control rotation we will lock it.
      m_LockControlRotation = !oldUsesStandardControlRotation || !newUsesStandardControlRotation;
  }

    public void OnSave(Stream stream, IFormatter formatter)
    {
        SaveUtils.SerializeVector3(stream, formatter, transform.position);
        SaveUtils.SerializeQuaternion(stream, formatter, transform.rotation);

        SaveUtils.SerializeVector3(stream, formatter, ControlRotation);
        SaveUtils.SerializeVector3(stream, formatter, LookPos);
        SaveUtils.SerializeVector3(stream, formatter, PivotRotation);
        SaveUtils.SerializeVector3(stream, formatter, m_LedgeDir);
    }

    public void OnLoad(Stream stream, IFormatter formatter)
    {
        transform.position = SaveUtils.DeserializeVector3(stream, formatter);
        transform.rotation = SaveUtils.DeserializeQuaternion(stream, formatter);

        ControlRotation = SaveUtils.DeserializeVector3(stream, formatter);
        LookPos = SaveUtils.DeserializeVector3(stream, formatter);
        PivotRotation = SaveUtils.DeserializeVector3(stream, formatter);
        m_LedgeDir = SaveUtils.DeserializeVector3(stream, formatter);
    }


    CameraBehaviour m_CurrentBehaviour;
    Player m_Player;
    Vector3 m_LedgeDir;
    bool m_LockControlRotation;
}
