using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.IO;
using UnityEngine.AI;

public class Player : MonoBehaviour, Saveable
{
	public Transform LocalFireTransform;
	public float InAirMoveAccel = 30.0f;
	public float InAirMaxHorizSpeed = 20.0f;
	public float InAirMaxVertSpeed = 50.0f;

	public float OnGroundMoveAccel = 10.0f;
	public float OnGroundMaxSpeed = 10.0f;
	public float OnGroundStopEaseSpeed = 10.0f;

	public float MaxOnGroundAnimSpeed = 10.0f;

	public bool InstantStepUp = false;
	public float StepUpEaseSpeed = 10.0f;
	public float MinAllowedSurfaceAngle = 15.0f;

	public float GravityAccel = -10.0f;
	public float JumpSpeed = 10.0f;
	public float MaxJumpHoldTime = 0.5f;

	public float GroundCheckStartOffsetY = 0.5f;
	public float CheckForGroundRadius = 0.5f;
	public float GroundResolutionOverlap = 0.05f;

	public float MaxMidAirJumpTime = 0.3f;

	public float JumpPushOutOfGroundAmount = 0.5f;

	public float Mass = 1.0f;

	public GameObject FootLocationObj;

	public float VectorVisualizeScale = 2.0f;

	public float RotateWithVelocityEaseSpeed = 2.0f;
	public float RotateWithVelocityMinSpeed = 1.0f;

	public float JiggleFrequency = 0.0f;
	public float MaxJiggleOffset = 3.0f;

	public float MaxAimAngle = 70.0f;
	public float MinAimAngle = -70.0f;

	public float StartAimingEaseAmount = 5.0f;
	public float StopAimingEaseAmount = 5.0f;

	public float BulletSpeed = 3.0f;
	public GameObject Projectile;
	public float fireFrequency = 0.25f;
    [SerializeField] Renderer m_Renderer;

    [SerializeField] Transform m_PlayerStartingPos;

    [SerializeField] int m_ShaderIndex;

    public PlayerController Controller { get; set; }

	public Vector3 GroundVelocity { get; private set; }
	public Vector3 GroundAngularVelocity { get; private set; }
	public Vector3 GroundNormal { get; private set; }
	public float CenterHeightOffGround { get; private set; }
	public Animator Animator { get; private set; }
	
	public bool OnGround
	{
		get
		{
			return m_MovementState == MovementState.OnGround;
		}
	}
	public Vector3 Velocity
	{
		get
		{
			return m_Velocity;
		}

		set
		{
			m_Velocity = value;
		}
	}

	void Start()
	{
        //Initialize Controller
        //If the player has an AI player controller assume that it's meant to be controlled usingAI.
        //Otherwise set up the player for human control.
        Controller = GetComponent<AIPlayerController>();
        if (Controller != null)
		{
            Controller.Init(this);
            //if (!SetupAIPlayer())
            //{
            //	return;
            //}
        }
        else
        {
            if (!SetupHumanPlayer())
            {
                return;
            }
        }

		m_TimeToNextShot = fireFrequency;
		m_CanFire = true;

		CenterHeightOffGround = transform.position.y - FootLocationObj.transform.position.y;

		//Set up animator
		{
			GameObject modelObject = transform.Find("Model").gameObject;

			Animator = modelObject.GetComponent<Animator>();

			m_AimAnimLayerIndex = Animator.GetLayerIndex("AimingLayer");
		}

		//Initialing miscellaneous values
		m_GroundCheckMask = ~LayerMask.GetMask("Player", "Ignore Raycast");

		m_RigidBody = GetComponent<Rigidbody>();

		m_Velocity = Vector3.zero;

		m_AllowJump = true;

        m_Renderer = GameObject.Find("TestSurface").GetComponent<Renderer>();
        m_Renderer.material.shader = Shader.Find("FieldOfView");
    }

