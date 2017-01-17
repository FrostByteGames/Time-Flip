using UnityEngine;
using ExitGames.Client.Photon;
using System;
using System.Collections;
using Photon;
using UnityEngine.SceneManagement;
using DG.Tweening;
using UnityEngine.UI;

[RequireComponent (typeof (PhotonView))]
public class PhotonNetworkHandler : PunBehaviour {
	// Static singleton property
	public static PhotonNetworkHandler Instance { get; private set; }

	public enum ConnectionState {
		NOT_CONNECTED,
		STEAM_ONLY,
		CONNECTED_INLOBBY,
		CONNECTED_INGAME
	}

	public ConnectionState multiplayerConnectionState = ConnectionState.NOT_CONNECTED;

	private MenuController menuController;
	private ControlHandler controlHandler;

	private GameObject player1;
	private GameObject player2;

	private Text connectionStateText;
	private Text pingText;

	private Text inControlText;




	void Awake () {
		// First we check if there are any other instances conflicting
		if (Instance != null && Instance != this) {     // If there is an instance set already that isn't us...

			Debug.Log ("A PhotonNetworkHandler Singleton already exists: destroying GameObject...");

			// Destroy this GameObject and make sure we definitely stop the code here.
			Destroy (gameObject);
			return;
		}

		Debug.Log ("A PhotonNetworkHandler Singleton could not be found, assigning this as the Singleton...");

		// If we have got this far then the Instance is already us, or there is no Instance set yet.
		// Therefore here is where we set the Instance to us and make sure that we don't destroy between scenes.
		Instance = this;
		DontDestroyOnLoad (gameObject);
	}


	void Start () {

		menuController = FindObjectOfType<MenuController> ();
		if (!menuController) {
			Debug.LogWarning ("No Menu Controller script found in the scene...");
		}

		controlHandler = ControlHandler.Instance;


		if (SteamManager.Initialized) {
			multiplayerConnectionState = ConnectionState.STEAM_ONLY;

			ExitGames.Client.Photon.Hashtable propertiesToSet = new ExitGames.Client.Photon.Hashtable ();
			propertiesToSet["displayName"] = SteamManager.displayName;
			propertiesToSet["steamID"] = SteamManager.steamIDstring;
			PhotonNetwork.SetPlayerCustomProperties (propertiesToSet);

			Debug.Log ("Player Steam details: " + SteamManager.displayName + " (" + SteamManager.steamIDstring + ")");

			PhotonNetwork.sendRate = 15;
			PhotonNetwork.sendRateOnSerialize = 15;
			PhotonNetwork.automaticallySyncScene = true;
			//PhotonNetwork.playerName = SteamManager.displayName;

			Debug.Log ("Connecting to the Photon Network...");
			PhotonNetwork.ConnectUsingSettings ("0.1");
		}


		connectionStateText = GameObject.Find ("Connection State Text").GetComponent<Text> ();
		pingText = GameObject.Find ("Ping Text").GetComponent<Text> ();
	}


	void Update () {

	}


	void OnGUI () {
		if (multiplayerConnectionState != ConnectionState.CONNECTED_INGAME) {
			/*
            GUILayout.BeginHorizontal ();
				GUILayout.Label (PhotonNetwork.connectionStateDetailed.ToString ());
				GUILayout.Label (PhotonNetwork.GetPing ().ToString () + "ms");
			GUILayout.EndHorizontal ();

			GUILayout.Label (PhotonNetwork.countOfPlayers.ToString() + " players");
            */

			if (PhotonNetwork.connectionStateDetailed.ToString () != "Authenticated") {
				connectionStateText.text = PhotonNetwork.connectionStateDetailed.ToString ();
			} else {
				connectionStateText.text = "Connected";
				if (pingText) pingText.text = PhotonNetwork.GetPing ().ToString () + "ms";
			}

		}
	}


	public override void OnJoinedLobby () {
		Debug.Log ("Connected to the Photon Network. Entered the main lobby.");
		multiplayerConnectionState = ConnectionState.CONNECTED_INLOBBY;
	}


	public void OnPhotonJoinRoomFailed () {
		Debug.Log ("[PUN] The room could not be joined.");

	}

	// MOVED TO GAME CONTROLLER
	public override void OnJoinedRoom () {
		multiplayerConnectionState = ConnectionState.CONNECTED_INGAME;
		menuController.GoToMenu (menuController.canvasMultiplayerLobby);

		if (PhotonNetwork.isMasterClient) {
			menuController.UpdatePlayerNames (SteamManager.displayName);
			menuController.lobby_beginGameButton.SetActive (true);
			menuController.lobby_beginGameReplacementObject.SetActive (false);
		} else {
			menuController.UpdatePlayerNames (PhotonNetwork.room.customProperties["hoststeamname"] as string, SteamManager.displayName);
			menuController.lobby_beginGameButton.SetActive (false);
			menuController.lobby_beginGameReplacementObject.SetActive (true);
		}

		// Cache other PhotonPlayer on the GameController
		foreach (PhotonPlayer p in PhotonNetwork.playerList) {
			if (p != PhotonNetwork.player) GameController.Instance.otherPhotonPlayer = p;
		}		

		GameController.Instance.photonView.RPC ("FetchAndStorePlayerAvatar", PhotonTargets.AllBuffered, (int)GameController.Instance.localPlayerNumber);

		Debug.Log ("JOINED THE ROOM (" + PhotonNetwork.playerList.Length + " other players)");
	}


