using UnityEngine;
using System.Collections.Generic;

// Do not operate position/rotation directly, these works should be done using [Player Motor]
public class FPSController : AbstractPlayer {

    // Public Parameters
    [Header("Movement")]
	[Tooltip("The absolute velocity magnitude when player walks")]
	public float walkSpeed;
	[Tooltip("The multiplier to apply when player is runnning")]
	public float sprintSpeedMultiplier;
    [Tooltip("Jump force to add when player jumps up")]
    public float initialJumpSpeed = 2f;
    [Tooltip("The minimum distance allow to judge whether a player is floating or not")]
    public float floatDistance = 0.01f;
    [Tooltip("The layer mask used to indicate ground")]
    public LayerMask groundMask = 1;
    [Tooltip("Foward detect distance, used to accurate slope movement")]
    public float forwardDetectDistance = 0.05f;
    [Tooltip("The stuff used to check ground.")]
    public Transform groundChecker;
    [Tooltip("The vibration of a camera when sprinting.")]
    public float cameraVibrationLimit = 5f;
    [Tooltip("The vibration lerp speed of a camera while sprinting")]
    public float cameraVibrationAcceleration = 15f;
    [Tooltip("The animator attached to character.")]
    public Animator characterAnimator;

    // Essential Components
	private PlayerMotor motor;
    private FPSController player;
	private Animator weaponAnimator;
    private AvatarIKController ikController;
    private Camera cam;
    private LineRenderer lineRenderer;
    private AttractionSource attractionSource;
	public BaseWeaponAnimationController controller{get; set;}
    public Animator WeaponAnimator { get; set; }

    // progress | state machina helpers
    // -- Weapon Pos
    private Vector3 weaponRelativeVec;
    private Vector3 camRelativeVec;
    private float weaponDistance;
    // -- Aim
    private float aimingProgress = 0;
    // -- Bob
    private Vector3 bobbingRelativePosition;
    private float bobbingProgress = 0;
    private float bobbingCoolDown = 0;
    private float maxBobbingCoolDown = 60;  // Stop bobbing if you started firing. After certain amount of time, you can start bobbing again.
    // -- Recoil
    private float recoil = 0;   // Camera goes up when firing.
    private float weaponBackRecoil = 0;     // Weapon goes backward when firing.
    // -- Bool Guardian
    private bool hasFiredThisFrame = false;  // Has fired in this frame.
    private bool hasReloadCancelled = false;   // Has [actually] fired when reloading, causing a reload to fail. EG: Shotgun fire.
    private bool isReloading = false;   // Reload lock, prevent multiple reloading coroutines operating at same time.
    private bool isSwapping = false;    // All action's lock.
    // -- Jump
    private int maxJumpCount = 1;   // Max time that a player can jumps;
    private int currentJumpCount = 0;   // Current jump counter;
    private float lastTimeJumped;    // Time stamp to mark last jump time;
    private const float jumpPreventionTime = 0.5f;    // In this duration the player is unable to jump. Prevent multiple force appliance at the beginning of jumping.
    private bool isAir = true;
    // -- Shoot
    private float lastShootTime;      // Prevent you from sprinting for a short time if you fired during last sprint. Otherwise it causes animation to swap frequently.
	// -- Inertia
    private Vector3 inertia = Vector3.zero;     // Remember last flat motion
    private float lastMotionTime;               // Remember last flat motion
    private float maxInertiaApplyTime = 1f;
    // -- Motion stuff
    private float sprintBanTime = 1f;       // For 1 sec you cannot sprint if you fired during a sprint
    private float lastJumpTime;
    private bool isSprinting = false;
    // -- Camera Motion Handler
    private float currentCameraVibrationAngle = 0;
    private float defaultFov;

    // @Warning: BAD CODE SMELL!
    public void ChangeAnimatorOnAnchor(Animator animator) {
		Animator existedAnimator = anchor.GetComponent<Animator>();
		existedAnimator.runtimeAnimatorController = animator.runtimeAnimatorController;
	}

    // @Warning: BAD CODE SMELL!
	public Animator GetWeaponAnimator() {
		return weaponData.GetComponent<Animator>();
	}

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

    private bool CanSwap() {
        return !isReloading && !isSwapping;
    }

