using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// Do not operate position/rotation directly
public class PlayerShoot : MonoBehaviour
{
    private Camera cam;
    private GameObject weaponPrefab;
    private Player player;
    private LineRenderer lineRenderer;
    private BaseWeapon weaponData;
    private PlayerWeaponManager weaponManager;
    private PlayerMotor motor;

    
    private CrossHairData crossHair;
    public LayerMask mask;

    // Fov
    private float defaultFov;

    // weapon pos
    private Vector3 weaponRelativeVec;
    private Vector3 camRelativeVec;
    private float weaponDistance;

    // progress | state machina helpers
    private Vector3 bobbingRelativePosition;
    private float aimingProgress = 0;

    private float bobbingProgress = 0;
    private float bobbingCoolDown = 0;
    private float maxBobbingCoolDown = 60;  // Stop bobbing if you started firing. After certain amount of time, you can start bobbing again.

    private float recoil = 0;   // Camera goes up when firing.
    private float weaponBackRecoil = 0;     // Weapon goes backward when firing.

    private bool hasFired = false;  // Has fired in this frame.
    private bool hasReloadCancelled = false;   // Has [actually] fired when reloading, causing a reload to fail. EG: Shotgun fire.
    private bool isReloading = false;   // Reload lock, prevent multiple reloading coroutines operating at same time.
    private bool isSwapping = false;

    private bool CanTryShoot() {
        return !isSwapping && weaponData.freeze == 0 &&
         (aimingProgress == 0 || aimingProgress >= weaponData.maxAimingProgress);
    }

    private bool CanAim() {
        return !isSwapping && isReloading == false;
    }

    private bool CanBob() {
        return !isSwapping && isReloading == false && bobbingCoolDown == 0f;
    }

    private void CancelBobbing() {
        bobbingProgress = 0;
        bobbingCoolDown = maxBobbingCoolDown;
    }

    private bool IsAiming() {
        return aimingProgress > 0;
    }

    // Aiming perform
    private void HandleAiming() {
        // Aim input
        if(InputHandler.AimKeyDown() && CanAim()) {
            if(aimingProgress < weaponData.maxAimingProgress) {
                aimingProgress += CommonUtil.GetStepUpdate();
            }
        } else {
            if(aimingProgress > 0) {
                aimingProgress -= CommonUtil.GetStepUpdate();
            }
        }
        aimingProgress = Mathf.Clamp(aimingProgress, 0, weaponData.maxAimingProgress);
        // Aim cam zoom and weapon move
        AnimateAiming();
    }

    // Recoil perform
    private void HandleRecoil() {
        if(hasFired) {
            // Recoil differs according to shooting state.
            if(IsAiming()) {
                recoil += weaponData.aimedRecoilCoef;
            } else {
                recoil += weaponData.recoilCoef;    // Is shooting in normal mode, it has bigger recoil, usually.
            }
            recoil = Mathf.Clamp(recoil, 0f, weaponData.maxRecoil);
            weaponBackRecoil += weaponData.backCoilCoef;
        } else if(recoil > 0f) {
            // Force that makes camera go up. Animated.
            recoil -= weaponData.recoilCoolDownCoef;
            recoil = recoil < 0? 0: recoil;
            // Force that makes weapon go backward. Animated.
        }
        
        if(weaponBackRecoil > 0) {
            weaponBackRecoil -= weaponData.backCoilCoolDownCoef;
            weaponBackRecoil = weaponBackRecoil < 0? 0f: weaponBackRecoil;
        }

        //motor.ApplyWeaponLocalPosition(cam.transform.forward * -weaponBackRecoil);
        motor.ApplyWeaponLocalPosition(new Vector3(0,0,-weaponBackRecoil));
        motor.ApplyCamRotation(Quaternion.Euler(-recoil, 0f, 0f));
        
    }

    // Weapon goes up and down when idling
    private void HandleBobbing() {

        if(!CanBob()) {
            bobbingRelativePosition = Vector3.zero;
            if(bobbingCoolDown > 0) {
                bobbingCoolDown -= CommonUtil.GetStepUpdate();
            } else {
                bobbingCoolDown = 0;
            }
            return;
        }

        if(bobbingProgress < weaponData.bobbingFrequency) {
            if(! IsAiming()) {
                bobbingProgress += CommonUtil.GetStepUpdate();
            }
        } else {
            bobbingProgress = 0;
        }
        float progressToAngleMult = 2*Mathf.PI / weaponData.bobbingFrequency ;
        bobbingRelativePosition = new Vector3(0f,Mathf.Sin(bobbingProgress * progressToAngleMult) * weaponData.bobbingAmplitude,0f);
        motor.ApplyWeaponLocalPosition(bobbingRelativePosition);
    }

