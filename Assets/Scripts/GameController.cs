using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using Photon;

[RequireComponent (typeof (PhotonView))]
public class GameController : PunBehaviour {

	public static GameController Instance { get; private set; }

	private MenuController menuController;
	private ControlHandler controlHandler;
	private PhotonNetworkHandler photonNetworkHandler;

	public GameObject player { get; private set; }
	public GameObject otherPlayer { get; private set; }


	// LEVEL SETTINGS
	public string levelName = "";
	public bool? singleplayer = null;
	public bool? inLobby = null;
	public bool? isHost = null;
	public uint localPlayerNumber = 0;

	public string player1Name;
	public string player2Name;
	public string player1SteamID;
	public string player2SteamID;
	public PhotonPlayer otherPhotonPlayer;


	// Status of the level load so we know when to set up the level
	public enum LoadStatus {
		NULL,
		AWAITING_LOAD,
		COMPLETE
	}

	public LoadStatus loadStatusP1 = LoadStatus.NULL;
	public LoadStatus loadStatusP2 = LoadStatus.NULL;

	// FRAMES PER SECOND
	private Text fpsText;
	private int frames = 0;
	public bool updateFPS = true;


	// Level specific variables
	private Transform spawnPoints;
	private GameObject waitingCanvasObject;
	private new GameObject camera;



	// Set up singleton Instance
	void Awake () {
		// First we check if there are any other instances conflicting
		if (Instance != null && Instance != this) {     // If there is an instance set already that isn't us...
			Debug.Log ("A GameController Singleton already exists: destroying GameController GameObject...");

			// Destroy this GameObject and make sure we definitely stop the code here.
			Destroy (gameObject);
			return;
		}

		Debug.Log ("A GameController Singleton could not be found, assigning this as the Singleton...");

		// If we have got this far then the Instance is already us, or there is no Instance set yet.
		// Therefore here is where we set the Instance to us and make sure that we don't destroy between scenes.
		Instance = this;
		DontDestroyOnLoad (gameObject);



		menuController = GameObject.FindObjectOfType<MenuController> ();
		if (!menuController) {
			Debug.LogError ("No MenuController script could be found in the scene!");
		}

		photonView.viewID = 103;
	}

	
	void Start () {
		controlHandler = ControlHandler.Instance;
		photonNetworkHandler = PhotonNetworkHandler.Instance;
		
		fpsText = GameObject.Find ("FPS Text").GetComponent<Text> ();
		if (fpsText != null) StartCoroutine (UpdateFPS ());
	}
	

	void Update () {
		// FPS
		frames++;
	}

	
	public IEnumerator UpdateFPS () {
		while (updateFPS) {
			fpsText.text = frames.ToString ();
			frames = 0;
			yield return new WaitForSecondsRealtime (1f);
		}
	}


	public void SteamOverlayActivated () {
		controlHandler.photonView.RPC ("PauseControl", PhotonTargets.All, true);
	}


	public void SetupLevel_Singleplayer (string levelname) {
		// In this case we have to do everything.
		// We load the level, spawn both player prefabs and set them up ready to play.



	}


	public void SetupLevel_Multiplayer () {
		// Find and cache the player spawns
		spawnPoints = GameObject.Find ("Spawn Points").transform;
		Vector3 p1spawn = spawnPoints.FindChild ("Player 1").position;
		Vector3 p2spawn = spawnPoints.FindChild ("Player 2").position;
		if (!spawnPoints) {
			Debug.LogError ("Could not find the 'Spawn Points' GameObject, so cannot spawn the players in their correct positions!");
		}

		// Spawn players
		player = PhotonNetwork.InstantiateSceneObject ("player", p1spawn, Quaternion.identity, 0, null);
		player.GetComponent<PlayerController> ().playerNumber = ControlHandler.Player.One;
		player.GetComponent<PhotonView> ().viewID = 001;
		otherPlayer = PhotonNetwork.InstantiateSceneObject ("player", p2spawn, Quaternion.identity, 0, null);
		otherPlayer.GetComponent<PlayerController> ().playerNumber = ControlHandler.Player.Two;
		otherPlayer.GetComponent<PhotonView> ().viewID = 002;

		// Cache player objects
		controlHandler.photonView.RPC ("CachePlayerObjects", PhotonTargets.AllBuffered);

		PhotonView player1view = player.GetPhotonView ();
		PhotonView player2view = otherPlayer.GetPhotonView ();
		player1view.TransferOwnership (PhotonNetwork.player.ID);
		player2view.TransferOwnership (otherPhotonPlayer.ID);

		photonView.RPC ("ConfigureLocalPlayerAndStartGame", PhotonTargets.All);
	}


