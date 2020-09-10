using UnityEngine;

public class AvatarIKController : MonoBehaviour{

	private Animator characterAnimator;

	public Transform leftHandIK;
	public Transform rightHandIK;

	private void Awake() {
		characterAnimator = GetComponent<Animator>();
	}

	private void OnAnimatorIK() {

		if(leftHandIK) {
			characterAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
			characterAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
			characterAnimator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandIK.position);
			characterAnimator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandIK.rotation);
		}
		if(rightHandIK) {
			characterAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
        	characterAnimator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
			characterAnimator.SetIKPosition(AvatarIKGoal.RightHand, rightHandIK.position);
			characterAnimator.SetIKRotation(AvatarIKGoal.RightHand, rightHandIK.rotation);
		}
        

    }
}