	// Update is called once per frame
	public void Update()
	{
		UpdateJiggles();
		UpdateAnimations();

        if (GetComponent<AIPlayerController>() && m_ShaderIndex == 1)
		{
            m_Renderer.material.SetVector("_Position1", transform.position);
            m_Renderer.material.SetVector("_Direction1", GetComponent<AIPlayerController>().NavAgent.transform.forward);
        }
        else if (GetComponent<AIPlayerController>() && m_ShaderIndex == 2)
        {
            m_Renderer.material.SetVector("_Position2", transform.position);
            m_Renderer.material.SetVector("_Direction2", GetComponent<AIPlayerController>().NavAgent.transform.forward);
        }
        else if (GetComponent<AIPlayerController>() && m_ShaderIndex == 3)
        {
            m_Renderer.material.SetVector("_Position3", transform.position);
            m_Renderer.material.SetVector("_Direction3", GetComponent<AIPlayerController>().NavAgent.transform.forward);
        }
    }


	void FixedUpdate()
	{
		//Update velocity from physics system
		m_Velocity = m_RigidBody.velocity;

		//Update ground info
		UpdateGroundInfo();

		//Get input
		Controller.UpdateControls();

		if (!m_CanFire)
		{
			m_TimeToNextShot += Time.fixedDeltaTime;

			if (m_TimeToNextShot >= fireFrequency)
			{
				m_CanFire = true;
				m_TimeToNextShot = 0.0f;
			}
		}


		if (m_CanFire && Controller.IsFiring() && IsReadyToFire())
		{
			m_CanFire = false;

			Vector3 firePos = LocalFireTransform.position;
			Vector3 fireDir = Controller.GetAimTarget() - firePos;

			fireDir.Normalize();

			Quaternion fireRot = Quaternion.Euler(fireDir);

			GameObject Bullet = PoolManager.Get(Projectile, firePos, fireRot);
			Bullet.GetComponent<Bullet>().Init(firePos, fireDir);

		}


		Vector3 localMoveDir = Controller.GetMoveInput();

		localMoveDir.Normalize();

		bool isJumping = Controller.IsJumping();

		//Update movement
		switch (m_MovementState)
		{
			case MovementState.OnGround:
				UpdateOnGround(localMoveDir, isJumping);
				break;
			case MovementState.InAir:
				UpdateInAir(localMoveDir, isJumping);
				break;

			case MovementState.Disable:
				break;

			default:
				DebugUtils.LogError("Invalid movement state: {0}", m_MovementState);
				break;
		}

		//Update rotation
		if(!GetComponent<AIPlayerController>())
		{
			Vector3 goalRotateDir = m_Velocity - GroundVelocity;
			goalRotateDir.y = 0.0f;
			//Figure out aim directions
			Vector3 aimTarget = Controller.GetAimTarget();
			Vector3 aimDirection = aimTarget - transform.position;

			if (ShouldUseAimingAnim() || goalRotateDir.sqrMagnitude < RotateWithVelocityMinSpeed * RotateWithVelocityMinSpeed)
			{
				Vector3 controlRotation = Controller.GetControlRotation();
				controlRotation.x = 0.0f;

				goalRotateDir = Quaternion.Euler(controlRotation) * Vector3.forward;
				goalRotateDir.y = 0.0f;
				if(Controller.GetLedgeDir().sqrMagnitude >  MathUtils.CompareEpsilon )
				{
					goalRotateDir = Vector3.Cross(Controller.GetLedgeDir(), goalRotateDir);
					goalRotateDir.y = 0.0f;
				}
				
			}
			else
			{
				aimDirection.y = 0.0f;
				aimDirection.Normalize();

				Vector3 aimRightDir = Vector3.Cross(Vector3.up, aimDirection);

				//Figure out the forward and tangent goal directions with respect
				//to the aim direction
				Vector3 forwardGoalDir = Vector3.Project(goalRotateDir, aimDirection);
				Vector3 tangentGoalDir = goalRotateDir - forwardGoalDir;

				//If the goal direction isn't facing forward, make it forward
				if (Vector3.Dot(forwardGoalDir, aimDirection) < 0.0f)
				{
					forwardGoalDir *= -1.0f;
				}
				

				//If the goal direction isn't facing left, make it left
				if (Vector3.Dot(tangentGoalDir, aimRightDir) > 0.0f)
				{
					tangentGoalDir *= -1.0f;
				}

				goalRotateDir = forwardGoalDir + tangentGoalDir;
			}

			Vector3 newRotateDir = MathUtils.SlerpTo(
					RotateWithVelocityEaseSpeed,
					transform.forward,
					goalRotateDir,
					Time.fixedDeltaTime
					);

            //SetFacingDir(newRotateDir);
            transform.rotation = Quaternion.LookRotation(newRotateDir);
		}
	}

