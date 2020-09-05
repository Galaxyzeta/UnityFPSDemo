using UnityEngine;

public class AttractionSource : MonoBehaviour {
	public float currentAttraction {get; set;} = 0;
	public float attractionDecay {get; set;}
	public Actor owner;

	public void MakeNoise(float strength, float decay) {
		this.currentAttraction = strength;
		this.attractionDecay = decay;
	}

	void Update() {
		if(currentAttraction > 0) {
			currentAttraction -= attractionDecay * Time.deltaTime;
		} else {
			currentAttraction = 0;
		}
	}

	// Display effective sound raidius.
	// For debug use.
	void OnDrawGizmos() {
		Gizmos.color = new Color(0, 1f, 0, 0.2f);
		Gizmos.DrawSphere(transform.position, CommonUtil.CalcNoiseThresholdRadius(currentAttraction, 20f));
	}
}