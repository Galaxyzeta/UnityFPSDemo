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

	[Header("Ammo")]
	public float reloadTime = 60;
	public float currentAmmo = 20;
	public float maxAmmo = 20;
	public float ammoConsumptionPerShot = 1;
	public float ammoReplenishPerShot = 20;

	[Header("Damage")]
	public float maxDamage = 20;
	public float minDamage = 10;
	public float damageDecayDistance = 0.5f;
	
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
	private LineRenderer lineRenderer;
	public Vector3 bodyToAimRelative {get; private set;}
	public Player owner {get; set;}
	public float currentCharge = 0;

	public bool HasSufficientAmmo() {
		return currentAmmo >= ammoConsumptionPerShot;
	}

	private void ConsumeAmmo(float ammoConsumption) {
		currentAmmo -= ammoConsumption;
		currentAmmo = currentAmmo < 0? 0: currentAmmo;
	}

	private void IncreaseUnstability() {
		unstability += unstabilityIncrement;
		unstability = unstability > maxUnstability? maxUnstability: unstability;
	}

	private void ResetFreeze() {
		freeze = maxFreeze;
	}

	private void ResetCharge() {
		currentCharge = 0;
	}

	// Get a ratio of Unstability / MaxUnstability
	private float GetUnstableProgress() {
		if(unstability < minStability) {
			return 0;
		} else {
			return unstability / maxUnstability;
		}
	}

	public void DoReload() {
		currentAmmo += ammoReplenishPerShot;
		currentAmmo = currentAmmo > maxAmmo? maxAmmo: currentAmmo;
	}

	public void DoShoot(Transform origin) {
		if(fireType == FireType.CHARGE) {
			ConsumeAmmo(currentCharge);
			currentCharge = 0;
		} else {
			ConsumeAmmo(ammoConsumptionPerShot);
		}

		ResetFreeze();
		IncreaseUnstability();

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

	private void DoRaycast(Transform origin) {
		RaycastHit hit;
        Quaternion angleBias = GetFireBiasQuaternion(origin);
        Vector3 emitPosition = origin.position;
        Vector3 towardsDirection = angleBias * origin.forward;
        Debug.DrawRay(origin.position, towardsDirection, Color.red);

        if (Physics.Raycast(emitPosition, towardsDirection, out hit, range, mask)) {
            Debug.DrawRay(emitPosition, towardsDirection, Color.red, 2f);
            if(hit.collider != null) {
                Debug.DrawLine(emitPosition, hit.point, Color.magenta, 2f);
                GameObject debugHitpointObject = Instantiate(debugHitPointPrefab, hit.point, Quaternion.Euler(Vector3.zero));
                Destroy(debugHitpointObject, 1.0f);
            }
        }
        RenderRay(hit, origin);
	}

	private void DoCreateProjectile(Transform origin) {

	}

	// Update line renderer.
    private void RenderRay(RaycastHit hit, Transform origin) {
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
	private Quaternion GetFireBiasQuaternion(Transform shootTransform) {
		float angle = Random.Range(0,360);
		float progress = GetUnstableProgress();
		float offset = Random.Range(0, maxSpreadRadius * progress);
		float xpos = Mathf.Cos(angle) * offset;
		float ypos = Mathf.Sin(angle) * offset;
		Vector3 actualPoint = shootTransform.TransformPoint(new Vector3(xpos,ypos,range));
		Quaternion quat = Quaternion.FromToRotation(shootTransform.forward, actualPoint-shootTransform.position);
		return quat;
	}

	// Set crosshair max distance whenever a new weapon is equipped.
	public void ResizeCrossHair() {
		owner.crossHair.maxScreenRadius = CommonUtil.DistanceProjectionOnCameraScreen(owner.cam, range, maxSpreadRadius);
	}

	// Change crosshair radius.
	public void UpdateCrossHairRadius() {
		owner.crossHair.SetProgressRadius(GetUnstableProgress()-minStability/maxUnstability);
	}

	private IEnumerator<int> _DisableFireLine() {
		for(float i=0; i<lineRendererDisplayTime; i+=CommonUtil.GetStepUpdate()) {
			yield return 0;
		}
		lineRenderer.enabled = false;
	}

	private void UpdateFreeze() {
		if(freeze > 0) {
			freeze -= CommonUtil.GetStepUpdate();
		} else {
			freeze = 0;
		}
	}

	private void UpdateAccuracy() {
		if(unstability > 0) {
			unstability -= CommonUtil.GetStepUpdate();
		} else {
			unstability = 0;
		}
	}

	void Awake() {
		lineRenderer = GetComponent<LineRenderer>();
		CommonUtil.IfNullLogError<LineRenderer>(lineRenderer);
		bodyToAimRelative = weaponAimPoint.position - transform.position;
	}

	void Update() {
		UpdateFreeze();
		UpdateAccuracy();
		UpdateCrossHairRadius();
	}
}