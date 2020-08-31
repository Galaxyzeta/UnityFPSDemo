using UnityEngine;

public class InputHandler: MonoBehaviour{

	public static bool ReloadKeyDown() {
		return Input.GetKeyDown(KeyCode.R);
	}

	public static bool SwapWeaponKeyDown() {
		return Input.GetKeyDown(KeyCode.Q);
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

	public static bool AimKeyDown() {
		return Input.GetButton("Fire2");
	}


}