    // 1. Move weapon position to aiming point.
    // 2. Change camera FOV to show bigger objects in the view.
    private void AnimateAiming() {
        float progress = (float)aimingProgress / weaponData.maxAimingProgress;
        if(progress > 0) {
            // FOV Adjust
            motor.ApplyCamFov( Mathf.Lerp(1.0f , weaponData.maxFovMultiplier, progress) * defaultFov);
            
            // WPN 'crosshair' move to cursor
            // Explain: 
            // 1. motor.weaponDefaultPosition: The position where [gun body] is.
            // 2. weaponData.weaponAimingPoint: The position where [gun's crosshair] is. Attached to weapon prefab.
            // @Warning: Loosy design !
            // OLD:
            //Vector3 aimTween = motor.globalAimPoint.position - (motor.globalDefaultPoint.position + cam.transform.rotation * weaponData.bodyToAimRelative + bobbingRelativePosition);
            //motor.ApplyWeaponPosition(Vector3.Lerp(Vector3.zero, aimTween, progress));

            // NEW:
            Vector3 aimTween = motor.globalAimPoint.localPosition - weaponData.weaponAimPoint.localPosition + bobbingRelativePosition;
            motor.ApplyWeaponLocalPosition(Vector3.Lerp(Vector3.zero, aimTween, progress));
            // Disable crosshair
            crossHair.SetVisible(false);
        } else {
            crossHair.SetVisible(true);
        }
    }

    // Check input for swapping weapon, then swap it down to the spawnPoint.
    // After countdown, the weapon is equipped, and it will move up to the defaultWeaponPoint.
    private void HandleWeaponSwap() {
        if(isSwapping) {
            return;
        }

        int wheelInput = 0;
        if(InputHandler.MouseWheelDown()) {
            wheelInput = -1;
        } else if(InputHandler.MouseWheelUp()) {
            wheelInput = 1;
        }

        if(wheelInput != 0) {
            isSwapping = true;
            int equipSlot = weaponManager.GetNextAvailable(wheelInput);
            if(weaponManager.CanEquip(equipSlot)) {
                StartCoroutine(_WeaponSwapProgress(equipSlot));
            }
        }
    }

    // Execute swapping progress
    private IEnumerator<int> _WeaponSwapProgress(int weaponIndex) {
        // 1. Swap weapon down.
        for(float i=0; i<weaponData.swapWeaponTime; i+=CommonUtil.GetStepUpdate()) {
            Vector3 relativePosition = Vector3.Lerp(Vector3.zero, motor.globalWeaponSpawnPoint.localPosition, i/weaponData.swapWeaponTime);
            motor.ApplyWeaponLocalPosition(relativePosition);
            yield return 0;
        }
        // 2. Update(Equip) new weapon prefab and its data.
        weaponManager.EquipWeapon(weaponIndex);
        yield return 0;
        // 3. Swap weapon up.
        for(float i=0; i<weaponData.swapWeaponTime; i+=CommonUtil.GetStepUpdate()) {
            Vector3 relativePosition = Vector3.Lerp(motor.globalWeaponSpawnPoint.localPosition, Vector3.zero, i/weaponData.swapWeaponTime);
            motor.ApplyWeaponLocalPosition(relativePosition);
            yield return 0;
        }
        // 4. Unlock swap state.
        isSwapping = false;
    }

    // The entrance of shooting action.
    private void HandleShoot() {
        bool isInputValid = false;
        // Check input
        switch(weaponData.fireType) {
            // Single shoot
            case BaseWeapon.FireType.MANUAL: {
                isInputValid = Input.GetButtonDown("Fire1");
                break;
            }
            // Auto fire
            case BaseWeapon.FireType.REPEAT: {
                isInputValid = Input.GetButton("Fire1");
                break;
            }
            // Charged shoot
            case BaseWeapon.FireType.CHARGE: {
                isInputValid = Input.GetButton("Fire1");
                break;
            }
        }

        if(isInputValid) {
            if(CanTryShoot()) {
                TryShoot();
            }
        }
    }

