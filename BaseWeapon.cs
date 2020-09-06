using UnityEngine;
using System.Collections.Generic;

public class BaseWeapon : MonoBehaviour
{
	public enum FireType {
		MANUAL, REPEAT, CHARGE
	}

	public enum ProjectileType {
		RAY, PROJECTILE
	}

	public float freeze {get; private set;} = 0;
	public float unstability {get; private set;} = 0;

	[Header("General")]
	public float maxFreeze = 5;
	public float range = 20;
	public float swapWeaponTime = 60;
	public FireType fireType = FireType.REPEAT;
	public ProjectileType projectileType = ProjectileType.RAY;
	public float lineRendererDisplayTime = 1;
	public Animator animator;
	public LayerMask mask;

	[Header("ProjectileOnly")]
	public GameObject projectilePrefab = null;
	public float inflictForce = 100f;
	public float projectileFlyingSpeed = 5f;

	[Header("Ammo")]
	public float maxReloadTime = 60;
	public float currentAmmo = 20;
	public float maxAmmo = 20;
	public float ammoConsumptionPerShot = 1;
	public float ammoReplenishPerShot = 20;

	[Header("Damage")]
	public float maxDamage = 20;
	public float minDamage = 10;
	public float damageDecayRatio = 0.5f;
	
	[Header("Recoil")]
	public float maxRecoil = 10;
	public float recoilCoef = 2;
	public float aimedRecoilCoef = 1;
	public float recoilCoolDownCoef = 0.2f;
	public float backCoilCoef = 0.5f;
	public float backCoilCoolDownCoef = 0.1f;

	[Header("Accuracy")]
	public float maxSpreadRadius = 5f;
	public float maxUnstability = 50f;
	public float minStability = 25f;
	public float unstabilityIncrement = 5f;

	[Header("Aiming")]
	public float maxAimingProgress = 20f;
	public float maxFovMultiplier = 0.8f;

	[Header("Bobbing")]
	public float bobbingFrequency = 240;
	public float bobbingAmplitude = 0.02f;

	[Header("Charge")]
	public float maxCharge = 10;

	[Header("Position")]
	public Transform weaponAimPoint;
	public Transform weaponEffectPoint;

	[Header("Debug")]
	public GameObject debugHitPointPrefab;

	// private / partly private
	protected LineRenderer lineRenderer;
	public Vector3 bodyToAimRelative {get; protected set;}
	public Actor owner {get; set;}
	public float baseUnstability {get; set;}
	public float currentCharge {get;set;} = 0;
	public bool ownerIsPlayer = false;
	public float reloadTime {get; private set;}
	public bool isReloading {get; private set;}
	public AttractionSource attractionSource {get; private set;}
	private AttractionSourceManager attractionSourceManager; 

	public bool HasSufficientAmmo() {
		return currentAmmo >= ammoConsumptionPerShot;
	}

	protected void ConsumeAmmo(float ammoConsumption) {
		currentAmmo -= ammoConsumption;
		currentAmmo = currentAmmo < 0? 0: currentAmmo;
	}

	protected void IncreaseUnstability() {
		unstability += unstabilityIncrement;
		float acutalUnstability = unstability + baseUnstability;
		unstability = acutalUnstability > maxUnstability? maxUnstability: acutalUnstability;
	}

	protected void ResetFreeze() {
		freeze = maxFreeze;
	}

	protected void ResetCharge() {
		currentCharge = 0;
	}

	// Get a ratio of Unstability / MaxUnstability
	protected float GetUnstableProgress() {
		float actualUnstability = unstability + baseUnstability;
		if(actualUnstability < minStability) {
			return 0;
		} else {
			return actualUnstability / maxUnstability;
		}
	}

	public void DoReload() {
		currentAmmo += ammoReplenishPerShot;
		currentAmmo = currentAmmo > maxAmmo? maxAmmo: currentAmmo;
	}

	// Designed for AI to operate the weapon automatically, with only one function call.
	// Return whether the fire was success.
	public bool TryShoot(Transform orgin) {
		if(HasSufficientAmmo()) {
			if(freeze == 0) {
				Debug.Log("Shoot");
				DoShoot(orgin);
				return true;
			}
		} else {
			// Start reloading...
			if(reloadTime != maxReloadTime && isReloading == false) {
				Debug.Log("Start to reload....");
				reloadTime = maxReloadTime;
				isReloading = true;
			} else if(reloadTime == 0) {
				// Reloading OK.
				Debug.Log("Reload OK");
				isReloading = false;
				DoReload();
			}
		}
		return false;
	}

	public void DoShoot(Transform origin) {
		if(fireType == FireType.CHARGE) {
			ConsumeAmmo(currentCharge);
			currentCharge = 0;
		} else {
			ConsumeAmmo(ammoConsumptionPerShot);
		}

		switch(projectileType) {
			case ProjectileType.RAY: {
				DoRaycast(origin);
				break;
			}
			case ProjectileType.PROJECTILE: {
				DoCreateProjectile(origin);
				break;
			}
		}

		ResetFreeze();
		IncreaseUnstability();

		// Handle noise
		attractionSource.AddNoise(100f);
	}