	[PunRPC]
	public void ConfigureLocalPlayerAndStartGame () {
		//PhotonView player1view = PhotonView.Find (1);
		//PhotonView player2view = PhotonView.Find (2);
		int localPlayerViewID = (int)localPlayerNumber;
		int otherPlayerViewID = (3 - (int)localPlayerNumber);   // Algebraicly will convert 1 into 2 and vice versa
		PhotonView localPlayerView = PhotonView.Find (localPlayerViewID);
		PhotonView otherPlayerView = PhotonView.Find (otherPlayerViewID);

		camera = GameObject.Find ("Main Camera");
		if (!camera) {
			Debug.Log ("Could not find 'Main Camera' in the scene! Game will probably not work so well without a camera...");
		}

		controlHandler.localPlayerNumber = (ControlHandler.Player)((int)localPlayerNumber);
		Debug.Log ("controlHandler.localPlayerNumber set to ControlHandler.Player." + ((ControlHandler.Player)((int)localPlayerNumber)).ToString ());

		if (PhotonNetwork.isMasterClient) {
			Debug.Log ("ConfigureLocalPlayerAndStartGame() called as player 1...");
			camera.transform.position = new Vector3 (localPlayerView.gameObject.transform.position.x, localPlayerView.gameObject.transform.position.y - 4f, camera.transform.position.z);
			camera.GetComponent<CameraController> ().target = localPlayerView.gameObject.transform;
			camera.GetComponent<CameraController> ().SetEffects (true);

		} else {
			Debug.Log ("ConfigureLocalPlayerAndStartGame() called as player 2...");
			camera.transform.position = new Vector3 (otherPlayerView.gameObject.transform.position.x, otherPlayerView.gameObject.transform.position.y, camera.transform.position.z);
			camera.GetComponent<CameraController> ().target = otherPlayerView.gameObject.transform;
			camera.GetComponent<CameraController> ().SetEffects (true);
		}

		if (!localPlayerView) {
			Debug.LogError ("Local player photonview could not be found. " + localPlayerViewID.ToString());
		}
		if (!otherPlayerView) {
			Debug.LogError ("Other player photonview could not be found. " + otherPlayerViewID.ToString ());
		}
		if (!localPlayerView.GetComponent<PlayerController> ()) {
			Debug.LogError ("could not find player controller on the local player photon view");
		}


		// LOCAL PLAYER THAT WE CONTROL
		localPlayerView.gameObject.name = "Player " + localPlayerViewID.ToString();
		localPlayerView.GetComponent<PlayerController> ().enabled = true;
		localPlayerView.GetComponent<Rigidbody2D> ().isKinematic = false;
		localPlayerView.GetComponent<BoxCollider2D> ().enabled = true;


		// OTHER PLAYER THAT WE DON'T CONTROL
		otherPlayerView.gameObject.name = "Player " + otherPlayerViewID.ToString ();
		otherPlayerView.GetComponent<PlayerController> ().enabled = false;
		otherPlayerView.GetComponent<Rigidbody2D> ().isKinematic = true;
		otherPlayerView.GetComponent<BoxCollider2D> ().enabled = false;
		

		// Start the game!
		waitingCanvasObject.SetActive (false);
		controlHandler.inControlText = GameObject.Find ("In Control Text").GetComponent<Text> ();
		controlHandler.UnpauseControl ();
		controlHandler.SetControl (ControlHandler.Player.One);
	}






