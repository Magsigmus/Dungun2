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
        if (orgShooter)
        {
            Vector2 dir = new Vector2(orgShooter.transform.position.x - transform.position.x, orgShooter.transform.position.y - transform.position.y);
            float dist = dir.magnitude;
            //shooter exists and bullet has hit SOMETHING
            if (collision.gameObject.CompareTag("Player"))
            {
                collision.gameObject.GetComponent<PlayerBehaviour>().TakeDamage(damage);
                Destroy(this.gameObject);
            }
            else if (collision.gameObject == orgShooter || dist <= deletionRadius)  //rasj: if colliding with original enemy or is close enough to it
            {
                Destroy(this.gameObject);
            }

            transform.position += (Vector3)dir.normalized;  //rasj: get bullet out of the wall
            transform.up = dir;
            GetComponent<Rigidbody2D>().velocity = transform.up * startVelocity;
            return;
        }
        Destroy(this.gameObject);
    }


    public void OnSpawn(GameObject shooter)
    {
        orgShooter = shooter;
        //rasj: if useful, reference this from enemy/shooter directly after spawn
        return;
    }
}
