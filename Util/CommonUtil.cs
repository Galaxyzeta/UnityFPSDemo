using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CommonUtil : MonoBehaviour {

	public static class AnimParameters {
		public static readonly string isReloading = "isReloading";
		public static readonly string multReloading = "multReloading";
	}

	// Explain 1: Not actual game FPS, but the virtual FPS that the game 'looks like'.
	// Explain 2: The amount to add in a second.
	// -----------------------------------------------------------
	// Example 1: 
	// Actual FPS = 60 | Virtual FPS = 60 => DeltaTime = 1/60 s
	// stepUpdate = 1  | timeCostToFill 120 is: 120 / (60*1) = 2s 
	// -----------------------------------------------------------
	// Example 2:
	// Actual FPS = 30 | virtual FPS = 60 => DeltaTime = 1/30 s
	// stepUpdate = 2  | timeCostToFill 120 is: 120 / (30*2) = 2s
	// -----------------------------------------------------------
	// Compare Ex1 and Ex2, filling the same guage took same time.

	
	public static readonly float FPS = 60;
	public static float GetStepUpdate() {
		return FPS * Time.deltaTime;
	}

	public static void IfNullLogError <T>(T obj) {
		if(obj == null) {
			Debug.LogError(string.Format("[%s] Can't be null !", obj.ToString()));
		}
	}

	public static float DistanceProjectionOnCameraScreen(Camera cam, float range, float distanceToConvert) {
		Vector3 point1 = cam.WorldToScreenPoint(cam.transform.position + new Vector3(0,0,range));
		Vector3 point2 = cam.WorldToScreenPoint(cam.transform.position + new Vector3(0,distanceToConvert, range));
		return Vector3.Distance(point1, point2);
	}
}