	public override void OnCreatedRoom () {
		// This will enter the lobby canvas and set it up properly
		menuController.GoToMenu (menuController.canvasMultiplayerLobby);
		inLobby = true;

		player1Name = SteamManager.displayName;
		player2Name = "";
		GameObject.Find ("Player 1 Name Text").GetComponent<Text> ().text = player1Name;
		GameObject.Find ("Player 2 Name Text").GetComponent<Text> ().text = player2Name;

	}


	public override void OnPhotonPlayerConnected (PhotonPlayer newPlayer) {
		if (localPlayerNumber == 1) {
			if (inLobby == true) {
				if (PhotonNetwork.playerList.Length > 2) {
					// If we now have more than the max players then kick this newly joined player
					PhotonNetwork.CloseConnection (newPlayer);
					return;
				} else {
					// We still have a slot open for the player to join so we can proceed
					player2Name = newPlayer.customProperties["displayName"] as string;
					player2SteamID = newPlayer.customProperties["steamID"] as string;

					otherPhotonPlayer = newPlayer;

					GameObject.Find ("Player 2 Name Text").GetComponent<Text> ().text = player2Name;
					Debug.Log ("Player '" + player2Name + "' has joined the lobby (" + player2SteamID + ")");
				}
			}
		}
	}



	public override void OnPhotonPlayerDisconnected (PhotonPlayer otherPlayer) {
		if (inLobby == true) {
			QuitPhotonRoom ();
		}
			
	}






	public void OnLevelWasLoaded (int levelNumber) {
		// Don't continue if this is the main menu
		if (SceneManager.GetSceneByBuildIndex (levelNumber).name == "Main") return;
		
		Debug.Log ("Level loading complete!");
		
		if (singleplayer == true) {
			Debug.Log ("Setting up the level for Singleplayer...");

			// If our load status is AWAITING_LOAD then we were actually supposed to be loading something
			if (loadStatusP1 == LoadStatus.AWAITING_LOAD || loadStatusP2 == LoadStatus.AWAITING_LOAD) {

				// SET UP THE LEVEL IN SINGLEPLAYER MODE

			} else {
				Debug.LogWarning ("Level load was not expected - there may be an error. Proceed with caution!");
			}


		} else {
			Debug.Log ("Setting up the level for Multiplayer...");

			// Check we have initialised our local player number
			if (localPlayerNumber == 0) {
				Debug.LogError ("No player number was assigned to the local player. Cannot continue with multiplayer level set up.");
				// Do game quit function here - return to menu
				return;
			}

			// Call the RPC to tell all clients that we just finished loading!!
			photonView.RPC ("UpdatePlayerLoadStatus", PhotonTargets.All, (int)localPlayerNumber, (int)LoadStatus.COMPLETE);
			
			
		}

	}


	public void CreatePhotonRoom () {
		RoomOptions roomOptions = new RoomOptions ();
			roomOptions.EmptyRoomTtl = 5000;
			roomOptions.MaxPlayers = 2;
			roomOptions.IsOpen = true;
			roomOptions.IsVisible = true;
			roomOptions.CustomRoomProperties = new ExitGames.Client.Photon.Hashtable ();
			roomOptions.CustomRoomProperties.Add ("hoststeamname", SteamManager.displayName);
			roomOptions.CustomRoomProperties.Add ("level", levelName);

		roomOptions.CustomRoomPropertiesForLobby = new string[2];
			roomOptions.CustomRoomPropertiesForLobby[0] = "hoststeamname";
			roomOptions.CustomRoomPropertiesForLobby[1] = "level";

		Debug.Log ("Hosting new game: " + SteamManager.steamID.ToString ());

		if (!PhotonNetwork.CreateRoom (SteamManager.steamID.ToString (), roomOptions, TypedLobby.Default)) {
			Debug.LogError ("An error occured when trying to create the Photon Room.");
		}
	}
	

