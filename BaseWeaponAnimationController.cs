using UnityEngine;

public class BaseWeaponAnimationController : ScriptableObject {

	public FPSController player {get; set;}

	// ==== Trigger ====
	public void TriggerReload() {
		Animator controller = player.GetWeaponAnimator();
		controller.SetTrigger("triggerReload");
	}

	public void TriggerReset() {
		Animator controller = player.GetWeaponAnimator();
		controller.SetTrigger("triggerReset");
	}
	

	// ==== Bool ====
	public void IsReloading(bool value) {
		player.GetWeaponAnimator().SetBool("isReloading", value);
	}


	public void IsRunning(bool value) {
		player.GetWeaponAnimator().SetBool("isRunning", value);
	}

	// ==== Float ====
	public void FloatMultReloading(float multReload) {
		player.GetWeaponAnimator().SetFloat("multReloading", multReload);
	}

}