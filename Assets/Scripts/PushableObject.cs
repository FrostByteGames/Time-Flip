using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PushableObject : MonoBehaviour {
	Rigidbody2D rbody;
	public float pushSpeed = 2f;
	[Tooltip ("Lower values mean more drag (speed is slowed at a faster rate). Valid values are between 0 and 1.")]
	public float horizontalDragFactor = 0.5f;

	// Use this for initialization
	void Awake () {
		rbody = GetComponent<Rigidbody2D> ();
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		rbody.velocity = new Vector2 (rbody.velocity.x * horizontalDragFactor, rbody.velocity.y); //replace the x velocity with a reducing function of time
	}

	protected void Push (float speed) {
		rbody.velocity = new Vector2 (speed, rbody.velocity.y);
	}

	public void PushRight () {
		Push (pushSpeed);
	}

	public void PushLeft () {
		Push (-pushSpeed);
	}
}