	public void DoCharge() {
		if(currentCharge > maxCharge) {
			currentCharge = maxCharge;
		} else {
			if(this.currentCharge >= currentAmmo) {
				this.currentCharge = currentAmmo;
			} else {
				this.currentCharge += this.ammoConsumptionPerShot;
			}
		}
	}

	protected void DoRaycast(Transform origin) {
		RaycastHit hit;
        Quaternion angleBias = GetFireBiasQuaternion(origin);
        Vector3 emitPosition = origin.position;
        Vector3 towardsDirection = angleBias * origin.forward;
        Debug.DrawRay(origin.position, towardsDirection, Color.red);

        if (Physics.Raycast(emitPosition, towardsDirection, out hit, range, mask)) {
            Debug.DrawRay(emitPosition, towardsDirection, Color.red, 2f);
            if(hit.collider != null) {
                Debug.DrawLine(emitPosition, hit.point, Color.magenta, 2f);
				// === Debug only ===
                // GameObject debugHitpointObject = Instantiate(debugHitPointPrefab, hit.point, Quaternion.Euler(Vector3.zero));
                // Destroy(debugHitpointObject, 1.0f);

				// Try to inflict some damage
				float damage = DamageInflictUtil.DamageLerp(maxDamage, minDamage, 
					Vector3.Distance(hit.point, origin.position), damageDecayRatio*range, range);

				DamageInflictUtil.InflictDamage(hit, owner.gameObject, damage, inflictForce);
            }
        }
        RenderRay(hit, origin);
	}

	protected void DoCreateProjectile(Transform origin) {
		// Because the weapon fire point and the aiming point wasn't same,
		// We need to correct its trajectory.
		Transform weaponFireTransform = weaponEffectPoint.transform;
		Vector3 correctionVector = weaponFireTransform.InverseTransformVector(origin.position - weaponEffectPoint.position);
		correctionVector.z = 0;

		GameObject obj = Instantiate(projectilePrefab, weaponEffectPoint.position, origin.rotation);
		BaseProjectile proj = obj.GetComponent<BaseProjectile>();
		proj.InitData(this, weaponFireTransform.rotation, correctionVector);
	}

	// Update line renderer.
    protected void RenderRay(RaycastHit hit, Transform origin) {
        Vector3 endPoint;
        if(hit.collider != null) {
            endPoint = hit.point;
        } else {
            endPoint = new Ray(origin.position, origin.forward).GetPoint(range);
        }

        lineRenderer.SetPosition(0, weaponEffectPoint.position);
		lineRenderer.SetPosition(1, endPoint);
		lineRenderer.enabled = true;
		StartCoroutine(_DisableFireLine());
    }

	// Get a random spread shot quaternion when firing.
	protected Quaternion GetFireBiasQuaternion(Transform shootTransform) {
		float angle = Random.Range(0,360);
		float progress = GetUnstableProgress();
		float offset = Random.Range(0, maxSpreadRadius * progress);
		float xpos = Mathf.Cos(angle) * offset;
		float ypos = Mathf.Sin(angle) * offset;
		Vector3 actualPoint = shootTransform.TransformPoint(new Vector3(xpos,ypos,range));
		Quaternion quat = Quaternion.FromToRotation(shootTransform.forward, actualPoint-shootTransform.position);
		return quat;
	}

	protected IEnumerator<int> _DisableFireLine() {
		for(float i=0; i<lineRendererDisplayTime; i+=CommonUtil.GetStepUpdate()) {
			yield return 0;
		}
		lineRenderer.enabled = false;
	}

	// Set crosshair max distance whenever a new weapon is equipped.
	// Player Only
	public void ResizeCrossHair() {
		if(ownerIsPlayer) {
			Player player = (Player)owner;
			player.crossHair.maxScreenRadius = CommonUtil.DistanceProjectionOnCameraScreen(player.cam, range, maxSpreadRadius);
		}
	}

	// Change crosshair radius.
	// Player Only
	public void UpdateCrossHairRadius() {
		if(ownerIsPlayer) {
			Player player = (Player)owner;
			player.crossHair.SetProgressRadius(GetUnstableProgress()-minStability/maxUnstability);
		}
	}

	protected void UpdateFreeze() {
		if(freeze > 0) {
			freeze -= CommonUtil.GetStepUpdate();
		} else {
			freeze = 0;
		}
	}

	protected void UpdateAccuracy() {
		if(unstability > 0) {
			unstability -= CommonUtil.GetStepUpdate();
		} else {
			unstability = 0;
		}
	}

	protected void UpdateReload() {
		if(reloadTime > 0) {
			reloadTime -= CommonUtil.GetStepUpdate();
		} else {
			reloadTime = 0;
		}
	}

	void Awake() {
		lineRenderer = CommonUtil.GetComponentFromSelfOrChildren<LineRenderer>(this);
		CommonUtil.IfNullLogError<LineRenderer>(lineRenderer);

		// Make attraction. AI use this to check attraction point.
		attractionSource = gameObject.AddComponent<AttractionSource>();
		AttractionSourceManager.Register(attractionSource);

		bodyToAimRelative = weaponAimPoint.position - transform.position;
	}

	void Update() {
		UpdateFreeze();
		UpdateAccuracy();
		UpdateReload();
		if(ownerIsPlayer) {
			UpdateCrossHairRadius();
		}
	}
}