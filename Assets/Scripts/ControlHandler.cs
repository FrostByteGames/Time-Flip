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

	public bool? singleplayer;

	private bool alreadyToggledThisFrame = false;       // This solves the problem of the two local player controller scripts both trying to toggle control in the same frame. This bool makes sure we only toggle once per frame.
	private bool alreadyPausedThisFrame = false;       // This solves the problem of the two local player controller scripts both trying to pause control in the same frame, and ending up not pausing (similar to line above).


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

		/*pauseMenu = GameObject.Find ("Pause Canvas");		// never going to find it because it's in the menu scene whenever this runs
		if (!pauseMenu) {
			Debug.LogWarning ("No Pause Canvas could be found in the scene by the Control Handler...");
		}*/

		if (!photonView) {
			gameObject.AddComponent<PhotonView> ();
		}
		photonView.viewID = 102;
		
	}

	private void LateUpdate () {
		alreadyToggledThisFrame = false;
		alreadyPausedThisFrame = false;
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



	// This method should be called by any external methods, and we will determine whether we need to execute locally or call an RPC
	public void SetControl (Player player) {
		if (singleplayer == true) {
			SetControlLocal (player);
		} else if (singleplayer == false) {
			photonView.RPC ("SetControlRPC", PhotonTargets.All, player);
		} else {
			Debug.LogError ("The 'singeplayer' bool on ControlHandler has not been set! Cannot set control.");
		}
	}
	
	// This method should not be directly accessed. Instead call SetControl(...) and it will call this if we are in singleplayer mode.
	public void SetControlLocal (Player player) {
		cameraController.SetEffects (false);        // Make sure effects are definitely still off (no camera effects in local play)

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

		Debug.Log ("[ControlHandler] Player " + player.ToString () + " has been given control.");
	}
	
	// This method should not be directly accessed. Instead call SetControl(...) and it will call this RPC if we are in multiplayer mode.
	[PunRPC]
	public void SetControlRPC (Player player) {
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
		if (!alreadyToggledThisFrame) {

			alreadyToggledThisFrame = true;

			Debug.Log ("[ControlHandler] Toggling control...");

			if (inControl == Player.One) {
				SetControl (Player.Two);
			} else if (inControl == Player.Two) {
				SetControl (Player.One);
			} else {
				ResetControl ();
			}
		}
	}
	
	// Reset control to the default state (Player.One)
	public void ResetControl () {
		Debug.Log ("[ControlHandler] Resetting control...");
		SetControl (Player.One);
	}




	// For use when control is paused (i.e. menus, animations, cutscenes, etc.)
	public void PauseControl (bool showPauseMenu = true) {
		if (!alreadyPausedThisFrame) {

			alreadyPausedThisFrame = true;

			Debug.Log ("[ControlHandler] Pausing control...");

			if (singleplayer == true) {
				PauseControlLocal (showPauseMenu);
			} else if (singleplayer == false) {
				photonView.RPC ("PauseControlRPC", PhotonTargets.All, showPauseMenu);
			} else {
				Debug.LogError ("The 'singeplayer' bool on ControlHandler has not been set! Cannot pause control.");
			}
		}
	}

	public void PauseControlLocal (bool showPauseMenu = true) {
		if (showPauseMenu) {
			if (!pauseMenu)
				pauseMenu = GameObject.Find ("Pause Canvas");
			pauseMenu.SetActive (true);
		}

		lastControl = inControl;
		SetControlLocal (Player.None); // was an RPC

		isPaused = true;
	}
	
	[PunRPC]
	public void PauseControlRPC (bool showPauseMenu = true) {
		if (showPauseMenu) {
			if (!pauseMenu) pauseMenu = GameObject.Find ("Pause Canvas");
			pauseMenu.SetActive (true);
		}

		lastControl = inControl;
		SetControlLocal (Player.None);		// We use the local variant here as the function is an RPC to be called on both clients.
											// This is done this way so that both clients will show the pause screen if required rather than only the client that paused the game seeing the pause screen!

		isPaused = true;
	}



	// For use when control needs to be resumed (i.e. menus being closed, animations ending, etc.)
	public void UnpauseControl () {
		if (!alreadyPausedThisFrame) {

			alreadyPausedThisFrame = true;

			Debug.Log ("[ControlHandler] Unpausing control...");

			if (singleplayer == true) {
				UnpauseControlLocal ();
			} else if (singleplayer == false) {
				photonView.RPC ("UnpauseControlRPC", PhotonTargets.All);
			} else {
				Debug.LogError ("The 'singeplayer' bool on ControlHandler has not been set! Cannot unpause control.");
			}
		}
	}

	public void UnpauseControlLocal () {
		if (!pauseMenu)
			pauseMenu = GameObject.Find ("Pause Canvas");
		pauseMenu.SetActive (false);

		if (lastControl != Player.None) {
			SetControlLocal (lastControl);
		} else {
			// Return to default value
			ResetControl ();
		}

		isPaused = false;
	}

	[PunRPC]
	public void UnpauseControlRPC () {
		if (!pauseMenu) pauseMenu = GameObject.Find ("Pause Canvas");
		pauseMenu.SetActive (false);

		if (lastControl != Player.None) {
			SetControlLocal (lastControl);
		} else {
			// Return to default value
			ResetControl ();
		}

		isPaused = false;
	}
	
}
