using UnityEngine;

///<summary>Helps build a bridge between Raycast and damage calculation.</summary>
public class DamageInflictUtil : MonoBehaviour {

	public static void TryInflictDamage(RaycastHit hit, GameObject owner, float damage, float inflictForce) {
		Inflictable hitObject = CommonUtil.GetComponentFromSelfOrParent<Inflictable>(hit.collider);
		if(hitObject == null) {
			return;
		}

		Transform damgeProviderTransform = owner.transform;
		// Inflict damage when hit a damageable.
		if(hitObject is Damageable) {
			Health hp = CommonUtil.GetComponentFromSelfOrParent<Health>(hitObject);
			hp.Damage(damage, owner);
			// Signal inflict
			((Damageable)hitObject).OnDamaged(owner);
		} else {
			// Hit an inflictable with no Hp bar, apply force if a rigidbody is attached on it.
			Rigidbody rigidbody = hit.rigidbody;
			if(rigidbody != null) {
				rigidbody.AddForce(damgeProviderTransform.forward * inflictForce, ForceMode.Force);
			}
			// Signal inflict
			hitObject.OnInflicted(owner);
		}
	}

	/// <summary>Calculate damage based on damage linear decrease model.</summary>
	public static float DamageLerp(float maxDamage, float minDamage, float currentDistance, float decayDistance, float maxDistance) {
		if(currentDistance < decayDistance) {
			return maxDamage;
		} else {
			return Mathf.Lerp(maxDamage, minDamage, (currentDistance-decayDistance)/maxDistance);
		}
	}
	
}