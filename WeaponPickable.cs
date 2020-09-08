using UnityEngine;

public class WeaponPickable : InteractivePickable {

	public GameObject weaponPrefab;
	public bool needInstantiation = true;

	protected override void Start() {
		// Is already placed in the editor.
		if(needInstantiation) {
			// Disable mesh renderer
			MeshRenderer mr = this.GetComponent<MeshRenderer>();
			if(mr != null) {
				mr.enabled = false;
			}
			// Create model
			weaponPrefab = Instantiate<GameObject>(weaponPrefab);
			weaponPrefab.transform.position = transform.position;
			weaponPrefab.transform.SetParent(this.transform);
		} else {
			// Is thrown by player.
		}
		base.Start();
	}

	// Try to pick, only player can pick this up.
	protected override void OnPick(GameObject obj) {
		Player player = obj.GetComponentInParent<Player>();
		if(player == null) {
			return;
		} else {
			player.weaponManager.AddExistWeapon(weaponPrefab);
			Destroy(this.gameObject);
		}
	}
	
	protected new void OnTriggerEnter(Collider other) {
		base.OnTriggerEnter(other);
	}
}