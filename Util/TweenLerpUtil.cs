using UnityEngine;

public class TweenLerpUtil : MonoBehaviour {

	public static float SpeedLerp(float currentSpeed, float targetSpeed, float acceleration) {
		if(currentSpeed < targetSpeed) {
			currentSpeed += acceleration * Time.deltaTime;
			currentSpeed = currentSpeed > targetSpeed? targetSpeed: currentSpeed;
		} else if (currentSpeed > targetSpeed) {
			currentSpeed -= acceleration * Time.deltaTime;
			currentSpeed = currentSpeed < targetSpeed? targetSpeed: currentSpeed;
		}
		return currentSpeed;
	}

}