    private bool CanReload() {
        return !isSwapping && !isReloading;
    }

    private bool CanSprint() {
        return !isSwapping && !isReloading && !isAir && Time.time - lastShootTime > sprintBanTime;
    }

    private void CancelBobbing() {
        bobbingProgress = 0;
        bobbingCoolDown = maxBobbingCoolDown;
    }

    private bool IsAiming() {
        return aimingProgress > 0;
    }

    private void MakeNoise(float volume) {
        attractionSource.AddNoise(volume);
    }

    private void HandlePlayerMovement() {
        float unstabilityModify = 0;
		PlayerMove(ref unstabilityModify);
        PlayerJump(ref unstabilityModify);
        PlayerInertia();
        ApplyBaseUnstability(unstabilityModify);
	}

    private void HandleThrowWeapon() {
        if(InputHandler.ThrowItemKeyDown()) {
            int weaponCurrent = weaponManager.weaponCurrent;
            if(weaponManager.weaponBag.Count > 1) {
                GameObject throwObject = weaponManager.ThrowWeapon(weaponCurrent);
                throwObject.transform.position = transform.position;
            } else {
                Debug.Log("Can't throw away last weapon.");
            }
        }
    }

    private void ApplyBaseUnstability(float unstabilityModify) {
        weaponData.baseUnstability = unstabilityModify;
    }

    private void PlayerInertia() {
        float deltaTime = Time.time - lastMotionTime;
        if(isAir) {
            // Apply total inertia.
            motor.ApplyFlatMotion(inertia);
        } else {
            if(deltaTime < maxInertiaApplyTime) {
                // Apply a decay inertia after stopped input, because there is friction resistance on the ground.
                motor.ApplyFlatMotion(Vector3.Lerp(inertia, Vector3.zero, deltaTime));
            } else {
                inertia = Vector3.zero;     // Clear inertia 
            }
        }
    }

    private bool GroundCheck() {
        RaycastHit hit;
        if(Physics.SphereCast(groundChecker.position, characterController.radius, Vector3.down, out hit, characterController.skinWidth+floatDistance, groundMask)) {
            if(Vector3.Angle(hit.normal, transform.up) < characterController.slopeLimit) {
                return true;
            }
        }
        return false;
    }

    private void PlayerJump(ref float unstabilityModify) {
        bool isJumpPressed = InputHandler.JumpKeyDown();    // Jump btn
        if(Time.time - lastJumpTime > jumpPreventionTime) {
            if(GroundCheck()) {
                isAir = false;
                currentJumpCount = maxJumpCount;
                motor.ResetVerticalSpeed();
            } else {
                isAir = true;
            }
        }

        // Try jump up. Successful only when these conditions are fulfilled :
        // 1. The jump key is pressed, the player is grounded.
        // 2. Max jump count not reached.
        // 3. User's input frequency is valid.
        bool jumpAvailableCondition = 
            isJumpPressed && !isAir && currentJumpCount-- > 0 && lastTimeJumped + jumpPreventionTime < Time.time;

        if(jumpAvailableCondition) {
            // Jump OK
            isAir = true;
            lastTimeJumped = Time.time;
            motor.ApplyVerticalSpeed(initialJumpSpeed);
            lastJumpTime = Time.time;
            // Handle noise
            MakeNoise(50f);   // Noise strength 50, decay 50 unit per second
        }

        if(isAir) {
            // @Warning : Loosely designed, magic number
            unstabilityModify += 20;
        }
    }