	public void UpdateStopping(float stopEaseSpeed)
	{
		//Ease down to the ground velocity to stop relative to the ground
		m_Velocity = MathUtils.LerpTo(stopEaseSpeed, m_Velocity, GroundVelocity, Time.fixedDeltaTime);
	}

	public void SetFacingDir(Vector3 facingDir)
	{
		facingDir.y = 0.0f;

		if (facingDir.sqrMagnitude > MathUtils.CompareEpsilon)
		{
			facingDir.Normalize();

			transform.rotation = Quaternion.LookRotation(facingDir);

			Controller.SetFacingDirection(facingDir);
		}
	}

	public bool IsReadyToFire()
	{
		return m_AimAnimLayerWeight >= 1.0f - MathUtils.CompareEpsilon;
	}
	void UpdateGroundInfo()
	{
		//Clear ground info.  Doing this here can simplify the code a bit since we deal with cases where the
		//ground isn't found more easily
		GroundAngularVelocity = Vector3.zero;
		GroundVelocity = Vector3.zero;
		GroundNormal.Set(0.0f, 0.0f, 1.0f);

		//Check for the ground below the player
		m_CenterHeight = transform.position.y;

		float footHeight = FootLocationObj.transform.position.y;

		float halfCapsuleHeight = m_CenterHeight - footHeight;

		Vector3 rayStart = transform.position;
		rayStart.y += GroundCheckStartOffsetY;

		Vector3 rayDir = Vector3.down;

		float rayDist = halfCapsuleHeight + GroundCheckStartOffsetY - CheckForGroundRadius;

		//Find all of the surfaces overlapping the sphere cast
		RaycastHit[] hitInfos = Physics.SphereCastAll(rayStart, CheckForGroundRadius, rayDir, rayDist, m_GroundCheckMask);

		//Get the closest surface that is acceptable to walk on.  The order of the 
		RaycastHit groundHitInfo = new RaycastHit();
		bool validGroundFound = false;
		float minGroundDist = float.MaxValue;

		foreach (RaycastHit hitInfo in hitInfos)
		{
			//Check the surface angle to see if it's acceptable to walk on.  
			//Also checking if the distance is zero I ran into a case where the sphere cast was hitting a wall and
			//returning weird resuls in the hit info.  Checking if the distance is greater than 0 eliminates this 
			//case. 
			float surfaceAngle = MathUtils.CalcVerticalAngle(hitInfo.normal);
			if (surfaceAngle < MinAllowedSurfaceAngle || hitInfo.distance <= 0.0f)
			{
				continue;
			}

			if (hitInfo.distance < minGroundDist)
			{
				minGroundDist = hitInfo.distance;

				groundHitInfo = hitInfo;

				validGroundFound = true;
			}
		}

		if (!validGroundFound)
		{
			if (m_MovementState != MovementState.Disable)
			{
				SetMovementState(MovementState.InAir);
			}
			return;
		}

		//Step up
		Vector3 bottomAtHitPoint = MathUtils.ProjectToBottomOfCapsule(
			groundHitInfo.point,
			transform.position,
			halfCapsuleHeight * 2.0f,
			CheckForGroundRadius
			);

		float stepUpAmount = groundHitInfo.point.y - bottomAtHitPoint.y;
		m_CenterHeight += stepUpAmount - GroundResolutionOverlap;

		GroundNormal = groundHitInfo.normal;

        WaterBuoyancyHandler buoyancyHandler = groundHitInfo.collider.GetComponent<WaterBuoyancyHandler>();
        if(buoyancyHandler != null)
        {
           buoyancyHandler.ApplyForce(Mass, groundHitInfo.point);
        }
		//Set the movement state to be on ground
		if (m_MovementState != MovementState.Disable)
		{
			SetMovementState(MovementState.OnGround);
		}
	}

