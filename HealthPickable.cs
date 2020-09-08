using UnityEngine;

public class HealthPickable : Pickable {

	public float healAmount = 50f;
	public float selfRotateSpeed = 120f;

	protected override void OnPick(GameObject picker) {
		Debug.Log(picker);
		Health health = picker.GetComponentInParent<Health>();
		health.Heal(healAmount, this.gameObject);
		Destroy(this.gameObject);
	}
}