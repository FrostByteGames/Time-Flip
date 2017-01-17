using UnityEngine;
using System.Collections;
using Photon;

public class NetworkPlayer : PunBehaviour {
	private float lastSynchronizationTime = 0f;
	private float syncDelay = 0f;
	private float syncTime = 0f;
	private Vector3 syncStartPosition = Vector3.zero;
	private Vector3 syncEndPosition = Vector3.zero;

	// Update is called once per frame
	void Update () {
		if (!photonView.isMine) {
			syncTime += Time.deltaTime;
			transform.position = Vector3.Lerp (syncStartPosition, syncEndPosition, syncTime / syncDelay);
		}
	}


	public void OnPhotonSerializeView (PhotonStream stream, PhotonMessageInfo info) {
		if (stream.isWriting) {			// LOCAL PLAYER
			stream.SendNext (transform.position);
			stream.SendNext (GetComponent<Rigidbody2D> ().velocity);

		} else {						// NETWORK PLAYER
			Vector3 syncPosition = (Vector3)stream.ReceiveNext ();
			Vector2 syncVelocity = (Vector2)stream.ReceiveNext ();

			syncTime = 0f;
			syncDelay = Time.time - lastSynchronizationTime;
			lastSynchronizationTime = Time.time;

			syncEndPosition = syncPosition + (Vector3)syncVelocity  * syncDelay;
			syncStartPosition = transform.position;


		}
	}
}
