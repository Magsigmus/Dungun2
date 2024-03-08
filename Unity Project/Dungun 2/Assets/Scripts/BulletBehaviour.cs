using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BulletBehaviour : MonoBehaviour
{
    public float startVelocity = 1f;
    public float desctructionTime = 5f;
    private float bulletLifeDuration = 0f;

    void Start()
    {
        GetComponent<Rigidbody2D>().velocity = transform.up * startVelocity;
        Destroy(this.gameObject, desctructionTime);
    }

    private void Update()
    {
        bulletLifeDuration += Time.deltaTime;
        if (bulletLifeDuration >= desctructionTime)
        {
            Destroy(this.gameObject);
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Destroy(this.gameObject);
    }
}
