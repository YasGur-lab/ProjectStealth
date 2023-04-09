using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameWonTrigger : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider coll)
    {
        if (coll.gameObject.tag == "Player")
        {
            Camera.main.gameObject.GetComponent<ThirdPersonCamera>().m_GGText.SetActive(true);
            coll.GetComponent<Player>().ResetPlayer();
        }
    }
}
