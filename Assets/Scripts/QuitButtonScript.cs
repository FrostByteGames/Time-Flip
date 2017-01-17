using UnityEngine;
using System.Collections;

public class QuitButtonScript : MonoBehaviour {

	private PhotonNetworkHandler photonNetworkHandler;
	
	void Start () {
		photonNetworkHandler = PhotonNetworkHandler.Instance;
	}

	public void QuitGame() {
		Debug.Log ("Quitting the game...");
		photonNetworkHandler.ExitRoom ();
	}
}
