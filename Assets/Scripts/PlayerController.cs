using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {

	public ControlHandler.Player playerNumber;

	public float moveSpeed = 4f;
	public float defaultPushSpeed = 1f;
	private float pushSpeed;
	public float jumpForce = 3f;
	public bool canDoubleJump = false;
	public float raycastDistance = 1.1f;

	private Transform groundCheck;
	private float groundCheckRadius = 0.3f;
	private LayerMask whatIsGround;
	private Vector3 raycastOffset = new Vector3 (0f, -0.2f);

	Rigidbody2D rbody;
	Animator anim;

	bool jumpKeyReleased;
	bool grounded = false;
	bool doubleJumped = false;
	[Tooltip ("Lower values mean more drag (speed is slowed at a faster rate). Valid values are between 0 and 1.")]
	float horizontalDragFactor = 0.75f;
	RaycastHit2D hitLeft;
	RaycastHit2D hitRight;

	private NetworkPlayer networkPlayer;
	private ControlHandler controlHandler;
	private PhotonView photonView;
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

		pushSpeed = defaultPushSpeed;
	}


	void FixedUpdate () {

		if (HasControl ()) {

			// JUMPING CODE

			if (Input.GetKey (KeyCode.W) || Input.GetKey (KeyCode.Space)) {    //(Input.GetAxis ("Jump") != 0)
				if (jumpKeyReleased == true) {
					jumpKeyReleased = false;

					if (grounded) {
						rbody.velocity = new Vector2 (rbody.velocity.x, jumpForce);
						anim.SetTrigger ("jump");
					} else if (!doubleJumped && canDoubleJump) {
						doubleJumped = true;
						rbody.velocity = new Vector2 (rbody.velocity.x, jumpForce);
						anim.SetTrigger ("jump");
					}
				}
			} else if (jumpKeyReleased == false) {
				jumpKeyReleased = true;
			}
			


			// MOVEMENT & PUSHING CODE

			if (Input.GetAxis ("Horizontal") > 0) {     //(Input.GetKey (KeyCode.D))
				transform.localScale = new Vector3 (1f, 1f, 1f);

				hitRight = Physics2D.Raycast (transform.position + raycastOffset, Vector2.right, raycastDistance, whatIsGround.value);
				Debug.DrawLine (transform.position + raycastOffset, transform.position + raycastOffset + (Vector3.right * raycastDistance), Color.red);
				if (hitRight && hitRight.collider != null) {
					if (hitRight.transform.tag == "Pushable") {
						pushSpeed = hitRight.transform.GetComponent<PushableObject> ().pushSpeed;		// Use the object's specific push speed
						rbody.velocity = new Vector2 (pushSpeed, rbody.velocity.y);
						hitRight.transform.GetComponent<PushableObject> ().PushRight ();                 // Move the pushable object
						//hitRight.transform.GetComponent<Rigidbody2D> ().velocity = new Vector2 (pushSpeed, hitRight.transform.GetComponent<Rigidbody2D> ().velocity.y);
						anim.SetBool ("pushing", true);
					} else {
						// We are going to walk into a wall, so stop moving
						rbody.velocity = new Vector2 (0f, rbody.velocity.y);
					}
				} else {
					// If not then we can move this way
					rbody.velocity = new Vector2 (moveSpeed, rbody.velocity.y);
				}
			}

			if (Input.GetAxis ("Horizontal") < 0) {     //(Input.GetKey (KeyCode.A))
				transform.localScale = new Vector3 (-1f, 1f, 1f);

				hitLeft = Physics2D.Raycast (transform.position + raycastOffset, Vector2.left, raycastDistance, whatIsGround.value);
				Debug.DrawLine (transform.position + raycastOffset, transform.position + raycastOffset + (Vector3.left * raycastDistance), Color.red);
				if (hitLeft && hitLeft.transform != null) {
					if (hitLeft.transform.tag == "Pushable") {
						pushSpeed = hitLeft.transform.GetComponent<PushableObject> ().pushSpeed;		// Use the object's specific push speed
						rbody.velocity = new Vector2 (-pushSpeed, rbody.velocity.y);					// Move this player object
						hitLeft.transform.GetComponent<PushableObject> ().PushLeft ();					// Move the pushable object
						anim.SetBool ("pushing", true);													// Animate!
					} else {
						// We are going to walk into a wall, so stop moving
						rbody.velocity = new Vector2 (0f, rbody.velocity.y);
					}
				} else {
					// If not then we can move this way
					rbody.velocity = new Vector2 (-moveSpeed, rbody.velocity.y);
				}
			}

		}




		if (Input.GetAxis ("Horizontal") == 0) {
			anim.SetBool ("pushing", false);

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
				controlHandler.UnpauseControl ();
			} else {
				controlHandler.PauseControl (true);
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
		//if (gameController.singleplayer == true) {
			if (controlHandler.inControl == playerNumber) {
				return true;
			} else {
				return false;
			}
		/*} else if (gameController.singleplayer == false) {
			if ((PhotonNetwork.isMasterClient && controlHandler.inControl == ControlHandler.Player.One)
				|| (!PhotonNetwork.isMasterClient && controlHandler.inControl == ControlHandler.Player.Two)) {
				return true;
			} else {
				return false;
			}
		} else {
			Debug.LogError ("GameController 'singleplayer' was not set - cannot determine if this player HasControl()...");
			return false;
		}*/
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
