using UnityEngine;
using System.Collections;

public class TriggerScreenShake : MonoBehaviour {

	private CameraController cameraController;

	// Use this for initialization
	void Awake () {
		cameraController = FindObjectOfType<CameraController> ();
		if (!cameraController) {
			Debug.LogWarning ("No Camera Controller script found in the scene! Screen shake will not work for this object...");
		}
	}


	public void Shake_Small () {
		cameraController.Shake (0.5f, 0.1f, 1.2f);
	}

	public void Shake_Medium () {
		cameraController.Shake (0.7f, 0.2f, 1.1f);
	}

	public void Shake_Large () {
		cameraController.Shake (1f, 0.5f, 1f);
	}

	public void Shake_ExtraLarge () {
		cameraController.Shake (1.1f,1.2f, 1f);
	}

	public void Shake_Huge () {
		cameraController.Shake (1.5f, 1.5f, 0.8f);
	}
}