	void UpdateOnGround(Vector3 localMoveDir, bool isJumping)
	{
		//if movement is close to zero just stop
		if (localMoveDir.sqrMagnitude > MathUtils.CompareEpsilon)
		{

			//Since the movement calculations are easier to do with out taking the ground velocity into account
			//we are calculating the velocity relative to the ground
			Vector3 localVelocity = m_Velocity - GroundVelocity;

			//The world movement accelration
			Vector3 moveAccel = CalcMoveAccel(localMoveDir);
			//Adjust acceleration to follow slope
			Vector3 groundTangent = moveAccel - Vector3.Project(moveAccel, GroundNormal);
			groundTangent.Normalize();
			moveAccel = groundTangent;

			//The velocity along the movement direction
			Vector3 velAlongMoveDir = Vector3.Project(localVelocity, moveAccel);

			//If we are changing direction, come to a stop first.  This makes the movement more responsive
			//since the stopping happens a lot faster than the acceleration typically allows
			if (Vector3.Dot(velAlongMoveDir, moveAccel) > 0.0f)
			{
				//Use a similar method to stopping to ease the movement to just be in the desired move direction
				//This makes turning more responsive
				localVelocity = MathUtils.LerpTo(OnGroundStopEaseSpeed, localVelocity, velAlongMoveDir, Time.fixedDeltaTime);
			}
			else
			{
				localVelocity = MathUtils.LerpTo(OnGroundStopEaseSpeed, localVelocity, Vector3.zero, Time.fixedDeltaTime);
			}

			//Apply acceleration to velocity
			moveAccel *= OnGroundMoveAccel;

			localVelocity += moveAccel * Time.fixedDeltaTime;

			localVelocity = Vector3.ClampMagnitude(localVelocity, OnGroundMaxSpeed);

			//Update the world velocity
			m_Velocity = localVelocity + GroundVelocity;
		}
		else
		{
			UpdateStopping(OnGroundStopEaseSpeed);
		}

		//Clear jump animation trigger
		Animator.ResetTrigger("JumpActivated");

		//Handle jump input
		if (isJumping)
		{
			ActivateJump();
		}
		else
		{
			m_AllowJump = true;
		}

		//Move the character controller
		ApplyVelocity(m_Velocity);

		//Ease the height up to the step up height
		Vector3 playerCenter = transform.position;

		if (InstantStepUp)
		{
			playerCenter.y = m_CenterHeight;
		}
		else
		{
			playerCenter.y = MathUtils.LerpTo(StepUpEaseSpeed, playerCenter.y, m_CenterHeight, Time.deltaTime);
		}

		transform.position = playerCenter;

		//Reset time in air
		m_TimeInAir = 0.0f;
	}

	void UpdateInAir(Vector3 localMoveDir, bool isJumping)
	{
		//Check if move direction is large enough before applying acceleration
		if (localMoveDir.sqrMagnitude > MathUtils.CompareEpsilon)
		{
			//The world movement accelration
			Vector3 moveAccel = CalcMoveAccel(localMoveDir);

			moveAccel *= InAirMoveAccel;

			m_Velocity += moveAccel * Time.fixedDeltaTime;

			//Clamp velocity
			m_Velocity = MathUtils.HorizontalClamp(m_Velocity, InAirMaxHorizSpeed);

			m_Velocity.y = Mathf.Clamp(m_Velocity.y, -InAirMaxVertSpeed, InAirMaxVertSpeed);
		}

		//Update mid air jump timer and related jump.  This timer is to make jump timing a little more forgiving 
		//by letting you still jump a short time after falling off a ledge.
		if (m_JumpTimeLeft <= 0.0f)
		{
			if (m_TimeLeftToAllowMidAirJump > 0.0f)
			{
				m_TimeLeftToAllowMidAirJump -= Time.fixedDeltaTime;

				if (isJumping)
				{
					ActivateJump();
				}
				else
				{
					m_AllowJump = true;
				}
			}
		}
		else
		{
			m_TimeLeftToAllowMidAirJump = 0.0f;
		}

		//Update gravity and jump height control
		if (m_JumpTimeLeft > 0.0f && isJumping)
		{
			m_JumpTimeLeft -= Time.fixedDeltaTime;
		}
		else
		{
			m_Velocity.y += GravityAccel * Time.fixedDeltaTime;

			m_JumpTimeLeft = 0.0f;
		}

		//Move the character controller
		ApplyVelocity(m_Velocity);

		//Increment time in air
		m_TimeInAir += Time.deltaTime;
	}

