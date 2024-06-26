using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class HomingBulletBehaviourScript : MonoBehaviour, BulletInterface
{
    public float startVelocity = 1f;
    public float desctructionTime = 5f;

    public float gravitationalForce = 5f;
    public float maxGravitationalDistance = 5f;

    public int damage = 1;

    private GameObject target;

    private float dist;
    private Vector2 relativeDir;
    private Rigidbody2D rb;
    private GameObject spriteGameobject;

    void Start()
    {
        target = GameObject.FindWithTag("Player");

        spriteGameobject = transform.GetChild(0).gameObject;
        rb = GetComponent<Rigidbody2D>();

        rb.velocity = transform.up * startVelocity;
        if (desctructionTime >= 0)  //rasj: if i.e. -1, then don't destroy after some time
        {  
            Destroy(this.gameObject, desctructionTime);
        }
    }

    private void FixedUpdate()
    {
        dist = Vector3.Distance(target.transform.position, transform.position);
        if (dist < maxGravitationalDistance)
        {
            relativeDir = (Vector2)(target.transform.position - transform.position).normalized;
            rb.AddForce(relativeDir * gravitationalForce);
        }

        spriteGameobject.transform.up = rb.velocity;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            collision.gameObject.GetComponent<PlayerBehaviour>().TakeDamage(damage);
        }
        Destroy(this.gameObject);
    }


    public void OnSpawn(GameObject shooter)
    {
        //rasj: if useful, reference this from enemy/shooter directly after spawn
        return;
    }
}
