using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class BulletBehaviourScript : MonoBehaviour, BulletInterface
{
    public float startVelocity = 1f;
    public float desctructionTime = 5f;
    public int damage = 1;

    public float gravitationalForce = -5f;
    public float maxGravitationalDistance = 5f;

    public GameObject boss;

    private float dist;
    private Vector2 relativeDir;

    void Start()
    {
        boss = GameObject.Find("Boss");
        GetComponent<Rigidbody2D>().velocity = transform.up * startVelocity;
        if (desctructionTime >= 0)  //rasj: if i.e. -1, then don't destroy after some time
        {  
            Destroy(this.gameObject, desctructionTime);
        }
    }

    private void FixedUpdate()
    {
        if (boss)
        {
            dist = Vector3.Distance(boss.transform.position, transform.position);
            if (dist < maxGravitationalDistance)
            {
                relativeDir = (Vector2)(boss.transform.position - transform.position).normalized;
                GetComponent<Rigidbody2D>().AddForce(relativeDir * gravitationalForce);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
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
