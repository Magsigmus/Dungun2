using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBehavior : MonoBehaviour
{
    [SerializeField] Transform target;
    public NavMeshAgent agent;
    public GameObject bulletPrefab;
    public float cooldownTime = 1f;
    private float cooldown = 0f;
    public float minWalkDistance = 1f;
    public float maxShootDistance = 10f;
    

    void Start() //
    {
        var agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    void Update() //
    {
        cooldown += Time.deltaTime;
        move(target.position, minWalkDistance);
        Shoot(target.position, maxShootDistance);
    }

    void move(Vector2 targetPos, float minDist)
    {
        float dist = Vector2.Distance(target.position, transform.position);  //rasj: Get distance
        if (dist > minDist)
        {
            agent.SetDestination(targetPos);  //rasj: Sets destination to target
        }
        else
        {
            agent.SetDestination(transform.position);  //rasj: Stops enemy from going further
        }
    }

    Vector2 pointTo(Vector2 targetPos)  //rasj: Sets rotation
    {
        Vector2 dir = new Vector2(targetPos.x - transform.position.x, targetPos.y - transform.position.y); //rasj: findes the vector to the target
        //transform.up = dir;
        return dir;
    }

    void Shoot(Vector2 targetPos, float maxDist)
    {
        Vector3 dir = pointTo(targetPos);
        float dist = Vector2.Distance(target.position, transform.position);  //rasj: Get distance

        if (cooldown > cooldownTime)
        {
            cooldown = 0f;
            if (dist <= maxDist) //rasj: if close enough
            {
                GameObject newBullet = Instantiate(bulletPrefab);
                newBullet.transform.up = dir;
                newBullet.transform.position = dir + transform.position;
                Debug.Log("pewpew");  //rasj: shoot
            }
            
        }
    }
}