using UnityEngine;

/// <summary>An interface that marks objects which are able to be interacted with attacks.</summary>
public class Inflictable : MonoBehaviour {

	public virtual void OnInflicted(GameObject inflictSource) {}

}