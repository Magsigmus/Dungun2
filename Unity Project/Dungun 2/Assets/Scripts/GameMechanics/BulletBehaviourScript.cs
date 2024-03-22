using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletBehaviourScript : MonoBehaviour
{
    public float startVelocity = 1f;
    public float desctructionTime = 5f;

    void Start()
    {
        GetComponent<Rigidbody2D>().velocity = transform.up * startVelocity;
        Destroy(this.gameObject, desctructionTime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Destroy(this.gameObject);
    }
}
