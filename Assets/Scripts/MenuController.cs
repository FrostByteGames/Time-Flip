using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
//using Photon;

public class MenuController : MonoBehaviour {

	private PhotonNetworkHandler photonNetworkHandler;
	private GameController gc;

	public Canvas canvasOverlay { get; private set; }
	public Canvas canvasMain { get; private set; }
	public Canvas canvasJoinGame { get; private set; }
	public Canvas canvasHostGame { get; private set; }
	public Canvas canvasSinglePlayer { get; private set; }
	public Canvas canvasMultiplayerLobby { get; private set; }

	public Canvas currentMenu { get; private set; } 
	private Canvas previousMenu;

	private GameObject scrollView;
	private Transform scrollViewContent;
	private Dropdown levelSelectDropdown;

	public Texture localPlayerAvatar;
	public Texture otherPlayerAvatar;

	public Text lobby_headerText;
	public Text lobby_playersSubheaderText;
	public Text lobby_player1NameText;
	public Text lobby_player2NameText;
	public RawImage lobby_player1ProfileImage;
	public RawImage lobby_player2ProfileImage;
	public GameObject lobby_beginGameButton;
	public GameObject lobby_beginGameReplacementObject;

	private GraphicRaycaster gRaycaster;
	private GameObject selectedHostEntry;
	private Color unselectedColor;
	private Color selectedColor;

	private float lastClickTime = 0f;

	// Object pooling variables
	private List<GameObject> hostListPool;
	private Dictionary<GameObject, RoomInfo> hostListDictionary;
	private float lastRefreshTime = 0f;
	
	

	void Start () {
		photonNetworkHandler = PhotonNetworkHandler.Instance;
		if (!photonNetworkHandler) {
			Debug.LogError ("No Photon Network Handler could be found in the scene!");
		}

		// Find and cache Canvas menus
		canvasOverlay = GameObject.Find ("Overlay Canvas").GetComponent<Canvas> ();
		canvasMain = GameObject.Find ("Main Canvas").GetComponent<Canvas> ();
		canvasJoinGame = GameObject.Find ("Join Game Canvas").GetComponent<Canvas> ();
		canvasHostGame = GameObject.Find ("Host Game Canvas").GetComponent<Canvas> ();
		canvasMultiplayerLobby = GameObject.Find ("Multiplayer Lobby Canvas").GetComponent<Canvas> ();
		
		DontDestroyOnLoad (canvasOverlay.gameObject);
		

		scrollView = GameObject.Find ("Host List Scroll View");
		if (!scrollView) {
			Debug.LogError ("No scroll view could be found!");
		}
		
		scrollViewContent = GameObject.Find ("Scroll View Content").transform;
		if (!scrollViewContent) {
			Debug.LogError ("No content transform could be found on the scroll view!");
		}

		levelSelectDropdown = FindObjectOfType<Dropdown> ();
		if (!levelSelectDropdown) {
			Debug.LogWarning ("No level select dropdown found in the scene.");
		}

		gRaycaster = GameObject.Find ("Join Game Canvas").GetComponent<GraphicRaycaster> ();
		if (!gRaycaster) {
			Debug.LogError ("No raycaster on an object named 'Join Game Canvas' could be found!");
		}

		if (canvasMultiplayerLobby) {
			lobby_headerText = canvasMultiplayerLobby.transform.FindChild ("Header Text").GetComponent<Text> ();
			lobby_playersSubheaderText = canvasMultiplayerLobby.transform.FindChild("Players Subheader Text").GetComponent<Text> ();
			lobby_player1NameText = canvasMultiplayerLobby.transform.FindChild("Player 1 Name Text").GetComponent<Text> ();
			lobby_player2NameText = canvasMultiplayerLobby.transform.FindChild ("Player 2 Name Text").GetComponent<Text> ();
			lobby_player1ProfileImage = canvasMultiplayerLobby.transform.FindChild ("Player 1 Profile Image").GetComponent<RawImage> ();
			lobby_player2ProfileImage = canvasMultiplayerLobby.transform.FindChild ("Player 2 Profile Image").GetComponent<RawImage> ();
			lobby_beginGameButton = canvasMultiplayerLobby.transform.FindChild ("Begin Game Button").gameObject;
			lobby_beginGameReplacementObject = canvasMultiplayerLobby.transform.FindChild ("Begin Game Replacement Object").gameObject;
		}
		

		if (hostListPool == null) {
			hostListPool = new List<GameObject> ();
			hostListDictionary = new Dictionary<GameObject, RoomInfo> ();
			unselectedColor = new Color (1f, 1f, 1f, 0f);		// White with alpha = 0%
			selectedColor = new Color (1f, 1f, 1f, 0.45f);		// White with alpha = 45%
		}


		// Go to main menu on startup
		GoToMenu (canvasMain);


	}


