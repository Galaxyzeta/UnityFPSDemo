using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ActorManager : MonoBehaviour {
	public static List<Actor> actors {get; set;} = new List<Actor>();

	public static void Register(Actor actor) {
		Debug.Log(actor);
		actors.Add(actor);
	}

	public static void Unregister(Actor actor) {
		actors.Remove(actor);
	}

	public static Actor[] GetHostile(int myTeam) {
		List<Actor> hostileTargets = new List<Actor>(); 
		foreach (Actor actor in actors) {
			if (actor.team != myTeam) {
				hostileTargets.Add(actor);
			}
		}
		return hostileTargets.ToArray();
	}
}