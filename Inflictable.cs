using UnityEngine;

/// <summary>An interface that marks objects which are able to be interacted with forces.</summary>
public class Inflictable : MonoBehaviour {

	public virtual void OnInflicted(GameObject inflictSource) {
		Rigidbody rb = this.GetComponent<Rigidbody>();
		if(rb != null) {
			// @Warning : Magic number, lossy design
			rb.AddForce((transform.position - inflictSource.transform.position) * 10f);
		}
	}

}