    private void PlayerMove(ref float unstabilityModify) {
        // Receive input
		float horizontalAxis = InputHandler.GetHorizontalAxis();	// Z - Movement
		float verticalAxis = InputHandler.GetVerticalAxis();		// X - Movement
		float viewRotX = InputHandler.GetViewVerticalAxis();		// View - X Rotation
		float viewRotY = InputHandler.GetViewHorizontalAxis();		// View - Y Rotation

        // First we assert the player is not running. This will be changed in next context.
        isSprinting = false;
		
		// Apply velocity and camera rotation
		Vector3 velocity = new Vector3(horizontalAxis, 0, verticalAxis);
		velocity.Normalize();	// Clamp magnitude
		velocity = transform.TransformVector(velocity);		// Local to world

        // Run
		float currentSpeed = walkSpeed;
		if(CanSprint()) {
            // If running key is held, and the player is tying to move:
            if(InputHandler.SprintKeyHeld() && velocity != Vector3.zero) {
                currentSpeed = walkSpeed * sprintSpeedMultiplier;
                isSprinting = true;
            }
		}
		// Do some animation
		//controller.IsRunning(isSprinting);

        // Apply motion
        // Detect available slope angle, and then move along it.
        Vector3 resultVector = velocity;

        if(!isAir && resultVector!=Vector3.zero) {
            CapsuleCollider capsule = GetComponentInChildren<CapsuleCollider>();
            Vector3 bottomPoint = transform.position + Vector3.down * (capsule.height * 0.5f + capsule.radius - 0.2f);
            float moveAngle = -characterController.slopeLimit;
            
            for(; moveAngle <= 0; moveAngle+=10) {
                resultVector = Vector3.Slerp(velocity, transform.up*-1, -moveAngle/90);
                if (! Physics.Raycast(bottomPoint, resultVector, forwardDetectDistance, groundMask)) {
                    break;
                }
            }
        }
        
        // Apply movement accordingly
        Vector3 resultVelocity = resultVector * currentSpeed;
		motor.ApplyFlatMotion(resultVelocity);	// Scale magnitude
		motor.ApplyInputRotation(Quaternion.Euler(0, viewRotY, 0));	// Restricted Rotate RB
		motor.ApplyBasicCameraRotation(Quaternion.Euler(-viewRotX, viewRotY, 0));	// Free Rotate cam

        // Add unstability to weapon
        unstabilityModify += resultVelocity.magnitude * 10;

        if(resultVector != Vector3.zero) {
            // Handle walking noise
            MakeNoise(currentSpeed/(sprintSpeedMultiplier*walkSpeed)*30f);
            // Remember inerita
            inertia = resultVector;
            lastMotionTime = Time.time;
        }
    }

