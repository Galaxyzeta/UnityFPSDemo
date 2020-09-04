using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PatrolPath : MonoBehaviour {
	
	public enum Connectivity {
		CIRCULAR, CIRCULAR_REVERSE, REVERSE
	}
	
	public List<Transform> pointList;
	public Connectivity patrolMode;
	public GameObject owner {get; set;}

	public int nextTarget = 0;
	private int direction = 1;

	/// <summary>Check whether the patrolling target has reached its next patrol point, then update the next patrol target.</summary>
	public Transform CheckAndHandleTargetReached(float thresholdDistance) {
		float dist = Vector3.Distance(owner.transform.position, pointList[nextTarget].position);
		if (dist < thresholdDistance) {
			// Target Completed, get next target
			return pointList[nextTarget = GetNextTargetIndex()];
		}
		return null;
	}

	public Transform GetNextTransform() {
		return pointList[nextTarget];
	}

	private int GetNextTargetIndex() {
		int currentIndex = nextTarget;
		int len = pointList.Count;
		switch (patrolMode) {
			case Connectivity.CIRCULAR: {
				if(currentIndex == len-1) {
					return 0;
				} else {
					return currentIndex + 1;
				}
			}
			case Connectivity.CIRCULAR_REVERSE: {
				if(currentIndex == 0) {
					return len-1;
				} else {
					return currentIndex - 1;
				}
			}
			case Connectivity.REVERSE: {
				if(currentIndex == 0) {
					direction = 1;
				} else if (currentIndex == len-1) {
					direction = -1;
				}
				return direction + currentIndex;
			}
			default: {
				// Actually this is an error, it cannot be occurred !
				return 0;
			}
		}
	}

	void Start() {
		if(pointList.Count < 2) {
			Debug.LogError("[PatrolPath] is invalid! Path node count must be equal or greater than 2!");
		}
	}
}