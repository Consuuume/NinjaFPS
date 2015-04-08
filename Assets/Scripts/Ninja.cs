using UnityEngine;
using System.Collections;

public class Ninja : MonoBehaviour
{

    public float RespawnTime = 2.0f;
    public Vector2 initialPosition;

	// Use this for initialization
	void Start ()
	{
	    initialPosition = new Vector2(transform.position.x, transform.position.y);
	}

    public void Respawn()
    {
        transform.position = initialPosition;
    }
}