	public void Update () {
		
		if (Input.GetKeyDown (KeyCode.Mouse0)) {

			//Begin raycast to see if we hit a host list entry
			List<RaycastResult> raycastResults = new List<RaycastResult> ();
			PointerEventData ped = new PointerEventData (null);
			ped.position = Input.mousePosition;
			gRaycaster.Raycast (ped, raycastResults);

			// Cycle through raycast results to find if we clicked a host list entry
			foreach (RaycastResult result in raycastResults) {

				if (result.gameObject.name == "Host List Entry") {

					if (result.gameObject == selectedHostEntry) {       // If the currently clicked host list entry is the one already selected...
						if ((Time.time - lastClickTime) < 0.28f) {       // If less that 0.28s has passed this is a double click (this number was picked just because it felt right...)
							Debug.Log ("DOUBLE CLICK REGISTERED");
							JoinMultiPlayerGame ();
							break;      // Break out of this foreach loop as we're leaving the menu now
						}
						lastClickTime = Time.time;      // Reset the last click time to enable us to see when the user double-clicks
					} else {
						// Unselect the currently selected host entry...
						if (selectedHostEntry != null) {
							selectedHostEntry.GetComponent<Image> ().color = unselectedColor;
						}
						// ...and select the new one!
						selectedHostEntry = result.gameObject;
						selectedHostEntry.GetComponent<Image> ().color = selectedColor;
						lastClickTime = Time.time;      // Reset the last click time to enable us to see when the user double-clicks
					}
				}
			}
		}


		/*if (Input.GetKeyDown(KeyCode.Escape)) {		// BUG: THIS WILL FLICKER BETWEEN TWO MENUS - NEED TO STORE MENU JOURNEY IN A LIST :/
			if (previousMenu != null) {
				GoToMenu (previousMenu);
			}
		}*/


		if (gc && gc.inLobby == true) {
			
		}

	}



	public void QuitGame () {
		Application.Quit ();
	}


	public void GoToMenu (Canvas menu) {
		if (menu.tag != "Menu Canvas") {
			Debug.LogWarning ("'" + menu.gameObject.name + "' is not a valid menu.");
			return;
		}

		// Update the tracking variables
		previousMenu = currentMenu;
		currentMenu = menu;

		// Hide all canvas
		Canvas[] allCanvases = GameObject.FindObjectsOfType<Canvas> ();
		foreach (Canvas canvas in allCanvases) {
			if (canvas.tag == "Menu Canvas") {
				canvas.gameObject.SetActive (false);
			}
		}
		// Except the menu we're navigating to and the overlay canvas
		menu.gameObject.SetActive (true);
		canvasOverlay.gameObject.SetActive (true);


		// If we're going back to the main menu then reset everything
		if (menu == canvasMain) {
			selectedHostEntry = null;
			lobby_player1ProfileImage.texture = new Texture ();
			lobby_player2ProfileImage.texture = new Texture ();
		}

		if (menu == canvasJoinGame) {
			RefreshHostList ();
		}
	}


