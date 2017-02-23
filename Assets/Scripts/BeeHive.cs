using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeeHive : MonoBehaviour {
	[SerializeField]
	public List<GameObject> swarms;
	
	public Vector2 centre = new Vector2(0f, 0f);
	public float swarmRange = 5f;
	public float swarmChaseRange = 5f;

	// Use this for initialization
	void Start () {
		if (swarms.Count == 0) {
			SpawnSwarm ();
		}
	}

	public void SpawnSwarm () {
		GameObject swarmObject = Instantiate (Resources.Load<GameObject> ("Bees"), transform.localPosition, Quaternion.identity, this.transform);
		BeeSwarm swarm = swarmObject.AddComponent<BeeSwarm> ();
		swarm.state = BeeSwarm.State.STATIONARY;

		swarms.Add (swarmObject);
		Debug.Log ("Spawned a bee swarm object.");
	}
}
