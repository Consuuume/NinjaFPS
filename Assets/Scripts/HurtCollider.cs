using UnityEngine;
using System.Collections;

public class HurtCollider : MonoBehaviour
{
    
    // Use this for initialization
	void Start ()
	{
	}

    private void OnTriggerEnter(Collider other)
    {
        if (other.name == "HitCollider")
        {
            transform.parent.GetComponent<Ninja>().Respawn();
        }
    }
}
