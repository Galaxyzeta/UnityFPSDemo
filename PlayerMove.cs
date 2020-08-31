using UnityEngine;

public class PlayerMove : MonoBehaviour {
	
	[Header("Movement")]
	public float speed;
	private PlayerMotor motor;

	void Start() {
		motor = GetComponent<PlayerMotor>();
		CommonUtil.IfNullLogError<PlayerMotor>(motor);
	}

	void Update() {
		float horizontalAxis = InputHandler.GetHorizontalAxis();	// Z - Movement
		float verticalAxis = InputHandler.GetVerticalAxis();		// X - Movement
		float viewRotX = InputHandler.GetViewVerticalAxis();				// View - X Rotation
		float viewRotY = InputHandler.GetViewHorizontalAxis();				// View - Y Rotation
		Vector3 velocity = new Vector3(horizontalAxis, 0, verticalAxis);

		motor.ApplyVelocity(transform.rotation * velocity * speed * CommonUtil.GetStepUpdate());
		motor.ApplyRotation(Quaternion.Euler(0, viewRotY, 0));	// Restricted Rotate RB
		motor.ApplyBasicCameraRotation(Quaternion.Euler(-viewRotX, viewRotY, 0));	// Free Rotate cam
	}
}