    // Perform shoot
    private void TryShoot() {
        // Once you try to shoot, cancel reload if ammo is already sufficient for shooting
        if(weaponData.HasSufficientAmmo()) {
            hasReloadCancelled = true;
            PerformShoot();
        } else if (isReloading == false){
            isReloading = true;
            Reload();
        }
    }

    // Perform actual shoot
    private void PerformShoot() {
        weaponData.DoShoot();
        // Stop bobbing when firing.
        CancelBobbing();
        hasFired = true;
        RaycastHit hit;
        Vector2 bias = weaponData.GetFireAngleBias();
        Vector3 emitPosition = cam.transform.position; //+ cam.transform.rotation * new Vector3(bias.x, bias.y, 0f);
        Vector3 towardsDirection = cam.transform.forward;
        if (Physics.Raycast(emitPosition, towardsDirection, out hit, weaponData.range, mask)) {
            Debug.DrawRay(emitPosition, towardsDirection, Color.red, 2f);
            if(hit.collider != null) {
                Debug.DrawLine(emitPosition, hit.point, Color.magenta, 2f);
            }
        }

        RenderRay(hit);
    }

    // Check reload input
    private void HandleReload() {
        if(InputHandler.ReloadKeyDown() && !isReloading) {
            isReloading = true;
            Reload();
        }
    }

    // Reload ammo
    private void Reload() {
        StartCoroutine(_ReloadCountDown());
    }

    // Reload countdown helper
    private IEnumerator<int> _ReloadCountDown() {

        for(float i=0; i<weaponData.reloadTime; i+=CommonUtil.GetStepUpdate()) {
            if(hasReloadCancelled) {
                isReloading = false;
                yield break;
            } else {
                yield return 0;
            }
        }
        PerformReload();
    }

    // Actual reload action
    private void PerformReload() {
        weaponData.DoReload();
        isReloading = false;
    }

    // Tell [BaseWeapon] to update its line renderer.
    private void RenderRay(RaycastHit hit) {
        Vector3 endPoint;
        if(hit.collider != null) {
            endPoint = hit.point;
        } else {
            endPoint = new Ray(cam.transform.position, cam.transform.forward).GetPoint(100f);
        }
        weaponData.EnableFireLine(endPoint);
    }

    // Reset all state variables to their defaults.
    private void InitBeforeUpdate() {
        hasFired = false;
        hasReloadCancelled = false;
        weaponData = player.weaponData;
        crossHair = player.crossHair;
        weaponPrefab = player.weaponPrefab;
    }

    // Set all complex animation to the weapon animator.
    
    private void SubmitAnimatorStatus() {
        Animator weaponAnimator = player.GetWeaponAnimator();
        // Whether reloading animation should be played or not
        weaponAnimator.SetBool(CommonUtil.AnimParameters.isReloading, isReloading);

        // @Improve @WIP
        AnimatorClipInfo[] clips = weaponAnimator.GetCurrentAnimatorClipInfo(0);
        float clipLength = 0;
        if(clips.Length > 0) {
            clipLength = clips[0].clip.length;  //Animation length in secs
        }
        // Set weapon reloading animation speed. (1.0 x multiplier)
        if(clipLength != 0) {
            weaponAnimator.SetFloat(CommonUtil.AnimParameters.multReloading, clipLength / weaponData.reloadTime * CommonUtil.FPS);
            motor.overrideTransform = true;
            Debug.Log(motor.overrideTransform);
        } else {
            motor.overrideTransform = false;
        }

    }

    // ==== Life span ====

    // Start is called before the first frame update
    void Start() {
        player = GetComponent<Player>();
        motor = GetComponent<PlayerMotor>();
        weaponManager = player.GetComponent<PlayerWeaponManager>();
        cam = player.cam;
        weaponPrefab = player.weaponPrefab;
        weaponData = player.weaponData;
        crossHair = player.crossHair;
        defaultFov = cam.fieldOfView;
        weaponRelativeVec = weaponPrefab.transform.position - cam.transform.position;
        camRelativeVec = cam.transform.position - this.transform.position;
        weaponDistance = weaponRelativeVec.magnitude;

    }

    void Update() {
        InitBeforeUpdate();

        HandleWeaponSwap();
        HandleShoot();
        HandleReload();
        HandleBobbing();
        HandleRecoil();
        HandleAiming();

        SubmitAnimatorStatus();
    }

    void OnRenderObject() {
		// Draw cross hair
		crossHair.DrawLine(new Vector2(Screen.width / 2, Screen.height / 2));
	}
}
