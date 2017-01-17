// IN YOUR EDITOR FOLDER, have SimpleEditorUtils.cs.
// paste in this text.
// to play, HIT COMMAND-ZERO rather than command-P
// (the zero key, is near the P key, so it's easy to remember)
// simply insert the actual name of your opening scene
// "__preEverythingScene" on the second last line of code below.

using UnityEditor;
using UnityEngine;
using System.Collections;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class SimpleEditorUtils {
	private static Canvas[] canvases;

	static SimpleEditorUtils () {
		canvases = Resources.FindObjectsOfTypeAll<Canvas> ();
	}

	[MenuItem ("Tools/Play From menu.unity %0")]
    public static void PlayFromPrelaunchScene () {
        if (EditorApplication.isPlaying == true) {
            EditorApplication.isPlaying = false;
            return;
        }

        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ();
        EditorSceneManager.OpenScene ("Assets/Scenes/menu.unity");
        EditorApplication.isPlaying = true;
    }


    [MenuItem ("Tools/Toggle between Demo Level and the Menu %PGDN")]
    [MenuItem ("Tools/Toggle between Demo Level and the Menu %PGUP")]
    public static void ToggleDemoLevelAndMenu() {

		EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ();

		if (EditorSceneManager.GetActiveScene().path == "Assets/Scenes/menu.unity") {
			EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ();
			EditorSceneManager.OpenScene ("Assets/Scenes/Demo Level.unity");
        } else {
			EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ();
			EditorSceneManager.OpenScene ("Assets/Scenes/menu.unity");
        }
    }


	[MenuItem ("Tools/Toggle Canvas Solo %&S")]
	[MenuItem ("GameObject/Toggle Canvas Solo", false, 0)]
	public static void SoloCanvas () {
		// If there is no object selected, return.
		if (!Selection.activeGameObject) {
			Debug.LogWarning ("No GameObject selected.");
			return;
		}

		Canvas selectedCanvas = Selection.activeGameObject.GetComponent<Canvas> ();

		// If the selected object has no canvas attached, return.
		if (!selectedCanvas) {
			Debug.LogWarning ("No Canvas found on this GameObject to solo.");
			return;
		}

		bool active = false;

		if (!selectedCanvas.gameObject.activeInHierarchy) {
			active = false;
		} else {
			int netActiveCount = 0;
			foreach (Canvas canvas in canvases) {
				if (canvas.gameObject.activeInHierarchy) {
					netActiveCount++;
				} else {
					netActiveCount--;
				}
			}

			if (netActiveCount <= 0)
				active = true;
		}
		
		// Set the canvases to active/inactive based on previous logic
		foreach (Canvas canvas in canvases) {
			if (canvas != selectedCanvas) {
				canvas.gameObject.SetActive (active);
			} else {
				canvas.gameObject.SetActive (true);
			}
		}
	}
}