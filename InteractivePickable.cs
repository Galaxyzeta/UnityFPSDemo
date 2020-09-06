using UnityEngine;

public abstract class InteractivePickable : Pickable {

	public KeyCode key;
	protected bool pickNextFixedUpdate = false;

	protected void Update() {
		if(Input.GetKeyDown(key)) {
			pickNextFixedUpdate = true;
		}
	}

	protected override void FixedUpdate() {
		if(pickNextFixedUpdate == true) {
			base.FixedUpdate();
		}
		pickNextFixedUpdate = false;
	}
}