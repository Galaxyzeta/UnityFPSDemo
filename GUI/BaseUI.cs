using UnityEngine;
using UnityEngine.UI;

// All UI widgets' father class.
// Attempt to get rid of writting resources fetch of player again and again.
public class BaseUI : MonoBehaviour {
	public Player player {get; protected set;}

	void Awake() {
		player = FindObjectOfType<Player>();
		CommonUtil.IfNullLogError(player);
	}
}