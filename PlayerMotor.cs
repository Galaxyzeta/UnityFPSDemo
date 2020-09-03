using UnityEngine;

public class PlayerMotor : MonoBehaviour {

	private Rigidbody rb;

	private Player player;
	private Camera cam;
	private GameObject weaponPrefab;

	private Vector3 velocity;
	private Quaternion playerRotation;
	private float thrustForce;
	private float jumpForce;
	private Quaternion camOffsetRotation;

	private Quaternion basicCamRotation;

	private Quaternion weaponOffSetRotation;
	private Vector3 weaponOffsetPosition;
	private Vector3 weaponlocalPositionOffset;
	private Vector3 weaponAimRelative;

	private float camFov;

	public float gravity = 0.1f;
	public float maxVerticalSpeed = 2f;
	public float verticalAngleRestriction = 89f;
	public Transform globalDefaultPoint;
    public Transform globalAimPoint;
	public Transform globalWeaponSpawnPoint;
	public bool isAir {get; set;} = true;

	public void ApplyVelocity(Vector3 deltaVelocity) {
		this.velocity += deltaVelocity;
	}

	public void ApplyThrustForce(float thrustForce) {
		this.thrustForce += thrustForce;
	}

	public void ApplyJumpForce(float jumpForce) {
		this.jumpForce = jumpForce;
	}

	public void ApplyRotation(Quaternion deltaRotation) {
		this.playerRotation *= deltaRotation;
	}

	public void ApplyCamRotation(Quaternion rotation) {
		this.camOffsetRotation *= rotation;
	}

	public void ApplyWeaponLocalPosition(Vector3 position) {
		this.weaponlocalPositionOffset += position;
	}

	public void ApplyCamFov(float fov) {
		this.cam.fieldOfView = fov;
	}

	public void ApplyBasicCameraRotation(Quaternion rotation) {
		this.basicCamRotation *= rotation;
	}

	public void ResetUpdateCache() {
		weaponOffsetPosition = weaponlocalPositionOffset = Vector3.zero;
		playerRotation = camOffsetRotation = weaponOffSetRotation = Quaternion.Euler(0f,0f,0f);
		
	}

	public void ResetFixedUpdateCache() {
		thrustForce = jumpForce = 0f;
		velocity = Vector3.zero;
	}

	private float ConvertAngle(float angle) {
		angle -= 180;
		if(angle > 0) {
			return angle - 180;
		} else if(angle == 0) {
			return 0;
		}
		return angle + 180;
	}
	
	// -90 deg
	//  | up
	//  +--- 0 deg (Horizon Line)---
	//  | down
	//  90 deg
	private float ClampVerticalAngle(float xRot) {
		float converted = ConvertAngle(xRot);
		if(converted < -85f) {
			// Almost bottom
			xRot = -85f;
		} else if (converted > 85f) {
			// Almost top of head.
			xRot = 85f;
		}
		return xRot;
	}

	// Erase tween action error.
	public void CorrectWeaponPosition() {
		weaponPrefab.transform.position = globalDefaultPoint.position;
	}

	// ==== Life Span ====

	void Start() {
		// Fetch resources
		rb = gameObject.GetComponent<Rigidbody>();
		player = gameObject.GetComponent<Player>();
		cam = player.cam;
		weaponPrefab = player.weaponPrefab;

		// Set basic rotations.
		basicCamRotation = cam.transform.rotation;
		
		// Clear data for initial use.
		ResetFixedUpdateCache();
		ResetUpdateCache();
	}

	void FixedUpdate() {
		
		// General movement
		if(velocity != Vector3.zero) {
            rb.MovePosition(transform.position + velocity * Time.fixedDeltaTime);
        }
		// Thruster movement
        if(thrustForce > 0f) {
            rb.AddForce(thrustForce * transform.up * Time.fixedDeltaTime, ForceMode.Acceleration);
        }
		// Jump
		if(jumpForce > 0) {
			rb.AddForce(new Vector3(0, jumpForce * Time.fixedDeltaTime, 0), ForceMode.VelocityChange);
		}

		// Clean up data to prevent accumulations
		ResetFixedUpdateCache();
		
	}

	// Calculate player's movement all in this function.
	void LateUpdate() {
		// Keep variable up-to-date. (A substitution of C lang pointer! )
		weaponPrefab = player.weaponPrefab;
		
		// General rotation: player, cam, weapon.
		// Rigidbody rotation only operate Y-Axis (horizontal rotation) !
        rb.MoveRotation(rb.rotation * playerRotation);

		// Calculate basic camera rotation. Important.
		float verticalAngle = basicCamRotation.eulerAngles.x;
		float horizontalAngle = rb.transform.rotation.eulerAngles.y;
		// Must clamp view rotation. Important
		basicCamRotation = Quaternion.Euler(ClampVerticalAngle(verticalAngle), horizontalAngle, 0f);

		// Reset camera rotation to its basic state (Only rotated through rigidbody movement)
		cam.transform.rotation = basicCamRotation;
		// Then, apply animated effects on it.
		cam.transform.rotation *= camOffsetRotation;

		// Weapon position: using localposition is much more easier!
		weaponPrefab.transform.localPosition = this.weaponlocalPositionOffset;

		ResetUpdateCache();
		
	}
}