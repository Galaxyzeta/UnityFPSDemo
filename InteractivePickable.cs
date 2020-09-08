using UnityEngine;

public abstract class InteractivePickable : Pickable {

	public KeyCode key;
	protected bool pickNextFixedUpdate = false;
	protected bool drawGUI = false;

	protected override void OnTriggerEnter(Collider other) {
		drawGUI = true;
	}

	protected void OnTriggerStay(Collider other) {
		if(Input.GetKey(key)) {
			OnPick(other.gameObject);
		}
	}

	protected void OnTriggerExit(Collider other) {
		drawGUI = false;
	}
}