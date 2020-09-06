using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AttractionSourceManager : MonoBehaviour {
	public static List<AttractionSource> attractionSources {get; set;} = new List<AttractionSource>();

	public static void Register(AttractionSource src) {
		attractionSources.Add(src);
	}

	public static void Unregister(AttractionSource src) {
		attractionSources.Remove(src);
	}
}