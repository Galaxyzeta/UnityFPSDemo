using UnityEngine;

public class BaseProjectile : MonoBehaviour {

	public float trajectionCorrectionTime = 1f;

	public float range {get; set;}
	public GameObject owner {get; set;}
	public Vector3 trajectoryCorrectionVector {get; set;}
	public Vector3 trajectoryDirection {get; set;}
	public float speed {get; set;}
	public float inflictForce {get; set;}
	public LayerMask damageMask {get; set;}
	public float maxDamage {get; set;}
	public float minDamage {get; set;}
	public float damageDecayRatio {get; set;}	// At which progress the damage starts to decay. Eg: range = 50, damageDecayRatio = 0.5, then damage decays at range 25.
	
	private float distanceTravelled = 0f;
	private float damageDecayRange = 0f;
	private float createdTime;

	public void InitData(BaseWeapon weaponData, Quaternion initialRotation, Vector3 trajectoryCorrectionVector) {
		this.range = weaponData.range;
		this.owner = weaponData.owner.gameObject;
		this.inflictForce = weaponData.inflictForce;
		this.maxDamage = weaponData.maxDamage;
		this.minDamage = weaponData.minDamage;
		this.damageDecayRatio = weaponData.damageDecayRatio;
		this.speed = weaponData.projectileFlyingSpeed;
		this.damageMask = weaponData.mask;
		
		this.trajectoryCorrectionVector = trajectoryCorrectionVector;
		this.damageDecayRange = range * damageDecayRatio;
		this.createdTime = Time.time;
		transform.rotation = initialRotation;
	}

	void Update() {
		// Correct trajectory
		float timeSpent = Time.time - this.createdTime;
		if(timeSpent < trajectionCorrectionTime) {
			transform.Translate(trajectoryCorrectionVector * Time.deltaTime * (1/trajectionCorrectionTime), Space.Self);
		}

		// Update position
		transform.Translate(speed * transform.forward * Time.deltaTime, Space.World);
		distanceTravelled += speed * Time.deltaTime;

		// Self-destruct if out of range.
		if(distanceTravelled > range) {
			Destroy(this.gameObject);
		}
		// Hit detection.
		RaycastHit hit;
		if (Physics.Raycast(transform.position, transform.forward, out hit, speed * Time.deltaTime, damageMask)) {
			float damage = DamageInflictUtil.DamageLerp(maxDamage, minDamage, distanceTravelled, damageDecayRange, range);
			DamageInflictUtil.TryInflictDamage(hit, owner, damage, inflictForce);
			Destroy(this.gameObject);
		}
	}
}