	public void RefreshHostList () {
		Debug.Log ("Refreshing host list...");
		//Only allow refreshing once a second
		if (Time.time > (lastRefreshTime + 1f)) {		// Force a 1s delay between refreshes to prevent spam
			
			hostListDictionary.Clear ();

			GameObject[] hostListObjects = GameObject.FindGameObjectsWithTag ("HostListEntry");
			foreach (GameObject hostListObject in hostListObjects) {
				Debug.Log ("   > Host list entry was added to the pool");
				AddToHostListPool (hostListObject);
			}

			
			RoomInfo[] hostListInfo = PhotonNetwork.GetRoomList ();
			Debug.Log ("host list length: " + hostListInfo.Length);

			for (int i = 0; i < hostListInfo.Length; i++) {
				Debug.Log (hostListInfo[i].name.ToString());
				GameObject entry = GetNextAvailableHostListEntry ();

				hostListDictionary.Add (entry, hostListInfo[i]);

				entry.transform.SetParent (scrollViewContent, false);
				entry.transform.localPosition = new Vector3 (920f, -30f - (50f * i), 0f);
				entry.transform.FindChild ("Room Name").GetComponent<Text> ().text = hostListInfo[i].customProperties["hoststeamname"] as string;
				entry.transform.FindChild ("Level Name").GetComponent<Text> ().text = hostListInfo[i].customProperties["level"] as string;
			}
		}
	}


	private GameObject GetNextAvailableHostListEntry () {
		GameObject entry = null;

		if (hostListPool.Count > 0) {		// If there is an entry in the hostListPool
			entry = hostListPool[0];		// then take this entry,
			hostListPool.RemoveAt(0);       // remove it from the array
			entry.SetActive (true);			// and activate it.

		} else {                            // If not then instantiate a new one to work with
			entry = Instantiate (Resources.Load ("Host List Entry")) as GameObject;
			entry.tag = "HostListEntry";
			entry.transform.SetParent(scrollViewContent, false);
			entry.name = entry.name.Replace ("(Clone)", "");
			entry.transform.localPosition = new Vector3 (920f, -30f, 0f); // Default position (top line in box)
		}
		
		
		return entry;
	}


	private void AddToHostListPool (GameObject entry) {
		//If this is the currently selected entry, unselect it before pooling
		if (selectedHostEntry == entry) {
			selectedHostEntry.GetComponent<Image> ().color = unselectedColor;
			selectedHostEntry = null;
		}
		
		hostListPool.Add (entry);
		entry.SetActive (false);
	}


	/*public void JoinGame () {
		if (selectedHostEntry) {
			RoomInfo roomInfo = null;
			hostListDictionary.TryGetValue (selectedHostEntry, out roomInfo);
			if (roomInfo != null) {
				photonNetworkHandler.JoinGame (roomInfo);
			} else {
				Debug.Log ("Could not join room as could not obtain host info from the currently selected host entry.");
				return;
			}
		} else {
			Debug.Log ("No host selected... cannot join a game that doesn't exist!");
		}
	}*/


	public void HostGame () {
		//photonNetworkHandler.HostGame(levelSelectDropdown.gameObject.transform.FindChild("Label").GetComponent<Text> ().text);
		photonNetworkHandler.HostGame ("Demo Level");
	}













	/******* BUTTON FUNCTIONS *******/

	public void Button_SinglePlayer () {
		SetupGameWithSettings ("Demo Level", true);
	}


	public void Button_HostMultiPlayer () {
		SetupGameWithSettings ("Demo Level", false);
	}


