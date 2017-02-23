using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeeSwarm : MonoBehaviour {

	public BeeHive hive;
	private ParticleSystem particles;

	public Vector2 centre = new Vector2(0f, 0f);
	public float swarmRange = 5f;
	public float swarmChaseRange = 5f;
	
	public enum State {
		STATIONARY,
		PASSIVE,
		AGGRESSIVE,
		CHASING,
		RETURNING,
		HIDDEN
	}

	public State state;


	void Start () {
		state = State.STATIONARY;

		particles = GetComponent<ParticleSystem> ();
		if (!particles) Debug.LogError ("No particles on this bee swarm object! The bees won't show up if there's no particles!!!");

		hive = transform.parent.GetComponent<BeeHive> ();
		if (!hive) Debug.LogWarning ("Parent of this bee swarm object does not have a BeeHive.cs script!");

		centre = hive.centre;
		swarmRange = hive.swarmRange;
		swarmChaseRange = hive.swarmChaseRange;
	}
	

	void Update () {
		switch (state) {
			
		}
	}


}