	void ApplyVelocity(Vector3 velocity)
	{
		Vector3 velocityDiff = velocity - m_RigidBody.velocity;

		m_RigidBody.AddForce(velocityDiff, ForceMode.VelocityChange);
	}

	void ActivateJump()
	{
		//The allowJump bool is to prevent the player from holding down the jump button to bounce up and down
		//Instead they will have to release the button first.
		if (m_AllowJump)
		{
			//Set the vertical speed to be the jump speed + the ground velocity
			m_Velocity.y = JumpSpeed + GroundVelocity.y;

			//This is to ensure that the player wont still be touching the ground after the jump
			transform.position += new Vector3(0.0f, JumpPushOutOfGroundAmount, 0.0f);

			//Set the jump timer
			m_JumpTimeLeft = MaxJumpHoldTime;

			m_AllowJump = false;

			//Activate jump animation
			Animator.SetTrigger("JumpActivated");
		}
	}

	Vector3 CalcMoveAccel(Vector3 localMoveDir)
	{
		Vector3 controlRotation = Controller.GetControlRotation();
		controlRotation.x = 0.0f;

		Vector3 moveAccel = Quaternion.Euler(controlRotation) * localMoveDir;

		return moveAccel;
	}
	public void SetEnableMovement(bool enable)
	{
		if (enable)
		{
			SetMovementState(MovementState.InAir);
		}
		else
		{
			SetMovementState(MovementState.Disable);
		}
	}

	void SetMovementState(MovementState movementState)
	{
		switch (movementState)
		{
			case MovementState.OnGround:
				m_TimeLeftToAllowMidAirJump = MaxMidAirJumpTime;
				break;

			case MovementState.InAir:
				break;

			case MovementState.Disable:
				m_Velocity = Vector3.zero;
				ApplyVelocity(m_Velocity);
				break;

			default:
				DebugUtils.LogError("Invalid movement state: {0}", movementState);
				break;
		}

		m_MovementState = movementState;
	}

	void UpdateJiggles()
	{
		if (JiggleFrequency <= 0.0f)
		{
			return;
		}

		//Update timer
		m_TimeTillNextJiggle -= Time.deltaTime;

		if (m_TimeTillNextJiggle <= 0.0f)
		{
			m_TimeTillNextJiggle = 1.0f / JiggleFrequency;

			//Approximate normal distribution
			float minRange = -1.0f;
			float maxRange = 1.0f;
			float offsetAmount = UnityEngine.Random.Range(minRange, maxRange);
			offsetAmount += UnityEngine.Random.Range(minRange, maxRange);
			offsetAmount += UnityEngine.Random.Range(minRange, maxRange);
			offsetAmount += UnityEngine.Random.Range(minRange, maxRange);
			offsetAmount /= 4.0f;

			offsetAmount *= MaxJiggleOffset;

			//Offset the player position
			Vector3 offset = UnityEngine.Random.onUnitSphere * offsetAmount;
			offset.y = Mathf.Abs(offset.y);

			transform.position += offset;
		}
	}

	//This function is called when the script is loaded or a value is changed in the inspector.
	//Note that this will only called in the editor.
	void OnValidate()
	{
		m_TimeTillNextJiggle = 0.0f;
	}

	bool SetupHumanPlayer()
	{
		if (LevelManager.Instance.GetPlayer() == null)
		{
			DontDestroyOnLoad(gameObject);

			LevelManager.Instance.RegisterPlayer(this);

			Controller = new MouseKeyPlayerController();

			Controller.Init(this);
            return true;
		}
		else
		{
			Destroy(gameObject);
			return false;
		}
	}

	bool SetupAIPlayer()
	{
		if (LevelManager.Instance.GetAI(GetSaveID()) == null)
		{
			DontDestroyOnLoad(gameObject);

			LevelManager.Instance.RegisterAI(GetSaveID(), this);

			Controller.Init(this);

			return true;
		}
		else
		{
			Destroy(gameObject);
			return false;
		}
	}

	string GetSaveID()
    {
		string SaveId = "";
		SaveHandler saveHandler = GetComponent<SaveHandler>();
		if(saveHandler)
        {
			SaveId = saveHandler.SaveId;

		}

		return SaveId;

	}

