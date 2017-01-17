using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {

	public ControlHandler.Player playerNumber;

	public float moveSpeed = 5f;
	public float jumpForce = 2f;
	public Transform groundCheck;
	public float groundCheckRadius = 0.3f;
	public LayerMask whatIsGround;

	Rigidbody2D rbody;
	Animator anim;

	bool jumpKeyReleased;
	bool grounded = false;
	bool doubleJumped = false;
	float horizontalDragFactor = 0.75f;

	private NetworkPlayer networkPlayer;
	private ControlHandler controlHandler;
	public PhotonView photonView;
	private GameController gameController;

    public GameObject closestButton;
	

	void Start () {
		photonView = GetComponent<PhotonView> ();
		if (!photonView) {
			Debug.LogError ("No photon view found on this player!");
		}

		gameController = GameController.Instance;

		networkPlayer = GetComponent<NetworkPlayer> ();
		if (!networkPlayer) {
			Debug.LogWarning ("No network player script on this player.");
		}

		controlHandler = ControlHandler.Instance;

		rbody = GetComponent<Rigidbody2D> ();
		if (!rbody) {
			Debug.LogError ("No Rigidbody2D found on the player.");
		} else {
			//rbody.isKinematic = false;
		}

		anim = GetComponent<Animator> ();
		if (!anim) {
			Debug.LogError ("No Animator found on the player.");
		}

		groundCheck = transform.FindChild ("Ground Check");
		if (!groundCheck) {
			Debug.LogError ("No Ground Check found on this player.");
		}

		// Check layermask has been set on the prefab
		if (LayerMask.LayerToName(whatIsGround) != "Ground") {
			whatIsGround = LayerMask.GetMask("Ground");
		}
	}


	void FixedUpdate () {

		if (HasControl ()) {

			if (Input.GetKey (KeyCode.W) || Input.GetKey (KeyCode.Space)) {    //(Input.GetAxis ("Jump") != 0)
				if (jumpKeyReleased == true) {
					jumpKeyReleased = false;

					if (grounded) {
						rbody.velocity = new Vector2 (rbody.velocity.x, jumpForce);
						anim.SetTrigger ("jump");
					} else if (!doubleJumped) {
						doubleJumped = true;
						rbody.velocity = new Vector2 (rbody.velocity.x, jumpForce);
						anim.SetTrigger ("jump");
					}
				}
			} else if (jumpKeyReleased == false) {
				jumpKeyReleased = true;
			}


			if (Input.GetAxis ("Horizontal") > 0) {     //(Input.GetKey (KeyCode.D))
				transform.localScale = new Vector3 (1f, 1f, 1f);

				RaycastHit2D hitRight = Physics2D.Raycast (transform.position, Vector2.right, 1.05f, whatIsGround.value);
				Debug.DrawLine (transform.position, transform.position + (Vector3.right * 1.05f), Color.red);
				if (hitRight.collider != null) {
					// If we are going to walk into a wall then stop moving
					rbody.velocity = new Vector2 (0f, rbody.velocity.y);
				} else {
					// If not then we can move this way
					rbody.velocity = new Vector2 (moveSpeed, rbody.velocity.y);
				}
			}


			if (Input.GetAxis ("Horizontal") < 0) {     //(Input.GetKey (KeyCode.A))
				transform.localScale = new Vector3 (-1f, 1f, 1f);

				RaycastHit2D hitLeft = Physics2D.Raycast (transform.position, Vector2.left, 1.05f, whatIsGround.value);
				Debug.DrawLine (transform.position, transform.position + (Vector3.left * 1.05f), Color.red);
				if (hitLeft.collider != null) {
					// If we are going to walk into a wall then stop moving
					rbody.velocity = new Vector2 (0f, rbody.velocity.y);
				} else {
					// If not then we can move this way
					rbody.velocity = new Vector2 (-moveSpeed, rbody.velocity.y);
				}
			}

		}




		if (Input.GetAxis ("Horizontal") == 0) {
			rbody.velocity = new Vector2 (rbody.velocity.x * horizontalDragFactor, rbody.velocity.y); //replace the x velocity with a reducing function

			if (Mathf.Approximately (rbody.velocity.x, 0f)) {
				rbody.velocity = new Vector2 (0f, rbody.velocity.y);
			}
		}


		//Check if grounded
		grounded = Physics2D.OverlapCircle (groundCheck.position, groundCheckRadius, whatIsGround);
		if (grounded && rbody.velocity.y <= 0) {
			doubleJumped = false;
		}


		// Update animator variables
		anim.SetFloat ("xspeed", rbody.velocity.x);
		anim.SetFloat ("yspeed", rbody.velocity.y);
		anim.SetBool ("grounded", grounded);

	}


	void Update() {
		if (Input.GetKey (KeyCode.LeftControl) && Input.GetKey(KeyCode.Q)) {
			Application.Quit ();
		}

		if (Input.GetKeyDown(KeyCode.F)) {
			controlHandler.camera.GetComponent<CameraController> ().Shake ();
		}

		if (Input.GetKeyDown (KeyCode.Escape)) {
			
			if (controlHandler.isPaused) {
				controlHandler.photonView.RPC("UnpauseControl", PhotonTargets.All);
			} else {
				controlHandler.photonView.RPC ("PauseControl", PhotonTargets.All, true);
			}
		}

		if (HasControl ()) {
			if (Input.GetKeyDown (KeyCode.Q)) {
				controlHandler.TogglePlayerInControl ();
			}

			if (Input.GetKeyDown (KeyCode.E) && closestButton != null) {
				closestButton.GetComponentInParent<DoorObject> ().ActivateDoorSwitch (closestButton.GetComponent<PhotonView> ().viewID);
			}
		}

	}


	public bool HasControl () {
		if ((PhotonNetwork.isMasterClient && controlHandler.inControl == ControlHandler.Player.One)
			|| (!PhotonNetwork.isMasterClient && controlHandler.inControl == ControlHandler.Player.Two)) {
			return true;
		} else {
			return false;
		}
	}


    void OnTriggerEnter2D (Collider2D other) {
        if (other.tag == "Interactable") {
            closestButton = other.gameObject;
            return;
        }

        if (other.tag == "Dangerous") {
           

            if (playerNumber == ControlHandler.Player.Two) {
                Debug.Log ("PLAYER TWO DIED");
                transform.position = GameObject.Find ("Spawn Points").transform.FindChild ("Player 2").position;
            } else {
                Debug.Log ("PLAYER ONE DIED");
                transform.position = GameObject.Find ("Spawn Points").transform.FindChild ("Player 1").position;
            }

            return;
        }
    }

    void OnTriggerExit2D (Collider2D other) {
        if (closestButton == other.gameObject) {
            closestButton = null;
        }
    }
}
