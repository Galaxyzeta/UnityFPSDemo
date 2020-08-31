using UnityEngine;
using System.Collections.Generic;

public class BaseWeapon : MonoBehaviour
{
	public enum FireType {
		MANUAL, REPEAT, CHARGE
	}

	public float freeze {get; private set;} = 0;
	public float unstability {get; private set;} = 0;

	[Header("General")]
	public float maxFreeze = 5;
	public float range = 20;
	public float swapWeaponTime = 60;
	public FireType fireType = FireType.REPEAT;
	public float lineRendererDisplayTime = 1;
	public Animator animator;

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

	[Header("Position")]
	public Transform weaponAimPoint;
	public Transform weaponEffectPoint;

	// private / partly private
	public Vector3 bodyToAimRelative {get; private set;}
	private LineRenderer lineRenderer;
	public Player owner {get; private set;}

	public void SetOwner(Player player) {
		this.owner = player;
	}

	public bool HasSufficientAmmo() {
		return currentAmmo >= ammoConsumptionPerShot;
	}

	private void ConsumeAmmo() {
		currentAmmo -= ammoConsumptionPerShot;
		currentAmmo = currentAmmo < 0? 0: currentAmmo;
	}

	private void IncreaseUnstability() {
		unstability += unstabilityIncrement;
		unstability = unstability > maxUnstability? maxUnstability: unstability;
	}

	public void DoShoot() {
		ConsumeAmmo();
		ResetFreeze();
		IncreaseUnstability();
	}

	private void ResetFreeze() {
		freeze = maxFreeze;
	}

	public void DoReload() {
		currentAmmo += ammoReplenishPerShot;
		currentAmmo = currentAmmo > maxAmmo? maxAmmo: currentAmmo;
	}

	private float GetUnstableProgress() {
		return unstability / maxUnstability;
	}

	public Vector2 GetFireAngleBias() {
		float angle = Random.Range(0,360);
		float progress = GetUnstableProgress();
		float offset = Random.Range(0, maxSpreadRadius * progress);
		float xpos = Mathf.Cos(angle) * offset;
		float ypos = Mathf.Sin(angle) * offset;
		Vector2 actualPoint = new Vector2(xpos, ypos);
		return actualPoint;
	}

	public void ResizeCrossHair() {
		owner.crossHair.maxScreenRadius = CommonUtil.DistanceProjectionOnCameraScreen(owner.cam, range, maxSpreadRadius);
	}

	public void EnableFireLine(Vector3 endPoint) {
		lineRenderer.SetPosition(0, weaponEffectPoint.position);
		lineRenderer.SetPosition(1, endPoint);
		lineRenderer.enabled = true;
		StartCoroutine(_DisableFireLine());
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
	}
}