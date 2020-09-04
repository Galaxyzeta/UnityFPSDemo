using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ActorManager : MonoBehaviour {
	public List<Actor> actors {get; set;}

	void Awake() {
		actors = new List<Actor>();
	}

	public void Register(Actor actor) {
		actors.Add(actor);
	}

	public Actor[] GetHostile(int myTeam) {
		List<Actor> hostileTargets = new List<Actor>(); 
		foreach (Actor actor in actors) {
			if (actor.team != myTeam) {
				hostileTargets.Add(actor);
			}
		}
		return hostileTargets.ToArray();
	}
}