using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BoomerangBulletBehaviourScript : MonoBehaviour, BulletInterface
{
    public float startVelocity = 10f;
    public float desctructionTime = 5f;
    public float deletionRadius = 3f;

    public int damage = 1;

    public GameObject orgShooter;

    //TODO: on collission spawn new bullet/rotate towards original shooter

    void Start()
    {
        GetComponent<Rigidbody2D>().velocity = transform.up * startVelocity;
        if (desctructionTime >= 0)  //rasj: if i.e. -1, then don't destroy after some time
        {
            Destroy(this.gameObject, desctructionTime);
        }
    }
    void Update()
    {
        /*
        if (Vector2.Distance(orgShooter.transform, transform.position))
        {

        }
        */
        //Vector2 dir = new Vector2(orgShooter.transform.position.x - transform.position.x, orgShooter.transform.position.y - transform.position.y);
        //Debug.DrawRay(transform.position, (Vector3)dir);
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Vector2 dir = new Vector2(orgShooter.transform.position.x - transform.position.x, orgShooter.transform.position.y - transform.position.y);
        bool hasPlayerTag = collision.gameObject.CompareTag("Player");

        if (hasPlayerTag)  //rasj: if hit player, make player take damage
        {
            collision.gameObject.GetComponent<PlayerBehaviour>().TakeDamage(damage);
        }

        if (collision.gameObject == orgShooter || hasPlayerTag)  //rasj: if hits original shooter
        {
            Destroy(this.gameObject);
        } else if (orgShooter)  //rasj: if hit and shooter still isn't dead
        {
            transform.position += (Vector3)dir.normalized;  //rasj: hopefully get the bullet out of the wall
            transform.up = dir;
            GetComponent<Rigidbody2D>().velocity = transform.up * startVelocity;
        }
        else  //rasj: in all other cases, die
        {
            Destroy(this.gameObject);
        }
    }


    public void OnSpawn(GameObject shooter)
    {
        orgShooter = shooter;
        //rasj: if useful, reference this from enemy/shooter directly after spawn
        return;
    }
}
