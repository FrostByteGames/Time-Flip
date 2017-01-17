using UnityEngine;
using UnityEngine.UI;
using System;
using Photon;

[RequireComponent (typeof (PhotonView))]
public class ControlHandler : PunBehaviour {
	// Static singleton property
	public static ControlHandler Instance { get; private set; }

	public enum Player {
		None,
		One,
		Two
	}

	public Player inControl { get; private set; }
	public Player localPlayerNumber = Player.None;
	public bool isPaused { get; private set; }
	private Player lastControl = Player.None;
	

	// Public vars for GameObjects to hook into
	public GameObject player1;
	public GameObject player2;
	new public Camera camera;
	public CameraController cameraController;
	public Text inControlText;
	private GameObject pauseMenu;

	void Awake () {
		// First we check if there are any other instances conflicting
		if (Instance != null && Instance != this) {     // If there is an instance set already that isn't us...

			Debug.Log ("ControlHandler Singleton already exists: destroying GameObject...");

			// Destroy this GameObject and make sure we definitely stop the code here.
			Destroy (gameObject);
			return;
		}

		Debug.Log ("A ControlHandler Singleton could not be found, assigning this as the Singleton...");

		// If we have got this far then the Instance is already us, or there is no Instance set yet.
		// Therefore here is where we set the Instance to us and make sure that we don't destroy between scenes.
		Instance = this;
		DontDestroyOnLoad (gameObject);

		pauseMenu = GameObject.Find ("Pause Canvas");
		if (!pauseMenu) {
			Debug.LogWarning ("No Pause Canvas could be found in the scene by the Control Handler...");
		}

		if (!photonView) {
			gameObject.AddComponent<PhotonView> ();
		}
		photonView.viewID = 102;
		
	}


	[PunRPC]
	public void CachePlayerObjects () {
		Debug.Log ("Caching player objects...");

		// Cache player 1 object
		GameObject playerOne = PhotonView.Find (001).gameObject;
		if (playerOne) {
			player1 = playerOne;
			Debug.Log (" - Player 1 cached.");
		} else {
			Debug.LogWarning (" - Could not find a PhotonView with ID: 001 (player one)");
		}

		// Cache player 2 object
		GameObject playerTwo = PhotonView.Find (002).gameObject;
		if (playerTwo) {
			player2 = playerTwo;
			Debug.Log (" - Player 2 cached.");
		} else {
			Debug.LogWarning (" - Could not find a PhotonView with ID: 002 (player two)");
		}

		Debug.Log ("Player objects cached.");
	}


	[PunRPC]
	public void CacheOtherPlayer (int p1viewID, int p2viewID) {
		Debug.Log ("Caching other player object...");

		// Cache player 1 object
		GameObject playerOne = PhotonView.Find (p1viewID).gameObject;
		if (playerOne) {
			player1 = playerOne;
			Debug.Log (" - Player 1 cached.");
		} else {
			Debug.LogWarning ("Could not find a PhotonView with ID: " + p1viewID);
		}

		// Cache player 2 object
		GameObject playerTwo = PhotonView.Find (p2viewID).gameObject;
		if (playerTwo) {
			player2 = playerTwo;
			Debug.Log (" - Player 2 cached.");
		} else {
			Debug.LogWarning ("Could not find a PhotonView with ID: " + p2viewID);
		}

		Debug.Log ("Player objects cached.");
	}


	// Set which player is currently in control
	[PunRPC]
	public void SetControl (Player player) {
		inControl = player;
		
		if (player == Player.One) {
			player1.GetComponent<ParticleSystem> ().Emit (25);
			cameraController.target = player1.gameObject.transform;
			inControlText.text = "Player One";

		} else if (player == Player.Two) {
			player2.GetComponent<ParticleSystem> ().Emit (25);
			cameraController.target = player2.gameObject.transform;
			inControlText.text = "Player Two";

		}


		if (player == localPlayerNumber) {
			cameraController.SetEffects (false);
		} else {
			cameraController.SetEffects (true);
		}


		Debug.Log ("[ControlHandler] Player " + player.ToString () + " has been given control.");
	}


	// Toggle between players 1 & 2 for control
	public void TogglePlayerInControl () {
		if (inControl == Player.One) {
			photonView.RPC("SetControl", PhotonTargets.All, Player.Two);
		} else if (inControl == Player.Two) {
			photonView.RPC ("SetControl", PhotonTargets.All, Player.One);
		} else {
			ResetControl ();
		}
	}


	// For use when control is paused (i.e. menus, animations, cutscenes, etc.)
	[PunRPC]
	public void PauseControl (bool showPauseMenu = true) {
		if (showPauseMenu) {
			if (!pauseMenu) pauseMenu = GameObject.Find ("Pause Canvas");
			pauseMenu.SetActive (true);
		}

		lastControl = inControl;
		SetControl(Player.None); // was an RPC

		isPaused = true;
	}


	// For use when control needs to be resumed (i.e. menus being closed, animations ending, etc.)
	[PunRPC]
	public void UnpauseControl () {
		if (!pauseMenu) pauseMenu = GameObject.Find ("Pause Canvas");
		pauseMenu.SetActive (false);

		if (lastControl != Player.None) {
			SetControl (lastControl);
		} else {
			// Return to default value
			ResetControl ();
		}

		isPaused = false;
	}


	// Reset control to the default state (Player.One)
	public void ResetControl () {
		photonView.RPC ("SetControl", PhotonTargets.All, Player.One);
	}
}
