using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LedgeTrigger : MonoBehaviour
{
    public Vector3 LedgeDir = Vector3.forward;

    void OnTriggerEnter(Collider collider)
    {
        if(collider.tag == "Player")
        {
            //Get the the camera and add the direction for this ledge
            Player player = collider.GetComponent<Player>();

            if (player != null)
            {
                player.Controller.AddLedgeDir(LedgeDir);
            }
        }
    }

    void OnTriggerExit(Collider collider)
    {
        if (collider.tag == "Player")
        {
            Player player = collider.GetComponent<Player>();

            //Get the the camera and remove the direction for this ledge
            if (player != null)
            {
                player.Controller.AddLedgeDir(-LedgeDir);
            }
        }
    }
}
