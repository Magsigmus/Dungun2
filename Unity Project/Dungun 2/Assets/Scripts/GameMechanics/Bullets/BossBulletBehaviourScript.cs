using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;

public class BossBulletBehaviourScript : MonoBehaviour, BulletInterface
{
    public float startVelocity = 1f;
    public float desctructionTime = 5f;

    public float gravitationalForce = 5f;
    public float maxGravitationalDistance = 5f;

    public GameObject enemyPrefab;
    public GameObject homingEnemyPrefab;
    public GameObject boomerangEnemyPrefab;

    public int damage = 1;

    public int maxRandIntForSpawn = 12;
    public int maxEnemies = 16;

    private GameObject target;

    private float dist;
    private Vector2 relativeDir;

    void Start()
    {
        target = GameObject.FindWithTag("Player");

        GetComponent<Rigidbody2D>().velocity = transform.up * startVelocity;
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
            GetComponent<Rigidbody2D>().AddForce(relativeDir * gravitationalForce);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Vector2 vel = gameObject.GetComponent<Rigidbody2D>().velocity;
        if (collision.gameObject.tag == "Player")
        {
            collision.gameObject.GetComponent<PlayerBehaviour>().TakeDamage(damage);
        } else
        {
            GameObject newEnemy;
            int randInt = (int)Random.Range(0, maxRandIntForSpawn);  //rasj: 3 slots where no enemies spawn
            switch (randInt)
            {
                case 0:
                    if(GameObject.FindGameObjectsWithTag("Enemy").Length > maxEnemies) { break; }

                    
                    newEnemy = Instantiate(enemyPrefab);
                    //Sig: collision.GetContact(0).normal is the normal vector to the collider collided with.
                    newEnemy.transform.position = transform.position + (Vector3)collision.GetContact(0).normal * -2;
                    newEnemy.GetComponent<NavMeshAgent>().enabled = true;
                    break;
                /*case 1:
                    newEnemy = Instantiate(homingEnemyPrefab);
                    newEnemy.transform.position = transform.position + ((Vector3)vel.normalized * -2);
                    break;
                case 2:
                    newEnemy = Instantiate(boomerangEnemyPrefab);
                    newEnemy.transform.position = transform.position + ((Vector3)vel.normalized * -1);
                    break;*/
                //rasj: no 3 bc one needs to be empty for balance
            }
        }
        Destroy(this.gameObject);
    }


    public void OnSpawn(GameObject shooter)
    {
        //rasj: if useful, reference this from enemy/shooter directly after spawn
        return;
    }
}
