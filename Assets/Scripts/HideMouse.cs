using UnityEngine;

public class HideMouse : MonoBehaviour {

	// Use this for initialization
	void Start ()
	{
	    Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
	}
	
	// Update is called once per frame
	void Update () {
	    if (Input.GetKeyDown(KeyCode.Escape))
	    {
            Application.Quit();
	    }
	}
}
