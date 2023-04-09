using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float MaxLifeTime = 3.0f;
    public float Speed = 20.0f;

    public void Init(Vector3 position, Vector3 direction)
    {
        transform.position = position;
        transform.rotation = Quaternion.identity;
        m_Velocity = direction * Speed;
        m_TimeLeftTillDestroy = MaxLifeTime;
        Rigidbody rigidBody = GetComponent<Rigidbody>();
        if(rigidBody)
        {
            rigidBody.velocity = new Vector3(0f, 0f, 0f);
            rigidBody.angularVelocity = new Vector3(0f, 0f, 0f);
        }
    }

	// Update is called once per frame
	void Update ()
    {
        //Update motion
       transform.position += m_Velocity * Time.deltaTime;

        m_TimeLeftTillDestroy -= Time.deltaTime;

        if(m_TimeLeftTillDestroy <= 0.0f)
        {
            gameObject.SetActive(false);
        }
	}

    void OnCollisionEnter(Collision collision)
    {
       if(collision.gameObject != null)
        {
            this.gameObject.SetActive(false);
        }
    }

    float m_TimeLeftTillDestroy;
    Vector3 m_Velocity;
}
