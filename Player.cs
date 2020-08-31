using UnityEngine;
using UnityEngine.UI;

// Annotations rules: @Readme @Todo @WIP @improve

// @ Readme
// The [Player] is designed as a resources / components [HUB], from which others can fetch properties.
// Other player script should fetch resouce from [Player] to avoid redundant, spoiled use of same components.

// Resource Dependency Relationships:
//							 + ------ +
// WeaponPrefab <---fetch--- | Player | <---fetch--- Other Player Scripts
//   |                       + ------ +
//   |____ BaseWeapon
//   |____ [Animator]

public class Player : MonoBehaviour {

	public BaseWeapon weaponData{get; set;}
	public GameObject weaponPrefab{get; set;}
	public CrossHairData crossHair{get; private set;}
	public Camera cam;
	[Tooltip("The root that controls all sub anchors. Attach animation controller here to perform anims properly.")]
	public GameObject anchor;
	public Health health{get; private set;}

	private PlayerWeaponManager weaponManager;

	public void ChangeAnimatorOnAnchor(Animator animator) {
		Animator existedAnimator = anchor.GetComponent<Animator>();
		existedAnimator.runtimeAnimatorController = animator.runtimeAnimatorController;
	}

	public Animator GetWeaponAnimator() {
		return weaponData.GetComponent<Animator>();
	}

	void Awake() {
		health = GetComponent<Health>();
		weaponManager = GetComponent<PlayerWeaponManager>();
		crossHair = GetComponent<CrossHairData>();
		
	}

	void Start() {
		
		// Instantiate and weapon count refresh
		weaponManager.Refresh();
		// Weapon prefab will be set when equipping.
		weaponManager.EquipWeapon(0);
	}

}