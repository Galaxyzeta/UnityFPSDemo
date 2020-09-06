using UnityEngine;

public abstract class Pickable : MonoBehaviour {

	public float pickupRadius = 0.2f;
	public LayerMask pickupMask;


	protected virtual void Start() {
		// Remove all colliders
		Collider[] allColliders = GetComponentsInChildren<Collider>();
		foreach(Collider collider in allColliders) {
			collider.enabled = false;
		}
	}

	protected virtual void FixedUpdate() {
		RaycastHit hit;
		if (Physics.SphereCast(transform.position, pickupRadius, Vector3.up, out hit, pickupRadius, pickupMask)) {
			Debug.Log("OK2");
			OnPick(hit.collider.gameObject);
		}
	}

	protected abstract void OnPick(GameObject obj);
}