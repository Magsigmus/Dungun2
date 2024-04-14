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

    private GameObject target;

    private float dist;
    private Vector2 relativeDir;
    private Rigidbody2D rb;

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
        
        Destroy(this.gameObject);
    }


    public void OnSpawn(GameObject shooter)
    {
        //rasj: if useful, reference this from enemy/shooter directly after spawn
        return;
    }
}
