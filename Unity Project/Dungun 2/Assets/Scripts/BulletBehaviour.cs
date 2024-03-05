using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletBehaviour : MonoBehaviour
{
    public float startVelocity = 1;
    public GameObject ObjectCollide;

    void Start()
    {
        GetComponent<Rigidbody2D>().velocity = transform.up * startVelocity;
    }
}
