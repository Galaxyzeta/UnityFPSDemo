using UnityEngine;
using UnityEngine.AI;

public class EnemyController : AbstractEnemyController {

	public float transitionDelay = 1f;
	public float detectionAngle = 90f;
	public float detectionDistance = 10f;
	public float patrolThresholdDistance = 0.5f;
	public bool patrolEnabled = true;
	public float attackRange = 5f;
	public Transform weaponDefaultPoint;
	public GameObject weaponPrefab;
	public LayerMask detectMask;

	public PatrolPath patrolPath {get; set;}
	public NavMeshAgent navMeshAgent {get; set;}
	private BaseWeapon weaponData;
	private State state;
	private ActorManager actorManager;
	private float lastThinkTime = 0;	// The last time before AI started to change strategy
	private State nextState;
	private GameObject lockedTarget;

	public enum State {
		ATTACK, PATROL, TRANSITION
	}

	private void Attack() {
		float distance = Vector3.Distance(transform.position, lockedTarget.transform.position);
		if(distance > attackRange) {
			// === Chase ===
			navMeshAgent.isStopped = false;
			navMeshAgent.SetDestination(lockedTarget.transform.position);
		} else {
			// === Start Attack ===
			weaponData.DoShoot(transform);
			navMeshAgent.isStopped = true;
		}
	}

	private void StartTransitionTo(State nextState) {
		state = State.TRANSITION;
		this.nextState = nextState;
		this.lastThinkTime = Time.time;
	}

	private void Transition() {
		if(Time.time - lastThinkTime >= transitionDelay) {
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
			lockedTarget = target.gameObject;
			StartTransitionTo(State.ATTACK);
		}
	}



	private Actor FindNearestVisibleHostile() {
		float tmpDistance;
		float distance = float.MaxValue;
		Actor target = null;
		foreach(Actor actor in actorManager.actors) {
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

			// Whether can see target.
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
	}
	
	void Start() {
		// AM init
		actorManager = FindObjectOfType<ActorManager>();
		CommonUtil.IfNullLogError<ActorManager>(actorManager);
		actorManager.Register(this);
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