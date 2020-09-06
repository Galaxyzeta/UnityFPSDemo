using UnityEngine;

public class WeaponPickable : InteractivePickable {

	public GameObject weaponPrefab;

	protected override void Start() {
		base.Start();

	}

	// Try to pick, only player can pick this up.
	protected override void OnPick(GameObject obj) {
		Player player = obj.GetComponentInParent<Player>();
		if(player == null) {
			return;
		} else {
			player.weaponManager.AddWeapon(weaponPrefab);
			Destroy(this.gameObject);
		}
	}
}