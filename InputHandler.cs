using UnityEngine;

public class InputHandler: MonoBehaviour{

	public static bool ReloadKeyDown() {
		return Input.GetKeyDown(KeyCode.R);
	}

	public static bool SwapWeaponKeyDown() {
		return Input.GetKeyDown(KeyCode.Q);
	}

	public static bool SprintKeyHeld() {
		return Input.GetKey(KeyCode.LeftShift);
	}

	public static bool FireKeyDown() {
		return Input.GetKeyDown(KeyCode.Mouse0);
	}

	public static bool FireKeyHeld() {
		return Input.GetKey(KeyCode.Mouse0);
	}

	public static bool FireKeyUp() {
		return Input.GetKeyUp(KeyCode.Mouse0);
	}

	public static bool MouseWheelUp() {
		return Input.GetAxis("Mouse ScrollWheel") > 0;
	}

	public static bool MouseWheelDown() {
		return Input.GetAxis("Mouse ScrollWheel") < 0;
	}

	public static float GetHorizontalAxis() {
		return Input.GetAxis("Horizontal");
	}

	public static float GetVerticalAxis() {
		return Input.GetAxis("Vertical");
	}

	public static float GetViewHorizontalAxis() {
		return Input.GetAxis("Mouse X");
	}

	public static float GetViewVerticalAxis() {
		return Input.GetAxis("Mouse Y");
	}

	public static bool CrouchKeyHeld() {
		return Input.GetKey(KeyCode.LeftControl);
	}

	public static bool AimKeyDown() {
		return Input.GetKey(KeyCode.Mouse1);
	}

	public static bool JumpKeyDown() {
		return Input.GetKeyDown(KeyCode.Space);
	}

	public static bool PickItemKeyDown() {
		return Input.GetKeyDown(KeyCode.E);
	}

	public static bool ThrowItemKeyDown() {
		return Input.GetKeyDown(KeyCode.G);
	}


}