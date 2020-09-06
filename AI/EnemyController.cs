using UnityEngine;
using UnityEngine.AI;

public class EnemyController : AbstractEnemyController {

	[Tooltip("Delay Between state transition.")]
	public float transitionDelay = 1f;
	[Tooltip("Angle span when detecting enemy.")]
	public float detectionAngle = 120f;
	[Tooltip("Max visual distance.")]
	public float detectionDistance = 10f;
	[Tooltip("Max distance to a partol road point to tell PatrolPath that the node has been reached.")]
	public float patrolThresholdDistance = 0.5f;
	[Tooltip("Whether to enable patrol or not.")]
	public bool patrolEnabled = true;
	[Tooltip("The max distance between attacker and target.")]
	public float maxAttackRange = 10f;
	[Tooltip("The minimum distance between attacker and target.")]
	public float minAttackRange = 5f;
	[Tooltip("The angle inside which the attacker will start attacking.")]
	public float attackAngle = 20f;
	[Tooltip("How many degs can the weapon owner rotate while aiming.")]
	public float aimAngularSpeed = 180;
	[Tooltip("If player is too close, the destination will be set backward.")]
	public float stepBackAmount = 5f;
	[Tooltip("Duration after target lost before state changes to ALERT")]
	public float maxTargetLostTime = 10;
	[Tooltip("Where to display weapon.")]
	public Transform weaponDefaultPoint;
	[Tooltip("What kind of weapon to use.")]
	public GameObject weaponPrefab;
	[Tooltip("Works as a virtual camera.")]
	public Transform shootTransform;
	[Tooltip("What target to hit.")]
	public LayerMask detectMask;
	[Tooltip("Noise alert threshold")]
	public float noiseDetectThreshold = 10f;

	public PatrolPath patrolPath {get; set;}
	public NavMeshAgent navMeshAgent {get; set;}
	private BaseWeapon weaponData;
	private State state;
	private float lastThinkTime = 0;	// The last time before AI started to change strategy
	private State nextState;
	private Actor lockedTarget;
	public float angleToTarget {get; set;}
	public float distanceToTarget {get; set;}
	private Vector3 lastKnownPosition;
	private float lastTargetLostTime;
	private bool isTargetLost = false;

	public enum State {
		ATTACK, PATROL, TRANSITION
	}

	// @Warning: lossy design! 
	private void Attack() {
		// Target might change.
		Actor target = FindNearestVisibleHostile();

		if(target != null) {
			lockedTarget = target;
			isTargetLost = false;
		} else {
			isTargetLost = true;
		}
		/*
		if(lockedTarget == null) {
			navMeshAgent.SetDestination(lastKnownPosition);
			return;
		}
		*/
		// === Target insight === 
		if (isTargetLost == false) {

			// Face towards target
			navMeshAgent.updateRotation = false;
			OrientTowardsTarget(lockedTarget);

			if(!IsVisible(lockedTarget)) {
				// Target already lost
				isTargetLost = true;
				lastKnownPosition = lockedTarget.transform.position;
				lastTargetLostTime = Time.time;
			} else {
				// Target not lost
				// Already in attack angle.
				if(angleToTarget < attackAngle) {
					if(distanceToTarget < minAttackRange) {
						// Too close, move back.
						navMeshAgent.isStopped = false;
						navMeshAgent.SetDestination(transform.position + -transform.forward * stepBackAmount);
					} else if (distanceToTarget > maxAttackRange) {
						// Too far, move close. Walk backward without automatic rotation.
						navMeshAgent.isStopped = false;
						navMeshAgent.SetDestination(lockedTarget.transform.position);
					} else {
						// Start to attack
						navMeshAgent.isStopped = true;
						shootTransform.rotation = Quaternion.LookRotation(lockedTarget.transform.position - transform.position);
						weaponData.TryShoot(shootTransform);
					}
				}
			}
		} else {
			// === Target lost ===
			lockedTarget = null;
			navMeshAgent.updateRotation = true;
			navMeshAgent.isStopped = false;
			
			// Find some evidence
			AttractionSource source = FindNearestAttraction();
			if(source != null) {
				// Detected noise
				Debug.Log(source);
				lastKnownPosition = source.transform.position;
				lastTargetLostTime = Time.time;
				navMeshAgent.SetDestination(lastKnownPosition);
			} else {
				// @Warning: lossy design -- cause robot to do nothing !
				// No evidence found
				// If target has not appeared for a long time, translate state into [ALERT]
				if (Time.time - lastTargetLostTime > maxTargetLostTime) {
					lockedTarget = null;
					StartTransitionTo(State.PATROL);
					navMeshAgent.SetDestination(patrolPath.GetNextTransform().position);
				} else {
					navMeshAgent.SetDestination(lastKnownPosition);
				}
			}
		}

	}

	private void StartTransitionTo(State nextState) {
		state = State.TRANSITION;
		this.nextState = nextState;
		this.lastThinkTime = Time.time;
	}

	private void Transition() {
		if(Time.time - lastThinkTime >= transitionDelay) {
			Debug.Log("Transition to [" + nextState + "] OK!");
			state = nextState;
		}
	}