	public override void OnCreatedRoom () {
		Debug.Log ("ROOM CREATED");
	}

	public override void OnPhotonCreateRoomFailed (object[] codeAndMsg) {
		Debug.LogError ("FAILED TO CREATE ROOM!");
		foreach (object msg in codeAndMsg) {
			Debug.Log (msg.ToString ());
		}
	}

	/*public override void OnPhotonPlayerConnected (PhotonPlayer otherPlayer) {
        if (PhotonNetwork.playerList.Length >= 2) {
			if (PhotonNetwork.isMasterClient) {
				Debug.Log ("[PUN] Second player joined, setting up level...");

				SpawnPlayerObjects ();
				
				// Transfer ownership of the player objects to their respective players
				PhotonView player1view = player1.GetPhotonView ();
				PhotonView player2view = player2.GetPhotonView ();
				player1view.TransferOwnership (PhotonNetwork.player.ID);
				player2view.TransferOwnership (otherPlayer.ID);

				Debug.Log ("Calling RPC: InitLocalPlayer(...)");
				photonView.RPC ("InitLocalPlayer", PhotonTargets.All, player1view.viewID, player2view.viewID);

			} else {
				// Wait for master client to set up the game
			}
		}
	}*/
	


	/*private void SpawnPlayerObjects () {
		
		Transform spawnPoints = GameObject.Find("Spawn Points").transform;
		Vector3 p1spawn = spawnPoints.FindChild ("Player 1").position;
		Vector3 p2spawn = spawnPoints.FindChild ("Player 2").position;

		// Spawn players
		player1 = PhotonNetwork.InstantiateSceneObject ("player", p1spawn, Quaternion.identity, 0, null);
		player2 = PhotonNetwork.InstantiateSceneObject ("player", p2spawn, Quaternion.identity, 0, null);

		// Assign player numbers of the player controllers so that the players can be found and cached later
		player1.GetComponent<PlayerController> ().playerNumber = ControlHandler.Player.One;
		player2.GetComponent<PlayerController> ().playerNumber = ControlHandler.Player.Two;

		Debug.Log ("Player objects spawned.");

		controlHandler.photonView.RPC ("CachePlayers", PhotonTargets.All, player1.GetPhotonView ().viewID, player2.GetPhotonView ().viewID);
		// The above RPC method was renamed to CachePlayerObjects
	}*/


	
	[PunRPC]
	public void InitLocalPlayer (int player1viewid, int player2viewid) {
		PhotonView player1view = PhotonView.Find (player1viewid);
		PhotonView player2view = PhotonView.Find (player2viewid);

		GameObject camera = GameObject.Find ("Main Camera");

		if (PhotonNetwork.isMasterClient) {
			// Then this client is player 1
			Debug.Log ("InitLocalPlayer() called as master client...");
			controlHandler.localPlayerNumber = ControlHandler.Player.One;

			player1view.GetComponent<PlayerController> ().enabled = true;
			player1view.GetComponent<Rigidbody2D> ().isKinematic = false;
            //player1.tag = "Local Player";

			camera.transform.position = new Vector3 (player1view.gameObject.transform.position.x, player1view.gameObject.transform.position.y-4f, camera.transform.position.z);
			camera.GetComponent<CameraController> ().target = player1view.gameObject.transform;
			camera.GetComponent<CameraController> ().SetEffects (true);

			player2view.GetComponent<PlayerController> ().enabled = false;
			player2view.GetComponent<Rigidbody2D> ().isKinematic = true;
			player2view.GetComponent<BoxCollider2D> ().enabled = false;

		} else {
			// Then this client is player 2
			Debug.Log ("InitLocalPlayer() called as secondary client...");
			controlHandler.localPlayerNumber = ControlHandler.Player.Two;

			player2view.GetComponent<PlayerController> ().enabled = true;
			player2view.GetComponent<Rigidbody2D> ().isKinematic = false;
            //player2.tag = "Local Player";

            camera.transform.position = new Vector3 (player2view.gameObject.transform.position.x, player2view.gameObject.transform.position.y, camera.transform.position.z);
			camera.GetComponent<CameraController> ().target = player2view.gameObject.transform;
			camera.GetComponent<CameraController> ().SetEffects (true);

			player1view.GetComponent<PlayerController> ().enabled = false;
			player1view.GetComponent<Rigidbody2D> ().isKinematic = true;
			player1view.GetComponent<BoxCollider2D> ().enabled = false;
		}


		// Start the game!

		GameObject.Find ("Waiting Canvas").SetActive (false);

		controlHandler.inControlText = GameObject.Find ("In Control Text").GetComponent<Text> ();
		controlHandler.UnpauseControl ();
		controlHandler.SetControl (ControlHandler.Player.One);
	}


	public void JoinGame (RoomInfo roomInfo) {
		PhotonNetwork.LoadLevel (roomInfo.customProperties["level"] as string);
		PhotonNetwork.JoinRoom (roomInfo.name);
	}


	public void HostGame (string levelName) {
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
		PhotonNetwork.CreateRoom (SteamManager.steamID.ToString (), roomOptions, TypedLobby.Default);
		PhotonNetwork.LoadLevel (levelName);
		
	}

	public void ExitRoom() {
		PhotonNetwork.LeaveRoom ();
		SceneManager.LoadScene ("menu");
	}

}