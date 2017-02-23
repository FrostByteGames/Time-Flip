using UnityEngine;
using System.Collections;
using UnityStandardAssets.ImageEffects;

public class CameraController : MonoBehaviour {

	public float dampTime = 0.15f;
	private Vector3 velocity = Vector3.zero;
	public Transform target;
	public Vector3 offset = new Vector3(0f, -4f, 0f);

	private VignetteAndChromaticAberration effect_vignette;
	private Twirl effect_twirl;
	private ColorCorrectionCurves effect_saturation;

	float shakeStartTime = 0;

	void Awake () {
		// Hook into the Control Handler to update the reference to the camera to this camera so that when...
		// ...the control handler issues a new player control, it can also make the camera look at that player.
		FindObjectOfType<ControlHandler> ().camera = GetComponent<Camera> ();
		FindObjectOfType<ControlHandler> ().cameraController = this;

		effect_vignette = GetComponent<VignetteAndChromaticAberration> ();
		if (!effect_vignette)
			Debug.LogWarning ("No Vignette and Chromatic Aberration effect found on the camera!");

		effect_twirl = GetComponent<Twirl> ();
		if (!effect_twirl)
			Debug.LogWarning ("No Twirl effect found on the camera!");

		effect_saturation = GetComponent<ColorCorrectionCurves> ();
		if (!effect_saturation)
			Debug.LogWarning ("No Saturation effect found on the camera!");

	}
	

	void FixedUpdate () {
		// Might need to cancel this if the camera is shaking? idk
		if (target)
		{
			Vector3 point = GetComponent<Camera>().WorldToViewportPoint(target.position);
			Vector3 delta = target.position - GetComponent<Camera>().ViewportToWorldPoint(new Vector3(0.5f, 0.5f, point.z)) + offset; //(new Vector3(0.5, 0.5, point.z));
			Vector3 destination = transform.position + delta;
			transform.position = Vector3.SmoothDamp(transform.position, destination, ref velocity, dampTime);
		}
		
	}


	void Update () {
		//slowly move twirl effect angle back and forth between 340 and 20 degrees (sine)
		if (effect_twirl) effect_twirl.angle = (Mathf.Sin (Time.time/7f) * 10f);
	}


	public void Shake (float seconds = 1f, float intensity = 0.5f, float rate = 1f) {
		shakeStartTime = Time.time;
		StopAllCoroutines ();
		StartCoroutine (ShakeCoroutine (seconds, intensity, rate));
	}


	private IEnumerator ShakeCoroutine (float seconds, float intensity, float rate) {
		float timeProgress = (Time.time - shakeStartTime)/seconds;

		// y = (x - 1)^2
		// where y = fadeFactor and x = timeProgress and 0 < x < 1
		float fadeFactor = Mathf.Pow((timeProgress - 1), 2f);
		
		// If current time + shakeStartTime < seconds ...
		if (timeProgress < 1) {
			Vector2 circle = Random.insideUnitCircle * fadeFactor * intensity;

			// Combine the random shake position with our current position
			Vector3 pos = transform.position;
			Vector3 shakePos = new Vector3 (pos.x + circle.x, pos.y + circle.y, pos.z);

			transform.position = shakePos;

			// Restart the coroutine after a delay as the shake time is not up yet
			yield return new WaitForSeconds (0.05f/rate);
			StartCoroutine (ShakeCoroutine (seconds, intensity, rate));
		} else {
			StopAllCoroutines ();
		}
	}

	public void SetEffects (bool enabled) {
		int multiplier = enabled ? 1 : 0;
		//effect_vignette.blurDistance = 5f * multiplier; // 0f if false, 5f if true..
		effect_vignette.chromaticAberration = 20f * multiplier; // 0f if false, 20f if true...

		effect_twirl.enabled = enabled;

		effect_saturation.enabled = enabled;
	}

}
