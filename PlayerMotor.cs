using UnityEngine;

public class PlayerMotor : MonoBehaviour {

	public float gravity = 9.81f;

	private CharacterController cc;

	private AbstractPlayer player;
	private Camera cam;
	private GameObject weaponPrefab;

	private Vector3 flatVelocity;
	public float verticalSpeed;
	private Quaternion inputRotation;
	private Quaternion camAdditionalRotation;

	private Quaternion basicCamRotation;

	private Quaternion weaponOffSetRotation;
	private Vector3 weaponOffsetPosition;
	private Vector3 weaponlocalPositionOffset;
	private Vector3 weaponAimRelative;

	private float camFov;

	public float maxVerticalSpeed = 2f;
	public float verticalAngleRestriction = 89f;
	public Transform globalDefaultPoint;
    public Transform globalAimPoint;
	public Transform globalWeaponSpawnPoint;

	public void ApplyFlatMotion(Vector3 deltaVelocity) {
		this.flatVelocity += deltaVelocity;
	}

	public void ApplyVerticalSpeed(float deltaSpeed) {
		this.verticalSpeed +=  deltaSpeed;
	}

	public void ResetVerticalSpeed() {
		this.verticalSpeed = 0;
	}

	public void ApplyInputRotation(Quaternion deltaRotation) {
		this.inputRotation *= deltaRotation;
	}

	public void ApplyCamAdditionalRotation(Quaternion rotation) {
		this.camAdditionalRotation *= rotation;
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
		inputRotation = camAdditionalRotation = weaponOffSetRotation = Quaternion.Euler(0f,0f,0f);
		flatVelocity = Vector3.zero;
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
		player = gameObject.GetComponent<AbstractPlayer>();
		cam = player.mainCamera;
		weaponPrefab = player.weaponPrefab;
		cc = player.characterController;

		// Set basic rotations.
		basicCamRotation = cam.transform.rotation;
		
		ResetUpdateCache();
	}

	// Calculate player's movement all in this function.
	void LateUpdate() {

		// General movement
		if(flatVelocity != Vector3.zero) {
            cc.Move(flatVelocity * Time.deltaTime);
        }
		// Vertical movement
		// -- Gravity effect

		if(verticalSpeed != 0) {
			cc.Move(verticalSpeed * Vector3.up * Time.deltaTime);
		}
		verticalSpeed -= gravity;

		// Keep variable up-to-date. (A substitution of C lang pointer! )
		weaponPrefab = player.weaponPrefab;
		
		// General rotation: player, cam, weapon.
		// Rigidbody rotation only operate Y-Axis (horizontal rotation) !
        //rb.MoveRotation(rb.rotation * inputRotation);
		Vector3 euler = inputRotation.eulerAngles;
		transform.Rotate(new Vector3(0, euler.y, 0));

		// Calculate basic camera rotation. Important.
		float verticalAngle = basicCamRotation.eulerAngles.x;
		float horizontalAngle = transform.eulerAngles.y;
		// Must clamp view rotation. Important
		basicCamRotation = Quaternion.Euler(ClampVerticalAngle(verticalAngle), horizontalAngle, 0f);

		// Reset camera rotation to its basic state (Only rotated through rigidbody movement)
		cam.transform.rotation = basicCamRotation;
		// Then, apply animated effects on it.
		cam.transform.rotation *= camAdditionalRotation;

		// Weapon position: using localposition is much more easier!
		if(weaponPrefab) {
			weaponPrefab.transform.localPosition = this.weaponlocalPositionOffset;
		}

		ResetUpdateCache();
		
	}
}