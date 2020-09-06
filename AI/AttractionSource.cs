using UnityEngine;

public class AttractionSource : MonoBehaviour {
	public static float MAX_NOISE = 100f;
	public static float NOISE_DECAY = 20f;

	public float currentAttraction {get; set;} = 0;
	public Actor owner;

	public void AddNoise(float strength) {
		this.currentAttraction = strength;
		this.currentAttraction = Mathf.Clamp(currentAttraction, 0, MAX_NOISE);
	}

	void Update() {
		if(currentAttraction > 0) {
			currentAttraction -= NOISE_DECAY * Time.deltaTime;
		} else {
			currentAttraction = 0;
		}
	}

	// Display effective sound raidius.
	// For debug use.
	void OnDrawGizmos() {
		Gizmos.color = new Color(0, 1f, 0, 0.2f);
		Gizmos.DrawSphere(transform.position, CommonUtil.CalcNoiseThresholdRadius(currentAttraction, 10f));
	}
}