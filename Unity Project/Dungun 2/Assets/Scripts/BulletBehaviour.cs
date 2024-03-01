using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BulletBehaviour : MonoBehaviour
{
    public float startVelocity = 1f;
    public float desctructionTime = 5f;
    private float durAlive = 0f;
    public GameObject ObjectCollide;

    void Start()
    {
        GetComponent<Rigidbody2D>().velocity = transform.up * startVelocity;
    }

    private void Update()
    {
        durAlive += Time.deltaTime;
        if (durAlive >= desctructionTime)
        {
            Destroy(this.gameObject);
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Destroy(this.gameObject);
    }

}
