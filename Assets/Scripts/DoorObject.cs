using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon;

public class DoorObject : PunBehaviour {

    [SerializeField]
    private List<GameObject> buttons;
    private Animator doorAnimator;

    [SerializeField]
    private bool doorOpen = false;

    // Use this for initialization
    void Start () {
        if (transform.childCount == 0) {
            Debug.LogError ("No buttons attached to this door! There's no way to make it open...");
        } else {
            foreach (Transform child in transform) {
                if (child.GetComponent<Collider2D> ().isTrigger) {
                    buttons.Add (child.gameObject);
                    Debug.Log (child.name);
                } else {
                    Debug.LogWarning (child.name + " does not have a trigger Collider2D attached, so it will be ignored.");
                }
            }

            /* foreach (GameObject button in buttons) {
                // do something?
            } */
        }
	    
        doorAnimator = GetComponent<Animator>();
        if (!doorAnimator) {
            Debug.LogError ("No animator attached to this door - door will not open correctly without one!");
            return;
        }


        if (doorOpen) {
            OpenDoor ();
        } else {
            CloseDoor ();
        }

    }

    [PunRPC]
    public void OpenDoor (int buttonViewID = 0) {
        Debug.Log ("Opening door!");
        doorAnimator.SetBool ("open", true);
        doorOpen = true;

        if (buttonViewID != 0) {
            // Animate this button to press
        }
    }

    [PunRPC]
    public void CloseDoor (int buttonViewID = 0) {
        Debug.Log ("Closing door!");
        doorAnimator.SetBool ("open", false);
        doorOpen = false;

        if (buttonViewID != 0) {
            // Animate this button to press
        }
    }


    ///<summary>
    ///<para>DO NOT USE THIS METHOD DIRECTLY.</para>
    ///<para>If you need to activate this door, please use ActivateDoorSwitch() instead.</para>
    ///</summary>
    [PunRPC]
    public void ToggleDoorRPC (int buttonViewID = 0) {
        if (doorOpen) {
            CloseDoor (buttonViewID);
        } else {
            OpenDoor (buttonViewID);
        }
    }


    ///<summary>
    ///<para>This will call the ToggleDoorRPC on all clients.</para>
    ///</summary>
    public void ActivateDoorSwitch (int buttonViewID = 0, string mode = "toggle") {
        switch (mode) {
            default:
            case "toggle":
                PhotonNetwork.RPC (photonView, "ToggleDoorRPC", PhotonTargets.AllBuffered, false, buttonViewID);
                break;

            case "open":
                PhotonNetwork.RPC (photonView, "OpenDoor", PhotonTargets.AllBuffered, false, buttonViewID);
                break;

            case "close":
                PhotonNetwork.RPC (photonView, "CloseDoor", PhotonTargets.AllBuffered, false, buttonViewID);
                break;
        }
    }
        
    
    
}