	public void QuitPhotonRoom () {
		PhotonNetwork.LeaveRoom ();
		menuController.GoToMenu (menuController.canvasMain);
	}
	
	



	public void LoadPhotonLevel (string levelName) {
		// Only actually load the level if we are player 1 (so that both players don't try to load it)
		if (localPlayerNumber == 1) {
			PhotonNetwork.LoadLevel (levelName);
		}
	}

	[PunRPC]
	public void FetchAndStorePlayerAvatar (int playerNumber) {
		// Before we begin check that the playerNumber given is valid...
		if (playerNumber != 1 && playerNumber != 2) {
			Debug.LogError ("Cannot fetch and store avatar as player number '" + playerNumber.ToString () + "' is not a valid player number!");
			return;
		}
		
		if (localPlayerNumber == playerNumber) {
			menuController.localPlayerAvatar = SteamManager.GetSmallAvatar (SteamManager.steamIDstring);
		} else {
			Debug.Log ("otherPhotonPlayer.customProperties[\"steamID\"] = " + otherPhotonPlayer.customProperties["steamID"].ToString ());
			menuController.otherPlayerAvatar = SteamManager.GetSmallAvatar (otherPhotonPlayer.customProperties["steamID"] as string);
		}

		menuController.UpdatePlayerAvatars ();
	}


	[PunRPC]
	public void UpdatePlayerLoadStatus (int playerNumber, int loadStatusInt) {
		// This function is an RPC to be called on all clients to let every client know that a player's load status has been updated.
		// If after both the loadStatuses of the clients are set to COMPLETE after this update then we know all players have now loaded the scene.
		// This means that the host (player 1) can start to set up the game without anything breaking.

		LoadStatus loadStatus = (LoadStatus)loadStatusInt;
		// 0 = NULL
		// 1 = AWAITING_LOAD
		// 2 = COMPLETE

		if (playerNumber == 1) {
			loadStatusP1 = loadStatus;
			Debug.Log ("Player 1 load status updated to " + loadStatus.ToString () + ".");
		} else if (playerNumber == 2) {
			loadStatusP2 = loadStatus;
			Debug.Log ("Player 2 load status updated to " + loadStatus.ToString () + ".");
		} else {
			Debug.LogWarning ("Invalid player number passed to function UpdatePlayerLoadStatus   :   UpdatePlayerLoadStatus (" + playerNumber.ToString () + ", " + loadStatus.ToString () + ")");
			return;
		}

		// If we are the host, and both players have been setup to begin loading the level, then begin the level load...
		if (localPlayerNumber == 1 && loadStatusP1 == LoadStatus.AWAITING_LOAD && loadStatusP2 == LoadStatus.AWAITING_LOAD) {
			// Then start loading up level (only run on the host client - player 1)
			LoadPhotonLevel (levelName);
		}

		// If we are the host, and both players have finished loading then we can start setting up the game...
		if (loadStatusP1 == LoadStatus.COMPLETE && loadStatusP2 == LoadStatus.COMPLETE) {
			waitingCanvasObject = GameObject.Find ("Waiting Canvas");
			waitingCanvasObject.transform.FindChild ("Main Text").GetComponent<Text> ().text = "Loading...";

			if (localPlayerNumber == 1) {
				// Reset both player load statuses to NULL so we don't do this again if this RPC get's called twice accidentally
				photonView.RPC ("UpdatePlayerLoadStatus", PhotonTargets.All, 1, (int)LoadStatus.NULL);
				photonView.RPC ("UpdatePlayerLoadStatus", PhotonTargets.All, 2, (int)LoadStatus.NULL);
				// Then start setting up the multiplayer level (only run locally on the host client - player 1)
				SetupLevel_Multiplayer ();
			}
		}
	}




}
