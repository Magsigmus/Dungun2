using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoomerangBulletBehaviourScript : MonoBehaviour, BulletInterface
{
    public float startVelocity = 10f;
    public float desctructionTime = 5f;

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
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == orgShooter)  //rasj: if hits original shooter
        {
            Destroy(this.gameObject);
        }
        Vector2 dir = new Vector2(orgShooter.transform.position.x - transform.position.x, orgShooter.transform.position.y - transform.position.y);
        transform.up = dir;
    }


    public void OnSpawn(GameObject shooter)
    {
        orgShooter = shooter;
        //rasj: if useful, reference this from enemy/shooter directly after spawn
        return;
    }
}
