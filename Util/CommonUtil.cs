using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CommonUtil : MonoBehaviour {

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

	// Not perfect accurate ! 
	// The calculation has an error of range 1-5 pixels
	public static float DistanceProjectionOnCameraScreen(Camera cam, float range, float distanceAtThatRange) {
		Transform origin = cam.transform;
		Vector3 point1 = cam.WorldToScreenPoint(origin.position + origin.TransformPoint(new Vector3(0,-distanceAtThatRange/2,range)));
		Vector3 point2 = cam.WorldToScreenPoint(origin.position + origin.TransformPoint(new Vector3(0,distanceAtThatRange/2,range)));
		return Vector3.Distance(point1, point2);
	}
}