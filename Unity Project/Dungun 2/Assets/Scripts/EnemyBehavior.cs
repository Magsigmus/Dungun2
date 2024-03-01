using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBehavior : MonoBehaviour
{
    [SerializeField] Transform target;
    public NavMeshAgent agent;
    public GameObject bulletPrefab;

    public float shootCooldownTime = 1f;  //1 second
    private float cooldown = 0f;

    public float minTargetDistance = 1f;
    public float maxTargetDistance = 10f;

    private Vector3 wandDesti = new Vector3 (0f, 0f, 0f);
    public float maxWanderDistance = 5f;
    public float maxWanderTime = 15f;
    private float wanderTime = 0f;
    

    void Start() //
    {
        var agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    void Update() //
    {
        cooldown += Time.deltaTime;
        wanderTime += Time.deltaTime;
        move(target.position, minTargetDistance, maxTargetDistance);
        Shoot(target.position, maxTargetDistance);
    }

    void move(Vector3 targetPos, float minDist, float maxDist)
    {
        float dist = Vector3.Distance(targetPos, transform.position);  //rasj: Get distance
        if (dist > minDist && dist < maxDist)  //rasj: if enemy is far enough away from target
        {
            agent.SetDestination(targetPos);  //rasj: Sets destination to target
        }
        else if (dist > maxDist)  //rasj: if enemy is too far away from target
        {
            wander(-maxWanderDistance, maxWanderDistance);
        }
        else  //rasj: if enemy is too close to target
        {
            agent.SetDestination(transform.position);  //rasj: Stops enemy from going further
        }
    }

    void wander(float min, float max)
    {
        
        if (wandDesti == transform.position || wanderTime >= maxWanderTime)  //rasj: if wander reached or too much time has passed
        {
            wanderTime = 0;
            Vector3 randVect = new Vector2(Random.Range(min, max), Random.Range(min, max));
            wandDesti = randVect + transform.position;
            agent.SetDestination(wandDesti);
        }
    }

    void Shoot(Vector3 targetPos, float maxDist)
    {
        Vector3 dir = pointTo(targetPos);
        float dist = Vector3.Distance(targetPos, transform.position);  //rasj: Get distance

        if (cooldown > shootCooldownTime)
        {
            cooldown = 0f;
            if (dist <= maxDist) //rasj: if close enough
            {
                GameObject newBullet = Instantiate(bulletPrefab);
                newBullet.transform.up = dir;
                newBullet.transform.position = transform.position;
            }
        }
    }
    Vector2 pointTo(Vector2 targetPos)  //rasj: Sets rotation
    {
        Vector2 dir = new Vector2(targetPos.x - transform.position.x, targetPos.y - transform.position.y); //rasj: findes the vector to the target
        //transform.up = dir;
        return dir;
    }
}