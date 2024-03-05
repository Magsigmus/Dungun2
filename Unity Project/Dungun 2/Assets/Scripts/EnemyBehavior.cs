using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBehavior : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] NavMeshAgent agent;
    [SerializeField] GameObject bulletPrefab;

    /*Cooldowns and timers*/
    public float shootCooldownTime = 1f;  //1 second
    private float shootCooldown = 0f;

    public float minTargetDistance = 1f;
    public float maxTargetDistance = 10f;

    private Vector3 wandDesti = new Vector3 (0f, 0f, 0f);
    public float maxWanderDistance = 5f;
    public float maxWanderTime = 15f;
    private float wanderTime = 0f;
    private bool wandering = false;

    public int healthPoints = 3;


    void Start()
    {
        var agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    void Update()
    {
        shootCooldown += Time.deltaTime;
        wanderTime += Time.deltaTime;
        move(target.position, minTargetDistance, maxTargetDistance);
        if (!wandering)
        {
            Shoot(target.position);
        }
    }

    void move(Vector3 targetPos, float minDist, float maxDist)
    {
        wandering = false;
        float dist = Vector3.Distance(targetPos, transform.position);  //rasj: Get distance
        if (dist > minDist && dist < maxDist)  //rasj: if enemy is far enough away from target
        {
            agent.SetDestination(targetPos);  //rasj: Sets destination to target
        }
        else if (dist > maxDist)  //rasj: if enemy is too far away from target
        {
            wandering = true;
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

    void Shoot(Vector3 targetPos)
    {
        Vector3 dir = pointTo(targetPos);
        float dist = Vector3.Distance(targetPos, transform.position);  //rasj: Get distance

        if (shootCooldown > shootCooldownTime)  //rasj: if shootCooldown ran out
        {
            shootCooldown = 0f;  //rasj: reset cooldown
            GameObject newBullet = Instantiate(bulletPrefab);
            newBullet.transform.up = dir;
            newBullet.transform.position = transform.position;  //rasj: set to current position
        }
    }

    Vector2 pointTo(Vector2 targetPos)  //rasj: Sets rotation
    {
        //TODO: make enemy point ahead of player, to actually hit
        //rasj: findes the vector to the target
        Vector2 dir = new Vector2(targetPos.x - transform.position.x, targetPos.y - transform.position.y);
        //transform.up = dir;
        return dir;
    }

    void TakeDamage(int damage)  //TODO: run this when collide with player bullet
    {
        healthPoints -= damage;

        if (healthPoints <= 0)
        {
            Destroy(this.gameObject);
        }
    }
}