	void UpdateAnimations()
	{
		//Update in air related parameters
		Animator.SetBool("OnGround", OnGround);
		Animator.SetFloat("TimeInAir", m_TimeInAir);

		//Update velocity params
		Vector3 localRelativeVelocity = m_Velocity - GroundVelocity;
		localRelativeVelocity = transform.InverseTransformDirection(localRelativeVelocity);
		localRelativeVelocity.y = 0.0f;

		Animator.SetFloat("ForwardSpeed", localRelativeVelocity.z / MaxOnGroundAnimSpeed);
		Animator.SetFloat("RightSpeed", localRelativeVelocity.x / MaxOnGroundAnimSpeed);

		//Update aiming
		if (ShouldUseAimingAnim())
		{
			//Set the aim layer weight
			m_AimAnimLayerWeight = MathUtils.LerpTo(StartAimingEaseAmount, m_AimAnimLayerWeight, 1.0f, Time.deltaTime);
			Animator.SetLayerWeight(m_AimAnimLayerIndex, m_AimAnimLayerWeight);

			//Calcuate the aiming angle based on the aim target
			Vector3 aimDir = Controller.GetAimTarget() - transform.position;
			aimDir.Normalize();

			float verticallookAngle = MathUtils.CalcVerticalAngle(aimDir);

			float aimPercent = Mathf.InverseLerp(MinAimAngle, MaxAimAngle, verticallookAngle);

			Animator.SetFloat("AimAnglePercent", aimPercent);
		}
		else
		{
			//Turn off the aiming animation by easing the weight back to zero.  
			m_AimAnimLayerWeight = MathUtils.LerpTo(StopAimingEaseAmount, m_AimAnimLayerWeight, 0.0f, Time.deltaTime);

			Animator.SetLayerWeight(m_AimAnimLayerIndex, m_AimAnimLayerWeight);
		}
	}

	bool ShouldUseAimingAnim()
	{
		return Controller.IsAiming() || Controller.IsFiring();
	}



    public void ResetPlayer()
    {
        StartCoroutine(ResetPlayerCoroutine());
    }

    public IEnumerator ResetPlayerCoroutine()
    {
        yield return new WaitForSeconds(1.0f);
        transform.position = m_PlayerStartingPos.position;
        transform.rotation = m_PlayerStartingPos.rotation;
        Camera.main.gameObject.GetComponent<ThirdPersonCamera>().m_DeadText.SetActive(false);
        Camera.main.gameObject.GetComponent<ThirdPersonCamera>().m_GGText.SetActive(false);
    }

    public void OnSave(Stream stream, IFormatter formatter)
    {
        SaveUtils.SerializeVector3(stream, formatter, transform.position);
        SaveUtils.SerializeQuaternion(stream, formatter, transform.rotation);
    }

    public void OnLoad(Stream stream, IFormatter formatter)
    {
        transform.position = SaveUtils.DeserializeVector3(stream, formatter);
        transform.rotation = SaveUtils.DeserializeQuaternion(stream, formatter);
       
        Controller.Init(this);
        
    }

    public void OnLevelTransition(Vector3 startPos, Vector3 startDir)
    {
        //Setup position
        transform.position = startPos;

        //Setup look direction
        startDir.y = 0.0f;
        startDir.Normalize();

        transform.rotation = Quaternion.LookRotation(startDir);

        //Clear velocity
        m_Velocity.Set(0.0f, 0.0f, 0.0f);

        ApplyVelocity(m_Velocity);

        //Setup controller
        Controller.Init(this);
    }

	enum MovementState
	{
		OnGround,
		InAir,
		Disable
	}

	MovementState m_MovementState;

	Rigidbody m_RigidBody;

	bool m_AllowJump;
	bool m_CanFire;
	float m_CenterHeight;
	float m_JumpTimeLeft;
	float m_TimeLeftToAllowMidAirJump;
	float m_TimeTillNextJiggle;
	float m_AimAnimLayerWeight;
	float m_TimeToNextShot;
	float m_TimeInAir;
	int m_AimAnimLayerIndex;
	int m_GroundCheckMask;
	Vector3 m_Velocity;
}
