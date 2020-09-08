using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TPSController : MonoBehaviour {

    public Camera mainCamera;
    public Transform groundCheckTransform;
    public float gravity = 9.81f;
    public float walkSpeed = 2f;
    public float runningSpeed = 4f;
    public float maxVerticalAngle = 85f;
    public float groundCheckDistance = 0;
    public float initialJumpVelocity = 20f;
    public LayerMask groundMask;
    public Transform rightHandTransform;
    public GameObject weaponPrefab;
    public Vector3 weaponOffeset = new Vector3(0f,0f,0.1f);

    public float veloctiy;
    public bool isGrounded {get; set;}
    public CharacterController characterController {get; set;}
    public Animator animator {get;set;}
    public Vector3 cameraRelative {get; set;}
    public float currentSpeed {get; set;} = 0;
    public float speedAcceleration = 4f;

    void Awake() {
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        cameraRelative = mainCamera.transform.position - this.transform.position;
    }

    void Start() {
        weaponPrefab = Instantiate<GameObject>(weaponPrefab);
        weaponPrefab.transform.SetParent(rightHandTransform);
        weaponPrefab.transform.localPosition = weaponOffeset;
    }

    void Update() {
        // Add some velocity
        veloctiy -= gravity * Time.deltaTime;

        // Ground check
        if(Physics.CheckSphere(groundCheckTransform.position, groundCheckDistance, groundMask)) {
            isGrounded = true;
            veloctiy = 0;
        }

        // Camera rotation
        float mouseX = InputHandler.GetViewHorizontalAxis();
        float mouseY = -InputHandler.GetViewVerticalAxis();
        mainCamera.transform.RotateAround(this.transform.position, Vector3.up, mouseX);
        mainCamera.transform.RotateAround(this.transform.position, mainCamera.transform.right, mouseY);

        // Camera position
        mainCamera.transform.position = transform.position + mainCamera.transform.rotation * cameraRelative;
        
        // Player Move
        float horizontal = InputHandler.GetHorizontalAxis();
        float vertical = InputHandler.GetVerticalAxis();

        // Player rotate with camera
        transform.Rotate(new Vector3(0, mouseX, 0), Space.Self);

        // Speed calculation
        float spdLimiter = walkSpeed;
        if(InputHandler.SprintKeyHeld()) {
            spdLimiter = runningSpeed;
        } else if (horizontal == 0 && vertical == 0) {
            spdLimiter = 0;
        } else if (vertical < 0) {
            spdLimiter = -walkSpeed;
        }
        currentSpeed = SpeedLerpUtil.SpeedLerp(currentSpeed, spdLimiter, speedAcceleration);

        // Apply motion
        Vector3 motion = transform.forward * vertical + transform.right * horizontal;
        motion.Normalize();
        motion = motion * Mathf.Abs(currentSpeed) * Time.deltaTime;

        // Apply movement
        characterController.Move(motion);

        // Apply animation
        animator.SetFloat("motionSpeed", currentSpeed);

        // === Player Jump ===
        if(InputHandler.JumpKeyDown() && isGrounded) {
            veloctiy += initialJumpVelocity;
        }

        characterController.Move(veloctiy * Time.deltaTime * this.transform.up);
    }
}
