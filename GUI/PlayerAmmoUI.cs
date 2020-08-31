using UnityEngine;
using UnityEngine.UI;

public class PlayerAmmoUI : BaseUI {
	
	private BaseUI parent = null;
	private BaseWeapon weaponData = null;
	public Text ammoText;
	
	private void BeforeUpdate() {
		weaponData = player.weaponData;
		CommonUtil.IfNullLogError<BaseWeapon>(weaponData);
	}

	void Update() {
		BeforeUpdate();
		if(weaponData != null) {
			this.enabled = true;
			ammoText.text = weaponData.currentAmmo+"/"+weaponData.maxAmmo;
		} else {
			this.enabled = false;
		}
	}
}