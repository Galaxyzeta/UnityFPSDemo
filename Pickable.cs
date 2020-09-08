using UnityEngine;

public abstract class Pickable : MonoBehaviour {

	public float pickupRadius = 0.2f;
	public LayerMask pickupMask;


	protected virtual void Start() {
		// Remove all children colliders
		Collider[] allColliders = GetComponentsInChildren<Collider>();
		foreach(Collider collider in allColliders) {
			if(collider.isTrigger == false) {
				collider.enabled = false;
			}
		}
	}

	protected virtual void OnTriggerEnter(Collider collider) {
		OnPick(collider.gameObject);
	}

	protected abstract void OnPick(GameObject obj);
}