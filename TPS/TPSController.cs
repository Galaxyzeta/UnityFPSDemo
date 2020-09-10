using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TPSController : AbstractPlayer {

    public Transform groundCheckTransform;
    public float gravity = 9.81f;
    public float walkSpeed = 2f;
    public float runningSpeed = 4f;
    public float maxVerticalAngle = 85f;
    public float groundCheckDistance = 0;
    public float initialJumpVelocity = 20f;
    public LayerMask groundMask;
    public Vector3 weaponOffeset = new Vector3(0f,0f,0.1f);
    public float speedAcceleration = 4f;
    public float veloctiy;
    public float maxAimValue = 30;
    public float maxAimMultiplier = 0.5f;

    public bool isGrounded {get; set;}
    public Animator animator {get;set;}
    public Vector3 cameraRelative {get; set;}
    public float currentSpeed {get; set;} = 0;
    private float aimValue = 0;

    protected override void Awake() {
        base.Awake();
        
        animator = GetComponentInChildren<Animator>();
        cameraRelative = mainCamera.transform.position - this.transform.position;
    }

    protected override void Start() {
        base.Start();
    }

    void Update() {
        // Add some velocity
        veloctiy -= gravity * Time.deltaTime;

        // Ground check
        if(Physics.CheckSphere(groundCheckTransform.position, groundCheckDistance, groundMask)) {
            isGrounded = true;
            veloctiy = 0;
        }

        // Camera Input
        float mouseX = InputHandler.GetViewHorizontalAxis();
        float mouseY = -InputHandler.GetViewVerticalAxis();

        // Aiming
        if(InputHandler.AimKeyHeld()) {
            aimValue = Mathf.Clamp(aimValue + CommonUtil.GetStepUpdate(), 0, maxAimValue);
        } else {
            aimValue = Mathf.Clamp(aimValue - CommonUtil.GetStepUpdate(), 0, maxAimValue);
        }
        mainCamera.transform.position = transform.position + mainCamera.transform.rotation*cameraRelative*(1-maxAimMultiplier * aimValue/maxAimValue);

        // Shoot
        if(InputHandler.FireKeyHeld()) {
            weaponData.TryShoot(mainCamera.transform);
        }

        // Rotation
        transform.RotateAround(this.transform.position, Vector3.up, mouseX);
        mainCamera.transform.RotateAround(this.transform.position, mainCamera.transform.right, mouseY);
        
        // Player Input
        float horizontal = InputHandler.GetHorizontalAxis();
        float vertical = InputHandler.GetVerticalAxis();

        // Player rotate with camera
        transform.Rotate(new Vector3(0, mouseX, 0), Space.Self);

        // Flat movement speed calculation
        float spdLimiter = walkSpeed;
        if(InputHandler.SprintKeyHeld()) {
            spdLimiter = runningSpeed;
        } else if (horizontal == 0 && vertical == 0) {
            spdLimiter = 0;
        } else if (vertical < 0) {
            spdLimiter = -walkSpeed;
        }
        currentSpeed = TweenLerpUtil.SpeedLerp(currentSpeed, spdLimiter, speedAcceleration);

        // === Player Jump ===
        if(InputHandler.JumpKeyDown() && isGrounded) {
            isGrounded = false;
            veloctiy += initialJumpVelocity;
        }
        characterController.Move(veloctiy * Time.deltaTime * this.transform.up);

        // Apply motion
        Vector3 motion = transform.forward * vertical + transform.right * horizontal;
        motion.Normalize();
        motion = motion * Mathf.Abs(currentSpeed) * Time.deltaTime;

        // Apply movement
        characterController.Move(motion);

        // Apply animation
        animator.SetFloat("motionSpeed", currentSpeed);
        animator.SetFloat("jumpSpeed", veloctiy);
        animator.SetBool("isGrounded", isGrounded);
        
    }

    void OnRenderObject() {
		// Draw cross hair
		crossHair.DrawLine(new Vector2(Screen.width / 2, Screen.height / 2));
	}
}