    // Aiming perform
    private void HandleAiming() {
        // Aim input
        if(InputHandler.AimKeyHeld() && CanAim()) {
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
        if(hasFiredThisFrame) {
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
        motor.ApplyCamAdditionalRotation(Quaternion.Euler(-recoil, 0f, 0f));
        
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
            Vector3 aimTween = motor.globalAimPoint.localPosition - weaponData.weaponAimPoint.localPosition - bobbingRelativePosition;
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
        if(! CanSwap()) {
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
    private void HandleShootInput() {
        bool isInputValid = false;
        // Check input
        switch(weaponData.fireType) {
            // Single shoot
            case BaseWeapon.FireType.MANUAL: {
                isInputValid = InputHandler.FireKeyDown();
                break;
            }
            // Auto fire
            case BaseWeapon.FireType.REPEAT: {
                isInputValid = InputHandler.FireKeyHeld();
                break;
            }
            // Charged shoot
            case BaseWeapon.FireType.CHARGE: {
                isInputValid = InputHandler.FireKeyUp();
                break;
            }
        }

        // Is trying to fire
        if(isInputValid) {
            
            if(CanTryShoot()) {
                TryShoot();
                lastShootTime = Time.time;
            }
            /*
            weaponData.TryShoot(cam.transform);
            CancelBobbing();
            hasFiredThisFrame = true;
            */
        } else {
            // Is charging
            if(weaponData.fireType == BaseWeapon.FireType.CHARGE && InputHandler.FireKeyHeld()) {
                weaponData.DoCharge();
            }
        }
        
    }

    // Perform shoot
    private void TryShoot() {
        // Once you try to shoot, cancel reload if ammo is already sufficient for shooting
        if(weaponData.HasSufficientAmmo()) {
            hasReloadCancelled = true;
            PerformShoot();
        } else {
            TryReload();
        }
    }

    // Perform actual shoot
    private void PerformShoot() {
        weaponData.DoShoot(cam.transform);
        // Stop bobbing when firing.
        CancelBobbing();
        hasFiredThisFrame = true;
    }

    public void TryReload() {
        if (CanReload()){
            isReloading = true;
            //controller.TriggerReload();
            Reload();
        }
    }

    // Check reload input
    private void HandleReload() {
        if(InputHandler.ReloadKeyDown()) {
            TryReload();
        }
    }

    // Reload ammo
    private void Reload() {
        StartCoroutine(_ReloadCountDown());
    }

    // Reload countdown helper
    private IEnumerator<int> _ReloadCountDown() {

        for(float i=0; i<weaponData.maxReloadTime; i+=CommonUtil.GetStepUpdate()) {
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

    // Reset all state variables to their defaults.
    private void InitBeforeUpdate() {
        hasFiredThisFrame = false;
        hasReloadCancelled = false;
        weaponData = weaponData;
        crossHair = crossHair;
        weaponPrefab = weaponPrefab;
    }

    // Communicate with IK mechanism
    private void HandleIK() {
        ikController.leftHandIK = weaponData.leftHandIK;
        ikController.rightHandIK = weaponData.rightHandIK;
    }

    // Set all complex animation to the weapon animator.
    private void SubmitAnimatorStatus() {
        Animator weaponAnimator = GetWeaponAnimator();
        // Whether reloading animation should be played or not
        controller.IsReloading(isReloading);
        // Whether to reset animation to idle or not:
        if(hasFiredThisFrame || IsAiming()) {
            controller.TriggerReset();
        }
        // @Improve @WIP
        AnimatorClipInfo[] clips = weaponAnimator.GetCurrentAnimatorClipInfo(0);
        float clipLength = 0;
        if(clips.Length > 0) {
            clipLength = clips[0].clip.length;  //Animation length in secs
        }
        // Set weapon reloading animation speed. (1.0 x multiplier)
        if(clipLength != 0) {
            controller.FloatMultReloading(clipLength / weaponData.maxReloadTime * CommonUtil.FPS);
        }
    }

    // Camera's gonna vibrate up and down when a player is running.
    // This must appear after player running script for some reason (See PlayerMove to find out)
    private void HandleCameraVibration() {
        if(isSprinting) {
            if(Mathf.Abs(currentCameraVibrationAngle) >= Mathf.Abs(cameraVibrationLimit)) {
                cameraVibrationLimit *= -1;
            }
            currentCameraVibrationAngle = TweenLerpUtil.SpeedLerp(currentCameraVibrationAngle, cameraVibrationLimit, cameraVibrationAcceleration);
            Debug.Log(currentCameraVibrationAngle);
            motor.ApplyCamAdditionalRotation(Quaternion.Euler(0, currentCameraVibrationAngle, currentCameraVibrationAngle/2));
        } else {
            currentCameraVibrationAngle = 0;
        }
    }

    // ==== Life span ====
    protected override void Awake() {
		base.Awake();

        motor = GetComponent<PlayerMotor>();
        CommonUtil.IfNullLogError<PlayerMotor>(motor);

        weaponManager = GetComponent<PlayerWeaponManager>();
		CommonUtil.IfNullLogError<PlayerWeaponManager>(weaponManager);

        attractionSource = gameObject.AddComponent<AttractionSource>();
		AttractionSourceManager.Register(attractionSource);

        ikController = GetComponentInChildren<AvatarIKController>();
	}

	protected override void Start() {
		base.Start();

		//controller = new BaseWeaponAnimationController();
		//controller.player = this;

        cam = mainCamera;
        weaponPrefab = weaponPrefab;
        weaponData = weaponData;
        crossHair = crossHair;
        defaultFov = cam.fieldOfView;
        camRelativeVec = cam.transform.position - this.transform.position;
	}

    void Update() {
        InitBeforeUpdate();

        HandleIK();
        HandlePlayerMovement();
        HandleThrowWeapon();
        HandleWeaponSwap();
        HandleShootInput();
        HandleReload();
        HandleBobbing();
        HandleRecoil();
        HandleAiming();
        HandleCameraVibration();

        //SubmitAnimatorStatus();

    }

    private void OnControllerColliderHit(ControllerColliderHit hit) {
        Inflictable inf = hit.collider.gameObject.GetComponent<Inflictable>();
        if(inf != null) {
            inf.OnInflicted(gameObject);
        }
    }

    void OnRenderObject() {
		// Draw cross hair
		crossHair.DrawLine(new Vector2(Screen.width / 2, Screen.height / 2));
	}

}
