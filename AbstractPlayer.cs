using UnityEngine;

public class AbstractPlayer : Actor {

	[Header("AbstractController")]
	public Camera mainCamera;
	public Transform anchor;

	public CharacterController characterController {get; private set;}
	public Health health{get; protected set;}
	public BaseWeapon weaponData{get; set;}
	public GameObject weaponPrefab{get; set;}
	public CrossHairData crossHair{get; protected set;}
	public PlayerWeaponManager weaponManager{get; protected set;}
	
	

	protected virtual void Awake() {
		health = GetComponent<Health>();
		weaponManager = GetComponent<PlayerWeaponManager>();
		crossHair = GetComponent<CrossHairData>();
		characterController = GetComponent<CharacterController>();
	}

	protected virtual void Start() {
		// Register actor
		ActorManager.Register(this);
		// Instantiate all weapon prefab definitions.
		// Should only be called ONCE!
		weaponManager.Init(this);
		// Weapon prefab will be set when equipping.
		weaponManager.EquipWeapon(0);
	}

}