	private void Patrol() {
		// === Patrol ===
		if(patrolEnabled == true) {
			// It returns the next transform only when the object has reached its current target.
			Transform next = patrolPath.CheckAndHandleTargetReached(patrolThresholdDistance);
			if(next != null) {
				navMeshAgent.SetDestination(next.position);
			}
		}
		// === Find Hostile ===
		Actor target = FindNearestVisibleHostile();
		if(target != null) {
			lockedTarget = target;
			StartTransitionTo(State.ATTACK);
		}
		// === Find Suspecious Point ===
		AttractionSource source = FindNearestAttraction();
		if(source != null) {
			Debug.Log(source);
			lastKnownPosition = source.transform.position;
			navMeshAgent.SetDestination(lastKnownPosition);
			StartTransitionTo(State.ATTACK);
		}
						
	}

	private AttractionSource FindNearestAttraction() {
		AttractionSource tgt = null;
		float distance, actualNoiseVolume;
		float cmp = float.MaxValue;
		// Traverse each actor to get its [AttractionSource]
		foreach(AttractionSource src in AttractionSourceManager.attractionSources) {
			// Find loudest noise source position.
			if(src != null) {
				distance = Vector3.Distance(src.transform.position, transform.position);
				actualNoiseVolume = CommonUtil.CalcNoiseAfterDecay(src.currentAttraction, distance);
				if(actualNoiseVolume > noiseDetectThreshold && actualNoiseVolume < cmp) {
					cmp = actualNoiseVolume;
					tgt = src;
				}
			}
		}
		return tgt;
	}

	private void OrientTowardsTarget(Actor target) {
		float angle = Vector3.SignedAngle(transform.forward, transform.position - target.transform.position, -transform.up);
		Quaternion targetQuaternion = transform.rotation * Quaternion.Euler(0f, angle, 0f);
		transform.rotation = Quaternion.RotateTowards(transform.rotation, targetQuaternion, navMeshAgent.angularSpeed * Time.fixedDeltaTime);
	}

	private Actor FindNearestVisibleHostile() {
		float tmpDistance;
		float distance = float.MaxValue;
		Actor target = null;
		foreach(Actor actor in ActorManager.actors) {
			if(actor.team != this.team && IsVisible(actor)) {
				tmpDistance = Vector3.Distance(actor.transform.position, transform.position);
				if(tmpDistance < distance) {
					target = actor;
					distance = tmpDistance;
				}
			}
		}
		return target;
	}

	private bool IsRaycastTestValid(Actor target) {
		RaycastHit hit;
		if (Physics.Raycast(transform.position, target.transform.position-transform.position, out hit, this.detectionDistance, detectMask)) {
			if(hit.collider != target.GetComponentInChildren<Collider>()) {
				Debug.DrawRay(transform.position, hit.point-transform.position, Color.white);
				return false;
			} else {
				Debug.DrawRay(transform.position, hit.point-transform.position, Color.red);
				return true;
			}
		} else {
			return false;
		}
	}

	private bool IsVisible(Actor target) {
		float distance = Vector3.Distance(target.transform.position, transform.position);
		// Whether target is too far
		if(distance > this.detectionDistance) {
			return false;
		} else {
			// Whether in detect range.
			float angle = Vector3.Angle(transform.forward, target.transform.position-transform.position);
			if(angle > detectionAngle/2) {
				return false;
			}

			return IsRaycastTestValid(target);
		}
	}

	private void OnHit() {
		StartTransitionTo(State.ATTACK);
	}

	protected void Start() {
		// Actor reg
		ActorManager.Register(this);
		// Nav init
		navMeshAgent = FindObjectOfType<NavMeshAgent>();
		CommonUtil.IfNullLogError<NavMeshAgent>(navMeshAgent);
		// Patrol init
		patrolPath = GetComponent<PatrolPath>();
		if(patrolPath != null) {
			patrolPath.owner = gameObject;
			// Init patrol
			navMeshAgent.SetDestination(patrolPath.GetNextTransform().position);
		} else if (patrolEnabled == false) {
			CommonUtil.IfNullLogError<PatrolPath>(patrolPath);
		}
		// Weapon init
		CommonUtil.IfNullLogError<GameObject>(weaponPrefab);
		weaponPrefab = Instantiate(weaponPrefab);
		weaponPrefab.transform.SetParent(transform);

		weaponData = weaponPrefab.GetComponent<BaseWeapon>();
		weaponData.ownerIsPlayer = false;
		weaponData.owner = this;

		// Init state 
		state = State.PATROL;
	}

	void LateUpdate() {
		weaponPrefab.transform.localPosition = weaponDefaultPoint.localPosition;
	}

	void FixedUpdate() {

		if(lockedTarget != null) {
			angleToTarget = Vector3.Angle(transform.forward, lockedTarget.transform.position-transform.position);
			distanceToTarget = Vector3.Distance(transform.position, lockedTarget.transform.position);
		}

		switch (state) {

			case State.ATTACK: {
				Attack();
				break;
			}

			case State.PATROL: {
				Patrol();
				break;
			}

			case State.TRANSITION: {
				Transition();
				break;
			}

		}
	}
}