using UnityEngine;
using UnityEngine.UI;

// All UI widgets' father class.
// Attempt to get rid of writting resources fetch of player again and again.
public class BaseUI : MonoBehaviour {
	public AbstractPlayer player {get; protected set;}

	void Awake() {
		player = FindObjectOfType<AbstractPlayer>();
		CommonUtil.IfNullLogError(player);
	}
}