	public void Button_BeginMultiplayerGame () {
		if (PhotonNetwork.playerList.Length == 2) {
			Debug.Log ("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
			Debug.Log ("STARTING GAME ...");
			
			gc.photonView.RPC ("UpdatePlayerLoadStatus", PhotonTargets.All, 1, (int)GameController.LoadStatus.AWAITING_LOAD);
			gc.photonView.RPC ("UpdatePlayerLoadStatus", PhotonTargets.All, 2, (int)GameController.LoadStatus.AWAITING_LOAD);

			Debug.Log ("	Loading level...");

			if (SceneManager.GetSceneByName (gc.levelName) != null) {
				// The level exists, so start loading...!
				PhotonNetwork.LoadLevel (gc.levelName);

			} else {
				Debug.LogError ("The Scene '" + gc.levelName + "' could not be found. Cannot load a level that does not exist!");
				return;
			}

			
		} else if (PhotonNetwork.playerList.Length == 1) {
			Debug.LogWarning ("Not enough players in the lobby to start the game yet!");
		} else {
			Debug.LogError ("ERROR: Player list length is an unexpected number: " + PhotonNetwork.playerList.Length);
		}
	}


	public void Button_LeaveMultiplayerLobby () {
		PhotonNetwork.LeaveRoom ();
		GoToMenu (canvasMain);
	}







	public void JoinMultiPlayerGame () {
		if (selectedHostEntry) {
			RoomInfo roomInfo = null;
			hostListDictionary.TryGetValue (selectedHostEntry, out roomInfo);
			if (roomInfo != null) {
				GameObject gcGameObject = new GameObject ("Game Controller");
				gc = gcGameObject.AddComponent<GameController> ();
					gc.levelName = roomInfo.customProperties["level"] as string;
					gc.singleplayer = false;
					gc.isHost = false;
					gc.localPlayerNumber = 2;
				DontDestroyOnLoad (gc);
				PhotonNetwork.JoinRoom (roomInfo.name);			// This will lead to OnJoinedRoom in the PhotonNetworkHandler
				//photonNetworkHandler.JoinGame (roomInfo);
			} else {
				Debug.Log ("Could not join room as could not obtain host info from the currently selected host entry.");
				return;
			}
		} else {
			Debug.Log ("No host selected... cannot join a game that doesn't exist!");
		}
	}



	public void SetupGameWithSettings (string levelname, bool singleplayer = true) {

		// First of all check that the level string is even a real level...
		if (SceneManager.GetSceneByName(levelname) == null) {
			Debug.LogError ("FAILED TO LOAD LEVEL: Could not find a scene named '" + levelname + "'");
			return;
		}


		// Create a new unique Game Controller and set up the settings
		if (GameController.Instance != null) GameObject.DestroyImmediate(GameController.Instance.gameObject);
		GameObject gcGameObject = new GameObject ("Game Controller");
		gc = gcGameObject.AddComponent<GameController> ();
			gc.levelName = levelname;
			gc.singleplayer = singleplayer;
			gc.isHost = true;
			gc.localPlayerNumber = 1;
		DontDestroyOnLoad (gc);

		
		if (singleplayer) {
			// Begin the level load
			gc.loadStatusP1 = GameController.LoadStatus.AWAITING_LOAD;
			gc.loadStatusP2 = GameController.LoadStatus.AWAITING_LOAD;
			SceneManager.LoadScene (levelname);


		} else if (!singleplayer) {
			// Create the Photon room and set our load status to AWAITING_LOAD
			gc.CreatePhotonRoom ();			// This will set up the Photon room and put us in the lobby menu to wait until the other player connects
		}

		// The newly created Game Controller will now wait until (the game is started from the lobby for multiplayer, then 
		// wait until...) level is loaded and then begin the setup of the game.

	}



	public void UpdatePlayerNames (string p1name = null, string p2name = null) {
		if (!string.IsNullOrEmpty(p1name)) {
			gc.player1Name = p1name;
			lobby_player1NameText.text = p1name;
		}

		if (!string.IsNullOrEmpty (p2name)) {
			gc.player2Name = p2name;
			lobby_player2NameText.text = p2name;
			lobby_player2ProfileImage.texture = SteamManager.GetSmallAvatar ("123");
		}
		
		//GameObject.Find ("Player 1 Name Text").GetComponent<Text> ().text = player1Name;
		//GameObject.Find ("Player 1 Name Text").GetComponent<Text> ().text = player1Name;
		
	}

	public void UpdatePlayerAvatars () {
		if (gc.localPlayerNumber == 1) {
			lobby_player1ProfileImage.texture = localPlayerAvatar;
			lobby_player2ProfileImage.texture = otherPlayerAvatar;
		} else if (gc.localPlayerNumber == 2) {
			lobby_player1ProfileImage.texture = otherPlayerAvatar;
			lobby_player2ProfileImage.texture = localPlayerAvatar;
		} else {
			Debug.LogError ("Player number '" + gc.localPlayerNumber + "' is not a valid player number!");
		}
	}


	



}
